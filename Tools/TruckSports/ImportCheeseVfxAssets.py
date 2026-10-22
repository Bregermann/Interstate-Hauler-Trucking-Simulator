"""Import owned GAPH VFX dependencies locally, excluding vendor demo code.

The older package contains binary prefabs. Import their complete material/texture
foundation and three explosion examples instead of guessing binary GUIDs.
Raw and generated vendor VFX stay ignored; the authoring recipe is tracked.
"""
import argparse
import hashlib
import json
import os
import re
import subprocess
import tarfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / '.sports-validation/TruckSportsPublicValidation'
TARGET = Path('Assets/TruckCheeseRolling/PrivateLicensedVfx/Source')
PREFIX = 'Assets/Particle Ingredient Pack/'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', type=Path, default=Path(os.environ['APPDATA']) / 'Unity/Asset Store-5.x/GAPH/Particle Systems/Particle Ingredient Pack.unitypackage')
    args = parser.parse_args()
    busy = subprocess.check_output(['powershell', '-NoProfile', '-Command', "Get-Process | Where-Object { $_.ProcessName -in @('Unity','TruckSports') } | Select-Object -ExpandProperty Id"], text=True).strip()
    if busy:
        raise RuntimeError('Wait for the owned Editor/player before importing: ' + busy)
    payloads = []
    with tarfile.open(args.package, 'r:gz') as package:
        entries = {m.name: m for m in package.getmembers()}
        for name in entries:
            if not name.endswith('/pathname'):
                continue
            path = package.extractfile(entries[name]).read().decode('utf-8-sig').splitlines()[0].strip('\0')
            if not path.startswith(PREFIX):
                continue
            leaf = Path(path); wanted = leaf.suffix.lower() in {'.mat', '.png', '.jpg', '.fbx'} or path in {PREFIX + 'Particle-Example/' + n for n in ['Explosion.prefab', 'Explosion2.prefab', 'Explosion3.prefab']}
            if not wanted:
                continue
            relative = TARGET / path[len(PREFIX):]
            assert relative.resolve().is_relative_to((ROOT / TARGET).resolve())
            key = name.rsplit('/', 1)[0]
            for member, suffix in [('asset', ''), ('asset.meta', '.meta')]:
                data = package.extractfile(entries[key + '/' + member]).read(); destination = Path(str(relative) + suffix)
                assert subprocess.run(['git', 'check-ignore', '--quiet', destination.as_posix()], cwd=ROOT).returncode == 0
                for project in [ROOT, CLEAN]:
                    target = project / destination
                    if target.exists() and target.read_bytes() != data:
                        same_guid = suffix == '.meta' and re.search(rb'^guid: ([a-f0-9]{32})', target.read_bytes(), re.M).group(1) == re.search(rb'^guid: ([a-f0-9]{32})', data, re.M).group(1)
                        if not same_guid:
                            raise RuntimeError('Preserve differing local asset: ' + str(target))
                payloads.append((destination, data))
    assert sum(1 for path, _ in payloads if path.suffix == '.prefab') == 3
    for relative, data in payloads:
        for project in [ROOT, CLEAN]:
            target = project / relative; target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                target.write_bytes(data)
    report = dict(package=str(args.package), license='Owner-installed Particle Ingredient Pack / GAPH; raw source and derived VFX excluded from public Git', files=[dict(path=p.as_posix(), sourceSha256=hashlib.sha256(data).hexdigest()) for p, data in payloads])
    for project in [ROOT, CLEAN]:
        (project / TARGET.parent / 'intake.json').write_text(json.dumps(report, indent=2))
    print('Imported', len(payloads), 'exact owned VFX/material/texture files into ignored local folders; no vendor scripts/scenes or original files modified.')


if __name__ == '__main__':
    main()
