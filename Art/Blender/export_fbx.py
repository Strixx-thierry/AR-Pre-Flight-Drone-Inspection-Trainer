"""
Re-export SKYFOLD_F4.blend to Unity FBX without rebuilding or rendering.

  blender -b Art/Blender/SKYFOLD_F4.blend -P Art/Blender/export_fbx.py

Uses standard FBX axes (no experimental space-transform baking, which misplaces nested
children like Arm → Motor → Prop). Unity's ModelImporter.bakeAxisConversion handles Y-up.
"""
import bpy, os

PROJECT = os.path.abspath(os.path.join(os.path.dirname(bpy.data.filepath), "..", ".."))
OUT = os.path.join(PROJECT, "Assets", "Models", "Drones")


def select_tree(obj):
    bpy.ops.object.select_all(action='DESELECT')
    stack = [obj]
    while stack:
        o = stack.pop()
        o.hide_set(False)
        o.select_set(True)
        stack.extend(o.children)
    bpy.context.view_layer.objects.active = obj


def export(path):
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', bake_space_transform=False,
        use_mesh_modifiers=True, mesh_smooth_type='OFF', add_leaf_bones=False, bake_anim=False)


root = bpy.data.objects["SKYFOLD_F4"]
select_tree(root)
export(os.path.join(OUT, "SKYFOLD_F4.fbx"))

dmg = bpy.data.objects["Prop_Damaged"]
saved = dmg.location.copy()
dmg.location = (0, 0, 0)
select_tree(dmg)
export(os.path.join(OUT, "SKYFOLD_F4_Prop_Damaged.fbx"))
dmg.location = saved
print("FBX export complete")
