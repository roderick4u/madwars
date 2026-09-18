import bpy, math, os, json, random
import numpy as np
from mathutils import Vector
OUT=os.path.join(os.path.dirname(__file__),'output'); os.makedirs(OUT,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
parts=[]
# A single 1024 atlas: cloth, gold, skin, beard, horn, cavity, eye, lip.
colors=[(.255,.125,.34),(.64,.46,.16),(.60,.39,.26),(.115,.13,.135),(.25,.235,.255),(.038,.026,.048),(.59,.64,.53),(.32,.17,.13)]
size=1024; rng=np.random.default_rng(13)
base=np.ones((size,size,4),dtype=np.float32); orm=np.ones_like(base); normal=np.ones_like(base)
for k,c in enumerate(colors):
 x0=(k%4)*256; y0=(k//4)*512
 yy,xx=np.mgrid[0:512,0:256]; noise=rng.normal(0,.009,(512,256))
 weave=(np.sin(xx*2.2)*np.sin(yy*2.2))*.012 if k==0 else np.sin(xx*.35+np.sin(yy*.04))*.009
 shade=.89+.11*np.sin(yy/512*math.pi)+noise+weave
 base[y0:y0+512,x0:x0+256,:3]=np.clip(np.array(c)[None,None,:]*shade[:,:,None],0,1)
 orm[y0:y0+512,x0:x0+256,:3]=np.array([1,.43 if k==1 else .84,.7 if k==1 else 0])
 normal[y0:y0+512,x0:x0+256,:3]=np.stack([.5+weave*.8,.5+noise*.3,np.ones_like(xx)],axis=-1)
def image(name,arr):
 im=bpy.data.images.new(name,width=size,height=size); im.pixels.foreach_set(arr.ravel()); im.filepath_raw=os.path.join(OUT,name+'.png'); im.file_format='PNG'; im.save(); return im
bc=image('Head_BaseColor',base); om=image('Head_ORM',orm); nm=image('Head_Normal',normal)
om.colorspace_settings.name='Non-Color'; nm.colorspace_settings.name='Non-Color'
mat=bpy.data.materials.new('Warlock_Atlas_PBR'); mat.use_nodes=True
ns=mat.node_tree.nodes; links=mat.node_tree.links; bs=next(n for n in ns if n.type=='BSDF_PRINCIPLED')
def tex(im):
 n=ns.new('ShaderNodeTexImage'); n.image=im; return n
links.new(tex(bc).outputs['Color'],bs.inputs['Base Color'])
sep=ns.new('ShaderNodeSeparateColor'); links.new(tex(om).outputs['Color'],sep.inputs[0]); links.new(sep.outputs[1],bs.inputs['Roughness']); links.new(sep.outputs[2],bs.inputs['Metallic'])
nn=ns.new('ShaderNodeNormalMap'); nn.inputs['Strength'].default_value=.22; links.new(tex(nm).outputs['Color'],nn.inputs['Color']); links.new(nn.outputs[0],bs.inputs['Normal'])
def mesh(name,v,f,k):
 me=bpy.data.meshes.new(name); me.from_pydata(v,[],f); me.update(); ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); me.materials.append(mat); parts.append(ob)
 uv=me.uv_layers.new(name='UVMap'); coords=np.array(v); lo=coords.min(0); span=np.maximum(coords.max(0)-lo,.001)
 for p in me.polygons:
  axis=max(range(3),key=lambda a:abs(p.normal[a])); axes=[a for a in range(3) if a!=axis]
  for li in p.loop_indices:
   co=coords[me.loops[li].vertex_index]; u=(co[axes[0]]-lo[axes[0]])/span[axes[0]]; w=(co[axes[1]]-lo[axes[1]])/span[axes[1]]
   uv.data[li].uv=((k%4+( .04+.92*u))/4,(k//4+(.025+.95*w))/2)
 return ob
def ell(name,center,scale,k,n=16,r=10):
 v=[]; f=[]
 for j in range(1,r):
  a=math.pi*j/r
  for i in range(n):
   b=2*math.pi*i/n; v.append(tuple(center[t]+scale[t]*[math.sin(a)*math.cos(b),math.sin(a)*math.sin(b),math.cos(a)][t] for t in range(3)))
 top=len(v); v.append((center[0],center[1],center[2]+scale[2])); bot=len(v); v.append((center[0],center[1],center[2]-scale[2]))
 for j in range(r-2):
  for i in range(n):
   a=j*n+i; b=j*n+(i+1)%n; f.append((a,a+n,b+n,b))
 for i in range(n): f.extend([(top,i,(i+1)%n),(bot,(r-2)*n+(i+1)%n,(r-2)*n+i)])
 return mesh(name,v,f,k)
def tube(name,points,radii,k,sides=8):
 v=[]; f=[]
 for j,p in enumerate(points):
  tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)]); tangent.normalize(); u=tangent.cross(Vector((0,1,0))).normalized(); w=tangent.cross(u).normalized()
  for i in range(sides): v.append(Vector(p)+radii[j]*(math.cos(i*2*math.pi/sides)*u+math.sin(i*2*math.pi/sides)*w))
 for j in range(len(points)-1):
  for i in range(sides): a=j*sides+i; b=j*sides+(i+1)%sides; f.append((a,b,b+sides,a+sides))
 f.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+i for i in range(sides))]); return mesh(name,v,f,k)
# Face points toward -Y. Neck base is the modular connection at Z=0.
# Contiguous facial planes: broad cheekbones, inset cheeks, squared jaw and chin.
profile=[(.085,.034,.036),(.108,.057,.048),(.135,.074,.057),(.163,.077,.061),(.191,.072,.064),(.218,.083,.067),(.239,.086,.068),(.260,.080,.067),(.283,.080,.061),(.310,.073,.047),(.332,.050,.026)]
v=[]; f=[]; sides=20
for z,width,depth in profile:
 for i in range(sides):
  a=2*math.pi*i/sides; x=width*math.cos(a); sy=math.sin(a)
  y=-.026+depth*(math.copysign(abs(sy)**.48,sy))
  if sy<0:
   # Subtle integrated malar and hollow-cheek shaping, with no floating cheek pieces.
   y-=.004*math.exp(-((abs(x)-.055)/.025)**2-((z-.22)/.018)**2)
   y+=.004*math.exp(-((abs(x)-.05)/.025)**2-((z-.19)/.016)**2)
  v.append((x,y,z))
for j in range(len(profile)-1):
 for i in range(sides):
  a=j*sides+i; b=j*sides+(i+1)%sides; f.append((a,b,b+sides,a+sides))
f.extend([tuple(reversed(range(sides))),tuple((len(profile)-1)*sides+i for i in range(sides))])
mesh('Face',v,f,2)
ell('Neck_socket',(0,.016,.035),(.064,.056,.05),2,12,4)
# Short swept charcoal hair, with tapered locks under the hood and at the temples.
def hair_lock(name,points,radii):
 ob=tube(name,points,radii,3,6)
 # Flatten the locks in depth to read as sculpted hair ribbons, not round ropes.
 for j,p in enumerate(points):
  for vert in list(ob.data.vertices)[j*6:(j+1)*6]: vert.co.y=p[1]+(vert.co.y-p[1])*.40
 return ob
for i in range(5):
 x=-.066+i*.027
 hair_lock('Swept_fringe_%02d'%i,[(x,-.070,.324),(x+.020,-.086,.311),(x+.032,-.093,.291-i*.001)], [.021,.020,.001])
for s in [-1,1]:
 hair_lock('Temple_hair',[(s*.068,-.054,.314),(s*.080,-.069,.281),(s*.080,-.067,.248),(s*.074,-.064,.223)],[.020,.018,.014,.001])
# Hood is an open shell, its front aperture follows an angular pointed arch.
outline=[(0,.404),(.060,.38),(.085,.319),(.117,.234),(.129,.13),(.074,.062),(0,.043),(-.074,.062),(-.129,.13),(-.117,.234),(-.085,.319),(-.060,.38)]
v=[]
for depth,fac in [(-.086,1),(-.025,1.10),(.065,1.06),(.125,.76),(.145,.35)]:
 for idx,(x,z) in enumerate(outline):
  if depth<-.08 and idx in [0,1,11]: z-=.065 if idx==0 else .048
  v.append((x*fac,depth,.225+(z-.225)*fac))
f=[]; n=len(outline)
for j in range(4):
 for i in range(n): a=j*n+i; b=j*n+(i+1)%n; f.append((a,b,b+n,a+n))
f.append(tuple(range(48,60))); mesh('Hood_outer',v,f,0)
# Dark lining and gold piping frame the visible face.
outline=[(x,z-(.065 if idx==0 else .048 if idx in [1,11] else 0)) for idx,(x,z) in enumerate(outline)]
inner=[(x*.88,-.077,.222+(z-.222)*.92) for x,z in outline]
mesh('Hood_lining',[(x,-.086,z) for x,z in outline]+inner,[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],5)
tube('Gold_hood_edge',[(x,-.090,z) for x,z in outline]+[(outline[0][0],-.090,outline[0][1])],[.006]*13,1,4)
for s in [-1,1]:
 tube('Horn_L' if s<0 else 'Horn_R',[(s*.095,.023,.326),(s*.145,.025,.341),(s*.178,.023,.375),(s*.18,.02,.410),(s*.163,.015,.443),(s*.173,.014,.474)],[.031,.03,.023,.017,.01,.0008],4,8)
 # Deep eyes, angular brow and small subdued irises.
 ell('Eye_socket',(s*.039,-.092,.249),(.025,.008,.008),5,8,4)
 ell('Eye',(s*.038,-.101,.247),(.016,.007,.0035),6,8,4)
 ell('Pupil',(s*.037,-.107,.247),(.004,.0018,.0045),5,6,3)
 tube('Heavy_brow',[(s*.016,-.103,.260),(s*.038,-.104,.266),(s*.064,-.094,.264)],[.005,.007,.002],3,5)
# Sculpted wedge nose.
mesh('Nose',[(-.010,-.092,.257),(.010,-.092,.257),(-.014,-.116,.209),(.014,-.116,.209),(0,-.133,.216),(0,-.097,.204)],[(0,1,4),(0,4,2),(1,3,4),(2,4,5),(4,3,5),(0,2,5,3,1)],2)
ell('Beard_mass',(0,-.058,.141),(.078,.065,.074),3,12,7)
for s in [-1,1]:
 tube('Moustache',[(0,-.118,.196),(s*.026,-.121,.196),(s*.048,-.109,.185)],[.007,.009,.005],3,5)
 tube('Beard_lock',[(s*.045,-.096,.167),(s*.035,-.113,.130),(s*.014,-.088,.079)],[.018,.021,.003],3,5)
ell('Lower_lip',(0,-.121,.180),(.022,.004,.004),7,8,3)
# Weld each component internally and recalculate normals, preserving intentional separations.
import bmesh
for ob in parts:
 bm=bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(ob.data); bm.free()
bpy.ops.object.select_all(action='DESELECT')
for ob in parts: ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join(); head=bpy.context.object; head.name='Warlock_Head_LOD0'
bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
head.data.calc_loop_triangles(); count=len(head.data.loop_triangles)
if count>2500:
 mod=head.modifiers.new('Budget','DECIMATE'); mod.ratio=2450/count; bpy.ops.object.modifier_apply(modifier=mod.name)
head.data.calc_loop_triangles(); stats={'triangles':len(head.data.loop_triangles),'vertices':len(head.data.vertices),'materials':len(head.data.materials),'texture_resolution':1024,'front':'-Y','neck_connection':'origin (0,0,0)','rigged':False}
head['asset_stats']=json.dumps(stats); assert 1500<=stats['triangles']<=2500,stats
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'Warlock_Head.glb'),use_selection=True,export_format='GLB')
world=bpy.context.scene.world; world.use_nodes=True; next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.035,.042,.06,1)
def track(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.57,-1.35,.53)); camera=bpy.context.object; track(camera,(0,0,.245)); camera.data.type='ORTHO'; camera.data.ortho_scale=.60; bpy.context.scene.camera=camera
for name,loc,power,color,sz in [('Key',(-.6,-.8,1),35,(1,.83,.67),.7),('Fill',(.5,-.4,.5),12,(.63,.73,1),.6),('Rim',(.2,.5,.75),40,(.65,1,.80),.5)]:
 bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object; light.name=name; light.data.energy=power; light.data.color=color; light.data.shape='DISK'; light.data.size=sz; track(light,(0,0,.23))
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.filepath=os.path.join(OUT,'Warlock_Head_preview_v2.png')
bpy.ops.object.select_all(action='DESELECT'); head.select_set(True); bpy.context.view_layer.objects.active=head
for im in [bc,om,nm]: im.pack()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Warlock_Head.blend'))
with open(os.path.join(OUT,'stats.json'),'w') as file: json.dump(stats,file,indent=2)
bpy.ops.render.render(write_still=True)
print('HEAD_STATS',stats)
