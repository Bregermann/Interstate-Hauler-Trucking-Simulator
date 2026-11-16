"""Validated owner-installed Emerald AI sound intake; dry-run unless --apply.

Writes only new exact files inside this mode's ignored licensed artifact root.
Never imports Unity, changes source metadata or overwrites a differing file.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT.parent / 'UnityAssetPackages/Unity Asset Packages/Assets/Plugins/Black Horizon Studios/Emerald AI/Sounds'
DESTINATION = Path('Assets/TruckDragonKingdom/Ambience/PrivateLicensedAudio')
FILES = ['Ambience/Birds Loop.wav', 'Spells/Fire Loop.wav']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--source', type=Path, default=SOURCE)
    args = parser.parse_args()
    payloads, records, guids = {}, [], []
    for name in FILES:
        source = args.source / name
        metadata = Path(str(source) + '.meta').read_bytes()
        guids.append(re.search(rb'^guid: ([a-f0-9]{32})', metadata, re.MULTILINE).group(1).decode())
        for suffix in ['', '.meta']:
            original = Path(str(source) + suffix)
            relative = DESTINATION / (source.name + suffix)
            data = original.read_bytes()
            target = ROOT / relative
            if not target.resolve().is_relative_to((ROOT / DESTINATION).resolve()):
                raise RuntimeError('Unsafe destination: ' + str(relative))
            if subprocess.run(['git', 'check-ignore', '--quiet', '--', relative.as_posix()], cwd=ROOT).returncode != 0:
                raise RuntimeError('Licensed destination must be ignored: ' + str(relative))
            if target.exists() and target.read_bytes() != data:
                raise RuntimeError('Preserve differing existing destination: ' + str(relative))
            payloads[relative] = data
            records.append(dict(source=str(original), relative=relative.as_posix(), sha256=hashlib.sha256(data).hexdigest(), bytes=len(data)))
    scan = subprocess.run(['rg', '--threads', '2', '--no-ignore', '--hidden', '-l', '^guid: (' + '|'.join(guids) + ')$', 'Assets', '-g', '*.meta'], cwd=ROOT, capture_output=True, text=True, encoding='utf-8')
    if scan.returncode not in (0, 1):
        raise RuntimeError(scan.stderr)
    for line in scan.stdout.splitlines():
        if Path(line).as_posix() not in {path.as_posix() for path in payloads}:
            raise RuntimeError('Preserve existing GUID collision outside exact destination: ' + line)
    copied = 0
    if args.apply:
        for relative, data in payloads.items():
            target = ROOT / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                with target.open('xb') as output:
                    output.write(data)
                copied += 1
            if target.read_bytes() != data:
                raise RuntimeError('Exact copied byte verification failed: ' + str(relative))
    receipt = dict(scope='Exact owned audio bytes only; no listening or gameplay acceptance', applied=args.apply, copied=copied, verifiedFiles=len(payloads), files=records)
    output = ROOT / 'Assets/TruckDragonKingdom/Ambience/Documentation/OwnedAudioIntake.json'
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(applied=args.apply, copied=copied, exactFiles=len(payloads), receipt=str(output))))


if __name__ == '__main__':
    main()
