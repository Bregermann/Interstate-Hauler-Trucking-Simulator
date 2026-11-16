"""Intake owned dragons and their dependency closures into ignored ROOT folders.

Dry-run by default. --apply writes only missing, preflighted exact assets and
project-owned assembly configuration. Never modifies vendor bytes, existing
differing files, packages/settings, CLEAN, Git refs or other sport assets.
"""
import argparse
import gzip
import hashlib
import json
import re
import subprocess
import tarfile
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[2]
BASE = Path('Assets/TruckDragonKingdom')
CACHE = Path.home() / 'AppData/Roaming/Unity/Asset Store-5.x'
DRAGONS = CACHE / 'Meshtint Studio/3D ModelsCharactersCreatures/Monsters Ultimate Pack 02 Cute Series.unitypackage'
ANIMALS = CACHE / 'MalberS Animations/Editor ExtensionsAnimation/Animal Controller Malbers Character Controller.unitypackage'
OPTIONAL_EXCLUDES = ('Assets/Malbers Animations/Common/Cinemachine/',
                     'Assets/Malbers Animations/Common/Input System/',
                     'Assets/Malbers Animations/Common/Scripts/Editor/')
GUID = re.compile(rb'guid:\s*([0-9a-fA-F]{32})')
TEXT_EXTENSIONS = {'.asset', '.prefab', '.controller', '.overrideController', '.mat', '.mask', '.asmdef', '.playable'}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def meta_guid(data):
    match = re.search(rb'^guid: ([0-9a-f]{32})', data, re.MULTILINE)
    return match.group(1).decode() if match else None


def open_archive(path):
    # gzip.GzipFile handles Asset Store gzip extra headers correctly in streaming mode.
    return tarfile.open(fileobj=gzip.open(path, 'rb'), mode='r|')


class OwnedPackage:
    def __init__(self, path, destination):
        self.path = path
        self.destination = BASE / destination
        self.index = {}
        with open_archive(path) as archive:
            for member in archive:
                if member.name.endswith('/pathname'):
                    original = archive.extractfile(member).read().decode('utf-8-sig').splitlines()[0].rstrip('\0')
                    pure = PurePosixPath(original)
                    if pure.is_absolute() or '..' in pure.parts:
                        raise ValueError('Unsafe package pathname: ' + original)
                    self.index[original] = member.name.rsplit('/', 1)[0]
        reverse = {value: key for key, value in self.index.items()}
        self.metadata, self.text = {}, {}
        with open_archive(path) as archive:
            for member in archive:
                parent, _, part = member.name.rpartition('/')
                original = reverse.get(parent)
                if original is None:
                    continue
                if part == 'asset.meta':
                    self.metadata[original] = archive.extractfile(member).read()
                elif part == 'asset' and Path(original).suffix in TEXT_EXTENSIONS:
                    self.text[original] = archive.extractfile(member).read()
        self.guid_paths = {meta_guid(meta): name for name, meta in self.metadata.items() if meta_guid(meta)}

    def permitted(self, original):
        if original.startswith('Assets/Malbers Animations/Common/Scripts/Editor/Icons/') and not original.endswith('.cs'):
            return True  # Preserve referenced MonoImporter icons without Editor code.
        return original.startswith('Assets/') and not original.endswith('.asmdef') and not any(original.startswith(prefix) for prefix in OPTIONAL_EXCLUDES)

    def target(self, original):
        return self.destination / PurePosixPath(original).relative_to('Assets')

    def references(self, original):
        data = self.metadata.get(original, b'') + self.text.get(original, b'')
        return {match.decode().lower() for match in GUID.findall(data) if not match.startswith(b'0000000000000000')}

    def read_selected(self, selected):
        wanted = {self.index[name] + '/' + part: (name, part) for name in selected for part in ('asset', 'asset.meta')}
        data = {}
        with open_archive(self.path) as archive:
            for member in archive:
                if member.name in wanted and member.isfile():
                    original, part = wanted[member.name]
                    data[(original, part)] = archive.extractfile(member).read()
        return data


def existing_guid_index():
    # rg performs one bounded metadata scan of this ROOT project only.
    roots = [name for name in ['Assets', 'Packages', 'Library/PackageCache'] if (ROOT / name).is_dir()]
    result = subprocess.run(['rg', '--threads', '2', '--no-ignore', '--hidden', '-n', '--no-heading', '^guid: [a-f0-9]{32}', *roots, '-g', '*.meta'], cwd=ROOT, capture_output=True, text=True, encoding='utf-8')
    if result.returncode not in (0, 1):
        raise RuntimeError(result.stderr)
    index = {}
    for line in result.stdout.splitlines():
        match = re.match(r'(.+):\d+:guid: ([a-f0-9]{32})', line)
        if match:
            index.setdefault(match.group(2), []).append(Path(match.group(1)))
    return index


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--dragon-package', type=Path, default=DRAGONS)
    parser.add_argument('--animal-package', type=Path, default=ANIMALS)
    parser.add_argument('--audio-source', type=Path, default=ROOT.parent / 'UnityAssetPackages/Unity Asset Packages/Assets/Plugins/Epic Sounds and FX/Fantasy_Game_16bit/Fantasy_Game_16Bit/Attacks_and_Creatures')
    args = parser.parse_args()
    packs = [OwnedPackage(args.dragon_package, 'PrivateLicensedDragons'), OwnedPackage(args.animal_package, 'PrivateLicensedAnimalController')]
    global_guids = {guid: (pack, path) for pack in packs for guid, path in pack.guid_paths.items()}
    selected = {pack: set() for pack in packs}
    dragon_prefix = 'Assets/Monsters Ultimate Pack 02 Cute Series/Dragon '
    for name in packs[0].index:
        if name.startswith(dragon_prefix) and ('/Scenes/' not in name and '/Read Me/' not in name) and Path(name).suffix.lower() in {'.fbx', '.prefab', '.controller', '.mat', '.psd', '.png', '.tga'}:
            selected[packs[0]].add(name)
    malbers = packs[1]
    # Smoke/Big Dragon Dust share a texture GUID with a DIFFERENT existing
    # Weather Maker texture. Leave those optional effects out; do not remap it.
    # Other optional effects have missing original texture/material GUIDs in
    # the owned cache. Retain the three complete breath closures only.
    effects = {'FireBreath.prefab', 'FireBreath Hard.prefab', 'FireBreath 1.prefab'}
    for name in malbers.index:
        if (name.startswith('Assets/Malbers Animations/Common/Scripts/') and name.endswith('.cs') and malbers.permitted(name)) or name.startswith('Assets/Malbers Animations/Common/Scriptable Assets/IDs/') or (name.startswith('Assets/Malbers Animations/Animal Controller/Empty Controller/') and Path(name).suffix in {'.asset', '.controller', '.mask'}) or (name.startswith('Assets/Malbers Animations/Common/Particles/Prefabs/') and Path(name).name in effects) or (name.startswith('Assets/Malbers Animations/Common/Audio/Fire/') and name.endswith('.wav')):
            selected[malbers].add(name)
    existing = existing_guid_index()
    unresolved, omitted, queue = {}, {}, [(pack, name) for pack in packs for name in selected[pack]]
    while queue:
        pack, name = queue.pop()
        for guid in pack.references(name):
            if guid in global_guids:
                other, dependency = global_guids[guid]
                if not other.permitted(dependency):
                    omitted.setdefault(dependency, set()).add(name)
                elif dependency not in selected[other]:
                    selected[other].add(dependency)
                    queue.append((other, dependency))
            elif guid not in existing:
                unresolved.setdefault(guid, set()).add(name)
    # Include each actual ancestor folder metadata, preserving original GUIDs.
    for pack in packs:
        for name in list(selected[pack]):
            for parent in PurePosixPath(name).parents:
                if str(parent) in pack.metadata and str(parent).startswith('Assets/'):
                    selected[pack].add(str(parent))
    payloads, provenance, reused, conflicts, folders = {}, [], [], [], set()
    for pack in packs:
        raw = pack.read_selected(selected[pack])
        for name in sorted(selected[pack]):
            meta = raw.get((name, 'asset.meta'))
            if meta is None:
                raise RuntimeError('Asset has no original metadata: ' + name)
            target = pack.target(name)
            guid = meta_guid(meta)
            other_locations = [entry for entry in existing.get(guid, []) if entry.as_posix().lower() != (target.as_posix() + '.meta').lower()]
            if other_locations:
                # Reuse an already imported byte-identical asset; never introduce duplicate GUIDs.
                asset = raw.get((name, 'asset'))
                if len(other_locations) != 1 or asset is None or not (ROOT / other_locations[0].with_suffix('')).is_file() or (ROOT / other_locations[0].with_suffix('')).read_bytes() != asset:
                    conflicts.append('Existing different/ambiguous GUID collision: ' + name + ' -> ' + str(other_locations))
                    continue
                reused.append(dict(original=name, existing=other_locations[0].with_suffix('').as_posix(), guid=guid))
                continue
            if (name, 'asset') in raw:
                payloads[target] = raw[(name, 'asset')]
            elif b'folderAsset: yes' in meta:
                folders.add(target)
            payloads[Path(str(target) + '.meta')] = meta
        provenance.append(dict(path=str(pack.path), sha256=digest(pack.path.read_bytes()), bytes=pack.path.stat().st_size, selectedAssetCount=len(selected[pack])))
    # Real owned creature sounds complement the controller's fire audio.
    for filename in ['Fantasy_Game_Creature_Longer_Low_Growl.wav', 'Fantasy_Game_Creature_Growl_Loud_Gritty.wav', 'Fantasy_Game_Creature_Growl_Loud_2.wav']:
        source = args.audio_source / filename
        meta = Path(str(source) + '.meta').read_bytes()
        guid = meta_guid(meta)
        relative = BASE / 'PrivateLicensedAudioVfx/EpicSounds' / filename
        collisions = [entry for entry in existing.get(guid, []) if entry.as_posix().lower() != (relative.as_posix() + '.meta').lower()]
        if collisions:
            if len(collisions) != 1 or (ROOT / collisions[0].with_suffix('')).read_bytes() != source.read_bytes():
                raise RuntimeError('Audio GUID collision: ' + filename)
            reused.append(dict(original=str(source), existing=collisions[0].with_suffix('').as_posix(), guid=guid))
        else:
            payloads[relative] = source.read_bytes()
            payloads[Path(str(relative) + '.meta')] = meta
    # Original cache asmdef contains an unresolved reference; v3 camera integrations
    # are optional. Configuration belongs to LWS; no copied vendor code is rewritten.
    assembly = dict(name='MalbersAnimations', references=['Unity.Mathematics', 'Unity.AI.Navigation', 'Unity.TextMeshPro', 'Unity.Splines', 'Unity.InputSystem', 'Unity.Timeline', 'UnityEngine.UI'], includePlatforms=[], excludePlatforms=[], allowUnsafeCode=False, autoReferenced=True)
    asm_path = BASE / 'PrivateLicensedAnimalController/LWS.DragonKingdom.MalbersRuntime.asmdef'
    payloads[asm_path] = (json.dumps(assembly, indent=2) + '\n').encode()
    payloads[Path(str(asm_path) + '.meta')] = b'fileFormatVersion: 2\nguid: 49a2a26dc8d94c67a5915cafec190b4e\nAssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: LWS owned assembly configuration; vendor source bytes preserved\n  assetBundleName: \n  assetBundleVariant: \n'
    failures = list(conflicts)
    for relative in folders:
        if (ROOT / relative).exists() and not (ROOT / relative).is_dir():
            failures.append('Folder target is an existing file: ' + str(relative))
    ignored_result = subprocess.run(['git', 'check-ignore', '--stdin', '-z'], input=b'\0'.join(p.as_posix().encode() for p in payloads) + b'\0', cwd=ROOT, capture_output=True)
    if ignored_result.returncode not in (0, 1):
        raise RuntimeError(ignored_result.stderr.decode(errors='replace'))
    ignored_paths = set(ignored_result.stdout.decode().split('\0'))
    for relative, data in payloads.items():
        target = ROOT / relative
        if not target.resolve().is_relative_to((ROOT / BASE).resolve()) or not any(part.startswith('PrivateLicensed') for part in relative.parts):
            failures.append('Target outside private intake roots: ' + str(relative))
        if relative.as_posix() not in ignored_paths:
            failures.append('Licensed destination is not Git-ignored: ' + str(relative))
        if target.exists() and (not target.is_file() or target.read_bytes() != data):
            failures.append('Preserve differing existing file: ' + str(relative))
    report = dict(mode='apply' if args.apply else 'dry-run', packages=provenance, folders=sorted(p.as_posix() for p in folders), optionalDirectoryExcludes=list(OPTIONAL_EXCLUDES), inspectorIconException='Referenced Editor/Icons image assets are retained; Editor C# is omitted.', optionalEffectsExcludedForExistingGuidCollision=['Smoke.prefab', 'Big Dragon Dust.prefab'], optionalEffectsExcludedForMissingCacheDependencies=['FireBall.prefab', 'FireBall Lite.prefab', 'Hit Effect Fire.prefab', 'Big Dragon Track.prefab'], excludedPackageManifest=True, excludedOriginalAssemblyDefinitions=True, projectOwnedAssembly=assembly, unresolvedGuidReferences={k: sorted(v) for k, v in unresolved.items()}, omittedOptionalReferences={k: sorted(v) for k, v in omitted.items()}, reusedExistingAssets=reused, preflightFailures=failures, files=[dict(relative=p.as_posix(), sha256=digest(data), bytes=len(data)) for p, data in sorted(payloads.items())])
    report_path = ROOT / 'Documentation/TruckSports/DragonKingdom/DragonAssetAudit.Intake.json'
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(mode=report['mode'], files=len(payloads), bytes=sum(map(len, payloads.values())), unresolvedGuids=len(unresolved), omittedOptionalReferences=len(omitted), preflightFailures=len(failures), report=str(report_path)), indent=2))
    if failures or unresolved or omitted:
        raise RuntimeError('Intake requires resolution of all recorded preflight/dependency issues before writes.')
    if args.apply:
        copied = 0
        for relative in sorted(folders):
            (ROOT / relative).mkdir(parents=True, exist_ok=True)
        for relative, data in payloads.items():
            target = ROOT / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                with target.open('xb') as output:
                    output.write(data)
                copied += 1
            if digest(target.read_bytes()) != digest(data):
                raise RuntimeError('Post-copy hash mismatch: ' + str(relative))
        report['copyVerified'] = True
        report['copiedFiles'] = copied
        report['alreadyExactFiles'] = len(payloads) - copied
        report_path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
        print('Copied/verified exact owned asset bytes in ROOT only; no vendor source or existing file overwritten.')


if __name__ == '__main__':
    main()
