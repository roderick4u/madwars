import bpy, math, json
from pathlib import Path
from mathutils import Vector, Quaternion, Matrix
ROOT=Path(__file__).parent/'output'; OUT=ROOT/'walk_cycle'; OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assembled_v2/Warlock_Complete.blend'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for ob in meshes:
 matrix=ob.matrix_world.copy(); ob.parent=None; ob.matrix_world=matrix
 ob.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]; bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
arm=bpy.data.armatures.new('Warlock_Skeleton'); rig=bpy.data.objects.new('Warlock_Rig',arm); bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig; bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,h,t,parent=None):
 b=arm.edit_bones.new(name); b.head=h; b.tail=t
 if parent: b.parent=arm.edit_bones[parent]
 return b
bone('Root',(0,0,0),(0,0,.12)); bone('Hips',(0,.016,.59),(0,.016,.74),'Root')
bone('Spine',(0,.016,.74),(0,.016,.88),'Hips'); bone('Chest',(0,.016,.88),(0,.016,1.02),'Spine')
bone('Neck',(0,.016,1.02),(0,.016,1.10),'Chest'); bone('Head',(0,.016,1.10),(0,.016,1.40),'Neck')
for s,l in [(1,'L'),(-1,'R')]:
 bone('Shoulder.'+l,(s*.03,.016,1.015),(s*.224,.016,1.005),'Chest')
 bone('UpperArm.'+l,(s*.224,.016,1.005),(s*.428,.016,.993),'Shoulder.'+l)
 bone('LowerArm.'+l,(s*.428,.016,.993),(s*.635,.016,.997),'UpperArm.'+l)
 bone('Hand.'+l,(s*.635,.016,.997),(s*.765,.016,.99),'LowerArm.'+l)
 bone('UpperLeg.'+l,(s*.08,.016,.587),(s*.101,.006,.370),'Hips')
 bone('LowerLeg.'+l,(s*.101,.006,.370),(s*.123,0,.105),'UpperLeg.'+l)
 bone('Foot.'+l,(s*.123,0,.105),(s*.123,-.11,.035),'LowerLeg.'+l)
 bone('Toe.'+l,(s*.123,-.11,.035),(s*.123,-.145,.035),'Foot.'+l)
 bone('Robe.'+l,(s*.10,-.105,.664),(s*.145,-.13,.15),'Hips')
bone('Cape.01',(0,.12,1.035),(0,.17,.70),'Chest')
bone('Cape.02',(0,.17,.70),(0,.22,.43),'Cape.01')
bone('Cape.03',(0,.22,.43),(0,.25,.20),'Cape.02')
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
def blend(a,b,t):
 t=max(0,min(1,t)); return {a:1-t,b:t}
for ob in meshes:
 groups={b.name:ob.vertex_groups.new(name=b.name) for b in arm.bones}
 # Connected cape geometry can be identified independently of its height.
 adjacency=[[] for _ in ob.data.vertices]
 for e in ob.data.edges:
  a,b=e.vertices; adjacency[a].append(b); adjacency[b].append(a)
 cape=set(); visited=set()
 if 'Torso' in ob.name:
  for start in range(len(adjacency)):
   if start in visited: continue
   todo=[start]; comp=[]; visited.add(start)
   while todo:
    i=todo.pop(); comp.append(i)
    for j in adjacency[i]:
     if j not in visited: visited.add(j); todo.append(j)
   pts=[ob.data.vertices[i].co for i in comp]
   if min(p.z for p in pts)<.4 and sum(p.y for p in pts)/len(pts)>.08: cape.update(comp)
 for vert in ob.data.vertices:
  x,y,z=vert.co; l='L' if x>=0 else 'R'; name=ob.name
  if 'Head' in name: weights={'Head':1}
  elif 'Cowl' in name: weights=blend('Chest','Head',(z-1.08)/.07)
  elif 'Arm_' in name:
   a=abs(x)
   if a<.30: weights=blend('Chest','UpperArm.'+l,(a-.204)/.085)
   else: weights=blend('UpperArm.'+l,'LowerArm.'+l,(a-.395)/.065) if a<.59 else blend('LowerArm.'+l,'Hand.'+l,(a-.61)/.042)
  elif 'Leg_' in name:
   if z>.41: weights={'UpperLeg.'+l:1}
   elif z>.32: weights=blend('LowerLeg.'+l,'UpperLeg.'+l,(z-.32)/.09)
   elif z>.16: weights={'LowerLeg.'+l:1}
   elif z>.085: weights=blend('Foot.'+l,'LowerLeg.'+l,(z-.085)/.075)
   else: weights={'Foot.'+l:1}
  elif 'Split_Robe' in name: weights=blend('Hips','Robe.'+l,(.65-z)/.18)
  elif 'Hips' in name: weights={'Hips':1}
  elif vert.index in cape:
   weights=blend('Cape.02','Cape.01',(z-.60)/.20) if z>.6 else blend('Cape.03','Cape.02',(z-.32)/.28)
  else:
   weights=blend('Hips','Spine',(z-.69)/.15) if z<.84 else blend('Spine','Chest',(z-.84)/.1)
  for k,w in weights.items():
   if w>1e-6: groups[k].add([vert.index],w,'REPLACE')
 mod=ob.modifiers.new('Warlock_skin','ARMATURE'); mod.object=rig; ob.parent=rig

scene=bpy.context.scene; scene.render.fps=30; scene.frame_start=1; scene.frame_end=33
rest={b.name:b.matrix_local.to_quaternion() for b in arm.bones}
def absolute(name,head,direction):
 pb=rig.pose.bones[name]; b=arm.bones[name]
 q=(b.tail_local-b.head_local).rotation_difference(direction.normalized())@rest[name]
 pb.matrix=Matrix.LocRotScale(head,q,Vector((1,1,1)))
def local_worldrot(name,q):
 rig.pose.bones[name].rotation_quaternion=rest[name].inverted()@q@rest[name]
for frame in range(1,34):
 scene.frame_set(frame); phase=2*math.pi*(frame-1)/32
 for pb in rig.pose.bones:
  pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=Quaternion(); pb.location=(0,0,0); pb.scale=(1,1,1)
 hip=rig.pose.bones['Hips']; hip.location.z=-.025+.008*math.cos(2*phase)
 local_worldrot('Chest',Quaternion((0,0,1),.025*math.sin(phase)))
 local_worldrot('Head',Quaternion((0,0,1),-.012*math.sin(phase)))
 bpy.context.view_layer.update()
 for s,l in [(1,'L'),(-1,'R')]:
  p=phase+(0 if s==1 else math.pi); swing=max(0,math.sin(p))
  hb=arm.bones['UpperLeg.'+l].head_local.copy()+Vector((0,0,hip.location.z))
  target=Vector((s*.123,.105*math.cos(p),.105+.062*swing))
  b1=arm.bones['UpperLeg.'+l]; b2=arm.bones['LowerLeg.'+l]; a=b1.length; b=b2.length
  delta=target-hb; d=min(delta.length,a+b-.001); axis=delta.normalized()
  along=(a*a-b*b+d*d)/(2*d); height=math.sqrt(max(0,a*a-along*along))
  bend=Vector((0,-1,0)); bend=(bend-axis*bend.dot(axis)).normalized()
  knee=hb+axis*along+bend*height
  absolute('UpperLeg.'+l,hb,knee-hb); bpy.context.view_layer.update()
  absolute('LowerLeg.'+l,knee,target-knee); bpy.context.view_layer.update()
  # Level stance foot, a small toe lift on the swing phase.
  q=Quaternion((1,0,0),-.10*swing)@rest['Foot.'+l]
  rig.pose.bones['Foot.'+l].matrix=Matrix.LocRotScale(target,q,Vector((1,1,1)))
  # Relaxed arms outside the robe, with opposite-phase swing.
  q=Quaternion((1,0,0),-.21*math.cos(p))@Quaternion((0,1,0),s*math.radians(78))
  local_worldrot('UpperArm.'+l,q)
  local_worldrot('LowerArm.'+l,Quaternion((0,0,1),-s*.12))
  local_worldrot('Robe.'+l,Quaternion((1,0,0),.10*math.cos(p)-.12-.24*swing))
 local_worldrot('Cape.01',Quaternion((1,0,0),.045+.025*math.sin(phase-.5)))
 local_worldrot('Cape.02',Quaternion((1,0,0),.025*math.sin(phase-1)))
 local_worldrot('Cape.03',Quaternion((1,0,0),.035*math.sin(phase-1.5)))
 bpy.context.view_layer.update()
 for pb in rig.pose.bones:
  pb.keyframe_insert('location',frame=frame,group=pb.name); pb.keyframe_insert('rotation_quaternion',frame=frame,group=pb.name)
rig.animation_data.action.name='Walk_InPlace'
# Validate loop closure and normalized, bounded skin weights.
scene.frame_set(1); first={pb.name:pb.matrix.copy() for pb in rig.pose.bones}
scene.frame_set(33); error=max(abs(first[pb.name][r][c]-pb.matrix[r][c]) for pb in rig.pose.bones for r in range(4) for c in range(4))
assert error<1e-5,error
for ob in meshes:
 for v in ob.data.vertices:
  assert 1<=len(v.groups)<=4
  assert abs(sum(g.weight for g in v.groups)-1)<1e-5
# Combine the skinned geometry into one renderer with the shared material.
bpy.ops.object.select_all(action='DESELECT')
for ob in meshes: ob.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]; bpy.ops.object.join(); character=bpy.context.object; character.name='Warlock_SkinnedMesh'
bpy.ops.object.select_all(action='DESELECT'); character.select_set(True); rig.select_set(True); bpy.context.view_layer.objects.active=rig
scene.frame_set(1)
for n in character.data.materials[0].node_tree.nodes:
 if n.type=='TEX_IMAGE' and n.image:
  n.image.filepath_raw=str(OUT/(n.image.name+'.png')); n.image.save(); n.image.pack()
bpy.ops.export_scene.fbx(filepath=str(OUT/'Warlock_Walk.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'Warlock_Walk.glb'),use_selection=True,export_format='GLB',export_animations=True)
scene.frame_end=32
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Warlock_Walk.blend'))
(OUT/'validation.json').write_text(json.dumps({'triangles':5097,'bones':len(arm.bones),'clip':'Walk_InPlace','fps':30,'export_frames':[1,33],'duration_seconds':32/30,'loop_matrix_error':error,'max_influences':max(len(v.groups) for v in character.data.vertices),'root_motion':False},indent=2))
# Render contact and passing poses for inspection.
scene.render.resolution_x=800; scene.render.resolution_y=900; scene.cycles.samples=20
for frame in [1,9,17,25]:
 scene.frame_set(frame); scene.render.filepath=str(OUT/('Walk_fixed_%02d.png'%frame)); bpy.ops.render.render(write_still=True)
print('WALK_READY',error)
