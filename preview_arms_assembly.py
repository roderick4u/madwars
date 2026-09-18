import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).parent/'output'
bpy.ops.wm.open_mainfile(filepath=str(root/'arms/Warlock_Arms.blend'))
for file in [root/'Warlock_Head.glb',root/'torso/Warlock_Torso.glb']:
 bpy.ops.import_scene.gltf(filepath=str(file))
scene=bpy.context.scene; camera=scene.camera
camera.location=(.85,-3.4,1.0)
camera.rotation_euler=(Vector((0,.02,-.22))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=1.95
scene.render.resolution_y=1200
scene.render.filepath=str(root/'arms/Assembly_preview.png')
bpy.ops.render.render(write_still=True)
