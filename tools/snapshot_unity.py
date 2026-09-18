"""Copy a Unity project into this repository without caches or machine-local files.
Usage: python tools/snapshot_unity.py PATH_TO_UNITY_PROJECT
Run with the Editor stopped or after saving the scene. Does not delete source files.
"""
from pathlib import Path
import argparse, shutil, re

parser=argparse.ArgumentParser()
parser.add_argument('source',type=Path)
args=parser.parse_args()
source=args.source.resolve()
root=Path(__file__).resolve().parent.parent
target=root/'UnityProject'
assert (source/'ProjectSettings/ProjectVersion.txt').is_file(),'Not a Unity project'
assert source != target,'Source and destination must differ'
excluded={'Screenshots','Screenshots.meta','_Recovery','_Recovery.meta'}
copied=0
for folder in ['Assets','Packages','ProjectSettings']:
 for file in (source/folder).rglob('*'):
  if not file.is_file():continue
  relative=file.relative_to(source)
  if any(p in excluded for p in relative.parts):continue
  if 'com.unity.ai.assistant' in relative.parts and folder=='ProjectSettings':continue
  out=target/relative;out.parent.mkdir(parents=True,exist_ok=True)
  shutil.copy2(file,out);copied+=1
# These are account/project associations, not credentials. Omit from the portable copy.
settings=target/'ProjectSettings/ProjectSettings.asset'
data=settings.read_text(encoding='utf-8-sig')
for key in ['cloudProjectId','organizationId','projectName']:
 data=re.sub(r'(?m)^(  '+key+r':).*$',r'\1',data)
settings.write_text(data,encoding='utf-8')
print(f'Copied {copied} files into {target}')
