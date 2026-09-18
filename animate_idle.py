from pathlib import Path
source=Path(__file__).with_name('animate_run.py').read_text()
header=source.split('for frame in range(1,26):')[0]
header=header.replace("OUT=ROOT/'run_cycle'","OUT=ROOT/'idle_cycle'").replace("ROOT/'walk_natural/Warlock_Walk.blend'","ROOT/'run_dynamic/Warlock_Run.blend'").replace('scene.frame_end=25','scene.frame_end=121')
exec(compile(header,'idle_setup','exec'))
for frame in range(1,122):
 scene.frame_set(frame); phase=math.tau*(frame-1)/120; breath=math.sin(phase)
 for pb in rig.pose.bones:
  pb.rotation_mode='QUATERNION'; pb.location=(0,0,0); pb.rotation_quaternion=Quaternion(); pb.scale=(1,1,1)
 hips=rig.pose.bones['Hips']; hips.location.x=.0025*math.sin(phase-.5); hips.location.y=-.010+.0015*breath
 rotate('Hips',Quaternion((0,1,0),math.radians(.35)*math.sin(phase-.5)))
 rotate('Spine',Quaternion((1,0,0),math.radians(.45)*breath))
 rotate('Chest',Quaternion((1,0,0),-math.radians(.75)*breath)@Quaternion((0,0,1),math.radians(.45)*math.sin(phase-.3)))
 rig.pose.bones['Chest'].scale=(1+.004*breath,1+.003*breath,1+.007*breath)
 rotate('Head',Quaternion((1,0,0),math.radians(.25)*math.sin(phase-.2))@Quaternion((0,0,1),math.radians(.6)*math.sin(phase-.5)))
 bpy.context.view_layer.update()
 for s,l in [(1,'L'),(-1,'R')]:
  target=arm.bones['LowerLeg.'+l].tail_local.copy(); hb=rig.pose.bones['UpperLeg.'+l].head.copy()
  a=arm.bones['UpperLeg.'+l].length; b=arm.bones['LowerLeg.'+l].length; delta=target-hb; d=delta.length; assert d<a+b
  axis=delta.normalized(); along=(a*a-b*b+d*d)/(2*d); h=math.sqrt(max(0,a*a-along*along)); bend=Vector((0,-1,0)); bend=(bend-axis*bend.dot(axis)).normalized(); knee=hb+along*axis+h*bend
  absolute('UpperLeg.'+l,hb,knee-hb); bpy.context.view_layer.update()
  absolute('LowerLeg.'+l,knee,target-knee); bpy.context.view_layer.update()
  rig.pose.bones['Foot.'+l].matrix=Matrix.LocRotScale(target,rest['Foot.'+l],Vector((1,1,1)))
  rotate('Shoulder.'+l,Quaternion((0,1,0),s*math.radians(.4)*breath))
  rotate('UpperArm.'+l,Quaternion((1,0,0),math.radians(-2+.5*math.sin(phase-.35)))@Quaternion((0,1,0),s*math.radians(79)))
  rotate('LowerArm.'+l,Quaternion((0,0,1),-s*math.radians(9+.8*math.sin(phase-.5))))
  rotate('Hand.'+l,Quaternion((0,0,1),s*math.radians(.6)*math.sin(phase-.7)))
  rotate('Robe.'+l,Quaternion((1,0,0),-.035+.007*math.sin(phase-.4+s*.1)))
 rotate('Cape.01',Quaternion((1,0,0),.025+.009*math.sin(phase-.5)))
 rotate('Cape.02',Quaternion((1,0,0),.012*math.sin(phase-.8)))
 rotate('Cape.03',Quaternion((1,0,0),.016*math.sin(phase-1.1)))
 bpy.context.view_layer.update()
 for pb in rig.pose.bones:
  for channel in ['location','rotation_quaternion','scale']: pb.keyframe_insert(channel,frame=frame,group=pb.name)
footer=source.split("rig.animation_data.action.name='Run_InPlace'")[1]
footer="rig.animation_data.action.name='Idle_Breathing'"+footer
footer=footer.replace('scene.frame_set(25)','scene.frame_set(121)').replace('scene.frame_end=24','scene.frame_end=120').replace('Warlock_Run','Warlock_Idle').replace("'Run_InPlace'","'Idle_Breathing'").replace("'duration_seconds':.8","'duration_seconds':4.0").replace("'frames':[1,25]","'frames':[1,121]")
footer=footer.replace('resolution_x=600','resolution_x=480').replace('resolution_y=700','resolution_y=560').replace('scene.cycles.samples=12','scene.cycles.samples=8').replace('range(1,25,2)','range(1,121,4)').replace("'Run_%02d.png'","'Idle_%03d.png'")
exec(compile(footer,'idle_export','exec'))
