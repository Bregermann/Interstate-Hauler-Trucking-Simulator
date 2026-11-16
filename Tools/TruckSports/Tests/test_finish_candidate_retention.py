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


class FinishCandidateRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsFinishRetention-')
        self.addCleanup(temporary.cleanup)
        self.root, self.clean = Path(temporary.name) / 'Root', Path(temporary.name) / 'Clean'
        self.candidate = 'RoundedIntegrated16'
        self.folder = 'Assets/TruckDragonKingdom/VisualCandidates/' + self.candidate
        self.mesh = self.folder + '/Models/Measured molten bed surface.asset'
        self.material = self.folder + '/Materials/Finish/Actual molten rock flow.mat'
        self.prefab = self.folder + '/Shared Dragon Kingdom.prefab'
        self.source = 'Assets/TruckDragonKingdom/Models/Ashen Crown molten river.asset'
        self.put(self.source, b'Original actual RiverModeler source')
        self.put(self.source + '.meta', self.meta(1))
        self.prior = {path: hashlib.sha256((self.clean / path).read_bytes()).hexdigest() for path in (self.source, self.source + '.meta')}
        for index, path in enumerate((self.mesh, self.material)):
            self.put(path, b'Actual generated presentation asset')
            self.put(path + '.meta', self.meta(index + 2))
        self.put(self.prefab, ('%YAML 1.1\n  m_Mesh: {fileID: 4300000, guid: ' + format(2, '032x') + ', type: 2}\n  moltenMaterial: {fileID: 2100000, guid: ' + format(3, '032x') + ', type: 2}\n').encode())
        self.put(self.prefab + '.meta', self.meta(4))
        self.receipt = dict(tag=self.candidate, prefab=self.prefab,
                            status='PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW',
                            sourceBytesPreserved=True, roadsAndCriticalPosesExact=True,
                            originalPhysicalGatesPassed=True, broadVendorHighlandMassRetained=True,
                            extractionFailures=[], extractionSweepQueries=500,
                            finish=dict(complete=True, failures=[], newStreetfronts=10, refugeHouses=4,
                                        marketStalls=6, farmPieces=24, harborProps=16, smallMooredBoats=2,
                                        underbrush=140, mushrooms=36, cavernCrystals=12, basalt=24,
                                        alpineRocks=24, nestRoostTrees=8, lanterns=28,
                                        waterways=dict(actualBedSamples=512, lavaVertices=512, maximumSeatedSurfaceGap=.12,
                                                       originalHeightmapsExact=True, originalColliderGeometryExact=True,
                                                       sourceMesh=self.source, candidateMesh=self.mesh, material=self.material)))

    @staticmethod
    def meta(number):
        return ('fileFormatVersion: 2\nguid: ' + format(number, '032x') + '\n').encode()

    def put(self, path, data):
        target = self.clean / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    def select(self):
        return retain.candidate_finish_paths(self.clean, self.candidate, self.receipt, self.prior)

    def native_world_pass(self, run):
        (run / 'tests.xml').write_text('<test-run><test-case fullname="' + retain.ROUNDED_WORLD_NATIVE_TEST + '" result="Passed"/></test-run>')
        evidence = run / 'rendered-evidence' / ('regional-world-' + self.candidate + '.json')
        evidence.parent.mkdir(parents=True, exist_ok=True)
        evidence.write_text(json.dumps(self.receipt))

    def test_only_two_exact_molten_outputs_and_metas_are_selected(self):
        self.put(self.folder + '/Models/Unrelated.asset', b'Not part of the new dependency allowlist')
        selected = {path.relative_to(self.clean).as_posix() for path in self.select()}
        self.assertEqual({path + suffix for path in (self.mesh, self.material) for suffix in ('', '.meta')}, selected)

    def test_failed_candidate_or_unpreserved_physics_cannot_be_retained(self):
        for field, value in (('status', 'REJECTED_CANDIDATE'), ('sourceBytesPreserved', False),
                             ('roadsAndCriticalPosesExact', False), ('originalPhysicalGatesPassed', False),
                             ('broadVendorHighlandMassRetained', False), ('extractionFailures', [dict()])):
            old = self.receipt[field]
            self.receipt[field] = value
            with self.subTest(field=field), self.assertRaises(AssertionError):
                self.select()
            self.receipt[field] = old
        for field in ('originalHeightmapsExact', 'originalColliderGeometryExact'):
            self.receipt['finish']['waterways'][field] = False
            with self.subTest(field=field), self.assertRaisesRegex(AssertionError, 'preserve original'):
                self.select()
            self.receipt['finish']['waterways'][field] = True

    def test_complete_boolean_cannot_hide_missing_actual_finish_roots(self):
        self.receipt['finish']['underbrush'] = 139
        with self.assertRaisesRegex(AssertionError, 'All334'):
            self.select()
        self.receipt['finish']['underbrush'] = 140
        for value in (False, 1, 'true'):
            self.receipt['finish']['complete'] = value
            with self.subTest(value=value), self.assertRaisesRegex(AssertionError, 'Complete actual'):
                self.select()

    def test_unmeasured_or_unseated_molten_geometry_is_rejected(self):
        water = self.receipt['finish']['waterways']
        for field, value in (('actualBedSamples', 100), ('lavaVertices', 511), ('maximumSeatedSurfaceGap', .131), ('maximumSeatedSurfaceGap', float('nan'))):
            old = water[field]
            water[field] = value
            with self.subTest(field=field, value=value), self.assertRaises(AssertionError):
                self.select()
            water[field] = old

    def test_another_candidate_or_broad_extra_output_is_rejected(self):
        for field, value in (('candidateMesh', self.mesh.replace(self.candidate, 'OtherCandidate')),
                             ('material', self.folder + '/Materials/Finish/Another.mat')):
            old = self.receipt['finish']['waterways'][field]
            self.receipt['finish']['waterways'][field] = value
            with self.subTest(field=field), self.assertRaisesRegex(AssertionError, 'exact molten'):
                self.select()
            self.receipt['finish']['waterways'][field] = old

    def test_original_mesh_and_guid_must_match_real_frozen_intake(self):
        for path in (self.source, self.source + '.meta'):
            old = (self.clean / path).read_bytes()
            self.put(path, b'Changed original after the native proof')
            with self.subTest(path=path), self.assertRaisesRegex(AssertionError, 'frozen intake'):
                self.select()
            self.put(path, old)

    def test_missing_guid_collision_or_unreferenced_saved_output_is_rejected(self):
        meta = self.clean / (self.material + '.meta')
        old = meta.read_bytes()
        meta.unlink()
        with self.assertRaisesRegex(AssertionError, 'metadata required'):
            self.select()
        meta.write_bytes(self.meta(2))
        with self.assertRaisesRegex(AssertionError, 'presentation GUIDs'):
            self.select()
        meta.write_bytes(old)
        self.put(self.prefab, b'Prefab does not reference newly measured mesh/material')
        with self.assertRaisesRegex(AssertionError, 'reference both'):
            self.select()

    def test_old_nonfinish_candidates_remain_compatible_but_new_outputs_require_receipt(self):
        self.receipt['finish'] = None
        with self.assertRaisesRegex(AssertionError, 'passed Finish receipt'):
            self.select()
        for path in (self.mesh, self.material):
            (self.clean / path).unlink()
            (self.clean / (path + '.meta')).unlink()
        self.assertEqual([], self.select())

    def test_candidate_retention_validates_finish_before_root_write_and_deduplicates_assets(self):
        run = self.root / 'Logs/TruckSports/DragonKingdom/NativeFinish'
        run.mkdir(parents=True)
        (run / 'input-manifest.json').write_text(json.dumps([dict(path=p, sha256=h) for p, h in self.prior.items()]))
        self.put(self.folder + '/Integration Receipt.json', json.dumps(self.receipt).encode())
        self.native_world_pass(run)
        with patch.object(retain, 'ROOT', self.root), patch.object(retain, 'CLEAN', self.clean), patch.object(sys, 'argv', ['retain', 'NativeFinish', '--scope', 'candidate', '--candidate', self.candidate]):
            retain.main()
        records = json.loads((run / 'retained-generated-assets.json').read_text())
        paths = [record['path'] for record in records]
        self.assertEqual(len(paths), len(set(paths)))
        self.assertEqual(1, paths.count(self.mesh))
        self.assertEqual(1, paths.count(self.material))

    def test_missing_finish_receipt_or_failed_complete_flag_blocks_all_root_writes(self):
        run = self.root / 'Logs/TruckSports/DragonKingdom/NativeFinish'
        run.mkdir(parents=True)
        (run / 'input-manifest.json').write_text(json.dumps([dict(path=p, sha256=h) for p, h in self.prior.items()]))
        with patch.object(retain, 'ROOT', self.root), patch.object(retain, 'CLEAN', self.clean), patch.object(sys, 'argv', ['retain', 'NativeFinish', '--scope', 'candidate', '--candidate', self.candidate]):
            with self.assertRaisesRegex(AssertionError, 'passed candidate integration receipt'):
                retain.main()
            self.receipt['finish']['complete'] = False
            self.put(self.folder + '/Integration Receipt.json', json.dumps(self.receipt).encode())
            self.native_world_pass(run)
            with self.assertRaisesRegex(AssertionError, 'Complete actual Finish'):
                retain.main()
        self.assertFalse((self.root / self.prefab).exists())
        self.assertFalse((run / 'retained-generated-assets.json').exists())


if __name__ == '__main__':
    unittest.main()
