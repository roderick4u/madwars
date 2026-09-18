import bpy,json,math
from pathlib import Path
from mathutils import Vector
out=Path(__file__).parent/'output/cast_fireball'
bpy.ops.wm.open_mainfile(filepath=str(out/'Warlock_Cast_Preview.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['Warlock_Rig'];staff=bpy.data.objects['Warlock_Wooden_Staff']
scene.frame_set(1);feet={n:rig.pose.bones[n].head.copy() for n in ['Foot.L','Foot.R']};root=rig.pose.bones['Root'].matrix.copy();pose={b.name:b.matrix.copy() for b in rig.pose.bones}
maxfoot=maxroot=maxgrip=0
for frame in range(1,32):
 scene.frame_set(frame)
 for n,p in feet.items():maxfoot=max(maxfoot,(rig.pose.bones[n].head-p).length)
 maxroot=max(maxroot,max(abs(root[r][c]-rig.pose.bones['Root'].matrix[r][c]) for r in range(4) for c in range(4)))
 grip=rig.pose.bones['Hand.R'].matrix@rig.data.bones['Hand.R'].matrix_local.inverted()@Vector((-.73,.016,.969))
 maxgrip=max(maxgrip,(staff.location-grip).length)
 assert all(math.isfinite(x) for b in rig.pose.bones for row in b.matrix for x in row)
enderror=max(abs(pose[b.name][r][c]-b.matrix[r][c]) for b in rig.pose.bones for r in range(4) for c in range(4))
data={'release_seconds':(16-1)/scene.render.fps,'duration_seconds':1,'foot_drift_m':maxfoot,'root_matrix_drift':maxroot,'staff_grip_error_m':maxgrip,'ready_pose_return_error':enderror,'finite_transforms':True}
assert maxfoot<.0001 and maxroot<.0001 and maxgrip<.0001 and enderror<.0001,data
(out/'validation.json').write_text(json.dumps(data,indent=2));print(data)
