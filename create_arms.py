"""Low-poly paired arms in assembly coordinates, matching warlock head/torso."""
from pathlib import Path
source=Path(__file__).with_name('create_head.py').read_text().split('# Face points toward -Y.')[0]
exec(compile(source.replace("'output'","'output/arms'"),'shared_helpers','exec'))
import bmesh, struct
arms=[]

def sleeve(name,s,sections,k,cap_start=False,cap_end=False):
 v=[]; f=[]; n=10
 for x,ry,rz,z in sections:
  for i in range(n):
   a=2*math.pi*i/n; v.append((s*x,.016+ry*math.cos(a),z+rz*math.sin(a)))
 for j in range(len(sections)-1):
  for i in range(n):
   a=j*n+i; b=j*n+(i+1)%n; f.append((a,b,b+n,a+n))
 if cap_start: f.append(tuple(reversed(range(n))))
 if cap_end: f.append(tuple((len(sections)-1)*n+i for i in range(n)))
 return mesh(name,v,f,k)

for s,label in [(1,'L'),(-1,'R')]:
 start=len(parts)
 sleeve('Sleeve_'+label,s,[(.204,.070,.075,-.122),(.255,.073,.075,-.125),(.325,.066,.065,-.128),(.397,.054,.054,-.133),(.428,.052,.05,-.134),(.452,.055,.052,-.133),(.515,.059,.054,-.131),(.58,.05,.045,-.13),(.624,.043,.04,-.129)],0)
 # Gold piping and leather forearm bracer.
 for x,ry,rz in [(.30,.069,.068),(.594,.049,.044),(.622,.045,.042)]:
  sleeve('Gold_sleeve_binding_'+label,s,[(x-.004,ry,rz,-.129),(x+.004,ry,rz,-.129)],1)
 sleeve('Forearm_bracer_'+label,s,[(.49,.06,.055,-.132),(.534,.058,.052,-.131),(.585,.05,.046,-.13)],4)
 # Raised gold sigil on the top of each bracer.
 tube('Bracer_sigil_'+label,[(s*.508,-.007,-.074),(s*.536,-.014,-.075),(s*.551,.002,-.077),(s*.569,-.004,-.081)],[.003]*4,1,4)
 sleeve('Wrist_'+label,s,[(.614,.028,.025,-.13),(.652,.029,.023,-.13)],2)
 # Compact palm with distinct fingers; broad flat top and underside.
 palm=ell('Palm_'+label,(s*.687,.016,-.13),(.05,.040,.022),2,10,5)
 # Four relaxed fingers, laid across palm width. Separate components for future rigging.
 for j,(y,length) in enumerate([(-.013,.071),(.007,.083),(.027,.077),(.047,.060)]):
  x=.714-abs(y-.015)*.12
  tube('Finger_%s_%d'%(label,j),[(s*x,y,-.13),(s*(x+length*.48),y-.001,-.132),(s*(x+length*.84),y-.003,-.138),(s*(x+length),y-.004,-.145)],[.010,.0095,.008,.0045],2,6)
 tube('Thumb_'+label,[(s*.664,-.009,-.133),(s*.681,-.041,-.135),(s*.709,-.057,-.14),(s*.726,-.058,-.143)],[.013,.012,.01,.005],2,6)
 subset=parts[start:]
 for ob in subset:
  bm=bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(ob.data); bm.free()
 bpy.ops.object.select_all(action='DESELECT')
 for ob in subset: ob.select_set(True)
 bpy.context.view_layer.objects.active=subset[0]; bpy.ops.object.join(); arm=bpy.context.object; arm.name='Warlock_Arm_'+label
 bpy.context.scene.cursor.location=(s*.224,.016,-.122); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 arm['attachment']='Shoulder '+label; arm['pose']='T-pose'; arms.append(arm)

bpy.ops.object.select_all(action='DESELECT')
for ob in arms: ob.select_set(True); ob.data.calc_loop_triangles()
stats={'triangles':sum(len(o.data.loop_triangles) for o in arms),'vertices_blender':sum(len(o.data.vertices) for o in arms),'triangles_per_arm':{o.name:len(o.data.loop_triangles) for o in arms},'material_count':1,'texture_resolution':1024,'pose':'T-pose','rigged':False,'shoulder_L':[.224,.016,-.122],'shoulder_R':[-.224,.016,-.122],'front':'-Y'}
assert stats['triangles']<=1500,stats
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'Warlock_Arms.glb'),use_selection=True,export_format='GLB')
raw=Path(OUT,'Warlock_Arms.glb').read_bytes(); gltf=json.loads(raw[20:20+struct.unpack_from('<I',raw,12)[0]])
stats['triangles_glb']=sum(gltf['accessors'][p['indices']]['count']//3 for m in gltf['meshes'] for p in m['primitives'])
stats['vertices_glb']=sum(gltf['accessors'][p['attributes']['POSITION']]['count'] for m in gltf['meshes'] for p in m['primitives'])
assert stats['triangles_glb']==stats['triangles']
Path(OUT,'stats.json').write_text(json.dumps(stats,indent=2))
world=bpy.context.scene.world; world.use_nodes=True; next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.035,.042,.06,1)
def track(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.65,-2.7,1.6)); camera=bpy.context.object; track(camera,(0,0,-.13)); camera.data.type='ORTHO'; camera.data.ortho_scale=1.8; bpy.context.scene.camera=camera
for name,loc,power,color,sz in [('Key',(-1,-1.3,1.5),130,(1,.83,.67),1.3),('Fill',(1,-.6,.6),65,(.63,.73,1),1),('Rim',(.3,1,1),140,(.65,1,.80),1)]:
 bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object; light.name=name; light.data.energy=power; light.data.color=color; light.data.shape='DISK'; light.data.size=sz; track(light,(0,0,-.13))
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.render.resolution_x=1400; scene.render.resolution_y=700; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'; scene.render.filepath=os.path.join(OUT,'Arms_preview.png')
bpy.ops.object.select_all(action='DESELECT')
for ob in arms: ob.select_set(True)
bpy.context.view_layer.objects.active=arms[0]
for im in [bc,om,nm]: im.pack()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Warlock_Arms.blend'))
bpy.ops.render.render(write_still=True)
# Assembly preview only: original head and torso stay unchanged on disk.
root=Path(__file__).parent/'output'
for file in [root/'Warlock_Head.glb',root/'torso/Warlock_Torso.glb']:
 bpy.ops.import_scene.gltf(filepath=str(file))
camera.location=(.85,-3.4,1.0); track(camera,(0,.02,-.22)); camera.data.ortho_scale=1.95
scene.render.resolution_y=1200; scene.render.filepath=os.path.join(OUT,'Assembly_preview.png'); bpy.ops.render.render(write_still=True)
print('ARMS_STATS',stats)
