from pathlib import Path
source=Path(__file__).with_name('preview_walk.py').read_text().replace("'output/walk_cycle'","'output/idle_cycle'").replace('Warlock_Walk','Warlock_Idle').replace('range(1,33)','range(1,121)').replace('if frame%2==1:','if False:')
exec(compile(source,'validate_idle_generated','exec'))
