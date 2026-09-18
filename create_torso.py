"""Modular mobile warlock torso. Neck matches head origin; front is -Y."""
from pathlib import Path
# Share the head's atlas generation and mesh helpers, but write separate assets.
source=Path(__file__).with_name('create_head.py').read_text()
source=source.split('# Face points toward -Y.')[0]
source=source.replace("'output'","'output/torso'")
exec(compile(source,'shared_head_helpers','exec'))

# Neck opening -> shoulder yoke -> fitted chest -> open waist socket.
rings=[(0,.066,.059),(-.028,.081,.068),(-.075,.224,.094),(-.14,.233,.111),(-.235,.191,.108),(-.365,.156,.095),(-.465,.154,.092)]
n=16; v=[]; f=[]
for z,rx,ry in rings:
 for i in range(n):
  a=2*math.pi*i/n
  v.append((rx*math.cos(a),.016+ry*math.sin(a),z))
for j in range(len(rings)-1):
 for i in range(n):
  a=j*n+i; b=j*n+(i+1)%n; f.append((a,b,b+n,a+n))
mesh('Fitted_robe_torso',v,f,0)

def ribbon(name,points,width,k=1):
 # Front-facing flat strips with enough thickness for mobile silhouettes.
 vv=[]; ff=[]
 for j,p in enumerate(points):
  tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
  perp=Vector((-tangent.z,0,tangent.x)).normalized()*width*.5
  vv.extend([Vector(p)+perp,Vector(p)-perp])
 for j in range(len(points)-1): ff.append((2*j,2*j+1,2*j+3,2*j+2))
 return mesh(name,vv,ff,k)

# Raised angular collar, nested purple and gold panels.
for s in [-1,1]:
 vv=[(s*.074,.025,.011),(s*.134,.025,.035),(s*.229,.010,-.045),(s*.182,-.111,-.156),(s*.078,-.113,-.267),(s*.045,-.070,-.043)]
 mesh('High_collar',vv,[(0,1,5),(1,2,3,5),(3,4,5)],0)
 ribbon('Collar_gold_outer',[(s*.132,.018,.034),(s*.224,.003,-.047),(s*.177,-.118,-.155),(s*.079,-.119,-.263)],.013)
 ribbon('Collar_inner_edge',[(s*.075,.018,.008),(s*.047,-.077,-.045),(s*.077,-.119,-.253)],.008)
 # Distinct faceted shoulder armor, no arms yet.
 vv=[(s*.15,-.071,-.068),(s*.232,-.079,-.086),(s*.293,-.043,-.123),(s*.286,.079,-.122),(s*.223,.107,-.064),(s*.155,.068,-.041),(s*.224,.014,-.038)]
 mesh('Shoulder_mantle',vv,[(0,1,6),(1,2,6),(2,3,6),(3,4,6),(4,5,6),(5,0,6)],0)
 tube('Shoulder_gold_rim',[vv[i] for i in [0,1,2,3,4,5,0]],[.005]*7,1,4)
 ribbon('Chest_gold_V',[(s*.193,-.103,-.157),(s*.138,-.120,-.235),(s*.021,-.093,-.346)],.017)
 # Crossed dark leather harness, over the robe, below collar.
 ribbon('Crossed_harness',[(s*.157,-.127,-.175),(0,-.116,-.300),(-s*.115,-.089,-.426)],.029,4)
 ell('Mantle_clasp',(s*.112,-.133,-.145),(.025,.012,.025),1,8,4)

# Small stylized skull brooch, the thematic focus at the center of the chest.
ell('Skull_brooch',(0,-.141,-.204),(.021,.012,.026),1,8,5)
for s in [-1,1]:
 ell('Skull_eye',(s*.008,-.152,-.200),(.006,.003,.007),5,6,3)
mesh('Skull_nose',[(-.003,-.154,-.213),(.003,-.154,-.213),(0,-.156,-.207)],[(0,1,2)],5)
for x in [-.010,-.0033,.0033,.010]:
 tube('Skull_tooth',[(x,-.144,-.223),(x,-.144,-.233)],[.0035,.0027],1,4)

# Belt and angular buckle remain part of the torso. Bottom stays open for hips.
def band(name,z1,z2,rx,ry,k):
 vv=[]; ff=[]
 for z in [z1,z2]:
  for i in range(16):
   a=2*math.pi*i/16; vv.append((rx*math.cos(a),.016+ry*math.sin(a),z))
 for i in range(16): ff.append((i,(i+1)%16,(i+1)%16+16,i+16))
 return mesh(name,vv,ff,k)
band('Leather_belt',-.400,-.447,.163,.103,4)
band('Belt_gold_upper',-.399,-.405,.164,.104,1)
band('Belt_gold_lower',-.441,-.448,.164,.104,1)
ell('Belt_buckle',(0,-.098,-.423),(.032,.011,.031),1,8,4)

# Full-length back cape, with longitudinal folds and a jagged scalloped hem.
# Two surfaces plus closed edge strips: visible from either side, no alpha blend.
cols=13; rows=6; front=[]
for j in range(rows):
 t=j/(rows-1); width=.223+.13*t
 for i in range(cols):
  u=-1+2*i/(cols-1)
  z=-.085-.85*t
  if j==rows-1: z+=.044*(i%2)+.045*abs(u)
  y=.088+.14*t+.032*math.cos(i*math.pi)*( .35+.65*t)-.068*abs(u)**3
  front.append((u*width,y,z))
vv=front+[(x,y+.005,z) for x,y,z in front]; ff=[]; cnt=len(front)
for j in range(rows-1):
 for i in range(cols-1):
  a=j*cols+i; ff.extend([(a,a+1,a+1+cols,a+cols),(a+cnt,a+cols+cnt,a+cols+1+cnt,a+1+cnt)])
boundary=list(range(cols))+[j*cols+cols-1 for j in range(1,rows)]+list(range((rows-1)*cols+cols-2,(rows-1)*cols-1,-1))+[j*cols for j in range(rows-2,0,-1)]
for a,b in zip(boundary,boundary[1:]+boundary[:1]): ff.append((a,b,b+cnt,a+cnt))
mesh('Folded_cape',vv,ff,0)
for idx in [0,cols-1]:
 tube('Cape_gold_selvedge',[front[j*cols+idx] for j in range(rows)],[.005]*rows,1,4)
# Gold rune-like angular marks at the bottom, on the outer/back surface.
for s in [-1,1]:
 ribbon('Cape_rune',[(s*.21,.239,-.765),(s*.23,.247,-.806),(s*.21,.252,-.841),(s*.24,.256,-.879)],.007)

import bmesh
for ob in parts:
 bm=bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(ob.data); bm.free()
bpy.ops.object.select_all(action='DESELECT')
for ob in parts: ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join(); torso=bpy.context.object; torso.name='Warlock_Torso_Cape_LOD0'
bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
torso.data.calc_loop_triangles()
count=len(torso.data.loop_triangles)
if count>1500:
 mod=torso.modifiers.new('Mobile_budget','DECIMATE'); mod.ratio=1480/count; bpy.ops.object.modifier_apply(modifier=mod.name)
torso.data.calc_loop_triangles()
stats={'triangles':len(torso.data.loop_triangles),'vertices_blender':len(torso.data.vertices),'materials':len(torso.data.materials),'texture_resolution':1024,'front':'-Y','head_connection':[0,0,0],'waist_z':-.465,'includes_cape':True,'rigged':False}
assert stats['triangles']<=1500
torso['asset_stats']=json.dumps(stats)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'Warlock_Torso.glb'),use_selection=True,export_format='GLB')
# Validate actual exported count; GLB vertices may split at flat normals and UV seams.
import struct
raw=Path(OUT,'Warlock_Torso.glb').read_bytes(); json_length=struct.unpack_from('<I',raw,12)[0]; gltf=json.loads(raw[20:20+json_length])
stats['triangles_glb']=sum(gltf['accessors'][p['indices']]['count']//3 for m in gltf['meshes'] for p in m['primitives'])
stats['vertices_glb']=sum(gltf['accessors'][p['attributes']['POSITION']]['count'] for m in gltf['meshes'] for p in m['primitives'])
assert stats['triangles_glb']<=1500
world=bpy.context.scene.world; world.use_nodes=True; next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.035,.042,.06,1)
def track(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1,-2.3,.35)); camera=bpy.context.object; track(camera,(0,.06,-.43)); camera.data.type='ORTHO'; camera.data.ortho_scale=1.22; bpy.context.scene.camera=camera
for name,loc,power,color,sz in [('Key',(-1,-1.3,1),110,(1,.83,.67),1.3),('Fill',(1,-.6,.3),60,(.63,.73,1),1),('Rim',(.3,1,.4),140,(.65,1,.80),1)]:
 bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object; light.name=name; light.data.energy=power; light.data.color=color; light.data.shape='DISK'; light.data.size=sz; track(light,(0,0,-.35))
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(OUT,'Torso_front.png')
bpy.ops.object.select_all(action='DESELECT'); torso.select_set(True); bpy.context.view_layer.objects.active=torso
for im in [bc,om,nm]: im.pack()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Warlock_Torso.blend'))
Path(OUT,'stats.json').write_text(json.dumps(stats,indent=2))
bpy.ops.render.render(write_still=True)
camera.location=(1,2.3,.25); track(camera,(0,.06,-.43)); scene.render.filepath=os.path.join(OUT,'Torso_back.png'); bpy.ops.render.render(write_still=True)
print('TORSO_STATS',stats)
