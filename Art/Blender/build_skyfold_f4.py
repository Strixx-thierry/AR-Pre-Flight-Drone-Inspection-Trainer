"""
SKYFOLD F4 — procedural foldable camera quad for the AR Pre-Flight trainer.

Run (headless):
  blender -b --factory-startup -P build_skyfold_f4.py -- --project "<repo root>"

Outputs
  Art/Blender/SKYFOLD_F4.blend                 editable source
  Assets/Models/Drones/SKYFOLD_F4.fbx          Unity import (real scale, 1 unit = 1 m)
  Assets/Models/Drones/SKYFOLD_F4_Prop_Damaged.fbx   chipped-blade swap mesh
  Design/renders/skyfold_*.png                 preview renders

Every inspectable part is its own object with its pivot where it moves:
  SKYFOLD_F4 (root, on the ground)
  ├─ Body
  ├─ Battery ── Battery_Latch, Battery_LED_1..4
  ├─ Gimbal ── Gimbal_Camera
  ├─ Gimbal_Clamp
  └─ Arm_FL/FR/RL/RR (pivot = fold hinge) ── Motor_xx (pivot = motor top) ── Prop_xx (pivot = spin axis)
Nose points -Y in Blender, which becomes +Z (forward) in Unity with the FBX settings below.
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector, Matrix

# ------------------------------------------------------------------ paths
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PROJECT = argv[argv.index("--project") + 1] if "--project" in argv else os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DRONE_ID = argv[argv.index("--id") + 1] if "--id" in argv else "SKYFOLD_F4"
# Variants share the same parts/pivots (same checklist) but differ in size and colour.
VARIANTS = {
    "SKYFOLD_F4": dict(scale=1.0, body=(0.78, 0.79, 0.81), mid=(0.60, 0.62, 0.65), suffix=""),
    "TALON_X4": dict(scale=1.35, body=(0.07, 0.075, 0.085), mid=(0.85, 0.30, 0.04), suffix="_TALON"),
}
V = VARIANTS[DRONE_ID]
BLEND_OUT = os.path.join(PROJECT, "Art", "Blender", f"{DRONE_ID}.blend")
FBX_DIR = os.path.join(PROJECT, "Assets", "Models", "Drones")
RENDER_DIR = os.path.join(PROJECT, "Design", "renders")
for d in (os.path.dirname(BLEND_OUT), FBX_DIR, RENDER_DIR):
    os.makedirs(d, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
COL = scene.collection
WORLD = {}  # object name -> world pivot (no object rotations are used, so translation is enough)


# ------------------------------------------------------------------ materials
def mat(name, color, rough=0.5, metal=0.0, alpha=1.0, emit=None, emit_strength=0.0, coat=0.0):
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    b.inputs["Alpha"].default_value = alpha
    if coat:
        b.inputs["Coat Weight"].default_value = coat
        b.inputs["Coat Roughness"].default_value = 0.25
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1)
        b.inputs["Emission Strength"].default_value = emit_strength
    if alpha < 1:
        for attr, val in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
            try:
                setattr(m, attr, val)
            except Exception:
                pass
    m.diffuse_color = (*color, alpha)
    return m

M = {
    "plastic": mat("M_Body_Plastic" + V["suffix"], V["body"], rough=0.42, coat=0.35),
    "plastic_mid": mat("M_Body_PlasticMid" + V["suffix"], V["mid"], rough=0.5, coat=0.2),
    "dark": mat("M_Dark_Plastic", (0.06, 0.065, 0.07), rough=0.55),
    "prop": mat("M_Propeller", (0.035, 0.038, 0.042), rough=0.38, coat=0.2),
    "prop_tip": mat("M_Propeller_Tip", (0.92, 0.92, 0.92), rough=0.4),
    "prop_damage": mat("M_Propeller_Damage", (0.72, 0.68, 0.6), rough=0.85),   # stress-whitened plastic at the break
    "metal": mat("M_Motor_Metal", (0.80, 0.81, 0.83), rough=0.22, metal=1.0),
    "metal_dark": mat("M_Metal_Dark", (0.18, 0.19, 0.2), rough=0.3, metal=1.0),
    "glass": mat("M_Lens_Glass", (0.005, 0.006, 0.01), rough=0.03, coat=1.0),
    "lens_tint": mat("M_Lens_Coating", (0.05, 0.12, 0.35), rough=0.05, metal=0.6),
    "led_white": mat("M_LED_White", (1, 0.97, 0.88), emit=(1, 0.95, 0.85), emit_strength=6),
    "led_green": mat("M_LED_Green", (0.2, 0.9, 0.45), emit=(0.25, 1, 0.5), emit_strength=5),
    "led_off": mat("M_LED_Off", (0.25, 0.27, 0.28), rough=0.3),
    "rubber": mat("M_Rubber", (0.02, 0.02, 0.022), rough=0.85),
    "clear": mat("M_Clamp_Clear", (0.85, 0.92, 1.0), rough=0.08, alpha=0.32),
    "floor": mat("M_Floor", (0.16, 0.17, 0.19), rough=0.55),
}


# ------------------------------------------------------------------ mesh helpers
def spow(v, e):
    return math.copysign(abs(v) ** e, v)

def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def superellipsoid(bm, a, b, c, e1=0.4, e2=0.4, nu=48, nv=24, deform=None, mi=0):
    """Rounded-box family. Smaller exponents = boxier. Returns the new verts."""
    rows, made = [], []
    for i in range(1, nv):
        phi = -math.pi / 2 + math.pi * i / nv
        row = []
        for j in range(nu):
            th = -math.pi + 2 * math.pi * j / nu
            p = Vector((a * spow(math.cos(phi), e1) * spow(math.cos(th), e2),
                        b * spow(math.cos(phi), e1) * spow(math.sin(th), e2),
                        c * spow(math.sin(phi), e1)))
            row.append(bm.verts.new(deform(p) if deform else p))
        rows.append(row)
        made += row
    pb, pt = Vector((0, 0, -c)), Vector((0, 0, c))
    vb = bm.verts.new(deform(pb) if deform else pb)
    vt = bm.verts.new(deform(pt) if deform else pt)
    made += [vb, vt]
    faces = []
    for r0, r1 in zip(rows, rows[1:]):
        for j in range(nu):
            faces.append(bm.faces.new((r0[j], r0[(j + 1) % nu], r1[(j + 1) % nu], r1[j])))
    for j in range(nu):
        faces.append(bm.faces.new((vb, rows[0][(j + 1) % nu], rows[0][j])))
        faces.append(bm.faces.new((vt, rows[-1][j], rows[-1][(j + 1) % nu])))
    for f in faces:
        f.material_index = mi
    return made

def rbox(bm, a, b, c, center, e=0.3, nu=32, nv=16, rotz=0.0, deform=None, mi=0):
    v = superellipsoid(bm, a, b, c, e, e, nu, nv, deform, mi)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(center) @ Matrix.Rotation(rotz, 4, 'Z'), verts=v)
    return v

def cyl(bm, r_bottom, r_top, depth, center, axis='Z', segs=40, mi=0):
    rot = {'Z': Matrix.Identity(4),
           'X': Matrix.Rotation(math.pi / 2, 4, 'Y'),
           'Y': Matrix.Rotation(math.pi / 2, 4, 'X')}[axis]
    res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs,
                                radius1=r_bottom, radius2=r_top, depth=depth,
                                matrix=Matrix.Translation(center) @ rot)
    for v in res['verts']:
        for f in v.link_faces:
            f.material_index = mi
    return res['verts']

def make_obj(name, bm, pivot=(0, 0, 0), mats=(), parent=None, sharp_deg=38):
    pivot = Vector(pivot)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-7)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for v in bm.verts:
        v.co -= pivot
    lim = math.radians(sharp_deg)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.calc_face_angle(0) > lim:
            e.smooth = False
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    ob = bpy.data.objects.new(name, me)
    COL.objects.link(ob)
    attach(ob, pivot, parent)
    return ob

def make_empty(name, pivot, parent=None, size=0.02):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_size = size
    ob.empty_display_type = 'PLAIN_AXES'
    COL.objects.link(ob)
    attach(ob, Vector(pivot), parent)
    return ob

def attach(ob, pivot, parent):
    WORLD[ob.name] = pivot.copy()
    if parent:
        ob.parent = parent
        ob.location = pivot - WORLD[parent.name]
    else:
        ob.location = pivot

def surface_z(ob, x, y):
    bpy.context.view_layer.update()
    o = Vector((x, y, 1.0)) - WORLD[ob.name]
    hit, loc, _, _ = ob.ray_cast(o, Vector((0, 0, -1)))
    return (loc + WORLD[ob.name]).z if hit else None


# ------------------------------------------------------------------ dimensions (metres)
B_A, B_B, B_C = 0.040, 0.086, 0.019      # body half-width / half-length / half-height
BODY_Z = 0.040                           # body centre height above the ground
PLATFORM_Z = BODY_Z + B_C * 0.25         # flat bay the battery sits in
ARMS = {
    "FL": dict(hinge=(-0.036, -0.050, BODY_Z + 0.006), motor=(-0.105, -0.100), ccw=False),
    "FR": dict(hinge=(0.036, -0.050, BODY_Z + 0.006), motor=(0.105, -0.100), ccw=True),
    "RL": dict(hinge=(-0.034, 0.052, BODY_Z - 0.004), motor=(-0.105, 0.105), ccw=True),
    "RR": dict(hinge=(0.034, 0.052, BODY_Z - 0.004), motor=(0.105, 0.105), ccw=False),
}

root = make_empty(DRONE_ID, (0, 0, 0), size=0.05)


# ------------------------------------------------------------------ body
def body_deform(p):
    t = p.y / B_B                         # -1 nose … +1 tail
    x, y, z = p
    w = 1.0
    if t < -0.25:
        w -= 0.30 * ((-t - 0.25) / 0.75) ** 1.5
    if t > 0.6:
        w -= 0.12 * ((t - 0.6) / 0.4) ** 2
    x *= w
    if z > 0:
        z *= 1.0 + 0.12 * (1 - t * t)                              # domed spine
        if t < -0.45:
            z *= 1 - 0.55 * ((-t - 0.45) / 0.55) ** 1.4            # wedge nose
        s = smoothstep(-0.15, 0.05, t)                             # battery bay on the rear deck
        z = z * (1 - s) + min(z, B_C * 0.25) * s
    else:
        z *= 0.85
    return Vector((x, y, z + BODY_Z))

bm = bmesh.new()
superellipsoid(bm, B_A, B_B, B_C, e1=0.45, e2=0.42, nu=72, nv=32, deform=body_deform, mi=0)
body = make_obj("Body", bm, (0, 0, BODY_Z), [M["plastic"], M["dark"], M["led_white"]], parent=root)

# surface details are baked into the body mesh so it stays one draw call
bm = bmesh.new()
bm.from_mesh(body.data)
for side in (-1, 1):
    for k in range(3):                                            # cooling vents
        y = -0.050 + k * 0.0065
        z = surface_z(body, side * 0.021, y)
        if z:
            rbox(bm, 0.0065, 0.0016, 0.0014, (side * 0.021 - BODY_Z * 0, y, z - BODY_Z), e=0.25, nu=16, nv=8, mi=1)
    z = surface_z(body, side * 0.017, -0.074)                    # front LEDs
    if z:
        rbox(bm, 0.0042, 0.0026, 0.0016, (side * 0.017, -0.074, z - BODY_Z), e=0.45, nu=20, nv=10, mi=2)
z = surface_z(body, 0, -0.030)                                    # power-button ring
cyl(bm, 0.0058, 0.0058, 0.0012, (0, -0.030, z - BODY_Z + 0.0002), mi=1)
cyl(bm, 0.0042, 0.0042, 0.0016, (0, -0.030, z - BODY_Z + 0.0003), mi=0)
cyl(bm, 0.0105, 0.0105, 0.0010, (0, 0.040, PLATFORM_Z - BODY_Z + 0.0004), mi=1)   # bay contacts
bm.to_mesh(body.data)
bm.free()


# ------------------------------------------------------------------ battery
BAT_C = 0.0085
BAT_Z = PLATFORM_Z + BAT_C - 0.0015
def bat_deform(p):
    t = p.y / 0.046
    x, y, z = p
    if t > 0.5:
        x *= 1 - 0.14 * ((t - 0.5) / 0.5) ** 2
    if z > 0:
        z *= 1 + 0.15 * (1 - t * t)
    return Vector((x, y, z))

bm = bmesh.new()
v = superellipsoid(bm, 0.0315, 0.046, BAT_C, e1=0.35, e2=0.38, nu=56, nv=20, deform=bat_deform, mi=0)
bmesh.ops.translate(bm, vec=(0, 0.042, BAT_Z), verts=v)
for k in range(3):                                                # finger-grip ridges
    rbox(bm, 0.017, 0.0013, 0.0012, (0, 0.012 + k * 0.006, BAT_Z + BAT_C * 1.12), e=0.3, nu=16, nv=8, mi=1)
battery = make_obj("Battery", bm, (0, 0.042, BAT_Z), [M["plastic_mid"], M["dark"]], parent=root)

top = surface_z(battery, 0, 0.066) or BAT_Z + BAT_C
for i in range(4):                                                # charge LEDs (toggle per level in Unity)
    x = -0.0096 + i * 0.0064
    bm = bmesh.new()
    cyl(bm, 0.0016, 0.0016, 0.0014, (x, 0.066, top + 0.0002), segs=20)
    make_obj(f"Battery_LED_{i + 1}", bm, (x, 0.066, top), [M["led_green"] if i < 3 else M["led_off"]], parent=battery)

bm = bmesh.new()
rbox(bm, 0.0115, 0.0035, 0.0048, (0, 0.0895, BAT_Z - 0.001), e=0.3, nu=24, nv=12)
cyl(bm, 0.0012, 0.0012, 0.018, (0, 0.0905, BAT_Z + 0.002), axis='X', segs=12, mi=1)
latch = make_obj("Battery_Latch", bm, (0, 0.0890, BAT_Z + 0.0035), [M["dark"], M["metal_dark"]], parent=battery)


# ------------------------------------------------------------------ gimbal + camera + clamp
G_Y, G_Z = -0.091, BODY_Z - 0.021
gimbal = None
bm = bmesh.new()
rbox(bm, 0.012, 0.009, 0.005, (0, -0.079, G_Z + 0.012), e=0.3)                     # mount under the nose
for s in (-1, 1):
    rbox(bm, 0.0028, 0.0085, 0.0105, (s * 0.0188, G_Y + 0.003, G_Z + 0.004), e=0.3)  # yoke arms
    cyl(bm, 0.0085, 0.0085, 0.0042, (s * 0.0215, G_Y, G_Z), axis='X', segs=40, mi=1) # tilt motors
gimbal = make_obj("Gimbal", bm, (0, -0.079, G_Z + 0.012), [M["plastic"], M["plastic_mid"]], parent=root)

bm = bmesh.new()
rbox(bm, 0.0152, 0.0122, 0.0098, (0, G_Y, G_Z), e=0.35, nu=40, nv=20)
cyl(bm, 0.0088, 0.0088, 0.0040, (0, G_Y - 0.0125, G_Z), axis='Y', segs=48, mi=1)    # bezel
cyl(bm, 0.0072, 0.0072, 0.0016, (0, G_Y - 0.0148, G_Z), axis='Y', segs=48, mi=3)    # coated ring
cyl(bm, 0.0058, 0.0058, 0.0016, (0, G_Y - 0.0154, G_Z), axis='Y', segs=48, mi=2)    # glass
camera = make_obj("Gimbal_Camera", bm, (0, G_Y, G_Z),
                  [M["plastic"], M["dark"], M["glass"], M["lens_tint"]], parent=gimbal)

bm = bmesh.new()
rbox(bm, 0.0235, 0.0175, 0.0150, (0, G_Y - 0.002, G_Z + 0.001), e=0.4, nu=40, nv=20)
rbox(bm, 0.013, 0.012, 0.0016, (0, -0.076, BODY_Z - 0.0155), e=0.3, mi=1)           # clip tab under the body
clamp = make_obj("Gimbal_Clamp", bm, (0, G_Y, G_Z), [M["clear"], M["clear"]], parent=root)


# ------------------------------------------------------------------ arms, motors, props
def prop_mesh(bm, ccw=False, R=0.064, chipped=False):
    """Two twisted, swept blades + hub. Built at the origin (spin axis = Z)."""
    sgn = -1 if ccw else 1
    for blade in range(2):
        N, Mseg = 20, 12
        secs = []
        broken = chipped and blade == 0
        for i in range(N + 1):
            s = 0.1 + 0.9 * i / N
            if broken and s > 0.84:
                break                                                 # tip snapped off
            chord = 0.0065 + 0.0085 * math.sin(math.pi * (0.2 + 0.7 * s))
            if s > 0.88:
                chord *= 0.25 + 0.75 * math.sqrt(max(0.0, (1 - s) / 0.12))
            if broken and s > 0.6:
                chord *= (0.42, 0.62, 0.38, 0.55)[i % 4]             # jagged chips along the edge
            thick = 0.0017 * (1 - 0.55 * s)
            twist = math.radians(24 - 15 * s) * sgn
            sweep = 0.006 * s * s * sgn
            ring = []
            for k in range(Mseg):
                a = 2 * math.pi * k / Mseg
                cx = 0.5 * chord * math.cos(a)
                cz = 0.5 * thick * math.sin(a) + 0.0007 * (1 - (2 * cx / chord) ** 2)
                y = cx * math.cos(twist) - cz * math.sin(twist)
                z = cx * math.sin(twist) + cz * math.cos(twist)
                bend = 0.009 * ((s - 0.6) / 0.24) ** 2 if broken and s > 0.6 else 0.0   # blade bent upward
                ring.append(bm.verts.new((s * R, y + sweep, z + 0.003 + bend)))
            secs.append((s, ring))
        new = []
        for (s0, r0), (_, r1) in zip(secs, secs[1:]):
            mi = 4 if broken and s0 >= 0.58 else 1 if 0.80 <= s0 < 0.855 else 0
            for k in range(Mseg):
                f = bm.faces.new((r0[k], r0[(k + 1) % Mseg], r1[(k + 1) % Mseg], r1[k]))
                f.material_index = mi
        bm.faces.new(list(reversed(secs[0][1]))).material_index = 0
        bm.faces.new(secs[-1][1]).material_index = 4 if broken else 0
        if blade == 0:
            first = [v for _, r in secs for v in r]
        else:
            bmesh.ops.rotate(bm, verts=[v for _, r in secs for v in r], cent=(0, 0, 0),
                             matrix=Matrix.Rotation(math.pi, 3, 'Z'))
    cyl(bm, 0.0068, 0.0068, 0.0058, (0, 0, 0.003), segs=32, mi=2)
    cyl(bm, 0.0045, 0.0030, 0.0022, (0, 0, 0.0069), segs=24, mi=3)

for key, cfg in ARMS.items():
    hx, hy, hz = cfg["hinge"]
    mx, my = cfg["motor"]
    d = Vector((mx - hx, my - hy))
    L, ang = d.length, math.atan2(d.y, d.x)

    bm = bmesh.new()
    taper = lambda p, L=L: Vector((p.x, p.y * (1 - 0.22 * max(0, min(1, (p.x + L / 2) / L))),
                                   p.z * (1 - 0.22 * max(0, min(1, (p.x + L / 2) / L)))))
    v = superellipsoid(bm, L / 2 + 0.004, 0.0085, 0.0062, 0.35, 0.3, nu=40, nv=14, deform=taper)
    bmesh.ops.transform(bm, matrix=Matrix.Translation((hx, hy, hz)) @ Matrix.Rotation(ang, 4, 'Z')
                        @ Matrix.Translation((L / 2, 0, 0)), verts=v)
    cyl(bm, 0.0076, 0.0076, 0.0145, (hx, hy, hz), segs=32, mi=1)            # fold hinge knuckle
    cyl(bm, 0.0125, 0.0118, 0.0100, (mx, my, hz), segs=40)                  # motor mount
    leg_top, foot = hz - 0.005, 0.0035
    cyl(bm, 0.0040, 0.0066, leg_top - foot, (mx, my, foot + (leg_top - foot) / 2), segs=32)  # landing leg
    cyl(bm, 0.0046, 0.0046, foot, (mx, my, foot / 2), segs=24, mi=2)        # rubber foot
    arm = make_obj(f"Arm_{key}", bm, (hx, hy, hz), [M["plastic"], M["metal_dark"], M["rubber"]], parent=root)

    base = hz + 0.005
    bm = bmesh.new()
    cyl(bm, 0.0104, 0.0104, 0.0040, (mx, my, base + 0.0020), segs=40, mi=1)  # stator base
    cyl(bm, 0.0110, 0.0106, 0.0090, (mx, my, base + 0.0085), segs=48)        # bell
    cyl(bm, 0.0060, 0.0060, 0.0016, (mx, my, base + 0.0138), segs=32, mi=1)  # top cap
    top = base + 0.0146
    motor = make_obj(f"Motor_{key}", bm, (mx, my, top), [M["metal"], M["metal_dark"]], parent=arm)

    bm = bmesh.new()
    prop_mesh(bm, ccw=cfg["ccw"])
    bmesh.ops.rotate(bm, verts=bm.verts[:], cent=(0, 0, 0),
                     matrix=Matrix.Rotation(math.radians({"FL": 25, "FR": -30, "RL": -15, "RR": 40}[key]), 3, 'Z'))
    bmesh.ops.translate(bm, vec=(mx, my, top), verts=bm.verts[:])
    make_obj(f"Prop_{key}", bm, (mx, my, top),
             [M["prop"], M["prop_tip"], M["prop"], M["metal"]], parent=motor, sharp_deg=60)


# ------------------------------------------------------------------ export to Unity
def select_tree(obj):
    bpy.ops.object.select_all(action='DESELECT')
    stack = [obj]
    while stack:
        o = stack.pop()
        o.select_set(True)
        stack.extend(o.children)
    bpy.context.view_layer.objects.active = obj

def export_fbx(path):
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', bake_space_transform=False,   # Unity bakeAxisConversion handles Y-up
        use_mesh_modifiers=True, mesh_smooth_type='OFF', add_leaf_bones=False, bake_anim=False,
        path_mode='AUTO')

bpy.context.view_layer.update()
root.scale = (V["scale"],) * 3
bpy.context.view_layer.update()
select_tree(root)
export_fbx(os.path.join(FBX_DIR, f"{DRONE_ID}.fbx"))

# chipped propeller variant (swap the mesh in Unity for fault-hunt / rotor step)
bm = bmesh.new()
prop_mesh(bm, ccw=True, chipped=True)
dmg = make_obj("Prop_Damaged", bm, (0, 0, 0), [M["prop"], M["prop_tip"], M["prop"], M["metal"], M["prop_damage"]], sharp_deg=60)
select_tree(dmg)
export_fbx(os.path.join(FBX_DIR, f"{DRONE_ID}_Prop_Damaged.fbx"))
dmg.location = (0.22, -0.12, 0.0)   # parked beside the drone in the .blend for reference


# ------------------------------------------------------------------ studio + renders
bpy.ops.mesh.primitive_plane_add(size=6, location=(0, 0, 0))
floor = bpy.context.active_object
floor.name = "Studio_Floor"
floor.data.materials.append(M["floor"])

world = bpy.data.worlds.new("Studio")
scene.world = world
try:
    world.use_nodes = True
except Exception:
    pass
bg = world.node_tree.nodes.get("Background")
bg.inputs["Color"].default_value = (0.32, 0.35, 0.38, 1)
bg.inputs["Strength"].default_value = 0.12

def light(name, loc, power, size, color=(1, 1, 1), target=(0, 0, 0.03)):
    ld = bpy.data.lights.new(name, 'AREA')
    ld.energy, ld.size, ld.color = power, size, color
    ob = bpy.data.objects.new(name, ld)
    COL.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()

light("Key", (-0.55, -0.65, 0.85), 14, 0.6)
light("Fill", (0.75, -0.25, 0.45), 4, 0.9)
light("Rim", (0.25, 0.75, 0.55), 12, 0.5, color=(0.62, 1.0, 0.95))

cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
COL.objects.link(cam)
scene.camera = cam

def aim(loc, target, lens=50):
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    cam_data.lens = lens

scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.cycles.max_bounces = 4
try:                                   # use the GPU when one is available
    prefs = bpy.context.preferences.addons["cycles"].preferences
    for backend in ("OPTIX", "CUDA", "HIP", "ONEAPI"):
        try:
            prefs.compute_device_type = backend
            prefs.get_devices()
            if any(d.type == backend for d in prefs.devices):
                for d in prefs.devices:
                    d.use = d.type == backend
                scene.cycles.device = 'GPU'
                break
        except Exception:
            continue
except Exception:
    pass
scene.render.resolution_x, scene.render.resolution_y = 1280, 800
scene.render.image_settings.file_format = 'PNG'
try:
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Medium High Contrast'
except Exception:
    pass

def render(name):
    scene.render.filepath = os.path.join(RENDER_DIR, f"{DRONE_ID.lower()}_{name}.png")
    bpy.ops.render.render(write_still=True)

dmg.hide_render = True
aim((-0.36, -0.46, 0.27), (0, -0.005, 0.03))
render("hero")

aim((-0.13, -0.24, 0.075), (0, -0.088, 0.022), lens=70)
render("gimbal_clamp")

cam.location = (0, 0, 0.78)
cam.rotation_euler = (0, 0, math.pi)
cam_data.lens = 50
render("top")

# exploded view: shows every detachable part
EXPLODE = {"Battery": (0, 0.025, 0.055), "Battery_Latch": (0, 0.015, 0),
           "Gimbal_Clamp": (0, -0.055, 0.0), "Gimbal": (0, -0.02, -0.004), "Gimbal_Camera": (0, -0.012, 0)}
for key, cfg in ARMS.items():
    d = (Vector(cfg["motor"]) - Vector(cfg["hinge"][:2])).normalized() * 0.03
    EXPLODE[f"Arm_{key}"] = (d.x, d.y, 0)
    EXPLODE[f"Motor_{key}"] = (0, 0, 0.022)
    EXPLODE[f"Prop_{key}"] = (0, 0, 0.040)
saved = {n: bpy.data.objects[n].location.copy() for n in EXPLODE}
for n, off in EXPLODE.items():
    bpy.data.objects[n].location += Vector(off)
aim((-0.44, -0.58, 0.46), (0, -0.01, 0.05))
render("exploded")
for n, loc in saved.items():
    bpy.data.objects[n].location = loc

aim((-0.36, -0.46, 0.27), (0, -0.005, 0.03))
dmg.hide_render = False
bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
print("SKYFOLD_F4 build complete")
