from pathlib import Path
import copy
import hashlib
import json
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain


def digest(data):
    return hashlib.sha256(data).hexdigest()


class OuterOutputRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsOuterRetention-')
        self.addCleanup(temporary.cleanup)
        self.root, self.clean = Path(temporary.name) / 'Root', Path(temporary.name) / 'Clean'
        self.tag, self.candidate = 'OuterNativeTest', 'OuterCoast01'
        self.run = self.root / 'Logs/TruckSports/DragonKingdom' / self.tag
        self.run.mkdir(parents=True)
        self.folder = 'Assets/TruckDragonKingdom/ReviewCandidates/' + self.candidate
        self.private = 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVista/' + self.candidate
        self.source = 'Assets/TruckDragonKingdom/VisualCandidates/RoundedIntegrated14/Shared Dragon Kingdom.prefab'
        self.prefab = self.folder + '/Outer Vista Band.prefab'
        self.receipt_path = self.folder + '/Outer Vista Receipt.json'
        self.prior = {}
        self.next_guid = 1
        originals = [self.source, 'Assets/TruckDragonKingdom/ReviewCandidates/RoundedVista01/Vista Regional Foundation.prefab',
                     'Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab']
        originals += [self.source.rsplit('/', 1)[0] + '/Terrain/Kingdom ' + str(x) + ' ' + str(z) + '.asset' for x in range(2) for z in range(2)]
        self.external = 'Assets/TruckDragonKingdom/Materials/Accepted terrain.mat'
        originals.append(self.external)
        preserved = []
        for path in originals:
            self.asset(path, b'Original unchanged source')
            preserved.extend(self.facts((path, path + '.meta')))
        self.external_guid = retain._forest_guid(self.clean / (self.external + '.meta'))
        tiles, terrain_refs, self.assets = [], [], []
        axis_x, axis_z, sizes = [-2400, -1600, 0, 1600], [-1600, -800, 800, 2400], [800, 1600, 1600, 800]
        for x in range(4):
            for z in range(4):
                if 0 < x < 3 and 0 < z < 3:
                    continue
                path = self.folder + '/Terrain/Outer ' + str(x) + ' ' + str(z) + '.asset'
                self.asset(path, b'\0Actual Unity binary terrain fixture')
                self.assets.append(path)
                guid = retain._forest_guid(self.clean / (path + '.meta'))
                for component in (218, 154):
                    terrain_refs.append('--- !u!' + str(component) + ' &' + str(len(terrain_refs) + 1) + '\n  m_TerrainData: {fileID: 15600000, guid: ' + guid + ', type: 2}\n')
                tiles.append(dict(path=path, resolution=513, position=dict(x=axis_x[x], y=-20, z=axis_z[z]),
                                  size=dict(x=sizes[x], y=600, z=sizes[z]), generatedHeightHash='1' * 64,
                                  coordinatedHeightHash='2' * 64, unchangedInteriorSamples=200000,
                                  minimum=-8, maximum=450, maximumCoordination=20))
        self.asset(self.prefab, ('%YAML 1.1\n' + ''.join(terrain_refs) + '  m_MaterialTemplate: {fileID: 2100000, guid: ' + self.external_guid + ', type: 2}\n').encode())
        self.assets.append(self.prefab)
        for name in ('Terrain graph 0.asset', 'Terrain graph 1.asset', 'Mask graph 0.asset', 'Vendor constant coastal bed.asset', 'Vendor smooth coastal falloff.asset'):
            path = self.private + '/' + name
            self.asset(path, b'%YAML 1.1\nGraph: fixture\n')
            self.assets.append(path)
        for folder in (self.folder, self.folder + '/Terrain', self.private):
            self.put(self.clean, folder + '.meta', self.meta())
        self.put(self.clean, self.receipt_path + '.meta', self.meta())
        dependency_paths = [path + suffix for path in self.assets for suffix in ('', '.meta')]
        dependency_paths += [self.external, self.external + '.meta']
        self.evidence = dict(status='MEASURED_SCENERY_CANDIDATE_REQUIRES_RENDER_AND_WORLD_ATTACHMENT_REVIEW',
                             scope='VISTA_GENERATED_SCENERY_ONLY_NO_NEW_PLAYABLE_AREA', folder=self.folder,
                             privateGraphs=self.private, prefab=self.prefab, source=self.source,
                             sourceBytesPreserved=True, temporaryObjectsRemoved=True, originalInnerNotGenerated=True,
                             validationIssues=[], heightCallbacks=12, uniqueHeightTiles=12,
                             seams=dict(issues=[], innerSamples=4096, outerSamples=8192, bedSamples=8192,
                                        innerMaximumError=.009, outerMaximumError=0, bedMaximumError=.003),
                             preservedPaths=sorted(row['path'] for row in preserved), preservedAssets=preserved,
                             outputDependencies=self.facts(dependency_paths), terrains=tiles,
                             clonedTerrainGraphs=2, clonedMaskGraphs=1)
        self.result('Passed')

    def meta(self):
        self.next_guid += 1
        return ('fileFormatVersion: 2\nguid: ' + format(self.next_guid, '032x') + '\n').encode()

    @staticmethod
    def put(base, path, data):
        target = base / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    def asset(self, path, data):
        self.put(self.clean, path, data)
        self.put(self.clean, path + '.meta', self.meta())

    def facts(self, paths):
        return [dict(path=path, sha256=digest((self.clean / path).read_bytes())) for path in paths]

    def refresh_dependency(self, path):
        for row in self.evidence['outputDependencies']:
            if row['path'] == path:
                row['sha256'] = digest((self.clean / path).read_bytes())

    def result(self, status, name=None):
        (self.run / 'tests.xml').write_text('<test-run><test-case fullname="' + (name or retain.OUTER_NATIVE_TEST) + '" result="' + status + '"/></test-run>')

    def receipts(self):
        data = json.dumps(self.evidence).encode()
        self.put(self.clean, self.receipt_path, data)
        self.put(self.run, 'rendered-evidence/outer-vista-' + self.candidate + '.json', data)

    def select(self):
        self.receipts()
        return retain.outer_paths(self.clean, self.run, self.candidate)

    def execute(self, extra=()):
        self.receipts()
        (self.run / 'input-manifest.json').write_text(json.dumps([dict(path=path, sha256=value) for path, value in self.prior.items()]))
        with patch.object(retain, 'ROOT', self.root), patch.object(retain, 'CLEAN', self.clean), patch.object(sys, 'argv', ['retain', self.tag, '--scope', 'outer', '--candidate', self.candidate, '--receipt-name', 'outer-retained', *extra]):
            retain.main()

    def test_exact_outputs_private_graphs_and_metas_without_other_candidate_sweep(self):
        self.put(self.clean, 'Assets/TruckDragonKingdom/ReviewCandidates/Other/Unreviewed.asset', b'Other candidate')
        self.put(self.clean, 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVista/Other/Graph.asset', b'Other graph')
        selected = {path.relative_to(self.clean).as_posix() for path in self.select()}
        expected = {path + suffix for path in self.assets for suffix in ('', '.meta')}
        expected.update((self.receipt_path, self.receipt_path + '.meta', self.folder + '.meta', self.folder + '/Terrain.meta', self.private + '.meta'))
        self.assertEqual(expected, selected)
        self.execute()
        self.assertFalse((self.root / 'Assets/TruckDragonKingdom/ReviewCandidates/Other').exists())
        self.assertFalse((self.root / self.external).exists())
        self.assertEqual(len(expected), len(json.loads((self.run / 'outer-retained.json').read_text())))

    def test_exact_passed_native_case_is_required(self):
        for status in ('Failed', 'Skipped', 'Inconclusive'):
            with self.subTest(status=status), self.assertRaisesRegex(AssertionError, 'actual outer native test'):
                self.result(status)
                self.select()
        self.result('Passed', 'Unrelated.Test')
        with self.assertRaisesRegex(AssertionError, 'actual outer native test'):
            self.select()

    def test_native_and_asset_receipts_must_match(self):
        self.receipts()
        (self.run / ('rendered-evidence/outer-vista-' + self.candidate + '.json')).write_text('{}')
        with self.assertRaisesRegex(AssertionError, 'receipts must match'):
            retain.outer_paths(self.clean, self.run, self.candidate)

    def test_every_source_cleanup_and_original_exclusion_flag_requires_real_true(self):
        for field in ('sourceBytesPreserved', 'temporaryObjectsRemoved', 'originalInnerNotGenerated'):
            for value in (False, 'true', 1, None):
                with self.subTest(field=field, value=value), self.assertRaisesRegex(AssertionError, 'assertions required'):
                    self.evidence[field] = value
                    self.select()
            self.evidence[field] = True

    def test_twelve_distinct_callbacks_and_measured_seams_are_mandatory(self):
        for field in ('heightCallbacks', 'uniqueHeightTiles'):
            old = self.evidence[field]
            self.evidence[field] = 11
            with self.subTest(field=field), self.assertRaisesRegex(AssertionError, 'Twelve distinct'):
                self.select()
            self.evidence[field] = old
        for field in ('innerMaximumError', 'outerMaximumError', 'bedMaximumError'):
            for value in (.041, float('nan'), -1):
                with self.subTest(field=field, value=value), self.assertRaisesRegex(AssertionError, '0.04m'):
                    self.evidence['seams'][field] = value
                    self.select()
            self.evidence['seams'][field] = 0

    def test_wrong_cell_dimensions_and_unpreserved_inner_source_are_rejected(self):
        self.evidence['terrains'][0]['size']['x'] = 1600
        with self.assertRaisesRegex(AssertionError, 'cell origins'):
            self.select()
        self.evidence['terrains'][0]['size']['x'] = 800
        self.put(self.clean, self.source, b'Changed after real native generation')
        with self.assertRaisesRegex(AssertionError, 'preserved source bytes differ'):
            self.select()

    def test_missing_hash_dependency_or_output_change_cannot_be_retained(self):
        path = self.assets[0]
        self.put(self.clean, path, b'Changed after measurement')
        with self.assertRaisesRegex(AssertionError, 'output bytes differ'):
            self.select()

    def test_external_reference_closure_cannot_expand_beyond_preserved_sources(self):
        path = 'Assets/Other/Unreviewed.mat'
        self.asset(path, b'Unreviewed external asset')
        self.evidence['outputDependencies'] += self.facts((path, path + '.meta'))
        with self.assertRaisesRegex(AssertionError, 'external output references'):
            self.select()

    def test_unexpected_scope_content_and_path_escape_are_rejected(self):
        path = self.private + '/ExtraVendor.asset'
        self.asset(path, b'Not part of generated closure')
        with self.assertRaisesRegex(AssertionError, 'Unexpected/missing'):
            self.select()
        (self.clean / path).unlink()
        (self.clean / (path + '.meta')).unlink()
        self.evidence['outputDependencies'][0]['path'] = 'Assets/../Other.asset'
        with self.assertRaisesRegex(AssertionError, 'Invalid outer asset path'):
            self.select()

    def test_each_terrain_and_collider_must_reference_same_exact_new_data_guid(self):
        text = (self.clean / self.prefab).read_text()
        first = retain._forest_guid(self.clean / (self.assets[0] + '.meta'))
        (self.clean / self.prefab).write_text(text.replace(first, self.external_guid, 1))
        self.refresh_dependency(self.prefab)
        with self.assertRaisesRegex(AssertionError, 'TerrainData GUID'):
            self.select()

    def test_unresolved_yaml_reference_and_guid_collisions_are_rejected(self):
        graph = self.assets[-1]
        self.put(self.clean, graph, b'%YAML 1.1\n  Source: {fileID: 11400000, guid: ffffffffffffffffffffffffffffffff, type: 2}\n')
        self.refresh_dependency(graph)
        with self.assertRaisesRegex(AssertionError, 'Unresolved actual outer YAML'):
            self.select()
        self.put(self.clean, graph, b'%YAML 1.1\nGraph: fixture\n')
        self.refresh_dependency(graph)
        self.put(self.clean, graph + '.meta', (self.clean / (self.assets[0] + '.meta')).read_bytes())
        self.refresh_dependency(graph + '.meta')
        with self.assertRaisesRegex(AssertionError, 'GUID collision'):
            self.select()

    def test_concurrent_root_customization_blocks_whole_batch_before_first_write(self):
        target = self.assets[-1]
        self.put(self.root, target, b'Concurrent ROOT customization')
        with self.assertRaisesRegex(AssertionError, 'concurrent ROOT'):
            self.execute()
        self.assertEqual(b'Concurrent ROOT customization', (self.root / target).read_bytes())
        self.assertFalse((self.root / self.prefab).exists())
        self.assertFalse((self.run / 'outer-retained.json').exists())

    def test_frozen_before_bytes_backed_up_once_and_existing_backup_never_overwritten(self):
        path = self.assets[0]
        before = b'Previous frozen ROOT outer asset'
        self.put(self.root, path, before)
        self.prior[path] = digest(before)
        backup = self.run / 'before-output-retention/outer-retained' / path
        self.put(self.run, backup.relative_to(self.run), b'Earlier preserved before image')
        with self.assertRaisesRegex(AssertionError, 'existing outer before bytes'):
            self.execute()
        self.assertEqual(before, (self.root / path).read_bytes())
        self.assertEqual(b'Earlier preserved before image', backup.read_bytes())
        backup.unlink()
        self.execute()
        self.assertEqual(before, backup.read_bytes())
        self.assertEqual((self.clean / path).read_bytes(), (self.root / path).read_bytes())

    def test_mode_mixing_and_wrong_candidate_are_rejected(self):
        with self.assertRaisesRegex(AssertionError, 'separately'):
            self.execute(extra=('--include-mode', 'TruckDragonRiders'))
        self.receipts()
        with self.assertRaises(AssertionError):
            retain.outer_paths(self.clean, self.run, '../Escape')

    def package(self, suffix='native-cache', name='com.unity.render-pipelines.universal', data=b'Actual native package source'):
        root = self.clean / 'Library/PackageCache' / (name + '@' + suffix)
        root.mkdir(parents=True, exist_ok=True)
        (root / 'package.json').write_text(json.dumps(dict(name=name, version='17.4.0')))
        path = root / 'Editor/AssetVersion.cs'
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        Path(str(path) + '.meta').write_bytes(self.meta())
        return path

    def test_real_package_cache_identity_and_exact_native_hash_resolve_readonly_without_copy(self):
        actual = self.package()
        logical = 'Packages/com.unity.render-pipelines.universal/Editor/AssetVersion.cs'
        rows = [dict(path=logical + suffix, sha256=digest(Path(str(actual) + suffix).read_bytes())) for suffix in ('', '.meta')]
        self.evidence['preservedAssets'] += rows
        self.evidence['preservedPaths'] += [row['path'] for row in rows]
        self.evidence['outputDependencies'] += rows
        self.assertEqual(actual, retain._outer_dependency_file(self.clean, logical, rows[0]['sha256']))
        selected = self.select()
        self.assertTrue(all(path.relative_to(self.clean).as_posix().startswith('Assets/') for path in selected))
        self.execute()
        self.assertFalse((self.root / 'Packages').exists())
        self.assertFalse((self.root / 'Library/PackageCache').exists())

    def test_package_version_selection_requires_matching_sha_and_actual_package_manifest_name(self):
        self.package('first-stale-cache', data=b'Wrong version bytes')
        actual = self.package('second-current-cache')
        logical = 'Packages/com.unity.render-pipelines.universal/Editor/AssetVersion.cs'
        expected = digest(actual.read_bytes())
        self.assertEqual(actual, retain._outer_dependency_file(self.clean, logical, expected))
        (actual.parents[1] / 'package.json').write_text(json.dumps(dict(name='com.unity.unrelated')))
        with self.assertRaisesRegex(AssertionError, 'native SHA256'):
            retain._outer_dependency_file(self.clean, logical, expected)

    def test_package_hash_mismatch_or_logical_path_escape_never_resolves(self):
        self.package()
        logical = 'Packages/com.unity.render-pipelines.universal/Editor/AssetVersion.cs'
        with self.assertRaisesRegex(AssertionError, 'native SHA256'):
            retain._outer_dependency_file(self.clean, logical, '0' * 64)
        for bad in ('Packages/com.unity.render-pipelines.universal/../Elsewhere.cs',
                    'Packages/com.unity.render-pipelines.universal//Editor/AssetVersion.cs',
                    'Packages/com.unity.*/Editor/AssetVersion.cs',
                    'Library/PackageCache/com.unity.render-pipelines.universal@cache/Editor/AssetVersion.cs'):
            with self.subTest(path=bad), self.assertRaises(AssertionError):
                retain._outer_dependency_file(self.clean, bad, '0' * 64)

    def test_actual_embedded_package_remains_readonly_and_cannot_be_selected_as_owned_output(self):
        package = self.clean / 'Packages/com.example.owned'
        package.mkdir(parents=True)
        (package / 'package.json').write_text(json.dumps(dict(name='com.example.owned')))
        actual = package / 'Runtime.cs'
        actual.write_bytes(b'Actual embedded package source')
        logical = 'Packages/com.example.owned/Runtime.cs'
        self.assertEqual(actual, retain._outer_dependency_file(self.clean, logical, digest(actual.read_bytes())))
        with self.assertRaisesRegex(AssertionError, 'Invalid outer asset path'):
            retain._outer_path(logical)

    def test_only_exact_unity_terrain_builtin_lightmap_resource_is_accepted_without_meta(self):
        before = (self.clean / self.prefab).read_bytes()
        exact = b'  m_LightmapParameters: {fileID: 15203, guid: 0000000000000000f000000000000000, type: 0}\n'
        self.put(self.clean, self.prefab, before + exact)
        self.refresh_dependency(self.prefab)
        self.select()
        for wrong in (exact.replace(b'm_LightmapParameters', b'UnreviewedReference'), exact.replace(b'15203', b'15204'), exact.replace(b'type: 0', b'type: 2')):
            self.put(self.clean, self.prefab, before + wrong)
            self.refresh_dependency(self.prefab)
            with self.assertRaisesRegex(AssertionError, 'Unresolved actual outer YAML'):
                self.select()


if __name__ == '__main__':
    unittest.main()
