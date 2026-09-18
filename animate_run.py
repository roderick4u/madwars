import bpy, math, json
from pathlib import Path
from mathutils import Vector, Quaternion, Matrix
ROOT=Path(__file__).parent/'output'; OUT=ROOT/'run_cycle'; OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'walk_natural/Warlock_Walk.blend'))
scene=bpy.context.scene; rig=bpy.data.objects['Warlock_Rig']; character=bpy.data.objects['Warlock_SkinnedMesh']; arm=rig.data
rig.animation_data.action.use_fake_user=True
rig.animation_data_clear(); scene.frame_start=1; scene.frame_end=25; scene.render.fps=30
rest={b.name:b.matrix_local.to_quaternion() for b in arm.bones}
def rotate(name,q): rig.pose.bones[name].rotation_quaternion=rest[name].inverted()@q@rest[name]
def absolute(name,head,direction):
 b=arm.bones[name]; q=(b.tail_local-b.head_local).rotation_difference(direction.normalized())@rest[name]
 rig.pose.bones[name].matrix=Matrix.LocRotScale(head,q,Vector((1,1,1)))
for frame in range(1,26):
 scene.frame_set(frame); t=(frame-1)/24; phase=t*math.tau
 for pb in rig.pose.bones:
  pb.rotation_mode='QUATERNION'; pb.location=(0,0,0); pb.rotation_quaternion=Quaternion(); pb.scale=(1,1,1)
 hips=rig.pose.bones['Hips']; hips.location.y=-.032+.017*math.cos(2*phase-1.6); hips.location.x=-.006*math.sin(phase)
 rotate('Hips',Quaternion((0,0,1),-math.radians(3)*math.cos(phase)))
 rotate('Spine',Quaternion((1,0,0),math.radians(7)+.012*math.sin(2*phase))@Quaternion((0,0,1),math.radians(2)*math.cos(phase-.15)))
 rotate('Chest',Quaternion((0,0,1),math.radians(5)*math.cos(phase-.2))@Quaternion((0,1,0),.02*math.sin(phase)))
 rotate('Head',Quaternion((1,0,0),-math.radians(5))@Quaternion((0,0,1),-math.radians(3)*math.cos(phase-.2)))
 bpy.context.view_layer.update()
 for s,l in [(1,'L'),(-1,'R')]:
  u=(t+(0 if s==1 else .5))%1; p=phase+(0 if s==1 else math.pi)
  if u<.35:
   y=-.13+.26*u/.35; lift=0; recovery=0
  else:
   v=(u-.35)/.65; y=.13*math.cos(math.pi*v); recovery=math.sin(math.pi*v); lift=.14*recovery**1.2
  target=Vector((s*.123,y,.105+lift)); hb=rig.pose.bones['UpperLeg.'+l].head.copy()
  a=arm.bones['UpperLeg.'+l].length; b=arm.bones['LowerLeg.'+l].length
  delta=target-hb; d=delta.length; assert d<a+b,(frame,l,d,a+b)
  axis=delta.normalized(); along=(a*a-b*b+d*d)/(2*d); h=math.sqrt(max(0,a*a-along*along)); bend=Vector((0,-1,0)); bend=(bend-axis*bend.dot(axis)).normalized(); knee=hb+along*axis+h*bend
  absolute('UpperLeg.'+l,hb,knee-hb); bpy.context.view_layer.update()
  absolute('LowerLeg.'+l,knee,target-knee); bpy.context.view_layer.update()
  rig.pose.bones['Foot.'+l].matrix=Matrix.LocRotScale(target,Quaternion((1,0,0),.18*recovery)@rest['Foot.'+l],Vector((1,1,1)))
  rotate('Shoulder.'+l,Quaternion((0,0,1),s*.035*math.cos(p-.15)))
  rotate('UpperArm.'+l,Quaternion((1,0,0),.46*math.cos(p-.18))@Quaternion((0,1,0),s*math.radians(75)))
  rotate('LowerArm.'+l,Quaternion((0,0,1),-s*math.radians(54+7*math.cos(p-.4))))
  rotate('Hand.'+l,Quaternion((0,0,1),s*.035*math.sin(p-.6)))
  rotate('Robe.'+l,Quaternion((1,0,0),-.14-.48*recovery-.15*max(0,-y/.13)))
 rotate('Cape.01',Quaternion((1,0,0),.18+.045*math.sin(2*phase-.5)))
 rotate('Cape.02',Quaternion((1,0,0),.07+.05*math.sin(2*phase-1)))
 rotate('Cape.03',Quaternion((1,0,0),.04+.06*math.sin(2*phase-1.5)))
 bpy.context.view_layer.update()
 for pb in rig.pose.bones:
  pb.keyframe_insert('location',frame=frame,group=pb.name); pb.keyframe_insert('rotation_quaternion',frame=frame,group=pb.name)
rig.animation_data.action.name='Run_InPlace'
scene.frame_set(1); first={pb.name:pb.matrix.copy() for pb in rig.pose.bones}
scene.frame_set(25); error=max(abs(first[pb.name][r][c]-pb.matrix[r][c]) for pb in rig.pose.bones for r in range(4) for c in range(4)); assert error<1e-5
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); character.select_set(True); bpy.context.view_layer.objects.active=rig
for n in character.data.materials[0].node_tree.nodes:
 if n.type=='TEX_IMAGE' and n.image:
  n.image.filepath_raw=str(OUT/(n.image.name+'.png')); n.image.save(); n.image.pack()
bpy.ops.export_scene.fbx(filepath=str(OUT/'Warlock_Run.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'Warlock_Run.glb'),use_selection=True,export_format='GLB',export_animations=True)
scene.frame_end=24
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Warlock_Run.blend'))
(OUT/'validation.json').write_text(json.dumps({'clip':'Run_InPlace','duration_seconds':.8,'fps':30,'frames':[1,25],'loop_error':error,'triangles':5097,'bones':27,'root_motion':False},indent=2))
scene.render.resolution_x=600; scene.render.resolution_y=700; scene.cycles.samples=12
scene.camera.location=(2.3,-3.4,1.7); scene.camera.rotation_euler=(Vector((0,0,.79))-scene.camera.location).to_track_quat('-Z','Y').to_euler(); scene.camera.data.ortho_scale=1.85
for frame in range(1,25,2):
 scene.frame_set(frame); scene.render.filepath=str(OUT/('Run_%02d.png'%frame)); bpy.ops.render.render(write_still=True)
print('RUN_READY',error)
