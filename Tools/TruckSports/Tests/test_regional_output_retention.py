from pathlib import Path
import json
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from PreserveDragonKingdomAuthoring import regional_folders


class RegionalOutputRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsRegionalRetention-')
        self.addCleanup(temporary.cleanup)
        self.clean = Path(temporary.name)
        self.candidate = 'VistaRegionalFoundation01'
        self.folder = 'ReviewCandidates/' + self.candidate
        self.private = 'PrivateLicensedEnvironment/DerivedVista/' + self.candidate
        base = self.clean / 'Assets/TruckDragonKingdom'
        self.receipt_path = base / self.folder / 'Foundation Receipt.json'
        self.receipt_path.parent.mkdir(parents=True)
        (base / self.private).mkdir(parents=True)
        (self.receipt_path.parent / 'Vista Regional Foundation.prefab').write_bytes(b'real saved prefab')
        self.receipt = dict(
            status='MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW',
            candidateFolder='Assets/TruckDragonKingdom/' + self.folder,
            privateGraphsFolder='Assets/TruckDragonKingdom/' + self.private,
            validationIssues=[], sourceBytesPreserved=True,
            originalScenePreserved=True, temporaryObjectsRemoved=True,
            prefab='Assets/TruckDragonKingdom/' + self.folder + '/Vista Regional Foundation.prefab')

    def select(self):
        self.receipt_path.write_text(json.dumps(self.receipt), encoding='utf-8')
        return regional_folders(self.clean, self.candidate)

    def test_only_exact_measured_candidate_and_private_derivatives_are_selected(self):
        self.assertEqual([self.folder, self.private], self.select())

    def test_rejected_generation_cannot_be_retained_as_accepted(self):
        self.receipt['status'] = 'MEASURED_CANDIDATE_REJECTED_BY_PREGRADING_GATES'
        with self.assertRaisesRegex(AssertionError, 'Only measured'):
            self.select()

    def test_private_dependency_scope_cannot_be_expanded(self):
        self.receipt['privateGraphsFolder'] = 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment'
        with self.assertRaisesRegex(AssertionError, 'Private graph scope'):
            self.select()

    def test_source_changes_or_incomplete_cleanup_block_retention(self):
        for field in ('sourceBytesPreserved', 'originalScenePreserved', 'temporaryObjectsRemoved'):
            with self.subTest(field=field):
                self.receipt[field] = False
                with self.assertRaises(AssertionError):
                    self.select()
                self.receipt[field] = True
        self.receipt['validationIssues'] = ['unsupported real terrain']
        with self.assertRaises(AssertionError):
            self.select()

    def test_candidate_identifier_cannot_escape_review_folder(self):
        for candidate in ('', '../Vendor', 'A/B', 'C:\\External', 'A.meta'):
            with self.subTest(candidate=candidate), self.assertRaises(AssertionError):
                regional_folders(self.clean, candidate)


if __name__ == '__main__':
    unittest.main()
