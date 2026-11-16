"""Retain only explicitly owned new-mode Unity-generated outputs.
Refuse to overwrite ROOT edits made since the frozen native intake.
"""
from pathlib import Path
import argparse, hashlib, json, math, re, struct
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / '.sports-validation/TruckSportsPublicValidation'
FOREST_PROOF = 'Assets/TruckDragonKingdom/VisualCandidates/ForestMaterialProof'
FOREST_VENDOR = 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/'
FOREST_MATERIALS = [
    'Assets/TruckDragonKingdom/Materials/Environment/a35168ec5f8b41447ba5916c93191534 URP.mat',
    'Assets/TruckDragonKingdom/Materials/Environment/d768d77458b842644b856380df9a044f URP.mat']
FOREST_SCOPES = ('forest-proof', 'forest-material')


def _forest_hash(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _forest_guid(path):
    matches = re.findall(r'^guid: ([0-9a-f]{32})\s*$', path.read_text(encoding='utf-8-sig'), re.M)
    assert len(matches) == 1, 'Actual unique Unity GUID required: ' + str(path)
    return matches[0]


def _forest_receipt_hashes(clean, evidence, paths_key, hashes_key, allowed):
    paths, hashes = evidence.get(paths_key), evidence.get(hashes_key)
    assert isinstance(paths, list) and paths and isinstance(hashes, list) and len(paths) == len(hashes), 'Actual forest preservation hashes required.'
    assert len(set(paths)) == len(paths), 'Duplicate forest preservation path.'
    for path, digest in zip(paths, hashes):
        assert isinstance(path, str) and '\\' not in path and '..' not in Path(path).parts and allowed(path), 'Invalid forest preservation scope: ' + str(path)
        assert isinstance(digest, str) and re.fullmatch('[0-9a-f]{64}', digest), 'Actual forest SHA256 required.'
        assert (clean / path).is_file() and _forest_hash(clean / path) == digest, 'Forest preserved bytes differ: ' + path
    return dict(zip(paths, hashes))


def forest_paths(clean, run, prior, scope):
    """Select exact forest outputs only after actual native preservation receipts."""
    assert scope in FOREST_SCOPES
    stage = 'prepared' if scope == 'forest-proof' else 'upgraded'
    evidence = json.loads((run / 'rendered-evidence/ForestMaterialProof' / (stage + '.json')).read_text(encoding='utf-8-sig'))
    assert all(evidence.get(field) is True for field in ('repeatByteExact', 'originalBytesExact', 'scenePrefabBytesExact', 'materialGuidsExact')), 'Actual forest GUID/preservation/repeat assertions required.'
    preserved = _forest_receipt_hashes(clean, evidence, 'sourcePaths', 'sourceHashes', lambda path: path.startswith(FOREST_VENDOR))
    raw = [FOREST_VENDOR + 'Materials/M_ENV_tree_leaves_ancient.mat', FOREST_VENDOR + 'Materials/M_ENV_wood_ancient_01.mat']
    assert all(path in preserved and path + '.meta' in preserved for path in raw), 'Both actual vendor material/source GUID hashes required.'
    if scope == 'forest-proof':
        targets = [FOREST_PROOF + '/' + name for name in (
            'Observed original Lit leaf.mat', 'Observed original Lit wood.mat',
            'Configured vendor leaf.mat', 'Configured vendor wood.mat')]
    else:
        targets = FOREST_MATERIALS[:]
        assert evidence.get('targets') == targets, 'Only the two canonical forest materials may be upgraded.'
        assert evidence.get('before') == [prior.get(path) for path in targets] and all(prior.get(path) for path in targets), 'Forest material before hashes must match frozen intake.'
        assert evidence.get('after') == [_forest_hash(clean / path) for path in targets], 'Actual upgraded forest material hashes required.'
        expected = ['Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab', 'Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity']
        expected += ['Assets/' + mode + '/Scenes/' + mode + '.unity' for mode in ('TruckHoardHeist', 'TruckDragonEggDelivery', 'TruckDragonEscape', 'TruckDragonRiders')]
        expected = {path + suffix for path in expected if (clean / path).is_file() for suffix in ('', '.meta') if (clean / (path + suffix)).is_file()}
        kept = _forest_receipt_hashes(clean, evidence, 'preservedPaths', 'preservedHashes', lambda path: path in expected)
        assert set(kept) == expected, 'All actual saved world/mode preservation hashes required.'
    selected, guids = [], set()
    for index, target in enumerate(targets):
        asset, meta = clean / target, clean / (target + '.meta')
        assert asset.is_file() and meta.is_file(), 'Actual forest material and Unity metadata required: ' + target
        guid = _forest_guid(meta); assert guid not in guids, 'Forest output GUID collision.'; guids.add(guid)
        if scope == 'forest-material':
            assert prior.get(target + '.meta') == _forest_hash(meta), 'Preserve the frozen canonical forest GUID metadata.'
        source = raw[0 if target.endswith('leaf.mat') or target == FOREST_MATERIALS[0] else 1]
        shader = re.search(r'm_Shader: \{[^\n]*guid: ([0-9a-f]{32})', asset.read_text(encoding='utf-8-sig'))
        original = re.search(r'm_Shader: \{[^\n]*guid: ([0-9a-f]{32})', (clean / source).read_text(encoding='utf-8-sig'))
        expected_shader = '933532a4fcc9baf4fa0491de14d08ed7' if '/Observed original Lit ' in target else original.group(1) if original else None
        assert shader and shader.group(1) == expected_shader, 'Forest output must retain its actual prepared material family: ' + target
        selected += [asset, meta]
    if scope == 'forest-proof':
        for relative in (FOREST_PROOF + '.meta', 'Assets/TruckDragonKingdom/VisualCandidates.meta'):
            path = clean / relative
            assert path.is_file(), 'Actual forest proof folder GUID required.'
            guid = _forest_guid(path); assert guid not in guids, 'Forest folder/material GUID collision.'; guids.add(guid); selected.append(path)
    return selected

def regional_folders(clean, candidate):
    """Retain a measured Vista review and its exact private graph derivatives."""
    assert candidate and all(c.isalnum() or c in '-_' for c in candidate), 'Explicit regional candidate identifier required.'
    folder = 'ReviewCandidates/' + candidate
    private = 'PrivateLicensedEnvironment/DerivedVista/' + candidate
    base = clean / 'Assets/TruckDragonKingdom'
    receipt = json.loads((base / folder / 'Foundation Receipt.json').read_text(encoding='utf-8-sig'))
    assert receipt['status'] == 'MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW', 'Only measured candidates may be retained.'
    assert receipt['candidateFolder'] == 'Assets/TruckDragonKingdom/' + folder
    assert receipt['privateGraphsFolder'] == 'Assets/TruckDragonKingdom/' + private, 'Private graph scope must match the explicit candidate.'
    assert receipt['validationIssues'] == [] and receipt['sourceBytesPreserved'] and receipt['originalScenePreserved'] and receipt['temporaryObjectsRemoved']
    assert receipt['prefab'] == 'Assets/TruckDragonKingdom/' + folder + '/Vista Regional Foundation.prefab'
    assert (base / folder / 'Vista Regional Foundation.prefab').is_file() and (base / private).is_dir()
    return [folder, private]


def cavern_folders(clean, run, candidate):
    """Preserve the measured vendor massif, not an excavated/accepted cave."""
    assert candidate and len(candidate) <= 40 and all(c.isalnum() or c in '-_' for c in candidate), 'Explicit short cavern candidate identifier required.'
    folder = 'ReviewCandidates/' + candidate
    private = 'PrivateLicensedEnvironment/DerivedVista/' + candidate
    base = clean / 'Assets/TruckDragonKingdom'
    evidence = json.loads((run / 'rendered-evidence' / ('cavern-foundation-' + candidate + '.json')).read_text(encoding='utf-8-sig'))
    assert evidence.get('status') == 'MEASURED_MASSIF_READY_FOR_SEPARATE_DIGGER_EXCAVATION_REVIEW', 'Only a passed actual massif may be retained.'
    assert evidence.get('folder') == 'Assets/TruckDragonKingdom/' + folder, 'Cavern folder must match the measured candidate.'
    assert evidence.get('prefab') == 'Assets/TruckDragonKingdom/' + folder + '/Vista Cavern Massif.prefab'
    assert evidence.get('graph') == 'Assets/TruckDragonKingdom/' + private + '/Cavern elevated MountainRange.asset', 'Cavern private graph scope must match the actual candidate.'
    assert all(evidence.get(field) is True for field in ('sourceBytesPreserved', 'outsideBiomePreserved', 'temporaryObjectsRemoved')), 'Actual source/outside-mask/cleanup preservation required.'
    assert evidence.get('populationCallbacks') == 4 and len(evidence.get('terrains', [])) == 4 and len(evidence.get('roof', [])) >= 100, 'Actual four-tile generation and measured roof required.'
    assert evidence['minimumRoofHeight'] >= evidence['semanticRoadFloor'] + 50, 'Meaningful measured overhead mass required before Digger excavation.'
    assert all(tile['outsideBiomeCoordinatedMaximumDelta'] <= .001 for tile in evidence['terrains']), 'Preserve actual terrain outside the vendor mask.'
    assert (base / folder / 'Vista Cavern Massif.prefab').is_file() and (base / private / 'Cavern elevated MountainRange.asset').is_file()
    return [folder, private]


def rounded_folders(clean, candidate):
    """Retain passed native Vista output for integration, not final-world adoption."""
    assert candidate and len(candidate) <= 40 and all(c.isalnum() or c in '-_' for c in candidate), 'Explicit short rounded candidate identifier required.'
    folder = 'ReviewCandidates/' + candidate
    private = 'PrivateLicensedEnvironment/DerivedVista/' + candidate
    base = clean / 'Assets/TruckDragonKingdom'
    evidence = json.loads((base / folder / 'Foundation Receipt.json').read_text(encoding='utf-8-sig'))
    assert evidence.get('status') == 'MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW', 'Only a passed rounded foundation may be retained.'
    assert evidence.get('folder') == 'Assets/TruckDragonKingdom/' + folder
    assert evidence.get('prefab') == 'Assets/TruckDragonKingdom/' + folder + '/Vista Regional Foundation.prefab'
    assert evidence.get('sourceBytesPreserved') is True and evidence.get('temporaryObjectsRemoved') is True and evidence.get('validationIssues') == [], 'Passed source/cleanup/geometry gates required.'
    tiles = evidence.get('terrains', [])
    assert len(tiles) == 4 and evidence.get('heightCallbacks') == 4 and len(evidence.get('centers', [])) == 9, 'Actual four-tile/nine-region generation required.'
    assert all(tile['resolution'] == 513 and tile['ceilingSamples'] == 0 for tile in tiles), 'Finite unclipped vendor terrain required.'
    assert max(tile['maximum'] for tile in tiles) >= 450 and sum(tile['above200'] for tile in tiles) >= 90000, 'Broad measured highland mass required.'
    assert sum(tile['steeper65'] for tile in tiles) < sum(tile['sourceSteeper65'] for tile in tiles) * .7, 'Actual severe-slope improvement must pass before retention.'
    expected_tiles = {'Assets/TruckDragonKingdom/' + folder + '/Terrain/Kingdom ' + str(x) + ' ' + str(z) + '.asset' for x in range(2) for z in range(2)}
    assert {tile['path'] for tile in tiles} == expected_tiles, 'Only the four exact candidate tiles may be retained.'
    assert all((clean / path).is_file() and (clean / (path + '.meta')).is_file() for path in expected_tiles)
    assert (base / folder / 'Vista Regional Foundation.prefab').is_file() and (base / private).is_dir()
    return [folder, private]


OUTER_NATIVE_TEST = 'DragonOuterVistaNativeTests.TwelveRealVistaSceneryTilesPreserveInnerWorldAndMeetEveryMeasuredSeam'
ROUNDED_WORLD_NATIVE_TEST = 'DragonKingdomRoundedWorldIntegrationTests.MeasuredRoundedVistaAndRealDiggerPreserveAcceptedSourcesAndPassCompleteWorldGates'


def require_rounded_world_native_pass(run):
    """An authoring receipt cannot override an unhandled native test failure."""
    result = run / 'tests.xml'
    assert result.is_file(), 'Exact full native rounded-world test result is required.'
    cases = [case for case in ET.parse(result).iter('test-case') if case.get('fullname') == ROUNDED_WORLD_NATIVE_TEST]
    assert len(cases) == 1 and cases[0].get('result') == 'Passed', 'Exactly one full native rounded-world case must PASS; missing/skipped/failed/duplicate or unrelated passes cannot authorize retention.'
    return result


def _outer_path(path, allow_packages=False):
    assert isinstance(path, str) and (path.startswith('Assets/') or allow_packages and path.startswith('Packages/')) and '\\' not in path and '..' not in Path(path).parts and Path(path).as_posix() == path, 'Invalid outer asset path: ' + str(path)
    return path


def _outer_dependency_file(clean, path, digest):
    """Resolve a native Unity dependency read-only; packages never become outputs."""
    _outer_path(path, allow_packages=True)
    assert isinstance(digest, str) and re.fullmatch('[0-9a-f]{64}', digest), 'Actual outer dependency SHA256 required.'
    if path.startswith('Assets/'):
        actual = clean / path
        assert actual.resolve().is_relative_to((clean / 'Assets').resolve()), 'Outer asset dependency escapes Assets.'
        assert actual.is_file() and _forest_hash(actual) == digest, 'Outer dependency bytes differ: ' + path
        return actual
    parts = path.split('/')
    assert len(parts) >= 3 and re.fullmatch('[a-z0-9][a-z0-9._-]*', parts[1]), 'Actual Unity package identity required.'
    package, relative = parts[1], Path(*parts[2:])
    bases = [(clean / 'Packages' / package, clean / 'Packages')]
    cache = clean / 'Library/PackageCache'
    if cache.is_dir():
        bases += [(root, cache) for root in sorted(cache.glob(package + '@*')) if root.is_dir()]
    matches = []
    for root, allowed in bases:
        if not root.is_dir():
            continue
        resolved_root = root.resolve()
        assert resolved_root.is_relative_to(allowed.resolve()), 'Unity package root escapes its read-only package location.'
        manifest = root / 'package.json'
        if not manifest.is_file():
            continue
        assert manifest.resolve().is_relative_to(resolved_root), 'Unity package manifest escapes package root.'
        identity = json.loads(manifest.read_text(encoding='utf-8-sig'))
        if identity.get('name') != package:
            continue
        actual = root / relative
        assert actual.resolve().is_relative_to(resolved_root), 'Unity package dependency escapes its declared package root.'
        if actual.is_file() and _forest_hash(actual) == digest:
            matches.append(actual)
    assert matches, 'No actual Unity package identity/file matches the native SHA256: ' + path
    return matches[0]


def _outer_hashes(clean, rows, label):
    assert isinstance(rows, list) and rows, 'Actual outer ' + label + ' dependency hashes required.'
    result = {}
    for row in rows:
        assert isinstance(row, dict), 'Actual outer dependency record required.'
        path, digest = _outer_path(row.get('path'), allow_packages=True), row.get('sha256')
        assert path not in result, 'Duplicate outer dependency path: ' + path
        assert isinstance(digest, str) and re.fullmatch('[0-9a-f]{64}', digest), 'Actual outer dependency SHA256 required.'
        try:
            _outer_dependency_file(clean, path, digest)
        except AssertionError as error:
            raise AssertionError('Outer ' + label + ' bytes differ: ' + path + '; ' + str(error)) from error
        result[path] = digest
    return result


def outer_paths(clean, run, candidate):
    """Receipt-gated exact outer scenery assets and matching private Vista graphs."""
    assert candidate and len(candidate) <= 40 and all(c.isalnum() or c in '-_' for c in candidate), 'Explicit short outer candidate identifier required.'
    prefix = 'Assets/TruckDragonKingdom/'
    folder, private = prefix + 'ReviewCandidates/' + candidate, prefix + 'PrivateLicensedEnvironment/DerivedVista/' + candidate
    receipt_path = folder + '/Outer Vista Receipt.json'
    evidence = json.loads((clean / receipt_path).read_text(encoding='utf-8-sig'))
    recorded = json.loads((run / 'rendered-evidence' / ('outer-vista-' + candidate + '.json')).read_text(encoding='utf-8-sig'))
    assert evidence == recorded, 'Actual native/asset outer receipts must match exactly.'
    cases = [case for case in ET.parse(run / 'tests.xml').iter('test-case') if case.get('fullname') == OUTER_NATIVE_TEST]
    assert len(cases) == 1 and cases[0].get('result') == 'Passed', 'The exact actual outer native test must pass before retention.'
    assert evidence.get('status') == 'MEASURED_SCENERY_CANDIDATE_REQUIRES_RENDER_AND_WORLD_ATTACHMENT_REVIEW', 'Only a measured outer scenery candidate may be retained.'
    assert evidence.get('scope') == 'VISTA_GENERATED_SCENERY_ONLY_NO_NEW_PLAYABLE_AREA', 'Outer scenery is not playable-world adoption.'
    assert evidence.get('folder') == folder and evidence.get('privateGraphs') == private and evidence.get('prefab') == folder + '/Outer Vista Band.prefab', 'Outer output paths must match the explicit candidate.'
    assert all(evidence.get(field) is True for field in ('sourceBytesPreserved', 'temporaryObjectsRemoved', 'originalInnerNotGenerated')), 'Actual outer source/cleanup/no-inner-generation assertions required.'
    assert evidence.get('validationIssues') == [] and type(evidence.get('heightCallbacks')) is int and evidence['heightCallbacks'] == 12 and type(evidence.get('uniqueHeightTiles')) is int and evidence['uniqueHeightTiles'] == 12, 'Twelve distinct actual outer height callbacks and passed validation required.'
    seams = evidence.get('seams', {})
    assert seams.get('issues') == [], 'Actual outer seam validation must pass.'
    for field, minimum in (('innerSamples', 2000), ('outerSamples', 2000), ('bedSamples', 6000)):
        assert type(seams.get(field)) is int and seams[field] >= minimum, 'Actual outer seam/bed sample coverage required.'
    for field in ('innerMaximumError', 'outerMaximumError', 'bedMaximumError'):
        value = seams.get(field)
        assert type(value) in (int, float) and math.isfinite(value) and 0 <= value <= .04, 'Actual measured outer seams/bed must remain within0.04m.'
    preserved = _outer_hashes(clean, evidence.get('preservedAssets'), 'preserved source')
    preserved_paths = evidence.get('preservedPaths')
    assert isinstance(preserved_paths, list) and len(preserved_paths) == len(preserved) and set(preserved_paths) == set(preserved), 'Exact original outer source path/hash correspondence required.'
    source = evidence.get('source')
    assert isinstance(source, str) and re.fullmatch(re.escape(prefix) + r'VisualCandidates/[A-Za-z0-9_-]{1,40}/Shared Dragon Kingdom\.prefab', source), 'Exact actual inner source prefab required.'
    originals = [source, prefix + 'ReviewCandidates/RoundedVista01/Vista Regional Foundation.prefab', prefix + 'Prefabs/Shared Dragon Kingdom.prefab']
    source_terrain = source.rsplit('/', 1)[0] + '/Terrain/'
    inner_data = [path for path in preserved if path.startswith(source_terrain) and path.endswith('.asset')]
    assert len(inner_data) == 4, 'Four actual preserved inner TerrainData dependencies required.'
    assert all(path + suffix in preserved for path in originals + inner_data for suffix in ('', '.meta')), 'Original source/canonical/inner GUID dependencies required.'
    dependencies = _outer_hashes(clean, evidence.get('outputDependencies'), 'output')
    tiles = evidence.get('terrains', [])
    expected_tiles = {folder + '/Terrain/Outer ' + str(x) + ' ' + str(z) + '.asset': (x, z) for x in range(4) for z in range(4) if not (0 < x < 3 and 0 < z < 3)}
    assert isinstance(tiles, list) and len(tiles) == 12 and {tile.get('path') for tile in tiles} == set(expected_tiles), 'Only the twelve exact measured outer terrain cells may be retained.'
    axis_x, axis_z, sizes = [-2400, -1600, 0, 1600], [-1600, -800, 800, 2400], [800, 1600, 1600, 800]
    for tile in tiles:
        x, z = expected_tiles[tile['path']]
        assert tile.get('resolution') == 513 and tile.get('position') == dict(x=axis_x[x], y=-20, z=axis_z[z]) and tile.get('size') == dict(x=sizes[x], y=600, z=sizes[z]), 'Actual outer cell origins/size/513resolution required.'
        assert all(isinstance(tile.get(field), str) and re.fullmatch('[0-9a-f]{64}', tile[field]) for field in ('generatedHeightHash', 'coordinatedHeightHash')), 'Actual generated/coordinated height hashes required.'
        assert type(tile.get('unchangedInteriorSamples')) is int and tile['unchangedInteriorSamples'] > 0, 'Actual unchanged Vista interiors required.'
        assert all(type(tile.get(field)) in (int, float) and math.isfinite(tile[field]) for field in ('minimum', 'maximum', 'maximumCoordination')) and -20 <= tile['minimum'] <= tile['maximum'] <= 580 and tile['maximumCoordination'] >= 0, 'Finite actual outer height range required.'
    terrain_count, mask_count = evidence.get('clonedTerrainGraphs'), evidence.get('clonedMaskGraphs')
    assert type(terrain_count) is int and 1 < terrain_count <= 100 and type(mask_count) is int and 0 <= mask_count <= 100, 'Bounded actual cloned Vista graph counts required.'
    graphs = {private + '/Terrain graph ' + str(i) + '.asset' for i in range(terrain_count)}
    graphs.update(private + '/Mask graph ' + str(i) + '.asset' for i in range(mask_count))
    graphs.update(private + '/' + name for name in ('Vendor constant coastal bed.asset', 'Vendor smooth coastal falloff.asset'))
    assets = set(expected_tiles) | graphs | {evidence['prefab']}
    selected = {path + suffix for path in assets for suffix in ('', '.meta')}
    owned_dependencies = {path for path in dependencies if path.startswith(folder + '/') or path.startswith(private + '/')}
    assert owned_dependencies == selected, 'Unity output dependency closure must match the exact outer assets and graph scope.'
    assert all(path in preserved and preserved[path] == digest for path, digest in dependencies.items() if path not in selected), 'All external output references must retain their actual original source hashes.'
    selected.update((receipt_path, receipt_path + '.meta', folder + '.meta', folder + '/Terrain.meta', private + '.meta'))
    actual = {path.relative_to(clean).as_posix() for base in (clean / folder, clean / private) for path in base.rglob('*') if path.is_file()}
    actual.update(path for path in (folder + '.meta', private + '.meta') if (clean / path).is_file())
    assert actual == selected, 'Unexpected/missing outer output files; no generic candidate sweep is permitted.'
    guids = {}
    for path in selected:
        assert (clean / path).is_file() and Path(path).suffix not in ('.cs', '.asmdef'), 'Actual outer authored assets/metadata required.'
        if path.endswith('.meta'):
            guid = _forest_guid(clean / path)
            assert guid not in guids, 'Outer output Unity GUID collision.'
            guids[guid] = path[:-5]
    prefab_text = (clean / evidence['prefab']).read_text(encoding='utf-8-sig')
    assert len(re.findall(r'^--- !u!218 ', prefab_text, re.M)) == 12 and len(re.findall(r'^--- !u!154 ', prefab_text, re.M)) == 12 and not re.search(r'^--- !u!114 ', prefab_text, re.M), 'Actual outer prefab requires twelve Terrain/Collider pairs and no generation behaviours.'
    references = re.findall(r'm_TerrainData:\s*\{[^}]*guid:\s*([0-9a-f]{32})[^}]*\}', prefab_text)
    tile_guids = {_forest_guid(clean / (path + '.meta')) for path in expected_tiles}
    assert len(references) == 24 and set(references) == tile_guids and all(references.count(guid) == 2 for guid in tile_guids), 'Each exact outer TerrainData GUID must be shared by its Terrain and Collider only.'
    reference_guids = {_forest_guid(_outer_dependency_file(clean, path, digest)) for path, digest in dependencies.items() if path.endswith('.meta')}
    preserved_guids = {_forest_guid(_outer_dependency_file(clean, path, digest)) for path, digest in preserved.items() if path.endswith('.meta')}
    assert not (set(guids) & preserved_guids), 'Outer output GUID collides with an original source asset.'
    for path in assets:
        data = (clean / path).read_bytes()
        if not data.startswith(b'%YAML'):
            continue  # Binary terrain references are the actual Unity closure above.
        if path == evidence['prefab']:
            # Unity's exact default Terrain lightmap-parameters resource has no
            # project/package .meta. Do not allow this GUID in arbitrary fields.
            data = re.sub(rb'(?m)^\s*m_LightmapParameters: \{fileID: 15203, guid: 0000000000000000f000000000000000, type: 0\}\r?$', b'', data)
        refs = set(re.findall(rb'guid:\s*([0-9a-f]{32})', data)) - {b'0' * 32}
        assert all(guid.decode('ascii') in reference_guids for guid in refs), 'Unresolved actual outer YAML reference: ' + path
    return [clean / path for path in sorted(selected)]


def candidate_digger_paths(clean, candidate, integration):
    """Keep exactly this passed cave's generated dependency tree, never other scenes."""
    cavern = integration.get('cavern')
    if not cavern:
        return []
    assert candidate and all(c.isalnum() or c in '-_' for c in candidate)
    assert integration.get('status') == 'PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW'
    assert cavern.get('status') == 'MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW', 'Rejected Digger output remains diagnostic only.'
    assert all(cavern.get(key) is True for key in ('exactHeightmaps', 'outsideHolesExact', 'streamingAssetsUntouched', 'pipelineSettingsPreserved', 'installedUrpDetectorSatisfied')), 'Actual cave source/settings/height/holes preservation required.'
    assert cavern.get('systems') == 2 and cavern.get('generatedMeshColliders', 0) > 0 and len(cavern.get('roof', [])) == 187 and cavern.get('minimumClearance', 0) >= 42, 'Complete actual cave geometry proof required.'
    folder = 'Assets/DiggerData/Scenes/DK-' + candidate
    assert cavern.get('dataFolder') == folder, 'Digger dependencies must belong only to the explicit candidate.'
    tiles = cavern.get('tileDataFolders', [])
    assert len(tiles) == 2 and len(set(tiles)) == 2 and all(re.fullmatch(re.escape(folder) + '/[0-9a-f]{32}', path) for path in tiles), 'Exact two generated terrain GUID folders required.'
    expected_streaming = ['Assets/StreamingAssets/DiggerData/' + Path(path).name for path in tiles]
    assert cavern.get('streamingAssetsFolders') == expected_streaming and all(not (clean / path).exists() for path in expected_streaming), 'This authoring pass must not create or sweep streaming assets.'
    base = clean / folder
    assert base.is_dir() and {path.relative_to(clean).as_posix() for path in base.iterdir() if path.is_dir()} == set(tiles), 'Unexpected generated Digger scene dependency scope.'
    assert Path(str(base) + '.meta').is_file() and all((clean / path).is_dir() for path in tiles), 'Actual generated cave data and GUID metadata required.'
    return [Path(str(base) + '.meta')] + [path for path in base.rglob('*') if path.is_file()]


FINISH_ARTIST_LAYOUT = 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVillageLayout/Artist Market Front v3.json'
FINISH_ARTIST_LAYOUT_SHA = '905b8b5007af673d13fdde44519797fa0a5a82313fe030d935836a5b61ccb238'
FINISH_ARTIST_SCENE_SHA = 'd65f8a0eb8f553e68e00913fa59170471a2dabeace8cbdf0f0d456de972a49bd'
FINISH_ARTIST_NAME = 'Owned original artist market frontage'
FINISH_SHIP = 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Tarbo Studio/TARBO-FantasyVillage/Prefabs/Props/Tools/TB_Prop_Ship.prefab'
FINISH_PRIVATE = 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment/'


def _finish_finite(value, label):
    assert type(value) in (int, float) and math.isfinite(value), 'Finite actual Finish ' + label + ' required.'
    return value


def _finish_vector(value, axes='xyz', label='vector'):
    assert isinstance(value, dict) and set(value) == set(axes), 'Actual Finish ' + label + ' components required.'
    return tuple(_finish_finite(value[axis], label) for axis in axes)


def _finish_pose(row):
    p = _finish_vector(row.get('position'), label='position')
    q = _finish_vector(row.get('rotation'), 'xyzw', 'rotation')
    scale = _finish_vector(row.get('scale'), label='scale')
    assert all(v > 0 for v in scale) and abs(sum(v*v for v in q)-1) < .001, 'Positive scale and real unit Finish rotation required.'
    return p, q, scale


def _finish_bounds(row, label):
    assert isinstance(row, dict) and set(row) == {'m_Center', 'm_Extent'}, 'Actual serialized Finish ' + label + ' bounds required.'
    center = _finish_vector(row['m_Center'], label=label + ' bounds center')
    extent = _finish_vector(row['m_Extent'], label=label + ' bounds extent')
    assert all(v >= 0 for v in extent), 'Nonnegative actual Finish bounds required.'
    return center, extent


def _finish_near(a, b, tolerance, label):
    assert len(a) == len(b) and math.dist(a, b) <= tolerance, 'Actual saved Finish ' + label + ' drifted.'


def _finish_rotation(a, b, tolerance, label):
    assert abs(sum(v*v for v in a)-1) < .001 and abs(sum(v*v for v in b)-1) < .001, 'Actual Finish unit quaternion required.'
    dot = abs(sum(x*y for x, y in zip(a, b))) / math.sqrt(sum(x*x for x in a)*sum(x*x for x in b))
    assert math.degrees(2*math.acos(min(1, dot))) <= tolerance, 'Actual saved Finish ' + label + ' drifted.'


def _finish_source(clean, prior, path, digest, meta_digest, guid):
    path = _outer_path(path)
    assert path.startswith(FINISH_PRIVATE) and re.fullmatch('[0-9a-f]{32}', guid or ''), 'Exact private Finish source/GUID scope required.'
    for name, expected in ((path, digest), (path+'.meta', meta_digest)):
        assert isinstance(expected, str) and re.fullmatch('[0-9a-f]{64}', expected) and prior.get(name) == expected and (clean/name).is_file() and _forest_hash(clean/name) == expected, 'Finish source/metadata must match actual frozen intake: ' + name
    assert _forest_guid(clean/(path+'.meta')) == guid, 'Actual Finish source GUID changed.'
    return path


def _finish_yaml(text):
    # Index headers, not144MB of copied document strings. Only the exact market,
    # original ferry and source-prefab root records are inspected below.
    headers = list(re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$', text, re.M))
    ids = [m.group(2) for m in headers]
    assert len(set(ids)) == len(ids), 'Duplicate actual saved prefab fileID.'
    return text, {m.group(2):(int(m.group(1)),m.start(),headers[i+1].start() if i+1<len(headers) else len(text)) for i,m in enumerate(headers)}


def _finish_doc(index, file_id):
    assert str(file_id) in index[1], 'Missing actual saved Finish object: ' + str(file_id)
    kind, start, end = index[1][str(file_id)]
    return kind, index[0][start:end]


def _finish_field(document, field):
    matches = re.findall(r'^ {2,4}'+re.escape(field)+r': (.+?)\r?$', document, re.M)
    assert len(matches) == 1, 'Exact actual saved Finish field required: ' + field
    return matches[0]


def _finish_ref(document, field):
    value = _finish_field(document, field)
    match = re.fullmatch(r'\{fileID: (-?\d+)\}', value)
    assert match, 'Exact local saved Finish reference required: ' + field
    return match.group(1)


def _finish_inline(value, axes):
    assert value.startswith('{') and value.endswith('}'), 'Actual Unity Finish vector required.'
    parts = dict(pair.strip().split(': ', 1) for pair in value[1:-1].split(','))
    return _finish_vector({key:float(v) for key,v in parts.items()}, axes)


def _finish_named_transform(index, name):
    objects = [key for key,value in index[1].items() if value[0] == 1 and re.search(r'^  m_Name: '+re.escape(name)+r'\r?$', _finish_doc(index,key)[1], re.M)]
    assert len(objects) == 1, 'Exactly one actual Finish root required: ' + name
    transforms = [key for key,value in index[1].items() if value[0] == 4 and re.search(r'^  m_GameObject: \{fileID: '+objects[0]+r'\}\r?$', _finish_doc(index,key)[1], re.M)]
    assert len(transforms) == 1, 'Actual unique Finish Transform required: ' + name
    return transforms[0]


def _finish_local_transform(index, file_id):
    kind, document = _finish_doc(index,file_id)
    assert kind == 4 and ' stripped' not in document.splitlines()[0], 'Actual original authored Finish Transform required.'
    return (_finish_inline(_finish_field(document,'m_LocalPosition'),'xyz'),_finish_inline(_finish_field(document,'m_LocalRotation'),'xyzw'),_finish_inline(_finish_field(document,'m_LocalScale'),'xyz'))


def _finish_multiply(a,b):
    x,y,z,w=a;u,v,t,s=b
    return (w*u+x*s+y*t-z*v,w*v-x*t+y*s+z*u,w*t+x*v-y*u+z*s,w*s-x*u-y*v-z*t)


def _finish_world_transform(index, file_id, seen=None):
    seen = set() if seen is None else seen
    assert file_id not in seen, 'Cyclic actual Finish hierarchy.'
    seen.add(file_id);p,q,scale=_finish_local_transform(index,file_id);parent=_finish_ref(_finish_doc(index,file_id)[1],'m_Father')
    if parent == '0':return p,q,scale
    pp,pq,ps=_finish_world_transform(index,parent,seen);scaled=tuple(x*y for x,y in zip(p,ps));rotated=_finish_multiply(_finish_multiply(pq,(*scaled,0)),(-pq[0],-pq[1],-pq[2],pq[3]))[:3]
    return tuple(x+y for x,y in zip(pp,rotated)),_finish_multiply(pq,q),tuple(x*y for x,y in zip(ps,scale))


def _finish_instance(index, transform_id, source_index, guid):
    kind, doc = _finish_doc(index,transform_id)
    assert kind == 4 and ' stripped' in doc.splitlines()[0], 'Actual source-backed Finish child Transform required.'
    source_ref=re.search(r'm_CorrespondingSourceObject: \{fileID: (-?\d+), guid: ([0-9a-f]{32}),',doc)
    assert source_ref and source_ref.group(2) == guid, 'Actual Finish child source changed.'
    instance_id=_finish_ref(doc,'m_PrefabInstance');kind,instance=_finish_doc(index,instance_id)
    assert kind == 1001 and re.search(r'm_SourcePrefab: \{fileID: 100100000, guid: '+guid+r', type: 3\}',instance), 'Actual Finish instance source/GUID changed.'
    assert re.search(r'^    m_AddedGameObjects: \[\]\r?$',instance,re.M), 'Unknown Finish child roots cannot be added to the source prefab.'
    source_root=source_ref.group(1);pose=[list(v) for v in _finish_local_transform(source_index,source_root)];changes={}
    for block in re.split(r'(?m)^    - target: ',instance)[1:]:
        target=re.match(r'\{fileID: (-?\d+), guid: ([0-9a-f]{32}),\s*type: 3\}',block)
        prop=re.search(r'^      propertyPath: ([^\r\n]+)',block,re.M);value=re.search(r'^      value: ([^\r\n]*)',block,re.M)
        if target and prop and value:
            key=(target.group(2),target.group(1),prop.group(1));assert key not in changes,'Duplicate actual Finish property override.';changes[key]=value.group(1)
    for field,axes,values in zip(('m_LocalPosition','m_LocalRotation','m_LocalScale'),('xyz','xyzw','xyz'),pose):
        for i,axis in enumerate(axes):
            value=changes.get((guid,source_root,field+'.'+axis))
            if value is not None:values[i]=_finish_finite(float(value),'saved source pose')
    return tuple(tuple(v) for v in pose),instance_id,_finish_ref(instance,'m_TransformParent'),changes,source_root


FINISH_IMPORT_PROVENANCE = FINISH_PRIVATE + 'DerivedVillageLayout/Material Import Provenance v1.json'
FINISH_IMPORT_PROVENANCE_SHA = '4c0f984d7487a2c7728c1c05559632c3bd90eaf3467ef3a2d1a85ce92a637415'


def _finish_property_text(text, row):
    kind,name=row.get('kind'),row.get('name')
    assert kind in ('Shader','Texture','Scalar','Color') and isinstance(name,str) and name, 'Exact approved native import property kind/name required.'
    pattern=(r'^    - '+re.escape(name)+r':\r?\n((?:        .*\r?\n)+)' if kind=='Texture' else r'^'+re.escape(('  ' if kind=='Shader' else '    - ')+name)+r': ([^\r\n]+)')
    matches=re.findall(pattern,text,re.M)
    assert len(matches)==1, 'Exact protected native material property missing/duplicated: '+name
    return matches[0].replace('\r\n','\n').strip()


def _finish_material_binding_signature(row):
    names=('path','guid','originalSha256','importedSha256','metaSha256','snapshot','snapshotSha256','snapshotMetaSha256','snapshotGuid','originalRenderQueue','importedRenderQueue')
    values=[row.get(key) for key in names]
    for key in ('protectedProperties','convertedProperties'):
        items=row.get(key);assert isinstance(items,list),'Exact native imported property records required.'
        values.append(tuple(tuple(item.get(field) or '' for field in ('kind','name','sourceValue','importedValue','reason')) for item in items))
    return values


def _finish_material_imports(clean, prior, layout, market):
    evidence=market.get('materialImports')
    assert isinstance(evidence,dict) and evidence.get('passed') is True, 'Actual approved native material import evidence required.'
    assert layout.get('materialImportProvenance')==FINISH_IMPORT_PROVENANCE and layout.get('materialImportProvenanceSha256')==FINISH_IMPORT_PROVENANCE_SHA and evidence.get('provenance')==FINISH_IMPORT_PROVENANCE and evidence.get('provenanceSha256')==FINISH_IMPORT_PROVENANCE_SHA, 'Exact native material provenance binding required; no blanket material waiver.'
    _finish_source(clean,prior,FINISH_IMPORT_PROVENANCE,FINISH_IMPORT_PROVENANCE_SHA,evidence.get('provenanceMetaSha256'),evidence.get('provenanceGuid'))
    proof=json.loads((clean/FINISH_IMPORT_PROVENANCE).read_text(encoding='utf-8-sig'))
    assert proof.get('schemaVersion')==1 and proof.get('scope')=='EXACT_V201_PUBLIC_URP_NATIVE_IMPORT_PROVENANCE_NOT_GENERAL_MATERIAL_WAIVER' and proof.get('sourceNativeRun')=='V201' and proof.get('nativeUpgraderFromVersion')==1 and proof.get('nativeUpgraderToVersion')==10, 'Exact measured native URP v1-to-v10 conversion provenance required.'
    assert proof.get('editorVersion')==evidence.get('editorVersion')=='6000.4.10f1' and proof.get('urpVersion')==evidence.get('urpVersion')=='17.4.0' and proof.get('upgraderPackage')=='com.unity.render-pipelines.universal' and proof.get('upgraderRelativePath')=='Editor/AssetPostProcessors/MaterialPostprocessor.cs' and proof.get('upgraderSourceSha256')==evidence.get('upgraderSourceSha256'), 'Actual approved native Editor/URP/upgrader identity differs.'
    assert re.search(r'^m_EditorVersion: 6000\.4\.10f1\r?$',(clean/'ProjectSettings/ProjectVersion.txt').read_text(),re.M), 'Actual native project Editor version differs.'
    upgrader=_outer_dependency_file(clean,'Packages/'+proof['upgraderPackage']+'/'+proof['upgraderRelativePath'],proof['upgraderSourceSha256'])
    package=json.loads((upgrader.parents[2]/'package.json').read_text(encoding='utf-8-sig'));assert package.get('name')==proof['upgraderPackage'] and package.get('version')==proof['urpVersion'], 'Actual native URP package version differs.'
    expected=dict(rawMaterialSnapshots=19,approvedNativeImports=19,exactNonmaterialDependencies=84,exactMetadataAndGuids=103,protectedProperties=769,convertedProperties=2)
    assert all(type(evidence.get(k)) is int and evidence[k]==v for k,v in expected.items()), 'Exact19snapshots/19imports/84unchanged/103metadata/769protected/2converted properties required.'
    bindings=proof.get('bindings');actual=evidence.get('bindings');assert isinstance(bindings,list) and len(bindings)==19 and len({r.get('path') for r in bindings})==19 and isinstance(actual,list) and len(actual)==19, 'Exact nineteen distinct approved native import bindings required.'
    assert [_finish_material_binding_signature(r) for r in actual]==[_finish_material_binding_signature(r) for r in bindings], 'Actual native import binding order/recipe differs from immutable approved proof.'
    assert sum(len(b.get('protectedProperties',[])) for b in bindings)==769 and sum(len(b.get('convertedProperties',[])) for b in bindings)==2, 'Actual protected/converted property counts differ.'
    result={}
    for row in bindings:
        path=_outer_path(row.get('path'));guid=row.get('guid');snapshot=FINISH_PRIVATE+'DerivedVillageLayout/OriginalMaterialSources/'+str(guid)+'.bytes'
        assert path.startswith(FINISH_PRIVATE) and path.endswith('.mat') and row.get('snapshot')==snapshot and row.get('snapshotSha256')==row.get('originalSha256'), 'Only exact approved material/snapshot pairs are allowed.'
        _finish_source(clean,prior,snapshot,row['originalSha256'],row.get('snapshotMetaSha256'),row.get('snapshotGuid'))
        assert prior.get(path)==row.get('originalSha256') and prior.get(path+'.meta')==row.get('metaSha256') and _forest_hash(clean/(path+'.meta'))==row['metaSha256'] and _forest_guid(clean/(path+'.meta'))==guid and _forest_hash(clean/path)==row.get('importedSha256'), 'Actual material bytes must be the exact approved native output over the frozen raw source and unchanged GUID/meta.'
        original=(clean/snapshot).read_text(encoding='utf-8-sig');current=(clean/path).read_text(encoding='utf-8-sig')
        for item in row['protectedProperties']:
            assert isinstance(item.get('sourceValue'),str) and _finish_property_text(original,item)==item['sourceValue'] and _finish_property_text(current,item)==item['sourceValue'], 'Protected native source shader/color/texture/scalar changed.'
        for item in row['convertedProperties']:
            assert Path(path).name=='M_CLR_yellow_E.mat' and item.get('kind')=='Color' and item.get('name') in ('_Color','_EmissionColor') and _finish_property_text(original,item)==item.get('sourceValue') and _finish_property_text(current,item)==item.get('importedValue'), 'Unreviewed native material conversion.'
            source=_finish_inline(item['sourceValue'],'rgba');imported=_finish_inline(item['importedValue'],'rgba')
            if item['name']=='_Color':expected_color=_finish_inline(_finish_property_text(original,dict(kind='Color',name='_BaseColor')),'rgba')
            else:
                def linear(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 if v<1 else v**2.2
                expected_color=tuple(linear(v) for v in source[:3])+(source[3],)
            assert all(abs(a-b)<=.00002 for a,b in zip(expected_color,imported)), 'Actual approved native legacy/HDR color conversion differs.'
        for text,key in ((original,'originalRenderQueue'),(current,'importedRenderQueue')):assert type(row.get(key)) is int and int(_finish_field(text,'m_CustomRenderQueue'))==row[key], 'Exact approved native render queue migration differs.'
        result[path]=row
    return result


FINISH_ASSEMBLY_PROVENANCE = FINISH_PRIVATE + 'DerivedFinishAssemblies/Material Import Provenance v1.json'
FINISH_ASSEMBLY_PROVENANCE_SHA = 'db8dce4f62cbfa89ce6346d3e59bbc62be63f3be36eac7ba7395891dddb75d82'
FINISH_ASSEMBLY_ARCHIVE_SHA = '067fbccad3f7d3af56944298a07fb630e641d0af109185655e3d5b5f1006d894'
FINISH_ASSEMBLY_LAYOUTS = (
    (FINISH_PRIVATE+'DerivedFinishAssemblies/Village Garden Court v1.json','df4f949e293852f8f14885f0d59df166a367baa06dcaf229558c0102d3ced7fe',89,44),
    (FINISH_PRIVATE+'DerivedFinishAssemblies/Autumn Roadside Assembly v1.json','7916ccb9daf0f9c304aa7f76df631894338f8cd40096c51ed066df37e63272c3',34,14))


def _finish_assembly_property_text(text, row):
    kind=row.get('kind');assert kind in ('Shader','Texture','Int','Float','Color'), 'Exact51 property kind required.'
    return _finish_property_text(text,dict(kind='Scalar' if kind in ('Int','Float') else kind,name=row.get('name')))


def _finish_assembly_properties(text):
    rows={('Shader','m_Shader'):_finish_assembly_property_text(text,dict(kind='Shader',name='m_Shader'))}
    for section,kind in (('m_TexEnvs','Texture'),('m_Ints','Int'),('m_Floats','Float'),('m_Colors','Color')):
        match=re.search(r'^    '+section+r':(?:\r?\n| \[\]\r?\n)(.*?)(?=^    m_|^  m_|\Z)',text,re.M|re.S)
        if match:
            for name in re.findall(r'^    - ([^:\r\n]+):',match.group(1),re.M):
                key=(kind,name);assert key not in rows, 'Duplicate exact51 authored property.'
                rows[key]=_finish_assembly_property_text(text,dict(kind=kind,name=name))
    return rows


def _finish_assembly_metadata(text):
    materials=[part for part in re.split(r'^--- !u!',text,flags=re.M) if part.startswith('21 ')]
    assert len(materials)==1, 'Exactly one original/native Material document required.'
    material=materials[0];headers=list(re.finditer(r'^  ([A-Za-z_][A-Za-z_0-9]*):',material,re.M));result={}
    for i,header in enumerate(headers):
        name=header.group(1);assert name not in result, 'Duplicate exact51 Material metadata field.'
        result[name]=material[header.end():headers[i+1].start() if i+1<len(headers) else len(material)].strip().replace('\r\n','\n')
    result.pop('m_SavedProperties',None)  # Exhaustively accounted separately above.
    versions=re.findall(r'^  version: (\d+)\r?$',text,re.M);assert len(versions)<=1, 'Duplicate native URP version record.'
    result['URP.AssetVersion.version']=versions[0] if versions else '<absent>'
    return result


def _finish_assembly_binding_signature(row):
    # Preserve the old19 signature and its semantics unchanged.
    result=_finish_material_binding_signature(row)
    for key,fields in (('addedProperties',('kind','name','sourceValue','importedValue','reason')),('metadataChanges',('name','sourceValue','importedValue','reason'))):
        assert isinstance(row.get(key),list), 'Exact51 added/default/metadata records required.'
        result.append(tuple(tuple(item.get(field) or '' for field in fields) for item in row[key]))
    return result


def _finish_assembly_material_imports(clean, prior, evidence):
    """Exact approved51 material stage only; never authorizes123 roots or a world."""
    assert isinstance(evidence,dict) and evidence.get('passed') is True, 'Actual exact51 native material evidence required.'
    assert evidence.get('provenance')==FINISH_ASSEMBLY_PROVENANCE and evidence.get('provenanceSha256')==FINISH_ASSEMBLY_PROVENANCE_SHA, 'Exact51 immutable provenance binding required.'
    _finish_source(clean,prior,FINISH_ASSEMBLY_PROVENANCE,FINISH_ASSEMBLY_PROVENANCE_SHA,evidence.get('provenanceMetaSha256'),evidence.get('provenanceGuid'))
    proof=json.loads((clean/FINISH_ASSEMBLY_PROVENANCE).read_text(encoding='utf-8-sig'))
    assert proof.get('schemaVersion')==1 and proof.get('scope')=='EXACT_V228_51_MATERIAL_NATIVE_STAGE_PROVENANCE_NOT_GENERIC_WAIVER_OR_HIERARCHY_VISUAL_ACCEPTANCE' and proof.get('materialStageOnly') is True and proof.get('sourceNativeCaseStatus')=='REJECTED_ASSEMBLY_PREFLIGHT', 'Original failed hierarchy case must remain explicit; material proof is not world acceptance.'
    assert proof.get('sourceNativeRun')==evidence.get('sourceNativeRun')=='V228' and proof.get('sourceNativeArchiveSha256')==evidence.get('sourceNativeArchiveSha256')==FINISH_ASSEMBLY_ARCHIVE_SHA, 'Exact51 archived native bytes required.'
    assert proof.get('editorVersion')=='6000.4.10f1' and proof.get('urpVersion')=='17.4.0' and proof.get('upgraderPackage')=='com.unity.render-pipelines.universal' and proof.get('upgraderRelativePath')=='Editor/AssetPostProcessors/MaterialPostprocessor.cs' and proof.get('nativeUpgraderFromVersion')==1 and proof.get('nativeUpgraderToVersion')==10, 'Exact51 actual public URP import identity required.'
    assert re.search(r'^m_EditorVersion: 6000\.4\.10f1\r?$',(clean/'ProjectSettings/ProjectVersion.txt').read_text(),re.M), 'Actual native project Editor version differs.'
    upgrader=_outer_dependency_file(clean,'Packages/'+proof['upgraderPackage']+'/'+proof['upgraderRelativePath'],proof.get('upgraderSourceSha256'))
    package=json.loads((upgrader.parents[2]/'package.json').read_text(encoding='utf-8-sig'));assert package.get('name')==proof['upgraderPackage'] and package.get('version')==proof['urpVersion'], 'Exact51 actual package differs.'
    shaders=proof.get('publicShaderSources');assert isinstance(shaders,list) and len(shaders)==2 and {s.get('relativePath') for s in shaders}=={'Shaders/Lit.shader','Shaders/Particles/ParticlesUnlit.shader'}, 'Both actual public URP main-color shaders required.'
    shader_guids=set()
    for shader in shaders:
        assert shader.get('package')==proof['upgraderPackage'] and shader.get('mainColor')=='_BaseColor', 'Exact public shader main-color authority required.'
        path=_outer_dependency_file(clean,'Packages/'+shader['package']+'/'+shader['relativePath'],shader.get('sha256'))
        assert _forest_guid(Path(str(path)+'.meta'))==shader.get('guid') and re.search(r'\[MainColor\]\s+_BaseColor',path.read_text()), 'Actual native shader/GUID/BaseColor authority differs.'
        shader_guids.add(shader['guid'])
    assert len(shader_guids)==2, 'Distinct native Lit/Particles shader identities required.'
    counts=dict(rawMaterialSnapshots=51,approvedNativeImports=51,protectedProperties=2544,convertedProperties=6,addedProperties=347,metadataChanges=358,exactNonmaterialDependencies=267,exactMetadataAndGuids=318)
    assert all(type(evidence.get(k)) is int and evidence[k]==v for k,v in counts.items()), 'Exact51/2544/6/347/358/267/318 native accounting required.'
    bindings=proof.get('bindings');actual=evidence.get('bindings')
    assert isinstance(bindings,list) and len(bindings)==51 and len({b.get('path') for b in bindings})==len({b.get('guid') for b in bindings})==51 and isinstance(actual,list) and len(actual)==51, 'Exactly51 distinct original/native bindings required.'
    assert [_finish_assembly_binding_signature(b) for b in actual]==[_finish_assembly_binding_signature(b) for b in bindings], 'Exact51 immutable native binding order/recipes differ.'
    assert all(sum(len(b.get(field,[])) for b in bindings)==count for field,count in (('protectedProperties',2544),('convertedProperties',6),('addedProperties',347),('metadataChanges',358))) and sum(b['originalSha256']!=b['importedSha256'] for b in bindings)==30, 'Exact51 measured property/byte-change accounting differs.'
    result={}
    metadata_allowed={'serializedVersion','m_ShaderKeywords','m_Parent','m_ModifiedSerializedProperties','m_ValidKeywords','m_InvalidKeywords','m_CustomRenderQueue','disabledShaderPasses','m_LockedProperties','m_BuildTextureStacks','m_AllowLocking','URP.AssetVersion.version'}
    conversion_base=FINISH_PRIVATE+'Plugins/Tidal Flask Studios/Fantastic Village Pack/materials/'
    alias_paths={conversion_base+name for name in ('M_CLR_blue.mat','M_CLR_yellow_E.mat','M_FX_steam.mat','M_PROP_paper.mat')};emission_paths={conversion_base+name for name in ('M_CLR_blue.mat','M_CLR_yellow_E.mat')}
    for binding in bindings:
        path=_outer_path(binding.get('path'));guid=binding.get('guid');snapshot=FINISH_PRIVATE+'DerivedFinishAssemblies/OriginalMaterialSources/'+str(guid)+'.bytes'
        assert path.startswith(FINISH_PRIVATE) and path.endswith('.mat') and binding.get('snapshot')==snapshot and binding.get('snapshotSha256')==binding.get('originalSha256'), 'Exact51 private material/snapshot pair required.'
        _finish_source(clean,prior,snapshot,binding['originalSha256'],binding.get('snapshotMetaSha256'),binding.get('snapshotGuid'))
        assert prior.get(path)==binding['originalSha256'] and prior.get(path+'.meta')==binding.get('metaSha256') and _forest_hash(clean/(path+'.meta'))==binding['metaSha256'] and _forest_guid(clean/(path+'.meta'))==guid and _forest_hash(clean/path)==binding.get('importedSha256'), 'Exact51 original/native/material/meta/GUID bytes differ.'
        original=(clean/snapshot).read_text(encoding='utf-8-sig');current=(clean/path).read_text(encoding='utf-8-sig');before=_finish_assembly_properties(original);after=_finish_assembly_properties(current)
        preserved=binding['protectedProperties'];converted=binding['convertedProperties'];added=binding['addedProperties'];original_keys=[(p.get('kind'),p.get('name')) for p in preserved+converted];added_keys=[(p.get('kind'),p.get('name')) for p in added]
        assert len(set(original_keys))==len(original_keys) and set(original_keys)==set(before) and len(set(added_keys))==len(added_keys) and set(added_keys)==set(after)-set(before) and set(before)<=set(after), 'Every actual authored/new native property must be accounted exactly once.'
        for item in preserved:assert before[(item['kind'],item['name'])]==item.get('sourceValue')==after[(item['kind'],item['name'])], 'Exact51 protected authored property changed.'
        for item in converted:
            name=item.get('name');alias=name=='_Color' and path in alias_paths;emission=name=='_EmissionColor' and path in emission_paths
            assert item.get('kind')=='Color' and (alias or emission) and before[('Color',name)]==item.get('sourceValue') and after[('Color',name)]==item.get('importedValue'), 'Unreviewed51-material color conversion.'
            expected_reason='PUBLIC_URP_MAINCOLOR_BASECOLOR_TO_LEGACY_COLOR_ALIAS_NO_AUTHORED_BASECOLOR_CHANGE' if alias else 'PUBLIC_URP_NATIVE_V1_TO_V10_HDR_EMISSION_COLOR_LINEAR_COMPATIBILITY'
            assert item.get('reason')==expected_reason and any('guid: '+g in after[('Shader','m_Shader')] for g in shader_guids), 'Exact public main-color/native HDR authority required.'
            source=_finish_inline(item['sourceValue'],'rgba');imported=_finish_inline(item['importedValue'],'rgba')
            if alias:expected=_finish_inline(before[('Color','_BaseColor')],'rgba');assert before[('Color','_BaseColor')]==after[('Color','_BaseColor')], 'Authored BaseColor must stay exact.'
            else:
                def linear(value):return value/12.92 if value<=.04045 else ((value+.055)/1.055)**2.4 if value<1 else value**2.2
                expected=tuple(linear(v) for v in source[:3])+(source[3],)
            assert all(abs(a-b)<=.00002 for a,b in zip(expected,imported)), 'Exact51 public alias/Color.linear conversion differs.'
        for item in added:assert item.get('sourceValue')=='<absent>' and after[(item['kind'],item['name'])]==item.get('importedValue') and item.get('reason')=='EXACT_NATIVE_PUBLIC_URP_DEFAULT_FIELD_ADDITION', 'Unreviewed native default property addition.'
        old_meta=_finish_assembly_metadata(original);new_meta=_finish_assembly_metadata(current);changes=binding['metadataChanges'];changed={k for k in old_meta.keys()|new_meta.keys() if old_meta.get(k,'<absent>')!=new_meta.get(k,'<absent>')}
        assert len({c.get('name') for c in changes})==len(changes) and {c.get('name') for c in changes}==changed, 'Every exact native metadata migration must be accounted once.'
        for item in changes:assert item.get('name') in metadata_allowed and old_meta.get(item['name'],'<absent>')==item.get('sourceValue') and new_meta.get(item['name'],'<absent>')==item.get('importedValue') and item.get('reason')=='EXACT_PUBLIC_URP_NATIVE_SERIALIZATION_QUEUE_KEYWORD_PASS_VERSION_MIGRATION', 'Unreviewed native serialization/queue/keyword/pass migration.'
        assert old_meta.get('m_CustomRenderQueue')==str(binding.get('originalRenderQueue')) and new_meta.get('m_CustomRenderQueue')==str(binding.get('importedRenderQueue')), 'Exact51 native queue migration differs.'
        result[path]=binding
    layouts=proof.get('sourceLayouts');assert isinstance(layouts,list) and len(layouts)==2, 'Exactly two immutable original source layouts required.'
    for entry,(path,digest,instances,prefabs) in zip(layouts,FINISH_ASSEMBLY_LAYOUTS):
        assert entry.get('path')==path and entry.get('sha256')==digest and entry.get('instances')==instances, 'Exact51 source layout identity/order changed.'
        _finish_source(clean,prior,path,digest,entry.get('metaSha256'),entry.get('guid'));layout=json.loads((clean/path).read_text(encoding='utf-8-sig'))
        assert layout.get('schemaVersion')==1 and layout.get('sourceSceneSha256')==entry.get('sourceSceneSha256') and len(layout.get('instances',[]))==instances and len({r.get('prefab') for r in layout['instances']})==prefabs and len({r.get('sceneInstanceId') for r in layout['instances']})==instances, 'Exact original89/34 instances and44/14 prefab identities required.'
        deps=layout.get('dependencies');assert isinstance(deps,list) and len(deps)==318 and len({d.get('path') for d in deps})==318 and len({d.get('guid') for d in deps})==318, 'Exact318 original dependency closure required.'
        assert {d['path'] for d in deps if d['path'].endswith('.mat')}==set(result), 'Exact51 source material closure differs.'
        for dep in deps:
            if dep['path'] in result:assert dep.get('sha256')==result[dep['path']]['originalSha256'] and dep.get('guid')==result[dep['path']]['guid'] and dep.get('metaSha256')==result[dep['path']]['metaSha256'], 'Exact original material dependency identity differs.'
            else:_finish_source(clean,prior,dep.get('path'),dep.get('sha256'),dep.get('metaSha256'),dep.get('guid'))
        originals=layout.get('originalMaterials');assert isinstance(originals,list) and len(originals)==51 and len({r.get('source') for r in originals})==51, 'Exactly51 immutable original snapshot bindings required.'
        for row in originals:
            binding=result.get(row.get('source'));assert binding and all(row.get(k)==binding.get(v) for k,v in (('path','snapshot'),('sourceGuid','guid'),('sha256','originalSha256'),('metaSha256','snapshotMetaSha256'),('guid','snapshotGuid'))), 'Exact51 layout raw snapshot/GUID metadata differs.'
    return result


def _finish_inactive_light_overrides(row, child):
    overrides=row.get('overrides');proofs=child.get('overrideEvidence')
    assert isinstance(overrides,list) and isinstance(proofs,list) and len(overrides)==len(proofs) and type(child.get('appliedSourceOverrides')) is int and child['appliedSourceOverrides']==0 and type(child.get('inactiveSourceOverrides')) is int and child['inactiveSourceOverrides']==len(overrides), 'Exact unapplied inherited Light evidence required.'
    for source,proof in zip(overrides,proofs):
        assert source.get('state')=='INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE' and source.get('applied') is False and source.get('targetGuid')==row['prefabGuid'] and source.get('property') in ('m_BoundingSphereOverride.x','m_BoundingSphereOverride.y','m_BoundingSphereOverride.z','m_BoundingSphereOverride.w'), 'Only exact actually disabled original Light bounds are accepted; absent-component guesses are obsolete.'
        assert _finish_finite(source.get('value'),'non-applied serialized placeholder')==0 and source.get('sourceComponentType')=='UnityEngine.Light' and source.get('sourceComponentGuid')=='3dc4d6bb2162865469ab19bc08d79628' and source.get('sourceComponentId')=='937063833075107752', 'Exact real inherited Light source identity and safe non-applied placeholder required.'
        assert isinstance(source.get('sourceValue'),str) and source['sourceValue'] and re.fullmatch('[0-9a-f]{8}',source.get('sourceValueFloatBits') or ''), 'Original raw override text/float bits required.'
        for key in ('targetId','nestedSourceInstanceId','sourceComponentId'):
            assert isinstance(source.get(key),str) and re.fullmatch('[0-9]+',source[key]) and 0<int(source[key])<2**64, 'Actual inherited Light persistent source IDs required.'
        assert int(source['nestedSourceInstanceId'])^int(source['sourceComponentId'])==int(source['targetId']), 'Exact nested/source Light identity derivation changed.'
        assert all(proof.get(key)==source.get(key) for key in ('targetId','targetGuid','property','sourceValue','sourceValueFloatBits','state')) and proof.get('authority')=='ACTUAL_IMPORTED_INHERITED_LIGHT_GUID_LOCAL_ID_AND_PUBLIC_DISABLED_OVERRIDE_CHECK' and proof.get('applied') is False and proof.get('publicOverrideDisabled') is True and proof.get('useBoundingSphereOverride') is False and proof.get('authoredOriginalOverrideDisabled') is True and type(proof.get('targetMatches')) is int and proof['targetMatches']==1 and type(proof.get('examinedPersistentComponents')) is int and proof['examinedPersistentComponents']>0 and proof.get('actualLightType')=='UnityEngine.Light' and proof.get('actualOriginalSourceGuid')==source['sourceComponentGuid'] and proof.get('actualOriginalSourceLocalId')==source['sourceComponentId'], 'Actual native inherited Light and public disabled original/current flags required; documentary NaN is never an active property.'
        assert proof.get('nativeIdentityMethod') in ('PERSISTENT_OR_GLOBAL_SOURCE_LOCAL_ID','NATIVE_NESTED_TRANSFORM_AND_ORIGINAL_SOURCE_XOR') and proof.get('actualPersistentGuid') in (row['prefabGuid'],source['sourceComponentGuid']), 'Actual native Light persistent identity method/GUID required.'
        for key in ('actualPersistentLocalId','actualPersistentPrefabId'):
            assert isinstance(proof.get(key),str) and re.fullmatch('[0-9]+',proof[key]) and 0<=int(proof[key])<2**64, 'Actual native Light persistent local/prefab IDs required.'
        assert int(proof['actualPersistentLocalId'])>0, 'Actual persistent Light local ID cannot be empty.'


def _finish_composition_v2(clean, integration, prior):
    finish=integration['finish'];market=finish.get('artistMarket');ship=finish.get('mooredShip')
    assert type(finish.get('traderShips')) is int and finish['traderShips'] == 1 and isinstance(market,dict) and isinstance(ship,dict), 'Schema2 requires one actual trader ship and the exact artist group.'
    assert all(market.get(key) is True for key in ('passed','exactSourcePoses','exactDependencies','roadAndObjectiveBuffersClear')), 'Actual artist source/pose/support gates required.'
    assert all(type(market.get(key)) is int and market[key] == value for key,value in dict(groups=1,instances=61,prefabs=28,groundSamples=49).items()), 'Exact1group/61instances/28prefabs/49dry samples required.'
    assert market.get('layout') == FINISH_ARTIST_LAYOUT and market.get('layoutSha256') == FINISH_ARTIST_LAYOUT_SHA and market.get('sourceSceneSha256') == FINISH_ARTIST_SCENE_SHA and market.get('identityParent') == '2075655012', 'Exact private source artist layout required.'
    _finish_source(clean,prior,FINISH_ARTIST_LAYOUT,market['layoutSha256'],market.get('layoutMetaSha256'),market.get('layoutGuid'))
    layout=json.loads((clean/FINISH_ARTIST_LAYOUT).read_text(encoding='utf-8-sig'))
    assert layout.get('schemaVersion') == 3 and layout.get('identityParent') == '2075655012' and layout.get('sourceSceneSha256') == FINISH_ARTIST_SCENE_SHA, 'Actual source artist identity changed.'
    rows=layout.get('instances');deps=layout.get('dependencies');children=market.get('children')
    assert isinstance(rows,list) and len(rows)==61 and len({r.get('sceneInstanceId') for r in rows})==61 and len({r.get('prefab') for r in rows})==28, 'Exact ordered original61artist instances required.'
    assert isinstance(deps,list) and len(deps)==103 and len({d.get('path') for d in deps})==103 and len({d.get('guid') for d in deps})==103 and isinstance(market.get('dependencies'),list) and len(market['dependencies'])==103, 'Exact103 original artist dependencies required.'
    imported=_finish_material_imports(clean,prior,layout,market)
    assert set(imported)=={d['path'] for d in deps if d['path'].endswith('.mat')}, 'Only the exact19artist material dependencies may use approved native import proof.'
    for dep,actual in zip(deps,market['dependencies']):
        assert all(actual.get(k)==dep.get(k) for k in ('path','guid','sha256','metaSha256')), 'Exact ordered103artist dependency identities changed.'
        binding=imported.get(dep['path'])
        if binding:
            assert binding['originalSha256']==dep['sha256'] and binding['metaSha256']==dep['metaSha256'] and binding['guid']==dep['guid'] and actual.get('actualSha256')==binding['importedSha256'] and actual.get('approvedNativeImport') is True, 'Exact approved material dependency receipt differs.'
        else:
            _finish_source(clean,prior,dep.get('path'),dep.get('sha256'),dep.get('metaSha256'),dep.get('guid'))
            assert actual.get('actualSha256')==dep['sha256'] and actual.get('approvedNativeImport') is False, 'All84 nonmaterial dependencies must remain byte-exact.'
    assert isinstance(children,list) and len(children)==61, 'Exact ordered61artist children required.'
    assert 0 <= _finish_finite(market.get('supportHeightRange'),'artist support') <= .6 and _finish_finite(market.get('minimumFreeboard'),'artist dry freeboard') >= 1.5, 'Actual artist dry support/freeboard gates failed.'
    assert type(market.get('trial')) is int and 0 <= market['trial'] < 256, 'Actual artist placement trial required.'
    parent_pose=_finish_pose(market);_finish_near(parent_pose[2],(1.75,1.75,1.75),.0001,'artist group scale');_finish_rotation(parent_pose[1],(0,1,0,0),.001,'artist group rotation');_finish_bounds(market.get('bounds'),'artist')
    index=_finish_yaml((clean/integration['prefab']).read_text(encoding='utf-8-sig'));group_id=_finish_named_transform(index,FINISH_ARTIST_NAME);group_doc=_finish_doc(index,group_id)[1]
    child_list=re.search(r'^  m_Children:\s*\r?\n(.*?)(?=^  \w)',group_doc,re.M|re.S);assert child_list,'Actual ordered artist child references required.'
    actual_children=re.findall(r'^  - \{fileID: (-?\d+)\}\r?$',child_list.group(1),re.M)
    assert len(actual_children)==61 and len(set(actual_children))==61, 'Actual saved artist group must have exactly61 ordered unique children.'
    wp,wq,_=_finish_world_transform(index,group_id);_finish_near(wp,parent_pose[0],.001,'artist parent');_finish_rotation(wq,parent_pose[1],.001,'artist parent');_finish_near(_finish_local_transform(index,group_id)[2],parent_pose[2],.0001,'artist parent scale')
    sources={};source_paths=set()
    for row,child,transform_id in zip(rows,children,actual_children):
        source=_finish_source(clean,prior,row.get('prefab'),row.get('sourceSha256'),row.get('sourceMetaSha256'),row.get('prefabGuid'));source_paths.add(source)
        assert all(child.get(a)==row.get(b) for a,b in [('sceneInstanceId','sceneInstanceId'),('source','prefab'),('guid','prefabGuid'),('sourceSha256','sourceSha256'),('sourceMetaSha256','sourceMetaSha256')]), 'Ordered artist child source/identity changed.'
        overrides=row.get('overrides');assert isinstance(overrides,list) and type(child.get('sourceOverrides')) is int and child['sourceOverrides']==len(overrides), 'Exact original artist override count required.'
        _finish_inactive_light_overrides(row,child)
        expected=(_finish_vector(row.get('position')), _finish_vector(row.get('rotation'),'xyzw'),_finish_vector(row.get('scale')))
        recorded=(_finish_vector(child.get('localPosition')), _finish_vector(child.get('localRotation'),'xyzw'),_finish_vector(child.get('localScale')))
        _finish_near(recorded[0],expected[0],.0001,'artist local position');_finish_near(recorded[2],expected[2],.0001,'artist local scale');_finish_rotation(recorded[1],expected[1],.005,'artist local rotation');_finish_bounds(child.get('actualBounds'),'artist child')
        if source not in sources:sources[source]=_finish_yaml((clean/source).read_text(encoding='utf-8-sig'))
        actual,_,parent,changes,source_root=_finish_instance(index,transform_id,sources[source],row['prefabGuid']);assert parent==group_id,'Actual artist child parent changed.'
        _finish_near(actual[0],expected[0],.0001,'saved artist child position');_finish_near(actual[2],expected[2],.0001,'saved artist child scale');_finish_rotation(actual[1],expected[1],.005,'saved artist child rotation')
        source_go=_finish_ref(_finish_doc(sources[source],source_root)[1],'m_GameObject');source_name=_finish_field(_finish_doc(sources[source],source_go)[1],'m_Name')
        assert child.get('name') == 'Artist source '+row['sceneInstanceId']+' '+source_name and changes.get((row['prefabGuid'],source_go,'m_Name'))==child['name'], 'Actual artist child name/identity changed.'
    assert type(market.get('appliedSourceOverrides')) is int and market['appliedSourceOverrides']==0 and type(market.get('inactiveSourceOverrides')) is int and market['inactiveSourceOverrides']==12 and sum(c['inactiveSourceOverrides'] for c in children)==12, 'Exact twelve original disabled Light bounds must remain unapplied.'
    assert all(ship.get(key) is True for key in ('passed','originalFerryPoseExact')), 'Actual trader/ferry preservation gates required.'
    assert ship.get('source')==FINISH_SHIP,'Only the actual owned trader ship source is allowed.'
    _finish_source(clean,prior,FINISH_SHIP,ship.get('sourceSha256'),ship.get('sourceMetaSha256'),ship.get('sourceGuid'))
    assert all(type(ship.get(key)) is int and ship[key]==value for key,value in [('depthSamples',25),('departureSweepSamples',53)]), 'Actual25depth/53departure sweeps required.'
    assert ship.get('seaLevel')==-2 and _finish_finite(ship.get('minimumKeelClearance'),'ship keel clearance')>=3 and _finish_finite(ship.get('departureLateralClearance'),'ferry departure clearance')>=10, 'Actual ship depth/departure gates failed.'
    pose=_finish_pose(ship);center,extent=_finish_bounds(ship.get('bounds'),'ship');assert abs(center[1]-extent[1]-_finish_finite(ship.get('keel'),'ship keel')) <= .01, 'Actual ship keel/bounds mismatch.'
    hull_length=_finish_finite(ship.get('hullLength'),'actual hull length');hull_height=_finish_finite(ship.get('hullHeight'),'actual hull height');hull_keel=_finish_finite(ship.get('hullKeel'),'actual hull keel');draft=_finish_finite(ship.get('hullDraft'),'actual hull draft')
    assert abs(hull_length-32)<=.01 and hull_height>0 and draft>0 and abs(draft-hull_height*.6)<=.01 and abs(ship['seaLevel']-hull_keel-draft)<=.01 and ship['keel']<=hull_keel+.01 and max(extent[0],extent[2])*2>=hull_length-.01, 'Actual32m source hull and measured60percent draft gates failed.'
    waterline=ship.get('waterline');count=ship.get('waterlineIntersections');assert type(count) is int and count>=12 and isinstance(waterline,list) and len(waterline)==count,'Actual ship waterline intersections required.'
    points=[_finish_vector(p,label='ship waterline') for p in waterline];assert all(abs(p[1]+2)<=.001 for p in points),'Actual ship waterline must remain at the real sea level.'
    assert all(all(abs(p[i]-center[i])<=extent[i]+.01 for i in (0,2)) for p in points) and all(v>0 for v in extent), 'Actual ship waterline must intersect its finite measured hull bounds.'
    width=max(p[0] for p in points)-min(p[0] for p in points);length=max(p[2] for p in points)-min(p[2] for p in points)
    assert _finish_finite(ship.get('waterlineWidth'),'waterline width')>=5 and abs(ship['waterlineWidth']-width)<=.001 and _finish_finite(ship.get('waterlineLength'),'waterline length')>=15 and abs(ship['waterlineLength']-length)<=.001,'Measured actual ship waterline span changed.'
    ship_instances=[key for key,value in index[1].items() if value[0]==1001 and re.search(r'^      value: Actual owned side-moored trader ship\r?$',_finish_doc(index,key)[1],re.M)]
    assert len(ship_instances)==1,'Exactly one saved source-backed trader root required.'
    ship_transforms=[key for key,value in index[1].items() if value[0]==4 and re.search(r'^  m_PrefabInstance: \{fileID: '+ship_instances[0]+r'\}\r?$',_finish_doc(index,key)[1],re.M)]
    ship_source=_finish_yaml((clean/FINISH_SHIP).read_text(encoding='utf-8-sig'));matches=[]
    for key in ship_transforms:
        doc=_finish_doc(index,key)[1]
        source_id=re.search(r'm_CorrespondingSourceObject: \{fileID: (-?\d+), guid: '+ship['sourceGuid']+r',',doc)
        if source_id and source_id.group(1) in ship_source[1] and _finish_ref(_finish_doc(ship_source,source_id.group(1))[1],'m_Father')=='0':matches.append(key)
    assert len(matches)==1,'Exact saved trader root Transform required.'
    local,_,parent,_,_=_finish_instance(index,matches[0],ship_source,ship['sourceGuid']);pp,pq,ps=_finish_world_transform(index,parent)
    scaled=tuple(x*y for x,y in zip(local[0],ps));rotated=_finish_multiply(_finish_multiply(pq,(*scaled,0)),(-pq[0],-pq[1],-pq[2],pq[3]))[:3]
    _finish_near(tuple(x+y for x,y in zip(pp,rotated)),pose[0],.001,'trader full position');_finish_rotation(_finish_multiply(pq,local[1]),pose[1],.001,'trader full rotation');_finish_near(local[2],pose[2],.0001,'trader local scale')
    canonical='Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab';assert all(prior.get(path) and (clean/path).is_file() and _forest_hash(clean/path)==prior[path] for path in (canonical,canonical+'.meta')), 'Original frozen ferry/world source and metadata required.'
    original=_finish_yaml((clean/canonical).read_text(encoding='utf-8-sig'))
    a=_finish_world_transform(index,_finish_named_transform(index,'Timber evacuation barge'));b=_finish_world_transform(original,_finish_named_transform(original,'Timber evacuation barge'))
    _finish_near(a[0],b[0],.0001,'original ferry full position');_finish_near(a[2],b[2],.0001,'original ferry full scale');_finish_rotation(a[1],b[1],.001,'original ferry full rotation')
    assert abs(ship['departureLateralClearance']-(center[0]-extent[0]-(a[0][0]+12)))<=.01, 'Actual trader/ferry lateral clearance differs from measured poses.'
    prefabs=finish.get('sourcePrefabs');assert isinstance(prefabs,list) and len(prefabs)==len(set(prefabs)) and source_paths|{FINISH_SHIP}<=set(prefabs), 'Actual artist/trader sources must remain in Finish provenance.'
    for path in prefabs:
        assert _outer_path(path).startswith(FINISH_PRIVATE) and all(prior.get(name) and (clean/name).is_file() and _forest_hash(clean/name)==prior[name] for name in (path,path+'.meta')), 'Finish source provenance cannot add unknown or drifted private assets.'


FINISH_SCHEMA3_GROUPS = (
    ('Owned authored village north garden','village',0,1.15,90),
    ('Owned authored village south garden','village',0,1.15,270),
    ('Owned authored harbor garden','harbor',0,1.1,180),
    ('Owned authored forest rock grove','forest',1,1,30),
    ('Owned authored nest rock grove','nests',1,1,155))
FINISH_SCHEMA3_EXTRA = ('Owned supported harbor merchant quayside','Owned supported moored trader cargo','Owned village stone retaining bank')
FINISH_SCHEMA3_SOURCE_FIELD_COUNTS = ((10,3,20),(22,0,0))  # Garden33, Natural22; three gardens and two natural groups.
FINISH_SCHEMA3_PLAN_TEST = 'DragonKingdomWorldAdoptionPlanTests.ReadOnlyPlanMapsEveryNonobsoleteOriginalObjectWithoutReplacingCanonicalIdentities'
FINISH_SCHEMA3_PLAN_REQUIRED_PATHS = (
    'Emberwick Village[0]/Owned authored village north garden[0]',
    'Emberwick Village[0]/Owned authored village south garden[0]',
    'Lanternwater Harbor[0]/Owned authored harbor garden[0]',
    'Whisperwood[0]/Owned authored forest rock grove[0]',
    'Skycradle Cliffs[0]/Owned authored nest rock grove[0]',
    'Lanternwater Harbor[0]/Owned supported harbor merchant quayside[0]',
    'Lanternwater Harbor[0]/Owned supported moored trader cargo[0]',
    'Emberwick Village[0]/Owned village stone retaining bank[0]',
    'Emberwick Village[0]/Owned original artist market frontage[0]')


def _finish_plan_required_paths(additions):
    """Unity PathOf writes each same-name sibling ordinal, including zero."""
    paths=[];leaves=[]
    for row in additions:
        path=row.get('path');assert isinstance(path,str) and path, 'Actual indexed native hierarchy path required.'
        segments=path.split('/');parsed=[re.fullmatch(r'([^\[\]/\\]+)\[(0|[1-9]\d*)\]',segment) for segment in segments]
        assert all(parsed), 'Every native hierarchy segment must retain its exact canonical sibling index.'
        paths.append(path);leaves.append((parsed[-1][1],int(parsed[-1][2])))
    assert len(paths)==len(set(paths)), 'Duplicate full native addition path.'
    for path in FINISH_SCHEMA3_PLAN_REQUIRED_PATHS:
        name=path.rsplit('/',1)[-1][:-3]
        assert paths.count(path)==1 and [ordinal for leaf,ordinal in leaves if leaf==name]==[0], 'Exact known native parent/root path and sole sibling index[0] required: '+path
    return paths


def _finish_native_sha(value):
    assert isinstance(value,str) and re.fullmatch(r'(?:[0-9a-fA-F]{64}|(?:[0-9a-fA-F]{2}-){31}[0-9a-fA-F]{2})',value), 'Exact native SHA256 encoding required.'
    return value.replace('-','').lower()


def require_schema3_saved_plan(clean, run, candidate, integration, prior):
    """Native saved-asset validators, not a parallel Python geometry engine."""
    assert run is not None, 'Schema3 requires the actual same-run saved-world identity plan.'
    run=Path(run);cases=[case for case in ET.parse(run/'tests.xml').iter('test-case') if case.get('fullname')==FINISH_SCHEMA3_PLAN_TEST]
    assert len(cases)==1 and cases[0].get('result')=='Passed', 'Exactly one actual saved-world identity plan must PASS for schema3.'
    path=run/'rendered-evidence'/('identity-adoption-plan-'+candidate+'.json');assert path.is_file(), 'Actual schema3 saved-world plan receipt required.'
    plan=json.loads(path.read_text(encoding='utf-8-sig'));canonical='Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab'
    assert plan.get('scope')=='READ_ONLY_ORIGINAL_OBJECT_ID_MAPPING_NOT_ADOPTION_OR_VISUAL_APPROVAL' and plan.get('tag')==candidate and plan.get('status')=='MAPPED_ORIGINAL_IDENTITIES_REQUIRES_VISUAL_COURSE_APPROVAL_AND_EXPLICIT_APPLICATION' and plan.get('allRetainedOriginalObjectsMapped') is True, 'Schema3 actual saved-source validation/map failed.'
    for asset,hash_key,guid_key in ((canonical,'canonicalPrefabHash','canonicalGuid'),(integration['prefab'],'candidatePrefabHash','candidateGuid')):
        assert _forest_hash(clean/asset)==_finish_native_sha(plan.get(hash_key)) and _forest_guid(clean/(asset+'.meta'))==plan.get(guid_key), 'Schema3 saved plan is stale for actual canonical/candidate bytes or GUID.'
    assert prior.get(canonical)==_forest_hash(clean/canonical) and prior.get(canonical+'.meta')==_forest_hash(clean/(canonical+'.meta')) and plan['canonicalGuid']!=plan['candidateGuid'], 'Schema3 preserves the exact frozen canonical world.'
    assert all(type(plan.get(k)) is int and plan[k]==v for k,v in (('ownedFinishRoots',343),('ownedArtistMarketRoots',1),('ownedCoastalRoots',1))) and type(plan.get('originalObjects')) is int and type(plan.get('mappedOriginalObjects')) is int and plan['originalObjects']>=plan['mappedOriginalObjects']>500, 'Schema3 requires343 ordinary/new roots plus one artist/one coast and actual original mappings.'
    ids=plan.get('retainedOriginalIds');assert isinstance(ids,list) and len(ids)==len(set(ids))==plan['mappedOriginalObjects'] and len(plan.get('obsolete',[]))==5, 'Exact native original/obsolete identity accounting required.'
    additions=plan.get('additions');bearings=bool((integration.get('bearings') or {}).get('patches',0))
    assert isinstance(additions,list) and len(additions)==343+1+1+4+int(bearings) and len({r.get('path') for r in additions})==len(additions), 'Schema3 cannot add unknown or duplicate root subtrees.'
    _finish_plan_required_paths(additions)
    return plan


# These are the nine explicitly reviewed V247/V252 imports, not an extension
# of the generic source hash policy. Native preparation owns the actual public
# importer and persistent model identity checks before the immutable baseline.
FINISH_QUAY_SCOPE = 'EXACT_V247_NATIVE_IMPORT_BOOKKEEPING_BEFORE_IMMUTABLE_QUAY_GEOMETRY_BASELINE_NOT_WORLD_OR_VISUAL_ACCEPTANCE'
FINISH_QUAY_CRACKS = 'Assets/TruckDragonEggDelivery/Materials/Dark visible shell cracks.mat'
FINISH_QUAY_VERSION_SHA = '228831cdcb22e317cec0d61a1a1ee3549687ecae83986323a2074c3d4ad4a334'
FINISH_QUAY_FVP = FINISH_PRIVATE+'Plugins/Tidal Flask Studios/Fantastic Village Pack/materials/'
FINISH_QUAY_DRAGONS = 'Assets/TruckDragonKingdom/PrivateLicensedDragons/Monsters Ultimate Pack 02 Cute Series/'
FINISH_QUAY_MATERIALS = {
    FINISH_QUAY_CRACKS:('b408d7c0d79df37ba29f40b3a84faa9100f459b280f001ff585163805839e278',None),
    FINISH_QUAY_FVP+'M_CLR_black.mat':('cebf853826f7dd071834316c9a133ae92392402305042d3855ec71ae2b051494','b106ce539f2ab9d3c01f648b1d7672e44b7e1207b35ace5f602920b167a8b8c6'),
    FINISH_QUAY_FVP+'M_leather.mat':('7af6186881e1211b03b1fffd0ce5298287b6e638b303c29c95e43733f69ce16d','9810dd1d5c9426576d4900dd0de1e0bab940b8e8497849119cd7336802a1842a'),
    FINISH_QUAY_FVP+'M_rooftiles_03.mat':('f53e988fcc7b54d0ac60bfc8f6a334b78de56d8cfdb815e8c53e0192684e0cc9','a5ce6aeb5177bbcc9e5df7e4ae4eeb15f831e2bdcc9ed6344cb1b9b012d18f52')}
FINISH_QUAY_METADATA = {
    FINISH_QUAY_DRAGONS+'Dragon Fire Cute Series/FBX/Dragon Fire.FBX.meta':('dbbb962e749065a4cef1fc69775756318c06627b45b82f704d5b5132d02aecb5','7dd861c62f1430b808d16f3597fbd38c158ada397e376c567a4ae8d20961aab8'),
    FINISH_QUAY_DRAGONS+'Dragon Inferno Cute Series/FBX/Dragon Inferno.FBX.meta':('e5ee9d5189e4084a1a2a4f610595d04f122204132cf8232e21cace2c0699d78b','13caec2a45c55624772b4e6c340857668b94b1888ab9cea92f0d48830956eaf6'),
    FINISH_QUAY_DRAGONS+'Dragon Spark Cute Series/FBX/Dragon Spark.FBX.meta':('d24f77c80c290db544ec5ef0c04ce2a52bc71e7dcd8830d6e76c1448e56fbcd2','359a2241ea7a47e87c16cb4ebb45ee24f458d0d5592fb276de83c4732e8620a9'),
    FINISH_PRIVATE+'Fantastic Ancient Forest/Shaders/S_foliage_wind_URP_advanced_lit_emission_U6-1.shader.meta':('a887211070fbd58c331b98aba5535d9c55d2eb1b835adf2bc464f717629416e3','fc9444c6239ddee359ab0953f142f1bb9e16490fa085a7c1a0f4fdd5f595e633'),
    'Assets/TruckDragonKingdom/ReviewCandidates/RoundedVista01/Foundation Receipt.json.meta':('078ea910c53413564c9d955812c1145cb123d5d28cd7b1a2b777d9584fabf592','20c7cbf12bc3d15975a70df603bd1882ad5c0df4a953ba330fe90b6eef1bbc89')}
FINISH_QUAY_MODELS = tuple((FINISH_QUAY_DRAGONS+name+' Cute Series/FBX/'+name+'.FBX',signature,assets,transforms) for name,signature,assets,transforms in (
    ('Dragon Fire','05bf71095d266ded7b70e3b4a2b7e96ee2d57d7a8eef306b50265ae4a0083b76',172,84),
    ('Dragon Inferno','d084b0bbb8e5c7f911b10b6bf29bee7e6cae57ea2611bc3c3a29c086ef3c36ab',184,90),
    ('Dragon Spark','d7a0d0318225c3c4e00b4491794ab7107bd6df8f1fe9d1eb8f9e3eb33e990dea',92,44)))


def _finish_quay_version(data):
    headers=list(re.finditer(rb'(?m)^--- !u!(\d+) &(-?\d+)\n',data))
    assert len(headers)==2 and sorted(h[1] for h in headers)==[b'114',b'21'], 'Owned material permits only original Material and public native version subasset.'
    documents={h[1]:(h,data[h.start():headers[i+1].start() if i+1<len(headers) else len(data)]) for i,h in enumerate(headers)}
    material,body=documents[b'21'];version,metadata=documents[b'114'];local=version[2].decode()
    assert material[2]==b'2100000' and hashlib.sha256(data[:headers[0].start()]+body).hexdigest()==FINISH_QUAY_MATERIALS[FINISH_QUAY_CRACKS][0], 'Original owned Material2100000 bytes changed.'
    canonical=re.sub(rb'\A--- !u!114 &-?\d+\n',b'--- !u!114 &0\n',metadata)
    assert local not in ('0','2100000') and -(2**63)<=int(local)<2**63 and hashlib.sha256(canonical).hexdigest()==FINISH_QUAY_VERSION_SHA, 'Only exact public AssetVersion10 new header identity may vary.'
    return local


def _finish_quay_imports(clean, integration, prior):
    """Validate explicit native import proof; never rewrite/relax frozen input."""
    evidence=integration.get('nativeImportPreflight')
    assert isinstance(evidence,dict) and evidence.get('scope')==FINISH_QUAY_SCOPE and all(evidence.get(k) is True for k in ('passed','allUnknownChangesRejected','protectedModelIdentitiesExact','nonMaterialSourceBytesExact')), 'Actual production native import preflight with unchanged protected sources required.'
    assert evidence.get('protectedModels')==3 and all(type(evidence.get(k)) is int and evidence[k]>0 for k in ('watchedPayloads','sourceFilesWatched','protectedOtherSourceFiles')) and evidence['sourceFilesWatched']==evidence['watchedPayloads']+evidence['protectedOtherSourceFiles'], 'Complete native payload/nonmaterial/source accounting required.'
    models=evidence.get('models');assert isinstance(models,list) and len(models)==3, 'All three actual native persistent model signatures required.'
    for row,(path,signature,assets,transforms) in zip(models,FINISH_QUAY_MODELS):
        assert row.get('path')==path and row.get('signature')==signature and all(type(row.get(k)) is int and row[k]==value for k,value in (('persistentAssets',assets),('meshes',1),('clips',0),('hierarchyTransforms',transforms))), 'Actual complete persistent model/mesh/hierarchy signatures and legitimate zero clip counts changed.'
    known={};rules={};local=None
    for path,(raw,native) in FINISH_QUAY_METADATA.items():
        assert prior.get(path) in (raw,native) and (clean/path).is_file() and _forest_hash(clean/path)==native, 'Only five exact reviewed native importer metadata pairs are permitted: '+path
        asset=path[:-5];assert prior.get(asset) and (clean/asset).is_file() and _forest_hash(clean/asset)==prior[asset], 'Actual source content under native bookkeeping changed: '+asset
        known[path]=(raw,native);rules[path]='EXACT_FIVE_NATIVE_IMPORTER_BOOKKEEPING_RECIPES_ALL_AUTHORED_SETTINGS_AND_FILE_IDS_PRESERVED'
    for path,(raw,native) in FINISH_QUAY_MATERIALS.items():
        assert (clean/path).is_file(), 'All four actual extra material sources required.'
        actual=_forest_hash(clean/path)
        if path==FINISH_QUAY_CRACKS:local=_finish_quay_version((clean/path).read_bytes());native=actual
        assert actual==native and prior.get(path) in (raw,native), 'Extra material must match exact reviewed original/native bytes: '+path
        assert prior.get(path+'.meta') and (clean/(path+'.meta')).is_file() and _forest_hash(clean/(path+'.meta'))==prior[path+'.meta'], 'Extra material GUID/meta must remain exact.'
        known[path]=(raw,native);rules[path]='OWNED_MATERIAL_BODY_BYTE_EXACT_ONLY_PUBLIC_ASSETVERSION10_NEW_HEADER_ID' if path==FINISH_QUAY_CRACKS else 'EXACT_THREE_FVP_NATIVE_BYTES_PROTECTED_AUTHORED_FIELDS_PUBLIC_COLOR_CONVERSIONS_ONLY'
    for path,digest in ((FINISH_IMPORT_PROVENANCE,FINISH_IMPORT_PROVENANCE_SHA),(FINISH_ASSEMBLY_PROVENANCE,FINISH_ASSEMBLY_PROVENANCE_SHA)):
        assert prior.get(path)==digest and (clean/path).is_file() and _forest_hash(clean/path)==digest, 'Existing immutable19/51 import provenance required.'
        for row in json.loads((clean/path).read_text(encoding='utf-8-sig'))['bindings']:
            pair=(row['originalSha256'],row['importedSha256']);asset=row['path']
            assert asset not in known or known[asset]==pair, 'Conflicting exact import provenance.'
            known[asset]=pair;rules[asset]='EXISTING_EXACT_19_OR_51_MATERIAL_PROVENANCE_UNCHANGED'
    changes=evidence.get('changes');assert isinstance(changes,list) and len({row.get('path') for row in changes})==len(changes), 'Distinct actual native import changes required.'
    for row in changes:
        path=row.get('path');assert path in known and row.get('authoredDataPreserved') is True and row.get('rule')==rules[path] and (row.get('beforeSha256'),row.get('afterSha256'))==known[path] and _forest_hash(clean/path)==row['afterSha256'], 'Unknown or stale native import delta rejected.'
        assert (row.get('nativeVersionLocalId') or None)==(local if path==FINISH_QUAY_CRACKS else None), 'Actual native added version identity must match its exact document.'
    # No mutable context, generalized material waiver or selected outputs: all
    # existing source/GUID checks and concurrent ROOT protections stay exact.
    return evidence


def _finish_geometry_sources(clean, prior, rows):
    assert isinstance(rows,list), 'Actual native source geometry records required.'
    assert len({row.get('key') for row in rows})==len(rows), 'Duplicate native model/collider binding.'
    for row in rows:
        assert isinstance(row.get('type'),str) and isinstance(row.get('rootMatrix'),list) and len(row['rootMatrix'])==16 and all(type(v) in (int,float) and math.isfinite(v) for v in row['rootMatrix']), 'Actual finite native model/collider transform required.'
        if row.get('mesh'):
            _finish_source(clean,prior,row['mesh'],row.get('meshSha256'),row.get('meshMetaSha256'),row.get('meshGuid'))
            assert re.fullmatch(r'-?\d+',row.get('meshLocalId','')), 'Original native mesh localID required.'


def _finish_schema3_overrides(source, evidence):
    changes=source.get('overrides');assert isinstance(changes,list) and isinstance(evidence,list) and len(changes)==len(evidence), 'Exact ordered original authored fields required.'
    absent={
        'ba43096d063f9e141bc66d23a6aad4b6':('3a3b5a6f7a64a67e7d99b70ecd9f5d641f12527a64ed1cec258b52bcbab5bf36',125,{'1439406413011755468'}),
        '8930c424f7d0037468691ac2f225bae8':('cfbc9055e88c85a7880a8cccfb89f720f48a53b703e8793883aa2062e138358a',112,{'6845380403947263104','2298868114731749878','6481620312478966445'}),
        'c0cacda008ac5324598ff77198b809b2':('c6d8fa4eca07072c6e8555b5490232dacb712cdf09ca55a0c3c426fc47637232',81,{'7892989606626299767'})}
    counts=[0,0,0]
    for change,proof in zip(changes,evidence):
        assert all(proof.get(a)==change.get(b) for a,b in (('targetId','targetId'),('targetGuid','targetGuid'),('property','property'),('rawValue','sourceValue'))) and proof.get('instance')==source.get('sceneInstanceId'), 'Actual authored override source/order/owner differs.'
        raw=change['sourceValue'];prop=change['property'];pin=absent.get(source.get('prefabGuid'))
        if pin and change['targetId'] in pin[2]:
            assert source.get('sourceSha256')==pin[0] and change.get('targetGuid')==source['prefabGuid'] and change.get('objectReference')=='fileID: 0', 'Exact obsolete native source binding required.'
            category='BOUND' if prop.startswith('m_BoundingSphereOverride.') else 'GAMEOBJECT' if prop=='m_IsActive' else 'TRANSFORM'
            assert proof.get('state')=='EXACT_OBSOLETE_SOURCE_'+category+'_TARGET_ABSENT_IN_NATIVE_HIERARCHY' and proof.get('nativeIdentityMethod')=='EXACT_PINNED_PREFAB_SHA_AND_COMPLETE_NATIVE_SOURCE_INSTANCE_ID_ABSENCE' and proof.get('targetMatches')==0 and proof.get('sourceObjectCount')==proof.get('instanceObjectCount')==pin[1] and proof.get('missingScripts')==0 and proof.get('applied') is False and proof.get('disabledPublicLightOverride') is False and not proof.get('appliedValueVerified'), 'Exact native-absent documentary fields cannot be applied/repointed or misclassified as Lights.'
            counts[2]+=1
        elif prop.startswith('m_BoundingSphereOverride.'):
            assert source.get('prefabGuid')=='4b1c95ef468f0ad42b07857ca0836f4f' and source.get('sourceSha256')=='cd287a962e95c8db6e5ce8e146098d50f76da314963a8c63ee014054c2fa1c39' and change['targetId']=='3877092548939707763' and proof.get('state')=='INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE' and proof.get('actualType')=='UnityEngine.Light' and proof.get('disabledPublicLightOverride') is True and proof.get('targetMatches')==1 and proof.get('applied') is False and not proof.get('appliedValueVerified'), 'Only actual original disabled Light fields remain documentary.'
            counts[1]+=1
        else:
            assert proof.get('state')=='APPLIED_EXACT_ORIGINAL_AUTHOR_VALUE' and proof.get('applied') is True and proof.get('appliedValueVerified') is True and proof.get('targetMatches')==1 and proof.get('nativeIdentityMethod')=='ACTUAL_PERSISTENT_SOURCE_GUID_LOCAL_ID_AND_EXACT_PREFAB_SIBLING_COMPONENT_SLOT_MAPPING' and proof.get('sourceGlobalId') and proof.get('instanceGlobalId'), 'Actual original active fields need exact native target, atomic write and readback.'
            if prop=='m_IsActive':assert raw in ('0','1') and proof.get('actualType')=='UnityEngine.GameObject' and proof.get('actualValue')==raw, 'Exact authored active flag required.'
            elif re.fullmatch(r'm_Local(?:Position\.[xyz]|Rotation\.[xyzw]|Scale\.[xyz])',prop):assert proof.get('actualType')=='UnityEngine.Transform' and abs(_finish_finite(float(raw),'active source field')-_finish_finite(float(proof.get('actualValue')),'active actual field'))<=.00002, 'Only finite actual authored transform fields may apply.'
            else:
                ref=re.fullmatch(r'fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 2',change.get('objectReference',''))
                assert re.fullmatch(r'm_Materials\.Array\.data\[\d+\]',prop) and ref and proof.get('assignedAssetGuid')==ref.group(2) and proof.get('assignedAssetLocalId')==ref.group(1) and proof.get('actualValue')==ref.group(2)+'/'+ref.group(1), 'Exact original renderer material assignment required.'
            counts[0]+=1
    return counts


def _finish_schema3_override_totals(group_counts, source_fields):
    expected=[list(FINISH_SCHEMA3_SOURCE_FIELD_COUNTS[row[2]]) for row in FINISH_SCHEMA3_GROUPS]
    assert group_counts==expected and source_fields==[sum(row) for row in expected], 'Every exact source group must preserve its original active/disabled/obsolete field classification.'
    totals=[sum(row[i] for row in group_counts) for i in range(3)]
    assert totals==[74,9,60] and sum(totals)==sum(source_fields)==3*33+2*22, 'Three original33-field gardens and two22-field natural groups require143 fields, including74 active writes.'
    return totals


def _finish_schema3_palette(clean, candidate, integration, prior):
    palette=integration.get('palette');folder='Assets/TruckDragonKingdom/VisualCandidates/'+candidate+'/Terrain/Palette/'
    assert isinstance(palette,dict) and palette.get('passed') is True and palette.get('appearanceOperationsPreservedGeometryAndAlpha') is True and palette.get('recipe')=='source-polyart-authored11m' and palette.get('diagnosticRun')=='V239c' and palette.get('terrains')==16 and palette.get('clonedOuterTerrainData')==12, 'Actual chosen source-authored palette requires all16 terrains/12 outer clones.'
    deps=palette.get('sourceDependencies');assert isinstance(deps,list) and deps and len({d.get('path') for d in deps})==len(deps), 'Exact actual native source-layer dependency closure required.'
    for dep in deps:_finish_source(clean,prior,dep.get('path'),dep.get('sha256'),dep.get('metaSha256'),dep.get('guid'))
    source_root=FINISH_PRIVATE+'Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Terrain/'
    layers=palette.get('layers');assert isinstance(layers,list) and len(layers)==4, 'Four actual source-authored terrain layers required.'
    outputs=set();layer_guids=[]
    for row,name in zip(layers,('GrassGreen','GrassYellow','Dirt','Rock')):
        source=source_root+name+'.terrainlayer';target=folder+name+'.terrainlayer'
        assert row.get('source')==source and row.get('candidate')==target, 'Exact chosen original layer/output path required.'
        _finish_source(clean,prior,source,row.get('sourceSha256'),row.get('sourceMetaSha256'),row.get('sourceGuid'))
        for path,digest in ((target,row.get('candidateSha256')),(target+'.meta',row.get('candidateMetaSha256'))):assert (clean/path).is_file() and _forest_hash(clean/path)==_finish_native_sha(digest), 'Actual saved source-layer output differs.'
        assert _forest_guid(clean/(target+'.meta'))==row.get('candidateGuid') and row['candidateGuid']!=row['sourceGuid'], 'Palette clone GUID must remain distinct.'
        original=(clean/source).read_text(encoding='utf-8-sig');cloned=(clean/target).read_text(encoding='utf-8-sig')
        # Compare all actual source-authored texture,UV/remap/scalar fields.
        def properties(text):return {name:_finish_field(text,name) for name in re.findall(r'^  (m_(?:DiffuseTexture|NormalMapTexture|MaskMapTexture|TileSize|TileOffset|Specular|Metallic|Smoothness|NormalScale|DiffuseRemapMin|DiffuseRemapMax|MaskMapRemapMin|MaskMapRemapMax)):',text,re.M)}
        assert len(properties(original))==13 and properties(original)==properties(cloned), 'Actual authored layer appearance must remain exact.'
        outputs|={target,target+'.meta'};layer_guids.append(row['candidateGuid'])
    assert len(set(layer_guids))==4, 'Four distinct palette output layer GUIDs required.'
    tiles=palette.get('tiles');assert isinstance(tiles,list) and len(tiles)==16 and len({t.get('name') for t in tiles})==16 and len({t.get('candidate') for t in tiles})==16 and len({t.get('candidateGuid') for t in tiles})==16, 'Exactly16 distinct actual saved terrain bindings required.'
    external=[];prefab=(clean/integration['prefab']).read_text(encoding='utf-8-sig');refs=re.findall(r'm_TerrainData:\s*\{[^}]*guid:\s*([0-9a-f]{32})[^}]*\}',prefab)
    assert len(refs)==32, 'Actual saved16Terrain/Collider pairs required.'
    for tile in tiles:
        target=_outer_path(tile.get('candidate'));outer=tile.get('externalDataCloned');assert type(outer) is bool and tile.get('appearancePreservedGeometryAndAlpha') is True, 'Actual native palette stage must preserve physical fields.'
        assert tile.get('heightsBefore')==tile.get('heightsAfter') and tile.get('alphaBefore')==tile.get('alphaAfter') and tile.get('holesBefore')==tile.get('holesAfter'), 'Palette appearance cannot change heights/alpha/holes.'
        for field in ('heightsBefore','heightsAfter','alphaBefore','alphaAfter','holesBefore','holesAfter','finalHoles'):_finish_native_sha(tile.get(field))
        _finish_vector(tile.get('position'));size=_finish_vector(tile.get('size'));assert all(v>0 for v in size) and all(type(tile.get(k)) is int and tile[k]>0 for k in ('heightResolution','alphaWidth','alphaHeight','holesResolution')), 'Actual finite terrain dimensions required.'
        if outer:
            assert target==folder+'Outer '+str(tiles.index(tile))+'.asset' and tile.get('holesAfter')==tile.get('finalHoles') and tile.get('candidateGuid')!=tile.get('sourceDataGuid'), 'Exact outer palette clone and unchanged holes required.'
            source=_outer_path(tile.get('sourceData'));assert source.startswith('Assets/TruckDragonKingdom/ReviewCandidates/') and source.endswith('.asset'), 'Only exact retained outer review TerrainData may be cloned.'
            assert prior.get(source)==tile.get('sourceDataSha256') and prior.get(source+'.meta')==tile.get('sourceDataMetaSha256') and _forest_hash(clean/source)==prior[source] and _forest_hash(clean/(source+'.meta'))==prior[source+'.meta'] and _forest_guid(clean/(source+'.meta'))==tile.get('sourceDataGuid'), 'Original outer TerrainData bytes/GUID/meta must remain frozen.'
            external.append(tile)
        else:assert target.startswith('Assets/TruckDragonKingdom/VisualCandidates/'+candidate+'/Terrain/') and '/Palette/' not in target and tile.get('sourceData')==target and tile.get('sourceDataGuid')==tile.get('candidateGuid'), 'Inner palette must retain its own four fresh TerrainData identities.'
        for path,digest in ((target,tile.get('candidateSha256')),(target+'.meta',tile.get('candidateMetaSha256'))):assert (clean/path).is_file() and _forest_hash(clean/path)==_finish_native_sha(digest), 'Actual final terrain data/GUID bytes differ.'
        assert _forest_guid(clean/(target+'.meta'))==tile.get('candidateGuid') and refs.count(tile['candidateGuid'])==2, 'Exact native Terrain/Collider saved data binding differs.'
        outputs|={target,target+'.meta'}
    assert len(external)==12 and len({t['sourceData'] for t in external})==12, 'Exactly12 original outer source identities required.'
    assert {p.relative_to(clean).as_posix() for p in (clean/folder).rglob('*') if p.is_file()}=={p for p in outputs if p.startswith(folder)}, 'Unknown palette output assets require explicit review.'
    return external


def _finish_schema3_quay(quay):
    assert isinstance(quay,dict) and quay.get('name')==FINISH_SCHEMA3_EXTRA[0] and all(quay.get(k) is True for k in ('passed','originalFerryPoseExact','departureCorridorClear','roadAndObjectiveBuffersClear')) and [quay.get(k) for k in ('modules','supportedDecorations','decorationFootSamples','departureQueries')]==[40,22,198,53], 'Actual40module/22decoration/198deck/53departure proof required.'
    piers,bearings=quay.get('piers'),quay.get('sourceBearingStations')
    assert type(piers) is int and type(bearings) is int and piers>0 and bearings>=0 and piers+bearings==12 and len(quay.get('parts',[]))==40+piers and len(quay.get('pierContacts',[]))==piers and len(quay.get('sourceBearingContacts',[]))==bearings, 'Exactly12 measured support stations require distinct generated-pier and existing-source-bearing accounting; no invented support pieces.'
    assert len(quay.get('decorations',[]))==22 and len(quay.get('deckContacts',[]))==198 and _finish_finite(quay.get('minimumDepartureLateralClearance'),'quay/ferry separation')>=10 and abs(_finish_finite(quay.get('deckSurface'),'actual native walkable deck'))<=.02, 'Complete actual zero-height deck/decorations/ferry-clearance proof required.'
    stations=[]
    for contact in quay['pierContacts']:
        point=_finish_vector(contact.get('position'),label='generated pier station');stations.append((point[0],point[2]));assert abs(point[1])<=.001, 'Actual native support station must retain its declared XZ sample.'
        bed=_finish_finite(contact.get('nativeBed'),'pier native terrain');bottom=_finish_finite(contact.get('postBottom'),'pier bottom');top=_finish_finite(contact.get('postTop'),'pier top');underside=_finish_finite(contact.get('deckUnderside'),'native source underside')
        assert contact.get('baseContact') is True and contact.get('deckContact') is True and isinstance(contact.get('baseCollider'),str) and contact['baseCollider'] and bottom<=bed+.02 and bed-bottom<=.2 and top>bottom and abs(top-underside)<.35, 'Actual generated post must retain its measured native bed-to-source-underside contact chain.'
    for contact in quay['sourceBearingContacts']:
        point=_finish_vector(contact.get('position'),label='existing source bearing station');stations.append((point[0],point[2]));assert abs(point[1])<=.001, 'Actual existing source-bearing station must retain its XZ sample.'
        bed=_finish_finite(contact.get('nativeBed'),'source bearing native terrain');underside=_finish_finite(contact.get('sourceUnderside'),'existing source underside')
        assert contact.get('passed') is True and isinstance(contact.get('sourceCollider'),str) and contact['sourceCollider'] and underside<=bed+.02, 'Existing native dock-to-ground support needs actual source/terrain/underside proof, not a synthetic pier.'
    expected={(x,z) for x in (674,695) for z in (-624,-637,-650,-663,-676,-688)}
    assert len(stations)==len(set(stations))==12 and set(stations)==expected, 'Every original native support station must occur exactly once across posts and source bearings.'
    for contact in quay['deckContacts']:
        requested=_finish_vector(contact.get('requested'),label='native decoration foot query');actual=_finish_vector(contact.get('actual'),label='native deck foot contact')
        assert contact.get('passed') is True and isinstance(contact.get('collider'),str) and contact['collider'] and abs(actual[1]-quay['deckSurface'])<=.04 and abs(requested[0]-actual[0])<=.001 and abs(requested[2]-actual[2])<=.001, 'All198 actual decoration-foot contacts must remain on the measured native walkable deck.'


FINISH_SCHEMA3_GROUP_HINTS = ((-595,0,-280),(-425,0,-485),(655,0,-365),(-850,0,300),(-940,0,890))
FINISH_SCHEMA3_REFINEMENT_OFFSETS = tuple((x*.5,0,z*.5) for x,z in sorted(((x,z) for x in range(-8,9) for z in range(-8,9) if 0<x*x+z*z<=64),key=lambda p:(p[0]*p[0]+p[1]*p[1],p[0],p[1])))


def _finish_schema3_group_search(clean, prior, group, plan, ridge):
    """Pin only the two approved whole-parent village search extensions."""
    name,region,layout_index,scale,yaw=plan;village=region=='village' and layout_index==0
    assert type(group.get('naturalMode')) is bool and group['naturalMode']==(layout_index==1) and type(group.get('solidColliders')) is int and group['solidColliders']>0, 'Exact authored court/grove classification and real solid collision required.'
    source_keys=('inputDefinition','inputDefinitionGuid','inputDefinitionSha256','inputDefinitionMetaSha256')
    assert isinstance(ridge,dict) and all(isinstance(group.get(k),str) and group[k]==ridge.get(k) for k in source_keys), 'All source groups and Ridge must share the same immutable INPUT definition provenance.'
    definition=group['inputDefinition'];canonical='Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset'
    assert definition==canonical or re.fullmatch(r'Assets/TruckDragonKingdom/VisualCandidates/[A-Za-z0-9_-]+/Dragon Kingdom\.asset',definition), 'Only an actual owned canonical/candidate input definition is eligible.'
    assert prior.get(definition)==group['inputDefinitionSha256'] and prior.get(definition+'.meta')==group['inputDefinitionMetaSha256'] and _forest_hash(clean/definition)==group['inputDefinitionSha256'] and _forest_hash(clean/(definition+'.meta'))==group['inputDefinitionMetaSha256'] and _forest_guid(clean/(definition+'.meta'))==group['inputDefinitionGuid'], 'Actual source-group INPUT definition bytes/GUID/meta changed.'
    region_center,region_extent=_finish_bounds(group.get('regionBounds'),'original group region');assert all(v>0 for v in region_extent), 'Real original region dimensions required.'
    index=FINISH_SCHEMA3_GROUPS.index(plan);hint=_finish_vector(group.get('hint'),label='source group hint');_finish_near(hint,FINISH_SCHEMA3_GROUP_HINTS[index],.0001,'original authored group hint')
    seeds=group.get('refinementSeeds');assert isinstance(seeds,list) and len(seeds)<=16 and (village or not seeds), 'Only the two village courts may retain up to16 actual native flat-peer parcel seeds.'
    trial=group.get('trial');base=1920 if village else 192;assert type(trial) is int and 0<=trial<base+196*len(seeds), 'Exact base and seed-count-bounded local refinement required; other groups retain192.'
    attempts=group.get('trials');assert isinstance(attempts,list) and len(attempts)==trial+1, 'Every ordered actual authored-group trial must be preserved.'
    eligible=[i for i,row in enumerate(attempts[:base]) if row.get('rejectedReason')=='ACTUAL_OTHER_ART_COLLIDER_OVERLAP'][:16] if village else []
    assert [seed.get('trial') for seed in seeds]==eligible, 'Seeds must be exactly the first16 eligible measured base-trial peer collisions in original order.'
    for seed in seeds:
        i=seed.get('trial');assert type(i) is int and 0<=i<base and type(seed.get('orientation')) is int and seed['orientation']==i//960, 'Native parcel seed must identify one earlier original/quarter-turn candidate.'
        witness=attempts[i];center=_finish_vector(seed.get('center'),label='native peer seed center');bounds_center,_=_finish_bounds(witness.get('bounds'),'native peer seed trial')
        assert center[1]==0 and math.hypot(center[0]-bounds_center[0],center[2]-bounds_center[2])<=.002, 'Native seed center must match its exact earlier measured parcel.'
        assert all(type(witness.get(k)) is int and witness[k]==v for k,v in (('samples',49),('notDrySamples',0),('underFreeboardSamples',0))) and 0<=_finish_finite(witness.get('range'),'native peer seed terrain range')<=.6, 'Only an actual49-point dry/freeboard/flat trial can seed local refinement.'
        floor=witness.get('floorPoints');assert isinstance(floor,list) and len(floor)==49 and all(p.get('classifiedDry') is True and p.get('freeboardPass') is True for p in floor) and len({_finish_vector(p.get('point'),label='native seed floor point') for p in floor})==49, 'All49 distinct actual native floor witnesses must remain dry and supported.'
        blocker=witness.get('blocker');assert isinstance(blocker,dict) and blocker.get('kind')=='NATIVE_OTHER_ART_COLLIDER' and isinstance(blocker.get('path'),str) and blocker['path'], 'Actual named native peer collision, not road/wing/support failure, must justify refinement.'
        _finish_bounds(blocker.get('bounds'),'actual native seed blocker')
    rejected={'OUTSIDE_ORIGINAL_VILLAGE_REGION','ACTUAL_CLASSIFIED_DRY_FOOTPRINT_OR_FREEBOARD','ACTUAL_49_POINT_TERRAIN_RANGE','ACTUAL_SOURCE_NATURAL_PART_SUPPORT_OR_MESH_EXPOSURE','ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE','ACTUAL_WING_CLEARANCE','ACTUAL_OTHER_ART_COLLIDER_OVERLAP'}
    for i,row in enumerate(attempts):
        if i>=base:
            assert village, 'No harbor or natural local refinement.'
            seed_index,offset_index=divmod(i-base,196);seed=seeds[seed_index];offset=FINISH_SCHEMA3_REFINEMENT_OFFSETS[offset_index];orientation=seed['orientation'];candidate=seed['trial']%960;parcel=candidate>=192
            center=_finish_vector(seed['center']);expected=(center[0]+offset[0],0,center[2]+offset[2]);radius=math.hypot(expected[0]-hint[0],expected[2]-hint[2]);phase='NATIVE_FLAT_PEER_PARCEL_HALF_METRE_LOCAL_REFINEMENT'
            assert type(row.get('refinementSeedTrial')) is int and row['refinementSeedTrial']==seed['trial'] and _finish_vector(row.get('refinementOffset'),label='exact local parcel offset')==offset, 'Each local trial requires the exact measured seed and ordered half-metre offset.'
            actual_center,_=_finish_bounds(row.get('bounds'),'actual refined trial');assert math.hypot(actual_center[0]-expected[0],actual_center[2]-expected[2])<=.01, 'Actual refined candidate must match its measured seed plus exact local offset.'
        else:
            orientation=i//960;candidate=i%960;parcel=candidate>=192
            phase=('WHOLE_PARENT_QUARTER_TURN_' if orientation else '')+('SAME_VILLAGE_55_TO_128M_PARCEL' if parcel else 'ORIGINAL_NEAR_HINT_192')
            radius=math.sqrt(55**2+(128**2-55**2)*((candidate-192)/767)) if parcel else 0 if candidate==0 else 4*math.sqrt(candidate)
            assert type(row.get('refinementSeedTrial')) is int and row['refinementSeedTrial']==-1 and _finish_vector(row.get('refinementOffset'),label='original zero local offset')==(0,0,0), 'Original base trials cannot claim local refinement.'
        assert type(row.get('trial')) is int and row['trial']==i and type(row.get('orientationIndex')) is int and row['orientationIndex']==orientation and row.get('phase')==phase and abs(_finish_finite(row.get('parentYaw'),'source parent yaw')-(yaw+orientation*90))<=.0001 and abs(_finish_finite(row.get('distanceFromHint'),'source parcel radius')-radius)<=.002, 'Exact original/parcel/whole-parent-quarter-turn trial order and radius required.'
        _finish_vector(row.get('rootPosition'),label='actual group trial position');_finish_bounds(row.get('bounds'),'actual group trial')
        assert (row.get('rejectedReason') in (None,'') if i==trial else row.get('rejectedReason') in rejected), 'Only the final measured group trial may be accepted; unknown earlier failure semantics rejected.'
    pose=_finish_pose(group);_finish_near(pose[2],(scale,)*3,.0001,'authored group scale');radians=math.radians(yaw+orientation*90)*.5;_finish_rotation(pose[1],(0,math.sin(radians),0,math.cos(radians)),.001,'authored whole-parent yaw')
    center,extent=_finish_bounds(group.get('bounds'),'authored group');distance=math.hypot(center[0]-hint[0],center[2]-hint[2])
    assert abs(distance-attempts[-1]['distanceFromHint'])<=.01, 'Actual saved authored group must match its recorded bounded hint radius.'
    if trial>=base or trial%960>=192 or trial//960:
        assert village and all(center[a]-extent[a]>=region_center[a]-region_extent[a] and center[a]+extent[a]<=region_center[a]+region_extent[a] for a in (0,2)), 'Extended village parcel stays wholly inside the original INPUT region.'
    if trial>=base:assert distance<=128.002, 'Accepted local refinement cannot exceed the original128m hint bound.'
    return orientation


def _finish_bank_frame(index):
    f=lambda value:struct.unpack('<f',struct.pack('<f',value))[0]
    magnitude=f(math.sqrt(f(200*200+400*400)));along=(f(200/magnitude),0,f(-400/magnitude));right=(along[2],0,-along[0]);side=-1 if index%2==0 else 1
    t=f(f(.37)+f(f((index//2)/11)*f(.61)));station=(f(-800+f(200*t)),0,f(-50+f(-400*t)))
    return station,along,right,side


def _finish_bank_candidate(index, trial, opposite=False):
    # Match the existing Unity float32 authoring recipe, not new placement.
    f=lambda value:struct.unpack('<f',struct.pack('<f',value))[0]
    station,along,right,side=_finish_bank_frame(index);side=-side if opposite else side;hint=tuple(f(station[a]+f(f(right[a]*side)*49)) for a in range(3))
    radius=0 if trial==0 else f(2*f(math.sqrt(trial))) if trial<96 else f(math.sqrt(f(400+f(1904*f((trial-96)/383)))))
    angle=f(trial*f(2.399963));cosine=f(math.cos(angle));sine=f(abs(f(math.sin(angle))))
    point=tuple(f(f(hint[a]+f(f(along[a]*cosine)*radius))+f(f(f(right[a]*side)*sine)*min(radius,8))) for a in range(3))
    return hint,point,radius


def _finish_bank_inside_fringe(index,hint,candidate):
    f=lambda value:struct.unpack('<f',struct.pack('<f',value))[0]
    dx,dz=f(candidate[0]-hint[0]),f(candidate[2]-hint[2]);side=-1 if index%2==0 else 1;magnitude=f(math.sqrt(200*200+400*400));ax,az=f(200/magnitude),f(-400/magnitude)
    longitudinal=f(f(dx*ax)+f(dz*az));outward=f(f(dx*f(az*side))+f(dz*f(-ax*side)))
    return abs(longitudinal)<=f(52.001) and f(-4.001)<=outward<=f(12.001)


def _finish_bank_near_margin_seeds(attempts, name):
    eligible=[]
    for row in attempts[:480]:
        blocker=row.get('blocker')
        if row.get('supportFound') is not True or row.get('boundsMeasured') is not True or row.get('rejectedReason')!='ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE' or not isinstance(blocker,dict) or blocker.get('kind')!='ROAD_GRAPH_ENVELOPE':continue
        ground_range=_finish_finite(row.get('range'),'bank seed support');allowed=_finish_finite(row.get('allowedRange'),'bank seed support threshold');creek=_finish_finite(row.get('minimumCreekDistance'),'bank seed creek distance');margin=_finish_finite(blocker.get('margin'),'original road graph margin');distance=_finish_finite(blocker.get('distance'),'actual road graph distance')
        if ground_range<=allowed and creek>=.25 and margin>0 and 0<margin-distance<=1:
            assert isinstance(blocker.get('path'),str) and blocker['path'], 'Near-margin bank seed requires the actual named graph edge.'
            eligible.append((margin-distance,row['trial'],dict(name=name,trial=row['trial'],point=row['point'])))
    return [entry[2] for entry in sorted(eligible,key=lambda entry:entry[:2])[:16]]


def _finish_schema3_bank(bank):
    assert isinstance(bank,dict) and bank.get('name')==FINISH_SCHEMA3_EXTRA[2] and bank.get('scope')=='SOURCE_OWNED_VISIBLE_STONE_EMBEDDED_AT_RETAINED_TWELVE_METRE_URBAN_BANK_WITH_UNCHANGED_CREEK_TERRAIN_ROADS' and all(type(bank.get(k)) is int and bank[k]==v for k,v in (('stones',24),('acceptedParts',24),('rejectedParts',0))) and bank.get('failures')==[] and all(bank.get(k) is True for k in ('passed','creekCoreClear','roadsObjectivesAndWingsClear','actualMeshSupport')), 'All24 actual same-bank source stones must pass; no rejected or partial bank.'
    parts=bank.get('parts');rows=bank.get('trials');seeds=bank.get('refinementSeeds');assert isinstance(parts,list) and len(parts)==24 and isinstance(rows,list) and isinstance(seeds,list) and all(isinstance(seed,dict) for seed in seeds), 'Exact24 source parts and complete native bank trial/seed history required.'
    source=FINISH_PRIVATE+'Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Cliffs/';ordered_seeds=[];original_rows=[];alternate_rows=[];ordered_alternates=[]
    alternates=bank.get('alternateParcels');assert isinstance(alternates,list) and len(alternates)<=24 and all(isinstance(parcel,dict) for parcel in alternates) and len({parcel.get('name') for parcel in alternates})==len(alternates) and type(bank.get('originalAcceptedBeforeAlternate')) is int and bank['originalAcceptedBeforeAlternate']==24-len(alternates), 'Only exact genuinely exhausted original-source opposite parcels may follow all original24 searches; accepted original poses remain reserved.'
    reasons={'ACTUAL_TERRAIN_SUPPORT_RANGE','ACTUAL_CREEK_CORE_CLEARANCE','ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE','ACTUAL_WING_CLEARANCE','ACTUAL_ACCEPTED_BANK_STONE_OVERLAP','ACTUAL_OTHER_ART_COLLIDER_OVERLAP','ACTUAL_PRIMARY_MESH_EXPOSURE_OR_SUPPORT','SAME_BANK_LOCAL_REFINEMENT_OUTSIDE_FOUR_METRE_FRINGE'}
    for index,part in enumerate(parts):
        name='Owned source village bank stone '+str(index+1);trial=part.get('trial');height=_finish_finite(part.get('modelHeight'),'bank actual static model height');allowed=_finish_finite(part.get('maximumAllowedSupportRange'),'bank unchanged support range');local_seeds=[seed for seed in seeds if seed.get('name')==name];ordered_seeds.extend(local_seeds)
        selected=[parcel for parcel in alternates if parcel.get('name')==name];parcel=selected[0] if selected else None;alternate_start=480+196*len(local_seeds)
        assert part.get('name')==name and part.get('region')=='village' and part.get('source')==source+'RockFormation_0'+str(index%4+1)+'.prefab' and part.get('accepted') is True and part.get('failure') in (None,'') and type(trial) is int and len(local_seeds)<=16 and 0<=trial<alternate_start+(480 if parcel else 0) and abs(height-(6+index%3))<=.002 and abs(allowed-height*.28)<=.001 and part.get('primaryGeometryMethod')=='EXISTING_STATIC_MODEL_BOUNDS_WITH_PRIMARY_MESH_EXPOSURE', 'Exact original bank source/name/height/method and seed-count-bounded search required.'
        assert all(type(seed.get('trial')) is int and 0<=seed['trial']<480 for seed in local_seeds), 'Local bank seed must refer to an original integer trial.'
        attempts=[row for row in rows if row.get('name')==name];assert len(attempts)==trial+1, 'Every actual bank trial must be retained.'
        original_rows.extend(attempts[:alternate_start]);alternate_rows.extend(attempts[alternate_start:])
        if parcel:
            assert trial>=alternate_start and all(row.get('rejectedReason') in reasons for row in attempts[:alternate_start]), 'Only a preserved original source instance may change banks after every original/local candidate actually rejected.'
            station,_,_,side=_finish_bank_frame(index);mirrored,_,_=_finish_bank_candidate(index,0,True);original_hint,_,_=_finish_bank_candidate(index,0)
            assert type(parcel.get('firstTrial')) is int and parcel['firstTrial']==alternate_start and type(parcel.get('attempts')) is int and parcel['attempts']==trial-alternate_start+1 and _finish_finite(parcel.get('originalSide'),'original bank side')==side, 'Exact same-station opposite trial start/count/side required.'
            for key,expected in (('station',station),('originalHint',original_hint),('alternateHint',mirrored)):_finish_near(_finish_vector(parcel.get(key)),expected,.002,'exact opposite parcel '+key)
            ordered_alternates.append(parcel)
        assert local_seeds==(_finish_bank_near_margin_seeds(attempts,name) if trial>=480 else []), 'Only the exact up-to16 original flat/creek-clear graph-near-margin candidates, sorted deficit then trial, may seed refinement.'
        center,extent=_finish_bounds(part.get('bounds'),'actual bank stone');half=max(.5,min(5,min(extent[0]*2,extent[2]*2)*.3));hint,_,_=_finish_bank_candidate(index,0)
        _finish_near(_finish_vector(part.get('hint')),hint,.002,'same-bank original hint')
        for number,row in enumerate(attempts):
            opposite=number>=alternate_start;local=480<=number<alternate_start
            if opposite:
                assert parcel is not None, 'No undeclared opposite-bank trials.'
                active_hint,candidate,search=_finish_bank_candidate(index,number-alternate_start,True);expected_phase='OPPOSITE_BANK_SAME_STATION_0_TO_48M_480'
            elif not local:
                active_hint,candidate,search=_finish_bank_candidate(index,number);expected_phase='ORIGINAL_96' if number<96 else 'SAME_BANK_ALONG_20_TO_48M_384'
            else:
                active_hint=hint;seed_index,offset_index=divmod(number-480,196);seed=local_seeds[seed_index];delta=FINISH_SCHEMA3_REFINEMENT_OFFSETS[offset_index];seed_point=_finish_vector(seed['point']);candidate=tuple(a+b for a,b in zip(seed_point,delta));_,_,search=_finish_bank_candidate(index,seed['trial']);expected_phase='SAME_BANK_NEAR_MARGIN_HALF_METRE_FOUR_METRE_FRINGE'
            assert type(row.get('oppositeBankTrial')) is int and row['oppositeBankTrial']==(number-alternate_start if opposite else -1), 'Exact declared opposite radial trial identity required.'
            _finish_near(_finish_vector(row.get('parcelHint')),active_hint,.002,'original or mirrored trial hint')
            if local:assert type(row.get('refinementSeedTrial')) is int and row['refinementSeedTrial']==seed['trial'] and _finish_vector(row.get('refinementOffset'))==delta, 'Each bank refinement must match its exact ranked seed and196 ordered half-metre offsets.'
            else:assert type(row.get('refinementSeedTrial')) is int and row['refinementSeedTrial']==-1 and _finish_vector(row.get('refinementOffset'))==(0,0,0), 'Original/opposite radial trial cannot claim local refinement.'
            assert row.get('name')==name and type(row.get('trial')) is int and row['trial']==number and row.get('phase')==expected_phase and abs(_finish_finite(row.get('searchRadius'),'bank search radius')-search)<=.002 and abs(_finish_finite(row.get('allowedRange'),'bank trial range')-allowed)<=.001 and abs(_finish_finite(row.get('halfFootprint'),'bank footprint')-half)<=.002, 'Exact ordered same-bank trial/source support recipe required.'
            _finish_near(_finish_vector(row.get('point')),candidate,.002,'actual bank trial point');assert type(row.get('supportFound')) is bool and type(row.get('boundsMeasured')) is bool, 'Actual support query and measured-bounds flags required.'
            reason=row.get('rejectedReason');assert (reason in (None,'') if number==trial else reason in reasons), 'Only the final bank trial may pass; unknown earlier failures rejected.'
            if local:assert _finish_bank_inside_fringe(index,hint,candidate)==(reason!='SAME_BANK_LOCAL_REFINEMENT_OUTSIDE_FOUR_METRE_FRINGE'), 'Every actual refined candidate preserves the exact approved four-metre fringe rejection.'
            if reason=='SAME_BANK_LOCAL_REFINEMENT_OUTSIDE_FOUR_METRE_FRINGE':assert local, 'Only actual local refinement may record the exact four-metre fringe rejection.'
            elif reason!='ACTUAL_TERRAIN_SUPPORT_RANGE':
                assert row['supportFound'] is True and row['boundsMeasured'] is True and 0<=_finish_finite(row.get('range'),'actual bank support')<=allowed, 'Actual successful ground support must precede creek/road/wing/peer/mesh acceptance.'
                _finish_bounds(row.get('bounds'),'actual measured bank trial')
        assert math.hypot(center[0]-candidate[0],center[2]-candidate[2])<=.01 and abs(_finish_finite(part.get('candidateDistanceFromHint'),'bank offset distance')-math.hypot(candidate[0]-hint[0],candidate[2]-hint[2]))<=.002 and part.get('candidatePhase')==expected_phase, 'Saved bank bounds center must match its exact native candidate, never the prefab pivot.'
        if 480<=trial<alternate_start:assert _finish_bank_inside_fringe(index,hint,candidate), 'Accepted local bank refinement cannot exceed the approved52m along/−4..12m outward fringe or its exact4m seed offset.'
        assert _finish_finite(attempts[-1].get('minimumCreekDistance'),'actual accepted bank creek clearance')>=.25 and abs(_finish_finite(part.get('supportHeightRange'),'accepted bank range')-attempts[-1]['range'])<=.002, 'Actual creek-core clearance and original support threshold cannot be waived.'
    assert original_rows+alternate_rows==rows and ordered_seeds==seeds and ordered_alternates==alternates, 'Unknown/reordered bank rows/seeds or alternate-before-all-originals sequence rejected.'
    return bank


def _finish_composition_v3(clean, candidate, integration, prior, native_run, saved_plan_run=None):
    assert native_run is not None, 'Schema3 requires both actual native world and saved-plan cases.'
    require_rounded_world_native_pass(Path(native_run))
    require_schema3_saved_plan(clean,saved_plan_run or native_run,candidate,integration,prior)
    _finish_quay_imports(clean,integration,prior)
    _finish_composition_v2(clean,integration,prior)
    finish=integration['finish'];_finish_assembly_material_imports(clean,prior,finish.get('assemblyMaterials'))
    groups=finish.get('authoredGroups');assert isinstance(groups,list) and len(groups)==5 and [g.get('name') for g in groups]==[p[0] for p in FINISH_SCHEMA3_GROUPS], 'Exact five ordered authored source groups required.'
    counts=[];source_fields=[];total=0
    for group,(name,region,layout_index,scale,yaw) in zip(groups,FINISH_SCHEMA3_GROUPS):
        path,digest,instances,_=FINISH_ASSEMBLY_LAYOUTS[layout_index];layout=json.loads((clean/path).read_text(encoding='utf-8-sig'))
        assert group.get('region')==region and group.get('layout')==path and group.get('layoutSha256')==digest and group.get('instances')==instances and all(group.get(k) is True for k in ('passed','sourceLocalPosesExact','roadObjectiveAndWingBuffersClear','otherArtClear')), 'Actual source group identity/support/buffer gates failed.'
        _finish_schema3_group_search(clean,prior,group,(name,region,layout_index,scale,yaw),finish.get('ridgeComposition'))
        assert type(group.get('supportSamples')) is int and group['supportSamples']>=49, 'Actual bounded source group trial/support samples required.'
        if layout_index==0:assert group['supportSamples']==49 and 0<=_finish_finite(group.get('supportHeightRange'),'garden support')<=.6 and _finish_finite(group.get('minimumFreeboard'),'garden freeboard')>=1.5, 'Actual original garden court must have49dry samples.'
        parts=group.get('parts');assert isinstance(parts,list) and len(parts)==instances and len({p.get('sceneInstanceId') for p in parts})==instances, 'Every ordered actual source child required.'
        group_counts=[0,0,0]
        for part,source in zip(parts,layout['instances']):
            assert all(part.get(a)==source.get(b) for a,b in (('sceneInstanceId','sceneInstanceId'),('source','prefab'),('guid','prefabGuid'),('sourceSha256','sourceSha256'),('sourceMetaSha256','sourceMetaSha256'))) and part.get('sourcePoseExact') is True and part.get('visibleAndSupported') is True and type(part.get('active')) is bool, 'Actual authored child source/owner/TRS/support changed.'
            _finish_source(clean,prior,part['source'],part['sourceSha256'],part['sourceMetaSha256'],part['guid']);_finish_near(_finish_vector(part.get('sourcePosition')),_finish_vector(source.get('position')),.0001,'authored source position');_finish_near(_finish_vector(part.get('sourceScale')),_finish_vector(source.get('scale')),.0001,'authored source scale');_finish_rotation(_finish_vector(part.get('sourceRotation'),'xyzw'),_finish_vector(source.get('rotation'),'xyzw'),.005,'authored source rotation');_finish_bounds(part.get('bounds'),'authored child');_finish_finite(part.get('supportTranslationY'),'actual separate support carrier')
            observed=_finish_schema3_overrides(source,part.get('overrideEvidence'));group_counts=[a+b for a,b in zip(group_counts,observed)];_finish_geometry_sources(clean,prior,part.get('geometry'));total+=1
        counts.append(group_counts);source_fields.append(sum(len(source['overrides'])for source in layout['instances']))
    assert total==335, 'All335 original source children required.'
    _finish_schema3_override_totals(counts,source_fields)
    ridge=finish.get('ridgeComposition');assert isinstance(ridge,dict) and all(ridge.get(k) is True for k in ('passed','sourceIdentitiesPreserved','countsPreserved','roadStartObjectiveAndWingBuffersClear','actualMeshExposureAndSupport')) and [ridge.get(k) for k in ('alpineRocks','nestRoostTrees','underbrush')]==[24,8,140] and len(ridge.get('parts',[]))==172, 'Exact same172 source roots/native ridge mesh proof required.'
    bank=_finish_schema3_bank(finish.get('retainingBank'))
    for part in ridge['parts']+bank['parts']:
        assert all(part.get(k) is True for k in ('sourceIdentityExact','roadStartObjectiveClear','wingClear','otherArtClear','visibleAndSupported')) and type(part.get('meshSamples')) is int and part['meshSamples']>=8 and type(part.get('exposedSamples')) is int and 0<part['exposedSamples']<=part['meshSamples'], 'Actual visible supported source mesh records required.'
        _finish_source(clean,prior,part.get('source'),part.get('sourceSha256'),part.get('sourceMetaSha256'),part.get('sourceGuid'));_finish_vector(part.get('after'));_finish_vector(part.get('scale'));_finish_vector(part.get('rotation'),'xyzw');_finish_bounds(part.get('bounds'),'ridge/bank');_finish_geometry_sources(clean,prior,part.get('geometry'))
    quay=finish.get('harborForecourt');cargo=finish.get('harborCargo')
    _finish_schema3_quay(quay)
    assert isinstance(cargo,dict) and cargo.get('name')==FINISH_SCHEMA3_EXTRA[1] and all(cargo.get(k) is True for k in ('passed','protectedHullExact','departureClear')) and [cargo.get(k) for k in ('deckPlanks','hullSupports','freight','canopies')]==[6,4,4,1] and len(cargo.get('parts',[]))==15 and len(cargo.get('hullContacts',[]))==4 and _finish_finite(cargo.get('minimumDeckFreeboard'),'trader cargo freeboard')>=.44 and isinstance(cargo.get('protectedHullGeometry'),str) and cargo['protectedHullGeometry'], 'Actual15source-root cargo deck/support/canvas/freight and protected original hull required.'
    assert all(c.get('passed') is True for c in quay['deckContacts']+cargo['hullContacts']) and all(c.get('baseContact') is True and c.get('deckContact') is True for c in quay['pierContacts']), 'Actual native deck/hull/bed contact chains failed.'
    for part in quay['parts']+quay['decorations']+cargo['parts']:
        _finish_source(clean,prior,part.get('source'),part.get('sourceSha256'),part.get('sourceMetaSha256'),part.get('guid'));_finish_pose(part);_finish_bounds(part.get('bounds'),'actual harbor source');_finish_geometry_sources(clean,prior,part.get('geometry'))
    _finish_schema3_palette(clean,candidate,integration,prior)


def candidate_finish_paths(clean, candidate, integration, prior, native_run=None, saved_plan_run=None):
    """Validate the two exact new molten presentation dependencies of a passed world."""
    folder = 'Assets/TruckDragonKingdom/VisualCandidates/' + candidate
    finish = integration.get('finish')
    if finish is None:
        assert not any((clean / (folder + path)).exists() for path in ('/Models/Measured molten bed surface.asset', '/Materials/Finish/Actual molten rock flow.mat')), 'Actual molten outputs require their passed Finish receipt.'
        return []  # Earlier measured candidates did not request Finish composition.
    assert candidate and len(candidate) <= 40 and all(c.isalnum() or c in '-_' for c in candidate), 'Explicit short Finish candidate required.'
    assert integration.get('tag') == candidate and integration.get('prefab') == folder + '/Shared Dragon Kingdom.prefab', 'Finish must belong to this exact saved candidate.'
    assert integration.get('status') == 'PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW', 'Failed Finish candidates remain diagnostic only.'
    assert all(integration.get(field) is True for field in ('sourceBytesPreserved', 'roadsAndCriticalPosesExact', 'originalPhysicalGatesPassed', 'broadVendorHighlandMassRetained')), 'Finish requires actual source/road/physical/highland preservation.'
    assert integration.get('extractionFailures') == [] and type(integration.get('extractionSweepQueries')) is int and integration['extractionSweepQueries'] > 400, 'Finish requires original full extraction gates.'
    assert isinstance(finish, dict) and finish.get('complete') is True and finish.get('failures') == [], 'Complete actual Finish composition required.'
    counts = dict(newStreetfronts=10, refugeHouses=4, marketStalls=6, farmPieces=24, harborProps=16,
                  smallMooredBoats=2, underbrush=140, mushrooms=36, cavernCrystals=12, basalt=24,
                  alpineRocks=24, nestRoostTrees=8, lanterns=28)
    assert all(type(finish.get(field)) is int and finish[field] == count for field, count in counts.items()), 'All334 actual Finish roots including14 houses must remain present.'
    schema = finish.get('compositionSchema', 0)
    assert type(schema) is int and schema in (0, 2, 3), 'Unknown Finish composition schema; preserve for explicit review.'
    if schema == 3:
        _finish_composition_v3(clean,candidate,integration,prior,native_run,saved_plan_run)
    elif schema == 2:
        _finish_composition_v2(clean, integration, prior)
    else:
        assert not finish.get('artistMarket') and not finish.get('mooredShip') and finish.get('traderShips', 0) == 0, 'Legacy334 Finish cannot silently contain new artist/trader roots.'
    water = finish.get('waterways')
    assert isinstance(water, dict) and all(water.get(field) is True for field in ('originalHeightmapsExact', 'originalColliderGeometryExact')), 'Actual molten seating must preserve original heights/collider geometry.'
    assert type(water.get('actualBedSamples')) is int and water['actualBedSamples'] > 100 and water.get('lavaVertices') == water['actualBedSamples'], 'Actual measured molten vertex/bed samples required.'
    gap = water.get('maximumSeatedSurfaceGap')
    assert type(gap) in (int, float) and math.isfinite(gap) and 0 <= gap <= .13, 'Actual molten surface must remain seated within0.13m of measured bed.'
    mesh, material = folder + '/Models/Measured molten bed surface.asset', folder + '/Materials/Finish/Actual molten rock flow.mat'
    assert water.get('candidateMesh') == mesh and water.get('material') == material, 'Only this candidate exact molten mesh/material may be retained.'
    source = _outer_path(water.get('sourceMesh'))
    assert source == 'Assets/TruckDragonKingdom/Models/Ashen Crown molten river.asset', 'Retain original real RiverModeler source mesh identity.'
    assert all(prior.get(path) and (clean / path).is_file() and _forest_hash(clean / path) == prior[path] for path in (source, source + '.meta')), 'Original molten source mesh/GUID bytes must match the frozen intake.'
    selected = [clean / (path + suffix) for path in (mesh, material) for suffix in ('', '.meta')]
    assert all(path.is_file() for path in selected), 'Actual saved candidate molten assets and GUID metadata required.'
    guids = [_forest_guid(clean / (path + '.meta')) for path in (mesh, material)]
    assert len(set(guids)) == 2 and _forest_guid(clean / (source + '.meta')) not in guids, 'New molten presentation GUIDs must preserve original mesh identity.'
    prefab = (clean / integration['prefab']).read_text(encoding='utf-8-sig')
    assert all(re.search(r'guid:\s*' + re.escape(guid) + r'\b', prefab) for guid in guids), 'Actual saved candidate must reference both exact molten output GUIDs.'
    return selected


def candidate_coast_paths(clean, candidate, integration, prior):
    """Validate actual attached Vista data and exact new RiverModeler presentation."""
    assert candidate and len(candidate) <= 40 and all(c.isalnum() or c in '-_' for c in candidate), 'Explicit short coastal world candidate required.'
    folder = 'Assets/TruckDragonKingdom/VisualCandidates/' + candidate
    mesh = folder + '/Models/Actual RiverModeler coastal sea.asset'
    material_folder = folder + '/Materials/Coast'
    materials = [material_folder + '/Terrain ' + dimensions + '.mat' for dimensions in ('800x800', '800x1600', '1600x800')]
    materials.append(material_folder + '/Actual vendor coastal water.mat')
    coast = integration.get('outerBand')
    if coast is None:
        assert not (clean / mesh).exists() and not (clean / material_folder).exists(), 'New coastal outputs require their passed attachment receipt.'
        return []
    assert integration.get('tag') == candidate and integration.get('prefab') == folder + '/Shared Dragon Kingdom.prefab' and integration.get('status') == 'PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW', 'Only this passed actual coastal world may be retained.'
    assert all(integration.get(field) is True for field in ('sourceBytesPreserved', 'roadsAndCriticalPosesExact', 'originalPhysicalGatesPassed', 'broadVendorHighlandMassRetained')), 'Coast requires actual original world/road/source/physical gates.'
    assert integration.get('extractionFailures') == [] and type(integration.get('extractionSweepQueries')) is int and integration['extractionSweepQueries'] > 400, 'Coast requires actual original extraction gates.'
    assert isinstance(coast, dict) and coast.get('status') == 'ATTACHED_REAL_VISTA_COAST_REQUIRES_FULL_WORLD_RENDER_AND_FLIGHT_REVIEW', 'Rejected/incomplete coastal attachment remains diagnostic only.'
    assert all(coast.get(field) is True for field in ('coreHeightmapsExact', 'coreColliderGeometryExact', 'sourceBytesPreserved', 'duplicateLakeRenderingReplaced')), 'Coast must preserve actual core/source geometry and retire duplicate lake rendering.'
    assert type(coast.get('outerTerrains')) is int and coast['outerTerrains'] == 12 and type(coast.get('totalTerrains')) is int and coast['totalTerrains'] == 16 and type(coast.get('seaVertices')) is int and coast['seaVertices'] > 0 and coast.get('seaLevel') == -2, 'Actual twelve outer/sixteen total terrains and vendor sea geometry required.'
    if (integration.get('finish') or {}).get('compositionSchema') in (2,3):
        assert coast.get('exactStraightSeaGeometry') is True and all(type(coast.get(field)) is int and coast[field] == 0 for field in ('invertedSeaTriangles','invalidSeaVertices')), 'Schema2 requires actual noninverted finite straight native sea geometry.'
        assert all(type(coast.get(field)) is int and coast[field] >= 3 for field in ('seaRows','seaColumns')) and coast['seaRows']*coast['seaColumns'] == coast['seaVertices'], 'Exact native rectangular sea row/column/vertex counts required.'
        assert 0 <= _finish_finite(coast.get('maximumSeaRowDrift'),'sea row drift') < .15 and _finish_finite(coast.get('minimumSeaTriangleArea'),'sea triangle area') > .01, 'Actual native sea row drift/triangle area gates failed.'
        center,extent=_finish_bounds(coast.get('actualSeaBounds'),'native sea')
        assert abs(center[0]-extent[0]+6200)<1 and abs(center[0]+extent[0]-6200)<1 and abs(center[2]-extent[2]+5400)<.02 and abs(center[2]+extent[2]-7000)<.02 and abs(center[1]+2)<=.001 and extent[1]<=.001, 'Actual finite native sea bounds must retain both original12.4km spans and water level.'

    tag = coast.get('tag')
    assert isinstance(tag, str) and 0 < len(tag) <= 40 and all(c.isalnum() or c in '-_' for c in tag), 'Exact retained outer tag required.'
    outer = 'Assets/TruckDragonKingdom/ReviewCandidates/' + tag
    source_prefab, source_receipt = outer + '/Outer Vista Band.prefab', outer + '/Outer Vista Receipt.json'
    assert coast.get('sourcePrefab') == source_prefab and coast.get('sourceReceipt') == source_receipt and coast.get('seaMesh') == mesh and coast.get('seaMaterial') == materials[-1], 'Exact retained outer source and candidate sea paths required.'
    digest = coast.get('sourceReceiptSha256')
    if isinstance(digest, str):
        digest = digest.replace('-', '').lower()  # V187 BitConverter form; exact bytes still required.
    assert isinstance(digest, str) and re.fullmatch('[0-9a-f]{64}', digest) and prior.get(source_receipt) == digest and (clean / source_receipt).is_file() and _forest_hash(clean / source_receipt) == digest, 'Actual retained outer receipt must match the frozen native SHA256.'
    proof = json.loads((clean / source_receipt).read_text(encoding='utf-8-sig'))
    assert proof.get('status') == 'MEASURED_SCENERY_CANDIDATE_REQUIRES_RENDER_AND_WORLD_ATTACHMENT_REVIEW' and proof.get('prefab') == source_prefab and proof.get('heightCallbacks') == 12 and proof.get('uniqueHeightTiles') == 12 and proof.get('validationIssues') == [] and all(proof.get(key) is True for key in ('sourceBytesPreserved', 'temporaryObjectsRemoved', 'originalInnerNotGenerated')), 'Actual passed twelve-tile outer proof required.'
    seams = coast.get('seams', {})
    assert seams.get('issues') == [], 'Attached outer seams must pass.'
    for field, minimum in (('innerSamples', 2000), ('outerSamples', 2000), ('bedSamples', 6000)):
        assert type(seams.get(field)) is int and seams[field] >= minimum, 'Actual attached seam/bed coverage required.'
    for field in ('innerMaximumError', 'outerMaximumError', 'bedMaximumError'):
        value = seams.get(field)
        assert type(value) in (int, float) and math.isfinite(value) and 0 <= value <= .04, 'Actual attached seams/bed must remain within0.04m.'
    expected_tiles = {outer + '/Terrain/Outer ' + str(x) + ' ' + str(z) + '.asset' for x in range(4) for z in range(4) if not (0 < x < 3 and 0 < z < 3)}
    rows = proof.get('terrains', [])
    assert len(rows) == 12 and {row.get('path') for row in rows} == expected_tiles, 'Exact twelve retained TerrainData source references required.'
    proof_dependencies = {row['path']: row['sha256'] for row in proof.get('outputDependencies', [])}
    sources = expected_tiles | {source_prefab}
    for path in sources:
        for suffix in ('', '.meta'):
            key = path + suffix
            assert key in proof_dependencies and prior.get(key) == proof_dependencies[key] and (clean / key).is_file() and _forest_hash(clean / key) == prior[key], 'Coastal attachment must retain exact original outer data/GUID bytes: ' + key
    assets = [mesh] + materials
    selected = [clean / (path + suffix) for path in assets for suffix in ('', '.meta')]
    selected.append(clean / (material_folder + '.meta'))
    assert all(path.is_file() for path in selected), 'Actual coastal mesh/material/folder GUID files required.'
    assert {path.relative_to(clean).as_posix() for path in (clean / material_folder).rglob('*') if path.is_file()} == {path + suffix for path in materials for suffix in ('', '.meta')}, 'Only the four exact coastal materials may be retained in the new folder.'
    output_guids = [_forest_guid(clean / (path + '.meta')) for path in assets]
    assert len(set(output_guids)) == len(output_guids), 'New coast presentation GUID collision.'
    prefab = (clean / integration['prefab']).read_text(encoding='utf-8-sig')
    assert all(re.search(r'guid:\s*' + re.escape(guid) + r'\b', prefab) for guid in output_guids), 'Actual saved coastal world must reference every exact new sea/terrain material asset.'
    terrain_refs = re.findall(r'm_TerrainData:\s*\{[^}]*guid:\s*([0-9a-f]{32})[^}]*\}', prefab)
    if (integration.get('finish') or {}).get('compositionSchema')==3:
        clones=_finish_schema3_palette(clean,candidate,integration,prior)
        assert {row['sourceData'] for row in clones}==expected_tiles, 'Palette outer clones must map exactly onto all twelve original retained outer data.'
        outer_guids={row['candidateGuid'] for row in clones}
    else:outer_guids = {_forest_guid(clean / (path + '.meta')) for path in expected_tiles}
    assert len(terrain_refs) == 32 and all(terrain_refs.count(guid) == 2 for guid in outer_guids), 'Saved world must retain all sixteen actual Terrain/Collider pairs including the twelve original outer GUIDs.'
    vendor = 'Assets/RiverModeler/ShaderGraph/Default River.mat'
    native_sources = {row['path']: row['sha256'] for row in proof.get('preservedAssets', [])}
    vendor_digest = prior.get(vendor) or native_sources.get(vendor)
    assert isinstance(vendor_digest, str) and re.fullmatch('[0-9a-f]{64}', vendor_digest) and (not prior.get(vendor) or not native_sources.get(vendor) or prior[vendor] == native_sources[vendor]) and (clean / vendor).is_file() and _forest_hash(clean / vendor) == vendor_digest, 'Actual vendor RiverModeler water material bytes must match frozen input or preserved native source SHA256.'
    shader_pattern = r'm_Shader:\s*\{[^}]*guid:\s*([0-9a-f]{32})[^}]*\}'
    original_shader = re.search(shader_pattern, (clean / vendor).read_text(encoding='utf-8-sig'))
    actual_shader = re.search(shader_pattern, (clean / materials[-1]).read_text(encoding='utf-8-sig'))
    assert original_shader and actual_shader and original_shader.group(1) == actual_shader.group(1), 'Coastal water must retain the actual installed vendor shader.'
    return selected


def main():
    parser = argparse.ArgumentParser(); parser.add_argument('tag')
    parser.add_argument('--include-mode', action='append', default=[], choices=['TruckDragonEscape','TruckHoardHeist','TruckDragonEggDelivery','TruckDragonRiders'])
    parser.add_argument('--scope', choices=['all','foundation','mayhem','candidate','regional','cavern','rounded','outer','material-proof','column-material','forest-proof','forest-material'], default='all')
    parser.add_argument('--candidate', help='Explicit new VisualCandidates or ReviewCandidates child to retain after its recorded gates.')
    parser.add_argument('--receipt-name', default='retained-generated-assets', help='Unique retention receipt label for an additional reviewed output subset.')
    parser.add_argument('--reader-run', help='Explicit later actual passed saved-reader run; requires an immutable continuation seal.')
    parser.add_argument('--continuation-seal', type=Path, help='Explicit honest post-diagnosis/pre-reader candidate+Digger seal.')
    args = parser.parse_args()
    assert args.tag and all(c.isalnum() or c in '-_' for c in args.tag)
    run = ROOT / 'Logs/TruckSports/DragonKingdom' / args.tag
    prior = {p['path']:p['sha256'] for p in json.loads((run/'input-manifest.json').read_text())}
    assert args.receipt_name and all(c.isalnum() or c in '-_' for c in args.receipt_name)
    receipt = run / (args.receipt_name+'.json'); assert not receipt.exists()
    saved_plan_run = None; continuation_evidence = None
    assert bool(args.reader_run) == bool(args.continuation_seal), 'Later readers require both explicit run and immutable continuation seal.'
    if args.reader_run:
        assert args.scope == 'candidate' and args.candidate and not args.include_mode, 'Continuation retains only the exact physical candidate.'
        from DragonKingdomReaderContinuation import validate_continuation
        saved_plan_run,continuation_evidence = validate_continuation(ROOT,CLEAN,run,args.reader_run,args.candidate,args.continuation_seal)
        assert not (run/(args.receipt_name+'-reader-continuation.json')).exists(), 'Preserve prior continuation evidence before any output copy.'
    paths = []
    folders = ['Settings','Animations','Prefabs','Materials','Terrain','FunctionalShoulders','Models','Configuration','Scenes']
    mayhem = ['Mayhem/Configuration','Mayhem/Prefabs','Mayhem/Art']
    if args.scope in FOREST_SCOPES:
        assert not args.include_mode and not args.candidate, 'Retain narrow forest output separately from candidate or mode output.'
        folders = []; paths += forest_paths(CLEAN, run, prior, args.scope)
    elif args.scope == 'outer':
        assert not args.include_mode, 'Retain exact outer scenery separately from gameplay modes.'
        folders = []; paths += outer_paths(CLEAN, run, args.candidate)
    elif args.scope == 'column-material':
        assert not args.include_mode and not args.candidate, 'Retain the selected column material separately.'
        evidence = json.loads((run/'rendered-evidence/MaterialFamilyProof/upgraded.json').read_text(encoding='utf-8-sig'))
        assert evidence['originalsPreserved'] and evidence['repeatByteExact'], 'Preserve original vendor bytes and repeatable configuration.'
        material = CLEAN/'Assets/TruckDragonKingdom/Materials/Environment/6045b0d731b32e345a193bf7cff0b759 URP.mat'
        assert material.is_file() and Path(str(material)+'.meta').is_file()
        folders = []; paths += [material, Path(str(material)+'.meta')]
    elif args.scope == 'material-proof':
        assert not args.include_mode and not args.candidate, 'Retain the controlled material proof separately.'
        evidence = json.loads((run/'rendered-evidence/MaterialFamilyProof/prepared.json').read_text(encoding='utf-8-sig'))
        assert evidence['originalsPreserved'] and evidence['repeatByteExact'], 'Preserve original vendor bytes and repeatable configuration.'
        folders = ['VisualCandidates/MaterialFamilyProof']
        base = CLEAN/'Assets/TruckDragonKingdom'/folders[0]
        for material in ['Observed pre-repair column.mat','Configured original triplanar.mat','Configured existing foliage.mat']:
            assert (base/material).is_file() and (base/(material+'.meta')).is_file(), 'Actual controlled proof materials and GUIDs required.'
        meta = CLEAN/'Assets/TruckDragonKingdom/VisualCandidates.meta'
        if meta.exists(): paths.append(meta)
    elif args.scope == 'rounded':
        assert not args.include_mode, 'Retain the rounded foundation separately from gameplay modes.'
        folders = rounded_folders(CLEAN, args.candidate)
        for parent in ['ReviewCandidates', 'PrivateLicensedEnvironment/DerivedVista']:
            meta = CLEAN / 'Assets/TruckDragonKingdom' / (parent + '.meta')
            if meta.exists(): paths.append(meta)
    elif args.scope == 'cavern':
        assert not args.include_mode, 'Retain the measured cavern massif separately from gameplay modes.'
        folders = cavern_folders(CLEAN, run, args.candidate)
        for parent in ['ReviewCandidates', 'PrivateLicensedEnvironment/DerivedVista']:
            meta = CLEAN / 'Assets/TruckDragonKingdom' / (parent + '.meta')
            if meta.exists(): paths.append(meta)
    elif args.scope == 'regional':
        assert not args.include_mode, 'Retain the measured regional foundation separately from gameplay modes.'
        folders = regional_folders(CLEAN, args.candidate)
        for parent in ['ReviewCandidates', 'PrivateLicensedEnvironment/DerivedVista']:
            meta = CLEAN / 'Assets/TruckDragonKingdom' / (parent + '.meta')
            if meta.exists(): paths.append(meta)
    elif args.scope == 'candidate':
        assert args.candidate and all(c.isalnum() or c in '-_' for c in args.candidate), 'Explicit candidate identifier required.'
        assert not args.include_mode, 'Retain candidate and gameplay modes as separate reviewed subsets.'
        folders = ['VisualCandidates/'+args.candidate]
        assert (CLEAN/'Assets/TruckDragonKingdom'/folders[0]/'Shared Dragon Kingdom.prefab').exists(), 'Only physically accepted saved candidates may be retained.'
        integration = CLEAN/'Assets/TruckDragonKingdom'/folders[0]/'Integration Receipt.json'
        assert integration.is_file() or not any((CLEAN/'Assets/TruckDragonKingdom'/folders[0]/path).exists() for path in ('Models/Measured molten bed surface.asset', 'Materials/Finish/Actual molten rock flow.mat')), 'Actual molten outputs require a passed candidate integration receipt.'
        assert integration.is_file() or not any((CLEAN/'Assets/TruckDragonKingdom'/folders[0]/path).exists() for path in ('Models/Actual RiverModeler coastal sea.asset', 'Materials/Coast')), 'Actual coastal outputs require a passed candidate integration receipt.'
        if integration.exists():
            measured = json.loads(integration.read_text(encoding='utf-8-sig'))
            assert not saved_plan_run or (measured.get('finish') or {}).get('compositionSchema') == 3, 'Explicit continuation supports only the reviewed schema3 reader chain.'
            if args.candidate.startswith('RoundedIntegrated'):
                require_rounded_world_native_pass(run)
                native_receipt = run / 'rendered-evidence' / ('regional-world-' + args.candidate + '.json')
                assert native_receipt.is_file() and json.loads(native_receipt.read_text(encoding='utf-8-sig')) == measured, 'Native passed world evidence must match this exact candidate receipt.'
            assert measured['status'] == 'PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW', 'Rejected world integration remains diagnostic only.'
            assert all(measured[field] for field in ['sourceBytesPreserved','roadsAndCriticalPosesExact','originalPhysicalGatesPassed','broadVendorHighlandMassRetained'])
            assert measured['extractionFailures'] == [] and measured['extractionSweepQueries'] > 400
            paths += candidate_digger_paths(CLEAN, args.candidate, measured)
            paths += candidate_finish_paths(CLEAN, args.candidate, measured, prior, run, saved_plan_run)
            paths += candidate_coast_paths(CLEAN, args.candidate, measured, prior)
        elif args.candidate.startswith('RoundedIntegrated'):
            raise AssertionError('RoundedIntegrated retention requires its actual passed integration receipt and exact native case.')
        surface = CLEAN/'Assets/TruckDragonKingdom'/folders[0]/'Surface Receipt.json'
        if surface.exists():
            measured = json.loads(surface.read_text(encoding='utf-8-sig'))
            assert measured['status'] == 'CAPTURED_VENDOR_SURFACE_REVIEW_REQUIRES_ACTUAL_IMAGE_APPROVAL', 'Rejected surface output remains diagnostic only.'
            assert all(measured[field] is True for field in ['sourceBytesPreserved','physicsHeightsAndWeightsExact','shaderErrorsAbsent']), 'Actual surface source and physics preservation required.'
            assert measured['folder'] == 'Assets/TruckDragonKingdom/'+folders[0] and len(measured['frames']) == 6
            assert all(frame['errors'] == [] for frame in measured['frames']), 'All actual surface comparison frames must compile.'
        parent_meta = CLEAN/'Assets/TruckDragonKingdom/VisualCandidates.meta'
        if parent_meta.exists(): paths.append(parent_meta)
    elif args.scope == 'foundation': folders = ['Settings','Animations','Prefabs/Dragons']
    elif args.scope == 'mayhem': folders = mayhem
    else:
        assert (CLEAN/'Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab').exists(), 'Do not promote partial failed world output; use foundation or mayhem scope.'
        folders += mayhem
        folders += ['Ambience/Generated']
    if args.scope in ['foundation','all']:
        # Narrow project-owned geometry derived from the actual licensed skin.
        # Preserve these exact private outputs with their configured prefabs;
        # never sweep the original licensed source tree into authored output.
        folders += ['PrivateLicensedDragons/DerivedEyelids']
    for folder in folders:
        base = CLEAN / 'Assets/TruckDragonKingdom' / folder
        if not base.exists(): continue
        meta = Path(str(base)+'.meta')
        if meta.exists(): paths.append(meta)
        paths += [p for p in base.rglob('*') if p.is_file() and not p.name.startswith('DragonKingdomFailedNativePreview')]
    for mode in args.include_mode:
        # Preserve generated scene/prefab/audio/fragment/controller output only.
        # Never overwrite a mode's source, tests, docs or concurrent edits.
        base = CLEAN / 'Assets' / mode
        for folder in ['Scenes','Prefabs','Materials','Models','Configuration','Settings','Animations','Controllers','Art','Rendering','PrivateLicensedFragments','PrivateLicensedPotteryFragments']:
            authored = base / folder
            if not authored.exists(): continue
            meta = Path(str(authored)+'.meta')
            if meta.exists(): paths.append(meta)
            paths += [p for p in authored.rglob('*') if p.is_file()]
        # Existing shared weather tooling writes each new sport's private
        # pipeline under the common Rendering directory. Retain only that
        # exact sport prefix, including its Unity GUID metadata.
        rendering = CLEAN / 'Assets/TruckSports/Rendering'
        if rendering.exists():
            paths += [p for p in rendering.glob(mode + ' *') if p.is_file()]
    reviewed = []
    for source in sorted(set(paths)):
        assert source.suffix not in ['.cs','.asmdef'], source
        relative = source.relative_to(CLEAN); key = relative.as_posix(); data = source.read_bytes()
        digest = hashlib.sha256(data).hexdigest(); target = ROOT / relative
        before = target.read_bytes() if target.exists() else None
        before_hash = hashlib.sha256(before).hexdigest() if before is not None else None
        assert before is None or before_hash == digest or prior.get(key) == before_hash, 'Preserve concurrent ROOT customization: '+key
        reviewed.append((target,data,key,digest,before,before_hash))
    records = []
    if args.scope in FOREST_SCOPES or args.scope == 'outer':
        # Validate every concurrent ROOT state and existing backup before the
        # first write. A new receipt cannot overwrite a previous before image.
        for target,data,key,digest,before,before_hash in reviewed:
            assert (target.read_bytes() if target.exists() else None) == before, 'ROOT changed during ' + ('outer' if args.scope == 'outer' else 'forest') + ' retention preflight: ' + key
            backup = run/'before-output-retention'/args.receipt_name/key
            assert before is None or before == data or not backup.exists() or backup.read_bytes() == before, 'Preserve existing ' + ('outer' if args.scope == 'outer' else 'forest') + ' before bytes: ' + key
    for target,data,key,digest,before,before_hash in reviewed:
        if before != data:
            if before is not None:
                backup = run/'before-output-retention'/key
                if args.scope in FOREST_SCOPES or args.scope == 'outer': backup = run/'before-output-retention'/args.receipt_name/key
                backup.parent.mkdir(parents=True,exist_ok=True)
                if args.scope not in FOREST_SCOPES and args.scope != 'outer': backup.write_bytes(before)
                elif not backup.exists():
                    with backup.open('xb') as output: output.write(before)
            target.parent.mkdir(parents=True,exist_ok=True); target.write_bytes(data)
        records.append(dict(path=key,sha256=digest,previousSha256=before_hash))
    receipt.write_text(json.dumps(records,indent=2),encoding='utf-8')
    if continuation_evidence is not None:
        evidence_path = run/(args.receipt_name+'-reader-continuation.json')
        assert not evidence_path.exists(), 'Preserve prior continuation evidence.'
        evidence_path.write_text(json.dumps(continuation_evidence,indent=2)+'\n',encoding='utf-8')
    print('Retained',len(records),'actual authored files; concurrent source/customization preserved.')

if __name__=='__main__': main()
