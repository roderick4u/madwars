from pathlib import Path
source=Path(__file__).with_name('create_head.py').read_text().split('# Face points toward -Y.')[0]
source=source.replace("'output'","'output/staff'").replace('Head_','Staff_').replace("colors=[(.255,.125,.34),(.64,.46,.16),(.60,.39,.26),(.115,.13,.135),(.25,.235,.255),(.038,.026,.048),(.59,.64,.53),(.32,.17,.13)]","colors=[(.255,.125,.34),(.64,.46,.16),(.25,.12,.045),(.13,.055,.022),(.36,.19,.075),(.038,.026,.048),(.23,.85,.13),(.68,.59,.39)]")
exec(compile(source,'staff_helpers','exec'))
import bmesh, struct
# Upright standalone prop, scaled to the 1.5 m stylized character.
tube('Ancient_wood_shaft',[(0,0,0),(-.014,.002,.13),(.009,0,.35),(-.014,.004,.57),(0,0,.79),(.013,.003,1.00),(.027,0,1.19),(-.005,.004,1.34),(.017,.006,1.45)],[.013,.018,.021,.023,.025,.028,.037,.044,.037],2,8)
# Asymmetric wooden crown, with a readable open silhouette.
tube('Left_root_crown',[(.01,0,1.28),(-.07,.004,1.39),(-.14,.005,1.52),(-.143,0,1.65),(-.10,0,1.75),(-.12,.002,1.80)],[.035,.040,.034,.025,.013,.001],2,7)
tube('Right_root_crown',[(.02,.008,1.32),(.085,.009,1.43),(.155,.012,1.56),(.14,.008,1.69),(.105,.007,1.74)],[.038,.033,.028,.017,.001],4,7)
tube('Rear_root',[(.005,.014,1.30),(-.02,.075,1.42),(.013,.086,1.51),(.04,.055,1.56)],[.026,.024,.018,.001],3,6)
# Bark ridges deliberately sparse and angular.
for j,(z,x) in enumerate([(.12,-.006),(.34,.004),(.94,.015),(1.13,.019)]):
 tube('Raised_bark_%d'%j,[(x-.01,-.012,z),(x-.017,-.020,z+.065),(x+.004,-.026,z+.13)],[.002,.004,.001],4,4)
# Purple spiral leather grip, with gold ferrules.
for j in range(7):
 z=.63+j*.025
 tube('Grip_%02d'%j,[(-.011,0,z),( -.009,0,z+.020)],[.027,.027],0,8)
for z,r in [(.025,.018),(.62,.029),(.81,.030),(1.27,.046)]:
 tube('Antique_gold_binding',[(0,0,z),(0,0,z+.017)],[r,r],1,8)
# Faceted soul crystal: broad middle, sharp tips.
v=[(0,-.008,1.425)]; n=6
for z,rad in [(1.51,.067),(1.60,.052)]:
 for i in range(n):
  a=math.tau*i/n;v.append((rad*math.cos(a),-.005+rad*math.sin(a),z))
v.append((-.009,-.006,1.697));f=[]
for i in range(n):
 a=1+i;b=1+(i+1)%n;f.extend([(0,b,a),(a,b,b+n),(a,b+n,a+n),(a+n,b+n,13)])
mesh('Soul_crystal',v,f,6)
# A small bone talisman and purple hanging ribbon beneath the crown.
tube('Talisman_cord',[(-.035,-.023,1.32),(-.072,-.034,1.27),(-.075,-.035,1.215)],[.003]*3,3,5)
ell('Bone_charm',(-.075,-.035,1.19),(.020,.011,.025),7,8,4)
for x in [-.082,-.068]:ell('Charm_eye',(x,-.045,1.193),(.005,.002,.006),5,6,3)
mesh('Purple_ribbon',[(-.023,.007,1.27),(-.048,.007,1.27),(-.067,.012,1.17),(-.038,.012,1.17),(-.07,.007,1.09),(-.085,.007,1.08)],[(0,1,2,3),(3,2,5,4)],0)
# Shared PBR material; only the green atlas region emits.
em=np.zeros_like(base);em[:,:,3]=1;em[512:1024,512:768,:3]=(.15,.8,.055)
ei=image('Staff_Emission',em); links.new(tex(ei).outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.65
for ob in parts:
 bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();staff=bpy.context.object;staff.name='Warlock_Wooden_Staff'
# Grip-centered pivot for future hand attachment, preserving upright world placement.
bpy.context.scene.cursor.location=(0,0,.72);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
staff['attachment']='Grip origin; local Z up; front -Y';staff['height_m']=1.80
staff.data.calc_loop_triangles();stats={'triangles':len(staff.data.loop_triangles),'vertices_blender':len(staff.data.vertices),'materials':1,'height_m':1.8,'grip_height_m':.72}
assert stats['triangles']<1500,stats
for im in [bc,om,nm,ei]:im.pack()
bpy.ops.export_scene.gltf(filepath=str(Path(OUT,'Warlock_Staff.glb')),use_selection=True,export_format='GLB')
bpy.ops.export_scene.fbx(filepath=str(Path(OUT,'Warlock_Staff.fbx')),use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False)
raw=Path(OUT,'Warlock_Staff.glb').read_bytes();gltf=json.loads(raw[20:20+struct.unpack_from('<I',raw,12)[0]])
stats['vertices_glb']=sum(gltf['accessors'][p['attributes']['POSITION']]['count'] for m in gltf['meshes'] for p in m['primitives'])
Path(OUT,'stats.json').write_text(json.dumps(stats,indent=2))
def track(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.world.use_nodes=True;next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.022,.027,.038,1)
bpy.ops.object.camera_add(location=(2,-5,2.2));cam=bpy.context.object;track(cam,(0,0,.91));cam.data.type='ORTHO';cam.data.ortho_scale=2.08;scene.camera=cam
for loc,power,color,sz in [((-2,-3,3),260,(1,.80,.57),2),((2,-2,1.8),160,(.65,.77,1),2),((0,2,2.3),290,(.56,1,.73),1.5)]:
 bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.color=color;light.data.size=sz;track(light,(0,0,1))
scene.render.engine='CYCLES';scene.cycles.samples=40;scene.render.resolution_x=850;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.render.filepath=str(Path(OUT,'Staff_preview.png'))
bpy.ops.object.select_all(action='DESELECT');staff.select_set(True);bpy.context.view_layer.objects.active=staff
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_distance=2.5;area.spaces.active.region_3d.view_location=(0,0,.9);area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();area.spaces.active.shading.type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(Path(OUT,'Warlock_Staff.blend')))
bpy.ops.render.render(write_still=True)
print('STAFF_STATS',stats)
