"""Copy the owner's existing HQ Residential House phone model locally only.

No original/vendor source is edited or added to public Git. All existing local
customizations are preserved; the owned sport recipe assigns its own material.
"""
import hashlib,json,re,shutil,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[2]
source=root.parent/'House-Cat-Simulator/Assets/HQ_ResidentialHouse/Objects/MobilePhone.FBX'
relative=Path('Assets/TruckDockDiving/PrivateLicensedProps/MobilePhone.FBX')
clean=root/'.sports-validation/TruckSportsPublicValidation'
entries=[]
for suffix in ['', '.meta']:
    original=Path(str(source)+suffix);dest=Path(str(relative)+suffix)
    assert original.is_file(),original
    assert subprocess.run(['git','check-ignore','--quiet',dest.as_posix()],cwd=root).returncode==0
    data=original.read_bytes();digest=hashlib.sha256(data).hexdigest()
    for project in [root,clean]:
        target=project/dest;assert target.resolve().is_relative_to(project.resolve())
        if target.exists():
            existing=target.read_bytes();assert existing==data or suffix=='.meta' and re.search(rb'^guid: .*',existing,re.M)[0]==re.search(rb'^guid: .*',data,re.M)[0],target
    entries.append(dict(source=str(original),relative=dest.as_posix(),sha256=digest,bytes=len(data)))
for entry in entries:
    for project in [root,clean]:
        target=project/entry['relative'];target.parent.mkdir(parents=True,exist_ok=True)
        if not target.exists():shutil.copy2(entry['source'],target)
for project in [root,clean]:(project/relative.parent/'intake.json').write_text(json.dumps(entries,indent=2))
print('Preserved two exact owned phone model/meta files in ignored local folders; vendor installation unchanged.')
