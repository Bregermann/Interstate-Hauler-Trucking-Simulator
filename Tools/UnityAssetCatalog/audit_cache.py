"""Read downloaded Unity packages without importing/extracting vendor content."""
import argparse
import gzip
import json
import os
from pathlib import Path
import tarfile

KEYWORDS = ('vfx', 'visual effect', 'waypoint', 'marker', 'objective', 'mission', 'checkpoint', 'ring',
            'circle', 'ground', 'area indicator', 'aoe', 'hologram', 'beacon', 'arrow', 'navigation', 'gps',
            'destination', 'target', 'pickup', 'quest', 'decal', 'particle', 'shader graph', 'portal',
            'teleporter', 'spawn', 'location', 'character', 'modular')


def audit(project, cache):
    assets = []
    for package in sorted(cache.rglob('*.unitypackage')):
        relative = package.relative_to(cache)
        entry = dict(name=package.stem, publisher=relative.parts[0], category=relative.parts[1],
                     assetStoreId=None, version=None, owned='unverified; downloaded cache evidence only',
                     downloaded=True, cachePath=str(package), importedIntoCurrentProject=None,
                     discoverySource='Unity Asset Store-5.x cache', tags=[], relevantContent=[])
        entry['tags'] = [word for word in KEYWORDS if word in (entry['name']+' '+entry['category']).lower()]
        # Bound archive inspection to relevant packs, not the entire asset library.
        if any(word in entry['name'].lower() for word in ('particle ingredient', 'hologram', 'character customizer', 'compass')):
            names = []
            with gzip.open(package, 'rb') as compressed, tarfile.open(fileobj=compressed, mode='r|') as archive:
                for member in archive:
                    if member.name.endswith('/pathname') and member.size < 8192:
                        names.append(archive.extractfile(member).read().decode('utf-8', errors='replace').splitlines()[0].rstrip('\x00'))
            relevant = [name for name in names if any(word in name.lower() for word in KEYWORDS)]
            entry['relevantContent'] = relevant[:120]
            entry['archiveAssetCount'] = len(names)
            entry['importedAssetCount'] = sum((project / name).exists() for name in names)
            entry['importedIntoCurrentProject'] = entry['importedAssetCount'] > 0
        assets.append(entry)
    return dict(schemaVersion=1, fullOwnedLibrary=False,
                access='FULL MY ASSETS LIBRARY NOT PROGRAMMATICALLY ACCESSIBLE: available Unity CLI has no My Assets listing API; auth session stale. Cache is not purchase inventory.',
                cachePath=str(cache), assets=assets)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--project', type=Path, default=Path.cwd())
    parser.add_argument('--cache', type=Path, default=Path(os.environ['APPDATA'])/'Unity/Asset Store-5.x')
    args = parser.parse_args()
    result = audit(args.project, args.cache)
    output = args.project/'Tools/UnityAssetCatalog/owned_assets.json'
    output.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(f'{len(result["assets"])} downloaded packages cataloged; full owned library: NOT verified')
    for item in result['assets']:
        if 'archiveAssetCount' in item:
            print(item['name'], item['archiveAssetCount'], 'assets;', item['importedAssetCount'], 'imported')
            print('\n'.join(item['relevantContent'][:12]))
