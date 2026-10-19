"""
Renders a transparent hero picture of the drone for the 2D menus.

  blender -b Art/Blender/SKYFOLD_F4.blend -P Art/Blender/render_menu_image.py

Output: Assets/_Project/Art/UI/Drones/skyfold_f4_render.png (2400x1500, transparent)
"""
import bpy, os, math
from mathutils import Vector

PROJECT = os.path.abspath(os.path.join(os.path.dirname(bpy.data.filepath), "..", ".."))
OUT = os.path.join(PROJECT, "Assets", "_Project", "Art", "UI", "Drones", "skyfold_f4_render.png")
scene = bpy.context.scene

for name in ("Studio_Floor", "Prop_Damaged"):
    if name in bpy.data.objects:
        bpy.data.objects[name].hide_render = True

scene.render.film_transparent = True
scene.render.resolution_x, scene.render.resolution_y = 2400, 1500
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.cycles.samples = 48

# hangar lighting: cool key, teal rim like the menu accent
for l in bpy.data.lights:
    if l.name == "Key": l.energy = 16
    if l.name == "Rim": l.energy = 22
    if l.name == "Fill": l.energy = 5

cam = scene.camera
loc, target = Vector((-0.40, -0.56, 0.34)), Vector((0, -0.005, 0.03))
cam.location = loc
cam.rotation_euler = (target - loc).to_track_quat('-Z', 'Y').to_euler()
cam.data.lens = 50

scene.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print("menu image:", OUT)
