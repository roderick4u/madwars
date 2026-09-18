from pathlib import Path
source=Path(__file__).with_name('preview_walk.py').read_text().replace("'output/walk_cycle'","'output/walk_natural'")
exec(compile(source,'preview_natural_generated','exec'))
