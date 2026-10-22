"""Import the owner's Milo/MalberS cat clips without publishing vendor source.

Copies only the model/avatar and four relevant ambient clips from an existing
licensed installation. Original files are read-only; existing differing local
copies are never overwritten. Generated sport prefabs/controllers stay separate.
"""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / '.sports-validation/TruckSportsPublicValidation'
RELATIVE = Path('Assets/TruckCheeseRolling/PrivateLicensedAnimations')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path):
    return re.search(rb'^guid: ([a-f0-9]{32})', path.read_bytes(), re.MULTILINE).group(1)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cat-source', type=Path, default=ROOT.parent / 'House-Cat-Simulator/Assets/Malbers Animations/Animals Packs/04 Pets/Cat')
    args = parser.parse_args()
    processes = subprocess.check_output(['powershell', '-NoProfile', '-Command', "Get-Process | Where-Object { $_.ProcessName -in @('Unity','TruckSports') } | Select-Object -ExpandProperty Id"], text=True).strip()
    if processes:
        raise RuntimeError('Wait for the owned Editor/player to close before importing: ' + processes)
    entries = [('Model/Malbers Cat.fbx', 'Cat rig.fbx')]
    entries += [('Animations/Idle/' + name + '.fbx', name + '.fbx') for name in ['Cat_Idle', 'Cat_Idle_Look_Around', 'Cat_Idle_Scratch_Ear_L', 'Cat_Idle_Yawn']]
    records = []
    for source_name, target_name in entries:
        for suffix in ['', '.meta']:
            source = (args.cat_source / (source_name + suffix)).resolve()
            if not source.is_file():
                raise FileNotFoundError('Owned cat asset required: ' + str(source))
            relative = RELATIVE / (target_name + suffix)
            assert subprocess.run(['git', 'check-ignore', '--quiet', relative.as_posix()], cwd=ROOT).returncode == 0, 'Keep licensed raw assets outside public Git.'
            digest = sha(source)
            for project in [ROOT, CLEAN]:
                target = project / relative
                assert target.resolve().is_relative_to(project.resolve())
                # Unity may upgrade importer metadata or the owner may tune it.
                # Retain those settings while requiring stable referenced GUIDs.
                if target.exists() and sha(target) != digest and not (suffix == '.meta' and guid(target) == guid(source)):
                    raise RuntimeError('Preserve differing local customization: ' + str(target))
            records.append(dict(source=str(source), relative=relative.as_posix(), sha256=digest, bytes=source.stat().st_size))
    # All paths and existing customizations are checked before the first copy.
    for record in records:
        for project in [ROOT, CLEAN]:
            target = project / record['relative']; target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                shutil.copy2(record['source'], target)
            assert sha(target) == record['sha256'] or (target.suffix == '.meta' and guid(target) == guid(Path(record['source'])))
    for project in [ROOT, CLEAN]:
        (project / RELATIVE / 'intake.json').write_text(json.dumps(dict(license='Owner-installed Milo the Cat / MalberS; raw source excluded from public repository', files=records), indent=2))
    print('Preserved original licensed source; imported', len(records), 'exact files into ignored local ROOT/validation animation folders.')


if __name__ == '__main__':
    main()
