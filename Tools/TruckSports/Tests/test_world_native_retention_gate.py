from pathlib import Path
import json
import hashlib
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain
import test_finish_candidate_retention as finish_fixture


class ActualWorldNativeRetentionGateTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsWorldCaseGate-')
        self.addCleanup(temporary.cleanup)
        self.run = Path(temporary.name)

    def result(self, status='Passed', duplicates=1, other=True):
        root = ET.Element('test-run')
        for _ in range(duplicates):
            ET.SubElement(root, 'test-case', fullname=retain.ROUNDED_WORLD_NATIVE_TEST, result=status)
        if other:
            ET.SubElement(root, 'test-case', fullname='DragonKingdomWorldAdoptionPlanTests.ReadOnlyPlanMapsEveryNonobsoleteOriginalObjectWithoutReplacingCanonicalIdentities', result='Passed')
        ET.ElementTree(root).write(self.run / 'tests.xml')

    def test_exact_single_pass_is_required(self):
        self.result()
        self.assertEqual(self.run / 'tests.xml', retain.require_rounded_world_native_pass(self.run))

    def test_missing_skipped_failed_inconclusive_cases_cannot_be_authorized_by_other_pass(self):
        for status in ('Skipped', 'Failed', 'Inconclusive', 'NotRunnable'):
            with self.subTest(status=status), self.assertRaisesRegex(AssertionError, 'full native rounded-world case must PASS'):
                self.result(status)
                retain.require_rounded_world_native_pass(self.run)
        self.result(duplicates=0)
        with self.assertRaisesRegex(AssertionError, 'missing/skipped/failed/duplicate'):
            retain.require_rounded_world_native_pass(self.run)

    def test_duplicate_passes_or_pass_failed_mix_are_rejected(self):
        self.result(duplicates=2)
        with self.assertRaisesRegex(AssertionError, 'duplicate'):
            retain.require_rounded_world_native_pass(self.run)
        root = ET.parse(self.run / 'tests.xml')
        root.getroot().find('test-case').set('result', 'Failed')
        root.write(self.run / 'tests.xml')
        with self.assertRaisesRegex(AssertionError, 'duplicate'):
            retain.require_rounded_world_native_pass(self.run)

    def test_missing_xml_cannot_authorize_retention(self):
        with self.assertRaisesRegex(AssertionError, 'test result is required'):
            retain.require_rounded_world_native_pass(self.run)

    def test_actual_v187_failed_world_with_passed_identity_plan_is_rejected(self):
        recorded = Path(__file__).parent / 'Fixtures/V187WorldCaseFailure.xml'
        actual = recorded.read_bytes()
        root = ET.fromstring(actual)
        self.assertRegex(root.get('sourceSha256'), r'^[0-9a-f]{64}$')
        cases = list(root.iter('test-case'))
        self.assertEqual(['Failed', 'Passed'], [case.get('result') for case in cases])
        self.assertIn('68211', cases[0].findtext('failure/message'))
        self.assertIn('UInt16', cases[0].findtext('failure/message'))
        original = retain.ROOT / 'Logs/TruckSports/DragonKingdom/V187/tests.xml'
        if original.is_file():
            self.assertEqual(root.get('sourceSha256'), hashlib.sha256(original.read_bytes()).hexdigest())
        (self.run / 'tests.xml').write_bytes(actual)
        with self.assertRaisesRegex(AssertionError, 'full native rounded-world case must PASS'):
            retain.require_rounded_world_native_pass(self.run)

    def world_fixture(self):
        fixture = finish_fixture.FinishCandidateRetentionTests('runTest')
        fixture.setUp()
        self.addCleanup(fixture.doCleanups)
        run = fixture.root / 'Logs/TruckSports/DragonKingdom/NativeWorld'
        run.mkdir(parents=True)
        (run / 'input-manifest.json').write_text(json.dumps([dict(path=p, sha256=h) for p, h in fixture.prior.items()]))
        fixture.put(fixture.folder + '/Integration Receipt.json', json.dumps(fixture.receipt).encode())
        fixture.native_world_pass(run)
        return fixture, run

    def execute(self, fixture):
        with patch.object(retain, 'ROOT', fixture.root), patch.object(retain, 'CLEAN', fixture.clean), patch.object(sys, 'argv', ['retain', 'NativeWorld', '--scope', 'candidate', '--candidate', fixture.candidate]):
            retain.main()

    def test_receipt_flags_true_cannot_override_actual_failed_world_and_no_root_write(self):
        fixture, run = self.world_fixture()
        (run / 'tests.xml').write_bytes((Path(__file__).parent / 'Fixtures/V187WorldCaseFailure.xml').read_bytes())
        self.assertTrue(fixture.receipt['sourceBytesPreserved'])
        self.assertEqual('PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW', fixture.receipt['status'])
        with self.assertRaisesRegex(AssertionError, 'full native rounded-world case must PASS'):
            self.execute(fixture)
        self.assertFalse((fixture.root / fixture.prefab).exists())
        self.assertFalse((run / 'retained-generated-assets.json').exists())

    def test_passed_world_from_other_candidate_cannot_authorize_this_receipt(self):
        fixture, run = self.world_fixture()
        evidence = run / 'rendered-evidence' / ('regional-world-' + fixture.candidate + '.json')
        stale = dict(fixture.receipt, tag='RoundedIntegrated14')
        evidence.write_text(json.dumps(stale))
        with self.assertRaisesRegex(AssertionError, 'this exact candidate receipt'):
            self.execute(fixture)
        self.assertFalse((fixture.root / fixture.prefab).exists())

    def test_valid14_and16_same_candidate_passes_still_retain_exact_selected_assets(self):
        for candidate in ('RoundedIntegrated14', 'RoundedIntegrated16'):
            with self.subTest(candidate=candidate):
                fixture, run = self.world_fixture()
                if candidate != fixture.candidate:
                    old = fixture.candidate
                    new_folder = fixture.folder.replace(old, candidate)
                    (fixture.clean / fixture.folder).rename(fixture.clean / new_folder)
                    for field in ('folder', 'prefab', 'mesh', 'material'):
                        setattr(fixture, field, getattr(fixture, field).replace(old, candidate))
                    fixture.candidate = candidate
                    fixture.receipt = json.loads(json.dumps(fixture.receipt).replace(old, candidate))
                    fixture.put(fixture.folder + '/Integration Receipt.json', json.dumps(fixture.receipt).encode())
                    fixture.native_world_pass(run)
                self.execute(fixture)
                records = json.loads((run / 'retained-generated-assets.json').read_text())
                self.assertTrue((fixture.root / fixture.prefab).is_file())
                self.assertIn(fixture.mesh, [row['path'] for row in records])
                self.assertIn(fixture.material, [row['path'] for row in records])


if __name__ == '__main__':
    unittest.main()
