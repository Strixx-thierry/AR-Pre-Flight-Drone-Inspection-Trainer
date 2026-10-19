"""
Adapt a downloaded drone model to the trainer's part standard, then export it for Unity.

  blender -b --factory-startup -P adapt_model.py -- --map mappings/<drone>.json [--list]

  --list   only import and print every object name with its size, so you can write the mapping

Mapping JSON (paths relative to this file):
{
  "id": "PICO_GUARD",
  "source": "../Source/pico_guard/scene.glb",      // .glb .gltf .fbx .obj .blend
  "span_m": 0.18,                                    // real tip-to-tip width; model is rescaled to it
  "forward": "-Y",                                   // which axis the nose points to AFTER import (-Y, +Y, -X, +X)
  "parts": {                                         // our part name -> list of source object names (merged)
    "Body":          ["Body_low", "Shell"],
    "Battery":       ["Battery"],
    "Battery_Latch": ["Latch"],
    "Gimbal_Camera": ["Cam", "Lens"],
    "Prop_FL":       ["Prop.001"]
  },
  "parents": { "Battery_Latch": "Battery", "Prop_FL": "Motor_FL" },   // optional, default = root
  "pivots": { "Battery_Latch": "top", "Prop_FL": "center" },          // center | top | bottom | origin
  "unmapped": "Body"                                 // leftover objects get merged into this part (or "delete")
}
Part names must match the Unity IDs used by the checklist JSON (see Design/components.html).
"""
import bpy, json, math, os, sys
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
cfg_path = os.path.join(HERE, argv[argv.index("--map") + 1])
LIST_ONLY = "--list" in argv
cfg = json.load(open(cfg_path, encoding="utf-8"))
src = os.path.normpath(os.path.join(os.path.dirname(cfg_path), cfg["source"]))

bpy.ops.wm.read_factory_settings(use_empty=True)
ext = os.path.splitext(src)[1].lower()
if ext in (".glb", ".gltf"):
    bpy.ops.import_scene.gltf(filepath=src)
elif ext == ".fbx":
    bpy.ops.import_scene.fbx(filepath=src)
elif ext == ".obj":
    bpy.ops.wm.obj_import(filepath=src)
elif ext == ".blend":
    with bpy.data.libraries.load(src) as (data_from, data_to):
        data_to.objects = data_from.objects
    for o in data_to.objects:
        if o:
            bpy.context.scene.collection.objects.link(o)
else:
    raise SystemExit(f"Unsupported format: {ext}")

meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.context.view_layer.update()

def world_bbox(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi

if LIST_ONLY:
    lo, hi = world_bbox(meshes)
    print(f"\n== {len(meshes)} meshes · overall size {hi - lo} ==")
    for o in sorted(meshes, key=lambda o: o.name):
        l, h = world_bbox([o])
        c = (l + h) / 2
        print(f"{o.name:40s} size=({(h-l).x:.3f},{(h-l).y:.3f},{(h-l).z:.3f})  center=({c.x:.3f},{c.y:.3f},{c.z:.3f})  tris~{sum(len(p.vertices)-2 for p in o.data.polygons)}")
    raise SystemExit(0)

# --- bake transforms, drop source hierarchy -----------------------------------------
for o in meshes:
    mw = o.matrix_world.copy()
    o.parent = None
    o.matrix_world = mw
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.make_single_user(object=True, obdata=True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.context.scene.objects):
    if o.type != 'MESH':
        bpy.data.objects.remove(o)

# --- orient nose to -Y, rescale to real span, sit on the ground ----------------------
turn = {"-Y": 0, "+Y": math.pi, "-X": -math.pi / 2, "+X": math.pi / 2}[cfg.get("forward", "-Y")]
lo, hi = world_bbox(meshes)
scale = cfg["span_m"] / max((hi - lo).x, (hi - lo).y)
T = Matrix.Rotation(turn, 4, 'Z') @ Matrix.Scale(scale, 4)
for o in meshes:
    o.data.transform(T)
lo, hi = world_bbox(meshes)
shift = Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
for o in meshes:
    o.data.transform(Matrix.Translation(shift))
    o.data.update()

# --- merge source objects into our named parts ---------------------------------------
by_name = {o.name: o for o in meshes}
used, parts = set(), {}

def join(name, objs):
    objs = [o for o in objs if o]
    if not objs:
        return None
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = ob.data.name = name
    return ob

for part, names in cfg["parts"].items():
    missing = [n for n in names if n not in by_name]
    if missing:
        print(f"WARNING {part}: not found {missing}")
    picked = [by_name[n] for n in names if n in by_name and n not in used]
    used.update(n for n in names)
    ob = join(part, picked)
    if ob:
        parts[part] = ob

left = [o for n, o in by_name.items() if n not in used and o.name in bpy.data.objects]
if left:
    if cfg.get("unmapped", "Body") == "delete":
        for o in left:
            bpy.data.objects.remove(o)
    else:
        tgt = cfg.get("unmapped", "Body")
        merged = join(tgt, ([parts[tgt]] if tgt in parts else []) + left)
        parts[tgt] = merged
        print(f"merged {len(left)} unmapped objects into {tgt}")

# --- pivots ---------------------------------------------------------------------------
def set_pivot(ob, mode):
    lo, hi = world_bbox([ob])
    p = {"center": (lo + hi) / 2,
         "top": Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, hi.z)),
         "bottom": Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))}.get(mode)
    if p is None:
        return
    ob.data.transform(Matrix.Translation(-p))
    ob.location = p

for name, ob in parts.items():
    set_pivot(ob, cfg.get("pivots", {}).get(name, "center"))

# --- hierarchy + export ---------------------------------------------------------------
root = bpy.data.objects.new(cfg["id"], None)
bpy.context.scene.collection.objects.link(root)
bpy.context.view_layer.update()
for name in sorted(parts, key=lambda n: n.count("_")):          # parents before children
    ob = parts[name]
    parent = parts.get(cfg.get("parents", {}).get(name), root)
    mw = ob.matrix_world.copy()
    ob.parent = parent
    ob.matrix_world = mw
    ob.matrix_parent_inverse = parent.matrix_world.inverted()

bpy.ops.object.select_all(action='DESELECT')
for o in [root] + list(parts.values()):
    o.select_set(True)
out_dir = os.path.join(PROJECT, "Assets", "Models", "Drones")
os.makedirs(out_dir, exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=os.path.join(out_dir, f"{cfg['id']}.fbx"), use_selection=True, object_types={'EMPTY', 'MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
    bake_space_transform=True, mesh_smooth_type='OFF', add_leaf_bones=False, bake_anim=False,
    path_mode='COPY', embed_textures=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, f"{cfg['id']}.blend"))
tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in parts.values())
print(f"{cfg['id']}: {len(parts)} parts, ~{tris} tris -> Assets/Models/Drones/{cfg['id']}.fbx")
