"""Locally reuse four owned GAPH water-splash examples and existing shared inputs.

Raw source stays ignored. Shared GAPH textures/materials are already imported by
ImportCheeseVfxAssets; never duplicate their GUIDs or modify vendor installations.
"""
import hashlib,json,os,subprocess,tarfile
from pathlib import Path
root=Path(__file__).resolve().parents[2];clean=root/'.sports-validation/TruckSportsPublicValidation'
package=Path(os.environ['APPDATA'])/'Unity/Asset Store-5.x/GAPH/Particle Systems/Particle Ingredient Pack.unitypackage'
prefix='Assets/Particle Ingredient Pack/Particle-Ingredient/Weather/'
names=['Ingredient-Weather-WaterSplash.prefab','Ingredient-Weather-WaterSplash2.prefab','Ingredient-Weather-WaterSplash(Splash).prefab','Ingredient-Weather-WaterSplash2(Splash).prefab']
records=[]
with tarfile.open(package,'r:gz') as archive:
    entries={m.name:m for m in archive.getmembers()}
    for name,member in entries.items():
        if not name.endswith('/pathname'):continue
        original=archive.extractfile(member).read().decode('utf-8-sig').splitlines()[0].strip('\0')
        if original not in [prefix+n for n in names]:continue
        key=name.rsplit('/',1)[0]
        for entry,suffix in [('asset',''),('asset.meta','.meta')]:
            data=archive.extractfile(entries[key+'/'+entry]).read();relative=Path('Assets/TruckDockDiving/PrivateLicensedVfx/Source')/(Path(original).name+suffix)
            assert subprocess.run(['git','check-ignore','--quiet',relative.as_posix()],cwd=root).returncode==0
            for project in [root,clean]:
                target=project/relative;assert target.resolve().is_relative_to(project.resolve())
                if target.exists():assert target.read_bytes()==data,'Preserve owner customization: '+str(target)
            records.append((relative,data,original))
assert len(records)==8
for relative,data,original in records:
    for project in [root,clean]:
        target=project/relative;target.parent.mkdir(parents=True,exist_ok=True)
        if not target.exists():target.write_bytes(data)
for project in [root,clean]:
    (project/'Assets/TruckDockDiving/PrivateLicensedVfx/intake.json').write_text(json.dumps([dict(path=p.as_posix(),source=o,sha256=hashlib.sha256(d).hexdigest()) for p,d,o in records],indent=2))
print('Reused four owned water-splash prefab sources privately; common material/texture GUIDs remain single-source.')
