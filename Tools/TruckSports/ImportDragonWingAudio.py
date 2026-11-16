"""Exact owned-cache Malbers wing audio intake; dry-run unless --apply.

Reads only the five named wing WAVs and original metadata from the owner cache.
No Unity import, vendor edits, overwrite of differing files, or external writes.
"""
import argparse
import gzip
import hashlib
import json
import re
import struct
import subprocess
import tarfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = Path.home() / 'AppData/Roaming/Unity/Asset Store-5.x/MalberS Animations/Editor ExtensionsAnimation/Animal Controller Malbers Character Controller.unitypackage'
PACKAGE_SHA = '390f0b3a0ba495aa18ff9dd7ba6ad0ccaa063dbfd1d4f0e2d5e3e9b7e1505b8e'
PREFIX = 'Assets/Malbers Animations/Common/Audio/Wings/'
DEST = Path('Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/MalbersWings')
# Exact audio and original cache metadata hashes, independently inspected before intake.
EXPECTED = {
    'Flap Wing 2.wav': ('06266e5efe4d1b043929c582b429bbd3', '80d1bfa3e32a0abc41efbc8c8b49bc687a787bec640d047b0325c9a9bf0cf097', '8eb8d38ea831f77ae1472c708e96d1fc5652b31f0e4ee476bc3e3407c1359bae'),
    'wing-flap-Big.wav': ('1b7be1853723ee64eb6cf485ca3fed8a', 'ad6dbe0e75a64e4bf1b9d19344e377eb1ebd19b8a7a36fd0a4985cdb42d2737b', 'f9c6feded948c533e87fdca3e4be5db5a0e182e7bf0ead0d50e2055014f91af1'),
    'Flap Wing 1.wav': ('5d899c056941065488afa4b7af4e16ae', '260bc5ac9a7634dfdcae02ec580b3cc9f3e0f583b5d94ae0e7dece6983f0ae39', '0baab905811dd1a238f021613961958e697a6775ff2711718108da25303656fd'),
    'Wing Flap Big1.wav': ('867ba3f2495c1cc4ea7e2ba3cec14107', '1cbb09911823e4f6b36f43f872e81d7ee42c626812d40f6b4c156d06a3effcbc', 'c4a725a98ea46866135bc5e2570faf87fba0e1fbcdcb2b598d5da31eb9a19343'),
    'Wing Flap 02.wav': ('f059f51c270dd6d479ad8b8338a7079f', 'd4cd58049dba5ecbaa78912a10b8649d2a5095b6d70c0de076b9ff696ece078b', '46b0827cc33d6def64d9c0398dd83920a1f95fd9935b1f12eecca893697d47f1'),
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def archive(path):
    return tarfile.open(fileobj=gzip.open(path, 'rb'), mode='r|')


def audio_format(data):
    if data[:4] != b'RIFF' or data[8:12] != b'WAVE':
        raise ValueError('Expected actual RIFF WAVE')
    offset, result = 12, {}
    while offset + 8 <= len(data):
        kind, length = struct.unpack_from('<4sI', data, offset)
        if kind == b'fmt ':
            tag, channels, rate, byte_rate, block, bits = struct.unpack_from('<HHIIHH', data, offset + 8)
            result.update(formatTag=tag, channels=channels, sampleRate=rate, bits=bits, byteRate=byte_rate)
        elif kind == b'data':
            result['dataBytes'] = length
        offset += 8 + length + (length & 1)
    result['seconds'] = result['dataBytes'] / result.pop('byteRate')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--package', type=Path, default=PACKAGE)
    args = parser.parse_args()
    with args.package.open('rb') as source:
        package_sha = hashlib.file_digest(source, 'sha256').hexdigest()
    if package_sha != PACKAGE_SHA:
        raise RuntimeError('Expected exact inspected owned cache; package SHA differs')
    selected = {}
    with archive(args.package) as package:
        for member in package:
            if member.name.endswith('/pathname'):
                name = package.extractfile(member).read().decode('utf-8-sig').splitlines()[0].rstrip('\0')
                if name.startswith(PREFIX) and name[len(PREFIX):] in EXPECTED:
                    if name in selected:
                        raise RuntimeError('Ambiguous duplicate package pathname: ' + name)
                    selected[name] = member.name.rsplit('/', 1)[0]
    if len(selected) != len(EXPECTED):
        raise RuntimeError('The five exact inspected source files are required')
    wanted = {folder + '/' + part: (name[len(PREFIX):], part) for name, folder in selected.items() for part in ('asset', 'asset.meta')}
    data = {}
    with archive(args.package) as package:
        for member in package:
            if member.name in wanted and member.isfile():
                data[wanted[member.name]] = package.extractfile(member).read()
    records, payloads = [], {}
    for name, (guid, audio_sha, meta_sha) in EXPECTED.items():
        audio, meta = data[name, 'asset'], data[name, 'asset.meta']
        found = re.search(rb'^guid: ([a-f0-9]{32})', meta, re.MULTILINE)
        if sha(audio) != audio_sha or sha(meta) != meta_sha or found is None or found.group(1).decode() != guid:
            raise RuntimeError('Exact cached audio/metadata identity failed: ' + name)
        for suffix, payload in [('', audio), ('.meta', meta)]:
            relative = DEST / (name + suffix)
            target = ROOT / relative
            if not target.resolve().is_relative_to((ROOT / DEST).resolve()):
                raise RuntimeError('Unsafe destination')
            if subprocess.run(['git', 'check-ignore', '--quiet', '--', relative.as_posix()], cwd=ROOT).returncode != 0:
                raise RuntimeError('Private licensed destination must be ignored: ' + str(relative))
            if target.exists() and target.read_bytes() != payload:
                raise RuntimeError('Preserve differing existing file: ' + str(relative))
            payloads[relative] = payload
        records.append(dict(source=PREFIX + name, destination=(DEST / name).as_posix(), guid=guid, audioSha256=audio_sha, originalMetaSha256=meta_sha, bytes=len(audio), format=audio_format(audio)))
    scan = subprocess.run(['rg', '--crlf', '--threads', '2', '--no-ignore', '--hidden', '-l', '^guid: (' + '|'.join(entry[0] for entry in EXPECTED.values()) + ')$', 'Assets', '-g', '*.meta'], cwd=ROOT, capture_output=True, text=True, encoding='utf-8')
    if scan.returncode not in (0, 1):
        raise RuntimeError(scan.stderr)
    for line in scan.stdout.splitlines():
        if Path(line).as_posix() not in {relative.as_posix() for relative in payloads}:
            raise RuntimeError('GUID collision outside exact destination: ' + line)
    copied = 0
    if args.apply:
        for relative, payload in payloads.items():
            target = ROOT / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                with target.open('xb') as output:
                    output.write(payload)
                copied += 1
            if target.read_bytes() != payload:
                raise RuntimeError('Copied byte verification failed: ' + str(relative))
    receipt = dict(scope='Exact owner-cache wing WAVs and original metadata; no Unity import, playback/listening or gameplay acceptance', package=str(args.package), packageSha256=package_sha, applied=args.apply, copied=copied, verifiedFiles=len(payloads), files=records)
    receipt_path = ROOT / 'Documentation/TruckSports/DragonKingdom/DragonWingAudioIntake.json'
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(applied=args.apply, copied=copied, verifiedFiles=len(payloads), receipt=str(receipt_path))))


if __name__ == '__main__':
    main()
