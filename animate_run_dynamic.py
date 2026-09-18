from pathlib import Path
source=Path(__file__).with_name('animate_run.py').read_text()
source=source.replace("OUT=ROOT/'run_cycle'","OUT=ROOT/'run_dynamic'")
source=source.replace("hips.location.y=-.032+.017*math.cos(2*phase-1.6); hips.location.x=-.006*math.sin(phase)","hips.location.y=-.058+.023*math.cos(2*phase-1.9); hips.location.x=-.008*math.sin(phase)")
source=source.replace('math.radians(3)*math.cos(phase)','math.radians(4)*math.cos(phase)')
source=source.replace('math.radians(7)+.012*math.sin(2*phase)','math.radians(14)+.025*math.sin(2*phase-.4)')
source=source.replace('math.radians(5)*math.cos(phase-.2)','math.radians(7)*math.cos(phase-.2)')
source=source.replace('-math.radians(5))','-math.radians(9))')
source=source.replace("if u<.35:\n   y=-.13+.26*u/.35; lift=0; recovery=0\n  else:\n   v=(u-.35)/.65; y=.13*math.cos(math.pi*v); recovery=math.sin(math.pi*v); lift=.14*recovery**1.2", "if u<.32:\n   y=-.16+.32*u/.32; lift=0; recovery=0\n  else:\n   v=(u-.32)/.68; recovery=math.sin(math.pi*v)\n   # Rear heel recovery, then fast forward knee drive and a soft landing.\n   keys=[(.32,.16,0),(.47,.22,.18),(.62,.07,.245),(.80,-.13,.135),(1.,-.16,0)]\n   for idx in range(len(keys)-1):\n    if keys[idx][0]<=u<=keys[idx+1][0]: break\n   t0,y0,z0=keys[idx]; t1,y1,z1=keys[idx+1]; vlocal=(u-t0)/(t1-t0)\n   smooth=vlocal*vlocal*(3-2*vlocal)\n   y=y0+(y1-y0)*smooth; lift=z0+(z1-z0)*smooth")
source=source.replace('.18*recovery)@rest', '.32*recovery)@rest')
source=source.replace('.46*math.cos(p-.18)', '.70*math.cos(p-.12)')
source=source.replace('math.radians(54+7*math.cos(p-.4))','math.radians(68-16*math.cos(p-.3))')
source=source.replace('-.14-.48*recovery-.15*max(0,-y/.13)','-.24-.65*recovery-.18*max(0,-y/.16)')
source=source.replace('.18+.045*math.sin(2*phase-.5)', '.28+.065*math.sin(2*phase-.5)')
source=source.replace("rig.animation_data.action.name='Run_InPlace'","rig.animation_data.action.name='Run_Dynamic_InPlace'")
source=source.replace("'clip':'Run_InPlace'","'clip':'Run_Dynamic_InPlace'")
# Relax fingers before animation; preserve topology, skin and all source files.
insert='''
adj=[[] for _ in character.data.vertices]
for e in character.data.edges:
 a,b=e.vertices; adj[a].append(b); adj[b].append(a)
seen=set()
for start in range(len(adj)):
 if start in seen: continue
 stack=[start]; comp=[]; seen.add(start)
 while stack:
  i=stack.pop(); comp.append(i)
  for j in adj[i]:
   if j not in seen: seen.add(j); stack.append(j)
 pts=[character.data.vertices[i].co for i in comp]
 if len(comp)==24 and min(abs(p.x) for p in pts)>.70 and min(p.z for p in pts)>.94:
  sign=1 if pts[0].x>0 else -1; base=min(abs(p.x) for p in pts); length=max(abs(p.x) for p in pts)-base
  for p in pts:
   t=(abs(p.x)-base)/length; k=1.65
   p.x=sign*(base+length*math.sin(k*t)/k); p.z-=length*(1-math.cos(k*t))/k
'''
source=source.replace("rig.animation_data.action.use_fake_user=True",insert+"\nrig.animation_data.action.use_fake_user=True")
exec(compile(source,'run_dynamic_generated','exec'))
