import bpy,json
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).parent/'output/walk_cycle'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Warlock_Walk.blend'))
scene=bpy.context.scene; rig=bpy.data.objects['Warlock_Rig']; mesh=bpy.data.objects['Warlock_SkinnedMesh']
scene.render.resolution_x=480; scene.render.resolution_y=560; scene.cycles.samples=10
scene.camera.location=(2.3,-3.4,1.7); scene.camera.rotation_euler=(Vector((0,0,.79))-scene.camera.location).to_track_quat('-Z','Y').to_euler(); scene.camera.data.ortho_scale=1.8
# Measure stance soles in the evaluated mesh throughout the entire loop.
ids={l:[v.index for v in mesh.data.vertices if v.co.z<.018 and (v.co.x>0)==(l=='L')] for l in ['L','R']}
contacts=[]
for frame in range(1,33):
 scene.frame_set(frame); dg=bpy.context.evaluated_depsgraph_get(); ev=mesh.evaluated_get(dg); me=ev.to_mesh()
 soles={l:min((ev.matrix_world@me.vertices[i].co).z for i in ix) for l,ix in ids.items()}
 contacts.append({'frame':frame,'soles':soles}); ev.to_mesh_clear()
 if frame%2==1:
  scene.render.filepath=str(OUT/('preview_%02d.png'%frame)); bpy.ops.render.render(write_still=True)
(OUT/'foot_contacts.json').write_text(json.dumps(contacts,indent=2))
# Reimport the FBX to check exported skin, clip and loop endpoint.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'Warlock_Walk.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
assert rig.animation_data and rig.animation_data.action
action=rig.animation_data.action
start,end=action.frame_range
scene=bpy.context.scene; scene.frame_set(int(start)); first={p.name:p.matrix.copy() for p in rig.pose.bones}
scene.frame_set(int(end)); error=max(abs(first[p.name][r][c]-p.matrix[r][c]) for p in rig.pose.bones for r in range(4) for c in range(4))
assert error<.0001,error
(OUT/'fbx_validation.json').write_text(json.dumps({'reimport_ok':True,'bones':len(rig.data.bones),'action':action.name,'frame_range':[start,end],'loop_error':error},indent=2))
print('FBX_VERIFIED',start,end,error)
