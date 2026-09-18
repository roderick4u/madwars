"""Static, read-only checks of the files that Git would publish."""
from pathlib import Path
import json,re,subprocess,sys

root=Path(__file__).resolve().parent.parent
listed=subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z'],cwd=root)
files=sorted({Path(p.decode('utf-8')) for p in listed.split(b'\0') if p})
errors=[]
lfs_ext={'.blend','.fbx','.glb','.png','.jpg','.gif','.mp4'}
size=0;lfs_bytes=0;lfs_files=0
for rel in files:
 file=root/rel
 if not file.is_file():continue
 length=file.stat().st_size;size+=length
 if file.suffix.lower() in lfs_ext:lfs_bytes+=length;lfs_files+=1
 elif length>=50*1024*1024:errors.append(f'Large non-LFS file: {rel}')
 if any(p.lower() in {'library','temp','logs','usersettings','.git'} for p in rel.parts):errors.append(f'Generated/local directory: {rel}')
 if file.suffix.lower() in {'.cs','.shader','.py','.json','.md','.asset','.unity','.prefab','.mat','.controller'}:
  data=file.read_text(encoding='utf-8-sig',errors='replace')
  # Fail on recognizable private keys and common access-token formats without printing them.
  if re.search(r'-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}|AKIA[0-9A-Z]{16}',data):errors.append(f'Potential credential: {rel}')
  if file.suffix in {'.cs','.shader'} and re.search(r'[A-Za-z]:[\\/]',data):errors.append(f'Host-specific code path: {rel}')
unity=root/'UnityProject'
for folder in ['Assets','Packages','ProjectSettings']:
 if not (unity/folder).is_dir():errors.append(f'Missing Unity folder: {folder}')
for name in ['manifest.json','packages-lock.json']:
 data=json.loads((unity/'Packages'/name).read_text())
 if not isinstance(data.get('dependencies'),dict):errors.append(f'Invalid {name}')
 if re.search(r'file:|[A-Za-z]:\\',json.dumps(data)):errors.append(f'Local package dependency in {name}')
assets=unity/'Assets';guids={}
for file in assets.rglob('*'):
 if file.name.endswith('.meta'):
  if not Path(str(file)[:-5]).exists():errors.append(f'Orphan meta: {file.relative_to(root)}')
  match=re.search(r'(?m)^guid: (\w+)',file.read_text(errors='replace'))
  if match:
   guid=match[1]
   if guid in guids:errors.append(f'Duplicate GUID: {file.relative_to(root)}')
   guids[guid]=file
 elif not Path(str(file)+'.meta').exists():errors.append(f'Missing meta: {file.relative_to(root)}')
scene=assets/'WarlockDemo/Scenes/WarlockArena.unity'
if not scene.is_file():errors.append('Missing WarlockArena scene')
timing=json.loads((root/'output/cast_fireball/timing.json').read_text())
if (timing['release_frame']-timing['start_frame'])/timing['fps']!=.5:errors.append('Incorrect cast release timing')
report={'files':len(files),'total_mib':round(size/2**20,2),'lfs_files':lfs_files,'lfs_mib':round(lfs_bytes/2**20,2),'unity_meta_guids':len(guids),'errors':errors,'validation':'static only; clean Unity import and Android build not run'}
print(json.dumps(report,indent=2))
sys.exit(bool(errors))
