from pathlib import Path
import json
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from PreserveDragonKingdomAuthoring import cavern_folders


class CavernOutputRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsCavernRetention-')
        self.addCleanup(temporary.cleanup)
        self.clean = Path(temporary.name) / 'clean'
        self.run = Path(temporary.name) / 'run'
        self.candidate = 'CavernMassif02'
        self.folder = 'ReviewCandidates/' + self.candidate
        self.private = 'PrivateLicensedEnvironment/DerivedVista/' + self.candidate
        base = self.clean / 'Assets/TruckDragonKingdom'
        for name in (self.folder + '/Vista Cavern Massif.prefab', self.private + '/Cavern elevated MountainRange.asset'):
            path = base / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b'actual saved output')
        self.evidence = dict(
            status='MEASURED_MASSIF_READY_FOR_SEPARATE_DIGGER_EXCAVATION_REVIEW',
            folder='Assets/TruckDragonKingdom/' + self.folder,
            prefab='Assets/TruckDragonKingdom/' + self.folder + '/Vista Cavern Massif.prefab',
            graph='Assets/TruckDragonKingdom/' + self.private + '/Cavern elevated MountainRange.asset',
            sourceBytesPreserved=True, outsideBiomePreserved=True, temporaryObjectsRemoved=True,
            populationCallbacks=4, semanticRoadFloor=105, minimumRoofHeight=178.53,
            terrains=[dict(outsideBiomeCoordinatedMaximumDelta=0) for _ in range(4)],
            roof=[dict(newSurface=178.53) for _ in range(117)])

    def select(self, candidate=None):
        path = self.run / 'rendered-evidence' / ('cavern-foundation-' + self.candidate + '.json')
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(self.evidence), encoding='utf-8')
        return cavern_folders(self.clean, self.run, candidate or self.candidate)

    def test_passed_massif_selects_only_exact_candidate_and_private_graphs(self):
        self.assertEqual([self.folder, self.private], self.select())

    def test_rejected_or_modified_terrain_never_promotes_as_passed(self):
        self.evidence['status'] = 'IN_PROGRESS'
        with self.assertRaises(AssertionError):
            self.select()
        self.evidence['status'] = 'MEASURED_MASSIF_READY_FOR_SEPARATE_DIGGER_EXCAVATION_REVIEW'
        self.evidence['terrains'][1]['outsideBiomeCoordinatedMaximumDelta'] = .1
        with self.assertRaises(AssertionError):
            self.select()

    def test_private_scope_escape_and_unmeasured_roof_are_rejected(self):
        self.evidence['graph'] = 'Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Vendor.asset'
        with self.assertRaises(AssertionError):
            self.select()
        self.evidence['graph'] = 'Assets/TruckDragonKingdom/' + self.private + '/Cavern elevated MountainRange.asset'
        self.evidence['minimumRoofHeight'] = 110
        with self.assertRaises(AssertionError):
            self.select()

    def test_each_preservation_failure_blocks_retention(self):
        for field in ('sourceBytesPreserved', 'outsideBiomePreserved', 'temporaryObjectsRemoved'):
            self.evidence[field] = False
            with self.subTest(field=field), self.assertRaises(AssertionError):
                self.select()
            self.evidence[field] = True


if __name__ == '__main__':
    unittest.main()
