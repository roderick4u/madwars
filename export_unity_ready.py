import bpy,bmesh
from pathlib import Path
root=Path(__file__).parent/'output'; out=root/'unity_ready'; out.mkdir(exist_ok=True)
for folder,name,end in [('idle_cycle','Idle',121),('walk_natural','Walk',33),('run_dynamic','Run',25)]:
 bpy.ops.wm.open_mainfile(filepath=str(root/folder/('Warlock_'+name+'.blend')))
 rig=bpy.data.objects['Warlock_Rig']; mesh=bpy.data.objects['Warlock_SkinnedMesh']
 bm=bmesh.new(); bm.from_mesh(mesh.data); bmesh.ops.triangulate(bm,faces=list(bm.faces)); bm.to_mesh(mesh.data); bm.free()
 scene=bpy.context.scene; scene.frame_end=end; scene.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); mesh.select_set(True); bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(out/('Warlock_'+name+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
