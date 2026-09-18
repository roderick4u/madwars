import bpy, math, json, bmesh
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(__file__).parent/'output';OUT=ROOT/'cast_fireball';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'idle_cycle/Warlock_Idle.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['Warlock_Rig'];character=bpy.data.objects['Warlock_SkinnedMesh'];arm=rig.data
scene.frame_set(1);rig.animation_data.action.use_fake_user=True;rig.animation_data_clear()
rest={b.name:b.matrix_local.to_quaternion() for b in arm.bones}
scene.render.fps=30;scene.frame_start=1;scene.frame_end=31
with bpy.data.libraries.load(str(ROOT/'staff/Warlock_Staff.blend'),link=False) as (src,dst):dst.objects=['Warlock_Wooden_Staff']
staff=dst.objects[0];bpy.context.collection.objects.link(staff);staff.animation_data_clear();staff.rotation_mode='QUATERNION'
# The same compact gripping-hand shape used in the Unity preview.
for v in character.data.vertices:
 if v.co.x<-.714 and v.co.z>.93:
  v.co.x=-.714+(v.co.x+.714)*.68;v.co.z=.997+(v.co.z-.997)*1.5
def rotate(name,q):rig.pose.bones[name].rotation_quaternion=rest[name].inverted()@q@rest[name]
def absolute(name,head,direction):
 b=arm.bones[name];q=(b.tail_local-b.head_local).rotation_difference(direction.normalized())@rest[name]
 rig.pose.bones[name].matrix=Matrix.LocRotScale(head,q,Vector((1,1,1)))
def solve(upper,lower,target,pole):
 start=rig.pose.bones[upper].head.copy();a=arm.bones[upper].length;b=arm.bones[lower].length
 delta=target-start;axis=delta.normalized();d=min(delta.length,a+b-.002);target=start+axis*d
 along=(a*a-b*b+d*d)/(2*d);bend=(pole-axis*pole.dot(axis)).normalized();joint=start+axis*along+bend*math.sqrt(max(0,a*a-along*along))
 absolute(upper,start,joint-start);bpy.context.view_layer.update();absolute(lower,joint,target-joint);bpy.context.view_layer.update();return target
def sample(keys,f):
 for i in range(len(keys)-1):
  if f<=keys[i+1][0]:break
 a,va=keys[i];b,vb=keys[i+1];t=max(0,min(1,(f-a)/(b-a)));t=t*t*(3-2*t)
 return va+(vb-va)*t
def vecsample(keys,f):return Vector([sample([(k,v[j]) for k,v in keys],f) for j in range(3)])
handkeys=[(1,(.293,-.04,.60)),(6,(.32,-.14,.89)),(11,(.235,-.18,1.105)),(13,(.225,-.155,1.12)),(16,(.24,-.385,1.06)),(19,(.25,-.375,1.045)),(25,(.31,-.16,.83)),(31,(.293,-.04,.60))]
handdirs=[(1,(0,-.12,-1)),(8,(-.20,-.3,.93)),(13,(-.12,-.45,.8)),(16,(0,-1,.05)),(20,(0,-1,-.1)),(31,(0,-.12,-1))]
releases={};contacts=[]
for frame in range(1,32):
 scene.frame_set(frame)
 for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.location=(0,0,0);pb.rotation_quaternion=Quaternion();pb.scale=(1,1,1)
 coil=sample([(1,0),(11,1),(13,1),(16,-.55),(20,-.35),(31,0)],frame)
 lean=sample([(1,0),(12,-3),(16,6),(20,4),(31,0)],frame)
 rig.pose.bones['Hips'].location.y=-.010-.009*max(0,coil)
 rotate('Hips',Quaternion((0,0,1),math.radians(-2.5*coil)))
 rotate('Spine',Quaternion((1,0,0),math.radians(lean*.45)))
 rotate('Chest',Quaternion((0,0,1),math.radians(-11*coil))@Quaternion((1,0,0),math.radians(lean*.55)))
 rotate('Head',Quaternion((0,0,1),math.radians(7*coil))@Quaternion((1,0,0),math.radians(-lean*.4)))
 bpy.context.view_layer.update()
 for side,s in [('L',1),('R',-1)]:
  target=arm.bones['LowerLeg.'+side].tail_local.copy();solve('UpperLeg.'+side,'LowerLeg.'+side,target,Vector((0,-1,0)))
  rig.pose.bones['Foot.'+side].matrix=Matrix.LocRotScale(target,rest['Foot.'+side],Vector((1,1,1)))
  rotate('Robe.'+side,Quaternion((1,0,0),-.03-.015*math.sin((frame-1)/30*math.pi*2)))
  contacts.append(tuple(target))
 rotate('Shoulder.L',Quaternion((0,0,1),math.radians(-3*coil)));bpy.context.view_layer.update()
 target=solve('UpperArm.L','LowerArm.L',vecsample(handkeys,frame),Vector((.8,.3,-1)))
 absolute('Hand.L',target,vecsample(handdirs,frame));bpy.context.view_layer.update()
 chest=rig.pose.bones['Chest'].head
 target=solve('UpperArm.R','LowerArm.R',Vector((-.29,-.10-.012*coil,chest.z-.035)),Vector((-.3,.25,-1)))
 tilt=Quaternion((1,0,0),math.radians(-7+2*coil))@Quaternion((0,1,0),math.radians(-5))
 basis=Matrix(((0,0,1),(1,0,0),(0,1,0))).to_quaternion()
 q=tilt@basis@rest['Hand.R'];rig.pose.bones['Hand.R'].matrix=Matrix.LocRotScale(target,q,Vector((1,1,1)))
 for i in range(1,4):rotate('Cape.%02d'%i,Quaternion((1,0,0),.01+.018*math.sin((frame-1)/30*math.tau-i*.45)))
 bpy.context.view_layer.update()
 staff.location=rig.pose.bones['Hand.R'].matrix@arm.bones['Hand.R'].matrix_local.inverted()@Vector((-.73,.016,.969));staff.rotation_quaternion=tilt;staff.scale=(.9,.9,.9)
 for channel in ['location','rotation_quaternion','scale']:staff.keyframe_insert(channel,frame=frame)
 releases[frame]=rig.pose.bones['Hand.L'].tail.copy()
 for pb in rig.pose.bones:
  for channel in ['location','rotation_quaternion','scale']:pb.keyframe_insert(channel,frame=frame,group=pb.name)
rig.animation_data.action.name='Cast_Fireball_050s';rig.animation_data.action.use_fake_user=True
for marker in list(scene.timeline_markers):scene.timeline_markers.remove(marker)
for name,frame in [('CAST_START 0.000s',1),('ANTICIPATION',11),('RELEASE_FIREBALL 0.500s',16),('RECOVERY',20),('READY 1.000s',31)]:scene.timeline_markers.new(name,frame=frame)
# Presentation-only fireball illustrates timing and local forward (-Y); excluded from animation export.
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1);fire=bpy.context.object;fire.name='PREVIEW_ONLY_Fireball'
fm=bpy.data.materials.new('Preview_Fire');fm.use_nodes=True;bs=next(n for n in fm.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(1,.20,.01,1);bs.inputs['Emission Color'].default_value=(1,.24,.005,1);bs.inputs['Emission Strength'].default_value=2;fire.data.materials.append(fm)
for frame in range(1,32):
 fire.location=releases[frame] if frame<16 else releases[16]+Vector((0,-(frame-16)/30*4.5,0))
 scale=sample([(1,.001),(6,.035),(13,.09),(16,.12),(31,.12)],frame);fire.scale=(scale,scale,scale)
 fire.keyframe_insert('location',frame=frame);fire.keyframe_insert('scale',frame=frame)
scene.frame_set(16)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);character.select_set(True);bpy.context.view_layer.objects.active=rig
bm=bmesh.new();bm.from_mesh(character.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(character.data);bm.free()
bpy.ops.export_scene.fbx(filepath=str(OUT/'Warlock_Cast.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
scene.camera.location=(2.8,-4,2.0);scene.camera.rotation_euler=(Vector((0,-.25,.91))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=2.35
scene.render.resolution_x=650;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.render.engine='CYCLES';scene.cycles.samples=8
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Warlock_Cast_Preview.blend'))
(OUT/'timing.json').write_text(json.dumps({'fps':30,'start_frame':1,'release_frame':16,'cast_seconds':.5,'end_frame':31,'total_clip_seconds':1,'forward_blender':[0,-1,0],'forward_unity':[0,0,1],'preview_fireball_only':True,'root_motion':False},indent=2))
for frame in range(1,32):
 scene.frame_set(frame);scene.render.filepath=str(OUT/('Cast_%03d.png'%frame));bpy.ops.render.render(write_still=True)
print('CAST_READY',OUT)
