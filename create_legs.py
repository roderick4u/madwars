"""Mobile warlock lower body, in the same assembly space as torso and arms."""
from pathlib import Path
source=Path(__file__).with_name('create_head.py').read_text().split('# Face points toward -Y.')[0]
exec(compile(source.replace("'output'","'output/legs'"),'shared_helpers','exec'))
import bmesh, struct

def rings_mesh(name,sections,k,n=10,cap_bottom=True):
 v=[]; f=[]
 for x,y,z,rx,ry in sections:
  for i in range(n):
   a=2*math.pi*i/n; v.append((x+rx*math.cos(a),y+ry*math.sin(a),z))
 for j in range(len(sections)-1):
  for i in range(n):
   a=j*n+i; b=j*n+(i+1)%n; f.append((a,b,b+n,a+n))
 if cap_bottom: f.append(tuple((len(sections)-1)*n+i for i in range(n)))
 return mesh(name,v,f,k)

def strip(name,pts,width,k=1):
 v=[]
 for j,p in enumerate(pts):
  t=Vector(pts[min(j+1,len(pts)-1)])-Vector(pts[max(j-1,0)])
  d=Vector((-t.z,0,t.x)).normalized()*width*.5; v.extend([Vector(p)+d,Vector(p)-d])
 return mesh(name,v,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(len(pts)-1)],k)

hips=rings_mesh('Warlock_Hips',[(0,.016,-.447,.153,.092),(0,.016,-.49,.159,.10),(0,.016,-.535,.152,.093),(0,.016,-.574,.12,.08)],0,16)
leg_groups=[]
for s,label in [(1,'L'),(-1,'R')]:
 start=len(parts)
 rings_mesh('Trousers_'+label,[(s*.080,.016,-.525,.071,.081),(s*.085,.017,-.603,.070,.075),(s*.093,.012,-.687,.060,.066),(s*.100,.003,-.747,.050,.054),(s*.103,.005,-.773,.050,.052),(s*.108,.009,-.815,.057,.057),(s*.114,.01,-.898,.049,.050),(s*.12,.007,-.976,.039,.043)],0)
 rings_mesh('Leather_boot_'+label,[(s*.108,.009,-.839,.058,.059),(s*.114,.009,-.91,.054,.055),(s*.12,.008,-.999,.042,.043),(s*.123,-.005,-1.056,.044,.058)],4)
 rings_mesh('Gold_boot_cuff_'+label,[(s*.108,.009,-.841,.060,.061),(s*.109,.009,-.853,.060,.061)],1,10,False)
 # Chamfered toe boxes, with a distinct sole and a sloped instep.
 outline=[(-.036,.052),(.036,.052),(.049,.016),(.054,-.099),(.033,-.144),(-.033,-.144),(-.054,-.099),(-.049,.016)]
 v=[]
 for z,scale in [(-1.122,1),(-1.097,1),(-1.055,.88)]:
  for dx,y in outline:
   zz=z if z< -1.08 else z+(.016 if y>-.02 else -.01)
   v.append((s*.123+dx*scale,y,zz))
 f=[]
 for j in range(2):
  for i in range(8): a=j*8+i; b=j*8+(i+1)%8; f.append((a,b,b+8,a+8))
 f.extend([tuple(reversed(range(8))),tuple(range(16,24))]); mesh('Boot_foot_'+label,v,f,4)
 mesh('Boot_sole_'+label,[(s*.123+dx,y,z) for z in [-1.127,-1.11] for dx,y in outline],[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]+[tuple(reversed(range(8))),tuple(range(8,16))],3)
 strip('Boot_gold_seam_'+label,[(s*.108,-.054,-.86),(s*.112,-.05,-.92),(s*.12,-.042,-.985)],.005)
 leg_groups.append((label,parts[start:]))

# Front split robe panels are separate from the legs for later cloth weighting.
start=len(parts)
for s,label in [(1,'L'),(-1,'R')]:
 rows=[(.012,.145,-.094,-.463),(.015,.165,-.115,-.565),(.026,.186,-.125,-.701),(.043,.211,-.13,-.852),(.069,.228,-.124,-.986)]
 v=[]
 for j,(inside,outside,y,z) in enumerate(rows):
  v.extend([(s*inside,y,z),(s*(inside+outside)*.5,y-.018,z-.008),(s*outside,y+.009,z+.015)])
 f=[]
 for j in range(4):
  for i in range(2): a=j*3+i; f.append((a,a+1,a+4,a+3))
 # Thin double surface with closed perimeter instead of alpha/two-sided shading.
 count=len(v); v += [(x,y+.004,z) for x,y,z in v]
 f += [tuple(i+count for i in reversed(face)) for face in f[:]]
 perimeter=[0,1,2,5,8,11,14,13,12,9,6,3]
 f += [(a,b,b+count,a+count) for a,b in zip(perimeter,perimeter[1:]+perimeter[:1])]
 mesh('Split_robe_'+label,v,f,0)
 for edge in [0,2]:
  strip('Robe_gold_border_'+label,[(v[j*3+edge][0],v[j*3+edge][1]-.003,v[j*3+edge][2]) for j in range(5)],.009)
 strip('Robe_gold_hem_'+label,[(v[i][0],v[i][1]-.004,v[i][2]) for i in [12,13,14]],.01)
 strip('Robe_rune_'+label,[(s*.139,-.15,-.838),(s*.126,-.151,-.882),(s*.151,-.151,-.911),(s*.159,-.145,-.949)],.005)
 strip('Robe_rune_branch_'+label,[(s*.18,-.15,-.86),(s*.177,-.151,-.90),(s*.151,-.151,-.911)],.005)
robe_parts=parts[start:]

for ob in parts:
 bm=bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(ob.data); bm.free()
def join_group(group,name,pivot):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in group: ob.select_set(True)
 bpy.context.view_layer.objects.active=group[0]; bpy.ops.object.join(); ob=bpy.context.object; ob.name=name
 bpy.context.scene.cursor.location=pivot; bpy.ops.object.origin_set(type='ORIGIN_CURSOR'); return ob
objects=[hips]
for label,group in leg_groups:
 objects.append(join_group(group,'Warlock_Leg_'+label,((.08 if label=='L' else -.08),.016,-.54)))
objects.append(join_group(robe_parts,'Warlock_Split_Robe',(0,.016,-.465)))
bpy.ops.object.select_all(action='DESELECT')
for ob in objects: ob.select_set(True); ob.data.calc_loop_triangles()
stats={'triangles':sum(len(o.data.loop_triangles) for o in objects),'vertices_blender':sum(len(o.data.vertices) for o in objects),'triangles_by_object':{o.name:len(o.data.loop_triangles) for o in objects},'materials':1,'texture_resolution':1024,'waist_connection':[0,.016,-.465],'ground_z':-1.127,'front':'-Y','rigged':False}
assert stats['triangles']<=1500,stats
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'Warlock_Legs.glb'),use_selection=True,export_format='GLB')
raw=Path(OUT,'Warlock_Legs.glb').read_bytes(); gltf=json.loads(raw[20:20+struct.unpack_from('<I',raw,12)[0]])
stats['triangles_glb']=sum(gltf['accessors'][p['indices']]['count']//3 for m in gltf['meshes'] for p in m['primitives'])
assert stats['triangles_glb']==stats['triangles']
Path(OUT,'stats.json').write_text(json.dumps(stats,indent=2))
world=bpy.context.scene.world; world.use_nodes=True; next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.035,.042,.06,1)
def track(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.75,-2.7,.25)); camera=bpy.context.object; track(camera,(0,-.02,-.79)); camera.data.type='ORTHO'; camera.data.ortho_scale=.87; bpy.context.scene.camera=camera
for name,loc,power,color,sz in [('Key',(-1,-1.3,.8),95,(1,.83,.67),1.3),('Fill',(1,-.6,0),55,(.63,.73,1),1),('Rim',(.3,1,.4),130,(.65,1,.80),1)]:
 bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object; light.name=name; light.data.energy=power; light.data.color=color; light.data.shape='DISK'; light.data.size=sz; track(light,(0,0,-.7))
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.render.resolution_x=1000; scene.render.resolution_y=1100; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'; scene.render.filepath=os.path.join(OUT,'Legs_preview.png')
bpy.ops.object.select_all(action='DESELECT')
for ob in objects: ob.select_set(True)
bpy.context.view_layer.objects.active=hips
for im in [bc,om,nm]: im.pack()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Warlock_Legs.blend'))
bpy.ops.render.render(write_still=True)
root=Path(__file__).parent/'output'
for file in [root/'Warlock_Head.glb',root/'torso/Warlock_Torso.glb',root/'arms/Warlock_Arms.glb']:
 bpy.ops.import_scene.gltf(filepath=str(file))
camera.location=(.65,-3.4,.7); track(camera,(0,.02,-.33)); camera.data.ortho_scale=1.95
scene.render.resolution_x=1300; scene.render.resolution_y=1400; scene.render.filepath=os.path.join(OUT,'Full_character_preview.png'); bpy.ops.render.render(write_still=True)
print('LEGS_STATS',stats)
