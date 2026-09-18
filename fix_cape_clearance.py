import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).parent/'output'; OUT=ROOT/'cape_fixed'; OUT.mkdir(exist_ok=True)
report={}
for folder,name,end in [('idle_cycle','Idle',121),('walk_natural','Walk',33),('run_dynamic','Run',25)]:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/folder/('Warlock_'+name+'.blend')))
 ob=bpy.data.objects['Warlock_SkinnedMesh']; rig=bpy.data.objects['Warlock_Rig']; scene=bpy.context.scene
 names={g.index:g.name for g in ob.vertex_groups}
 cape={v.index for v in ob.data.vertices if any(names[g.group].startswith('Cape.') and g.weight>.01 for g in v.groups)}
 arms={v.index for v in ob.data.vertices if any(names[g.group].startswith(('UpperArm.','LowerArm.','Hand.')) and g.weight>.1 for g in v.groups)}
 def intersections():
  counts=[]
  for frame in range(1,end):
   scene.frame_set(frame); dg=bpy.context.evaluated_depsgraph_get(); ev=ob.evaluated_get(dg); me=ev.to_mesh(); me.calc_loop_triangles()
   vertices=[v.co.copy() for v in me.vertices]
   cp=[tuple(t.vertices) for t in me.loop_triangles if all(i in cape for i in t.vertices)]
   ap=[tuple(t.vertices) for t in me.loop_triangles if all(i in arms for i in t.vertices)]
   pairs=BVHTree.FromPolygons(vertices,cp,all_triangles=True).overlap(BVHTree.FromPolygons(vertices,ap,all_triangles=True))
   if pairs: counts.append({'frame':frame,'pairs':len(pairs)})
   ev.to_mesh_clear()
  return counts
 before=intersections()
 for idx in cape:
  v=ob.data.vertices[idx]; t=max(0,min(1,(1.035-v.co.z)/.26))
  v.co.y+=.045+.16*t
  v.co.x*=.64+.21*t
 ob.data.update()
 after=intersections()
 report[name]={'before':before,'after':after,'changed_vertices':len(cape)}
 import bmesh
 bm=bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.triangulate(bm,faces=list(bm.faces)); bm.to_mesh(ob.data); bm.free()
 scene.frame_start=1; scene.frame_end=end; scene.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); rig.select_set(True); bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(OUT/('Warlock_'+name+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
 scene.frame_end=end-1
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('Warlock_'+name+'.blend')))
 if name=='Run':
  scene.frame_set(7); scene.render.resolution_x=700; scene.render.resolution_y=800; scene.render.resolution_percentage=100; scene.cycles.samples=16
  from mathutils import Vector
  scene.camera.location=(2.6,3.2,1.5); scene.camera.rotation_euler=(Vector((0,.05,.8))-scene.camera.location).to_track_quat('-Z','Y').to_euler(); scene.camera.data.ortho_scale=1.9
  scene.render.filepath=str(OUT/'Cape_clearance.png'); bpy.ops.render.render(write_still=True)
(OUT/'clearance_report.json').write_text(json.dumps(report,indent=2)); print('CLEARANCE',json.dumps(report))
