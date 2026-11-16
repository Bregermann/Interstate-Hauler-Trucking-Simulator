from pathlib import Path
import copy
import hashlib
import json
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain
import test_outer_output_retention as outer_fixture


class CoastCandidateRetentionTests(unittest.TestCase):
    def setUp(self):
        self.seed = outer_fixture.OuterOutputRetentionTests('runTest')
        self.seed.setUp()
        self.addCleanup(self.seed.doCleanups)
        self.seed.receipts()
        self.clean = self.seed.clean
        self.candidate = 'RoundedIntegrated17'
        self.folder = 'Assets/TruckDragonKingdom/VisualCandidates/' + self.candidate
        self.prefab = self.folder + '/Shared Dragon Kingdom.prefab'
        self.mesh = self.folder + '/Models/Actual RiverModeler coastal sea.asset'
        self.material_folder = self.folder + '/Materials/Coast'
        self.materials = [self.material_folder + '/Terrain ' + size + '.mat' for size in ('800x800', '800x1600', '1600x800')]
        self.materials += [self.material_folder + '/Actual vendor coastal water.mat']
        self.source_receipt = self.seed.receipt_path
        paths = [self.source_receipt, self.source_receipt + '.meta', self.seed.prefab, self.seed.prefab + '.meta']
        paths += [row['path'] + suffix for row in self.seed.evidence['terrains'] for suffix in ('', '.meta')]
        self.prior = {path: self.hash(path) for path in paths}
        references = []
        for row in self.seed.evidence['terrains']:
            guid = retain._forest_guid(self.clean / (row['path'] + '.meta'))
            for _ in range(2):
                references.append('  m_TerrainData: {fileID: 15600000, guid: ' + guid + ', type: 2}\n')
        for index in range(4):
            guid = format(60000 + index, '032x')
            for _ in range(2):
                references.append('  m_TerrainData: {fileID: 15600000, guid: ' + guid + ', type: 2}\n')
        for index, path in enumerate([self.mesh] + self.materials):
            guid = format(50000 + index, '032x')
            self.put(path, b'New actual generated sea/presentation asset')
            self.put(path + '.meta', ('fileFormatVersion: 2\nguid: ' + guid + '\n').encode())
            references.append('  Reference: {fileID: 4300000, guid: ' + guid + ', type: 2}\n')
        self.put(self.material_folder + '.meta', b'fileFormatVersion: 2\nguid: 0000000000000000000000000000ffff\n')
        self.put(self.prefab, ('%YAML 1.1\n' + ''.join(references)).encode())
        self.vendor = 'Assets/RiverModeler/ShaderGraph/Default River.mat'
        material = b'%YAML 1.1\n  m_Shader: {fileID: -1, guid: abcdefabcdefabcdefabcdefabcdefab, type: 3}\n'
        self.put(self.vendor, material)
        self.put(self.materials[-1], material)
        self.prior[self.vendor] = self.hash(self.vendor)
        self.receipt = dict(tag=self.candidate, prefab=self.prefab,
                            status='PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW',
                            sourceBytesPreserved=True, roadsAndCriticalPosesExact=True,
                            originalPhysicalGatesPassed=True, broadVendorHighlandMassRetained=True,
                            extractionFailures=[], extractionSweepQueries=500,
                            outerBand=dict(status='ATTACHED_REAL_VISTA_COAST_REQUIRES_FULL_WORLD_RENDER_AND_FLIGHT_REVIEW',
                                           tag=self.seed.candidate, sourcePrefab=self.seed.prefab, sourceReceipt=self.source_receipt,
                                           sourceReceiptSha256=self.hash(self.source_receipt), seaMesh=self.mesh,
                                           seaMaterial=self.materials[-1], outerTerrains=12, totalTerrains=16,
                                           seaVertices=6000, seaLevel=-2, coreHeightmapsExact=True,
                                           coreColliderGeometryExact=True, sourceBytesPreserved=True,
                                           duplicateLakeRenderingReplaced=True, seams=copy.deepcopy(self.seed.evidence['seams'])))

    def put(self, path, data):
        target = self.clean / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    def hash(self, path):
        return hashlib.sha256((self.clean / path).read_bytes()).hexdigest()

    def select(self):
        return retain.candidate_coast_paths(self.clean, self.candidate, self.receipt, self.prior)

    def test_exact_new_coast_outputs_only_without_resweeping_retained_outer_source(self):
        actual = {path.relative_to(self.clean).as_posix() for path in self.select()}
        expected = {path + suffix for path in [self.mesh] + self.materials for suffix in ('', '.meta')}
        expected.add(self.material_folder + '.meta')
        self.assertEqual(expected, actual)
        self.assertEqual(11, len(actual))
        self.assertFalse(any('ReviewCandidates' in path for path in actual))

    def test_failed_world_or_any_original_preservation_failure_blocks_coast(self):
        for field in ('coreHeightmapsExact', 'coreColliderGeometryExact', 'sourceBytesPreserved', 'duplicateLakeRenderingReplaced'):
            self.receipt['outerBand'][field] = False
            with self.subTest(field=field), self.assertRaisesRegex(AssertionError, 'preserve actual'):
                self.select()
            self.receipt['outerBand'][field] = True
        self.receipt['status'] = 'REJECTED_CANDIDATE'
        with self.assertRaisesRegex(AssertionError, 'passed actual coastal'):
            self.select()

    def test_actual_twelve_sixteen_terrain_counts_and_nonempty_real_sea_required(self):
        coast = self.receipt['outerBand']
        for field, value in (('outerTerrains', 11), ('totalTerrains', 15), ('seaVertices', 0), ('seaLevel', 0)):
            old = coast[field]
            coast[field] = value
            with self.subTest(field=field), self.assertRaisesRegex(AssertionError, 'actual|Actual'):
                self.select()
            coast[field] = old

    def test_attached_seams_cannot_relax_source_point04_gate(self):
        self.receipt['outerBand']['seams']['innerMaximumError'] = .041
        with self.assertRaisesRegex(AssertionError, '0.04m'):
            self.select()

    def test_v187_legacy_bitconverter_hash_normalizes_format_but_not_bytes(self):
        digest = self.receipt['outerBand']['sourceReceiptSha256']
        self.receipt['outerBand']['sourceReceiptSha256'] = '-'.join(digest[i:i+2] for i in range(0, 64, 2)).upper()
        self.select()
        self.receipt['outerBand']['sourceReceiptSha256'] = '00-' * 31 + '00'
        with self.assertRaisesRegex(AssertionError, 'frozen native SHA256'):
            self.select()

    def test_stale_source_receipt_or_original_outer_data_guid_bytes_reject(self):
        self.put(self.source_receipt, b'Changed outer receipt after native intake')
        with self.assertRaisesRegex(AssertionError, 'frozen native SHA256'):
            self.select()
        self.seed.receipts()
        source_meta = self.seed.evidence['terrains'][0]['path'] + '.meta'
        self.put(source_meta, b'Changed original TerrainData metadata')
        with self.assertRaisesRegex(AssertionError, 'exact original outer data/GUID'):
            self.select()

    def test_saved_world_must_reference_all_actual_original_outer_terrain_collider_pairs(self):
        text = (self.clean / self.prefab).read_text()
        guid = retain._forest_guid(self.clean / (self.seed.evidence['terrains'][0]['path'] + '.meta'))
        self.put(self.prefab, text.replace(guid, 'f' * 32, 1).encode())
        with self.assertRaisesRegex(AssertionError, 'sixteen actual Terrain/Collider'):
            self.select()

    def test_missing_actual_sea_reference_or_extra_coast_material_is_rejected(self):
        before = (self.clean / self.prefab).read_bytes()
        guid = retain._forest_guid(self.clean / (self.mesh + '.meta'))
        self.put(self.prefab, before.replace(guid.encode(), b'f' * 32))
        with self.assertRaisesRegex(AssertionError, 'reference every exact new'):
            self.select()
        self.put(self.prefab, before)
        self.put(self.material_folder + '/Unreviewed extra.mat', b'No generic material sweep')
        with self.assertRaisesRegex(AssertionError, 'Only the four exact'):
            self.select()

    def test_water_must_retain_actual_supported_vendor_shader_and_source_material_bytes(self):
        self.put(self.materials[-1], b'%YAML 1.1\n m_Shader: {fileID: 4800000, guid: ffffffffffffffffffffffffffffffff, type: 3}\n')
        with self.assertRaisesRegex(AssertionError, 'actual installed vendor shader'):
            self.select()
        self.put(self.materials[-1], (self.clean / self.vendor).read_bytes())
        self.put(self.vendor, b'Vendor original changed')
        with self.assertRaisesRegex(AssertionError, 'vendor RiverModeler water material bytes'):
            self.select()

    def test_vendor_baseline_may_use_exact_native_preserved_source_hash_when_not_in_owned_intake(self):
        expected = self.prior.pop(self.vendor)
        self.seed.evidence['preservedAssets'].append(dict(path=self.vendor, sha256=expected))
        self.seed.evidence['preservedPaths'].append(self.vendor)
        self.seed.receipts()
        self.prior[self.source_receipt] = self.hash(self.source_receipt)
        self.receipt['outerBand']['sourceReceiptSha256'] = self.hash(self.source_receipt)
        self.select()
        self.put(self.vendor, b'Vendor changed after actual native source hash')
        with self.assertRaisesRegex(AssertionError, 'preserved native source SHA256'):
            self.select()

    def test_earlier_candidates_with_no_coast_keep_behavior_but_generated_outputs_require_receipt(self):
        self.receipt['outerBand'] = None
        with self.assertRaisesRegex(AssertionError, 'passed attachment receipt'):
            self.select()
        (self.clean / self.mesh).unlink()
        for path in (self.clean / self.material_folder).iterdir():
            path.unlink()
        (self.clean / self.material_folder).rmdir()
        self.assertEqual([], self.select())


if __name__ == '__main__':
    unittest.main()
