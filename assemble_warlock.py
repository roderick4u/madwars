import bpy, json, struct, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).parent/'output'; OUT=ROOT/'assembled_v2'; OUT.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
objects=[]
for file,names in [(ROOT/'Warlock_Headv1.blend',['Warlock_Head_LOD0']),(ROOT/'torso/Warlock_Torso.blend',['Warlock_Torso_Cape_LOD0']),(ROOT/'arms/Warlock_Arms.blend',['Warlock_Arm_L','Warlock_Arm_R']),(ROOT/'legs/Warlock_Legs.blend',['Warlock_Hips','Warlock_Leg_L','Warlock_Leg_R','Warlock_Split_Robe'])]:
 with bpy.data.libraries.load(str(file),link=False) as (src,dst):
  assert all(n in src.objects for n in names),(str(file),src.objects)
  dst.objects=names
 for ob in dst.objects:
  bpy.context.collection.objects.link(ob); objects.append(ob)
# Seat the head inside the shoulders, eliminating the exposed neck-band gap.
head=objects[0]
for vertex in head.data.vertices: vertex.co.z-=.045
# A tailored cowl bridges the hood into the shoulder mantle, with continuous
# cloth across the nape and a pointed front bib behind the beard.
v=[]; f=[]; n=16
for j in range(3):
 for i in range(n):
  a=2*math.pi*i/n; front=max(0,-math.sin(a)); side=abs(math.cos(a))
  if j==0: rx,ry,z=.088,.088,.044-.022*front
  elif j==1: rx,ry,z=.119,.102,-.012-.047*front
  else: rx,ry,z=.166,.108,-.064-.105*front**3
  v.append((rx*math.cos(a),.016+ry*math.sin(a),z))
for j in range(2):
 for i in range(n):
  a=j*n+i; b=j*n+(i+1)%n; f.append((a,b,b+n,a+n))
me=bpy.data.meshes.new('Continuous_hood_cowl'); me.from_pydata(v,[],f); me.update()
import bmesh
bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
uv=me.uv_layers.new(name='UVMap')
for face in me.polygons:
 for li in face.loop_indices:
  idx=me.loops[li].vertex_index; uv.data[li].uv=(.012+.22*(idx%n)/(n-1),.02+.46*(idx//n)/2)
cowl=bpy.data.objects.new('Warlock_Hood_Cowl',me); bpy.context.collection.objects.link(cowl)
cowl.data.materials.append(head.data.materials[0]); objects.append(cowl)
# Every part was authored against the same atlas. Consolidate duplicate materials.
material=objects[0].data.materials[0]
for ob in objects:
 ob.data.materials.clear(); ob.data.materials.append(material)
 for face in ob.data.polygons: face.material_index=0
 ob.location.z+=1.127
 ob.data.calc_loop_triangles()
root=bpy.data.objects.new('Warlock_Root',None); bpy.context.collection.objects.link(root)
for ob in objects: ob.parent=root
stats={'triangles':sum(len(o.data.loop_triangles) for o in objects),'vertices_blender':sum(len(o.data.vertices) for o in objects),'materials':1,'mesh_parts':len(objects),'rigged':False,'front':'-Y','ground_z':0,'parts':{o.name:{'triangles':len(o.data.loop_triangles),'vertices':len(o.data.vertices)} for o in objects}}
# Check finite geometry, zero area triangles, and assembly bounds.
bad=[]
for ob in objects:
 for vert in ob.data.vertices:
  assert all(math.isfinite(c) for c in vert.co)
 for tri in ob.data.loop_triangles:
  if tri.area<1e-12: bad.append((ob.name,tri.index))
assert not bad,bad
coords=[ob.matrix_world@v.co for ob in objects for v in ob.data.vertices]
bpy.context.view_layer.update()
coords=[ob.matrix_world@v.co for ob in objects for v in ob.data.vertices]
stats['bounds']={'min':[min(v[a] for v in coords) for a in range(3)],'max':[max(v[a] for v in coords) for a in range(3)]}
# Export a single mesh primitive for one material draw, keeping editable parts in .blend.
bpy.ops.object.select_all(action='DESELECT')
duplicates=[]
for ob in objects:
 dup=ob.copy(); dup.data=ob.data.copy(); bpy.context.collection.objects.link(dup); dup.select_set(True); duplicates.append(dup)
bpy.context.view_layer.objects.active=duplicates[0]; bpy.ops.object.join(); combined=bpy.context.object; combined.name='Warlock_Mobile'
combined.parent=None; bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.export_scene.gltf(filepath=str(OUT/'Warlock_Complete.glb'),use_selection=True,export_format='GLB')
raw=(OUT/'Warlock_Complete.glb').read_bytes(); gltf=json.loads(raw[20:20+struct.unpack_from('<I',raw,12)[0]])
primitives=[p for m in gltf['meshes'] for p in m['primitives']]
stats['glb_triangles']=sum(gltf['accessors'][p['indices']]['count']//3 for p in primitives)
stats['glb_vertices']=sum(gltf['accessors'][p['attributes']['POSITION']]['count'] for p in primitives)
stats['glb_primitives']=len(primitives)
assert stats['glb_triangles']==stats['triangles']
assert len(primitives)==1
bpy.data.objects.remove(combined,do_unlink=True)
world=bpy.context.scene.world; world.use_nodes=True; next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.035,.042,.06,1)
def track(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.65,-3.4,1.827)); camera=bpy.context.object; track(camera,(0,.02,.797)); camera.data.type='ORTHO'; camera.data.ortho_scale=1.95; bpy.context.scene.camera=camera
for name,loc,power,color,sz in [('Key',(-1,-1.3,2.1),105,(1,.83,.67),1.3),('Fill',(1,-.6,1.4),65,(.63,.73,1),1),('Rim',(.3,1,2),140,(.65,1,.80),1)]:
 bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object; light.name=name; light.data.energy=power; light.data.color=color; light.data.shape='DISK'; light.data.size=sz; track(light,(0,0,.9))
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=40; scene.render.resolution_x=1200; scene.render.resolution_y=1300; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'
for n in material.node_tree.nodes:
 if n.type=='TEX_IMAGE' and n.image: n.image.pack()
# Open the editable file directly in a useful camera/material view.
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.object.select_all(action='DESELECT')
for ob in objects: ob.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
scene.render.filepath=str(OUT/'Warlock_front.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Warlock_Complete.blend'))
(OUT/'stats.json').write_text(json.dumps(stats,indent=2))
for label,loc in [('front',(.65,-3.4,1.827)),('back',(-.7,3.4,1.7)),('side',(3.4,-.25,1.5))]:
 camera.location=loc; track(camera,(0,.02,.797)); scene.render.filepath=str(OUT/('Warlock_'+label+'.png')); bpy.ops.render.render(write_still=True)
print('ASSEMBLY_STATS',stats)
