from pathlib import Path
p=Path('create_head.py')
s=p.read_text()
s=s.replace("for x,z in outline: v.append((x*fac,depth,.225+(z-.225)*fac))","for idx,(x,z) in enumerate(outline):\n  if depth<-.08 and idx in [0,1,11]: z-=.065 if idx==0 else .048\n  v.append((x*fac,depth,.225+(z-.225)*fac))")
s=s.replace("inner=[(x*.88", "outline=[(x,z-(.065 if idx==0 else .048 if idx in [1,11] else 0)) for idx,(x,z) in enumerate(outline)]\ninner=[(x*.88")
s=s.replace("(.018,.007,.006)","(.016,.007,.0035)")
s=s.replace("65,(1,.83,.67)","35,(1,.83,.67)").replace("28,(.63,.73,1)","12,(.63,.73,1)").replace("85,(.65,1,.80)","40,(.65,1,.80)")
p.write_text(s)
