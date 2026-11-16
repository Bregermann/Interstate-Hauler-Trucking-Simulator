from pathlib import Path
import json
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from PreserveDragonKingdomAuthoring import rounded_folders


class RoundedOutputRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsRoundedRetention-')
        self.addCleanup(temporary.cleanup)
        self.clean = Path(temporary.name)
        self.candidate = 'RoundedVista01'
        self.folder = 'ReviewCandidates/' + self.candidate
        self.private = 'PrivateLicensedEnvironment/DerivedVista/' + self.candidate
        base = self.clean / 'Assets/TruckDragonKingdom'
        (base / self.private).mkdir(parents=True)
        (base / self.folder / 'Terrain').mkdir(parents=True)
        (base / self.folder / 'Vista Regional Foundation.prefab').write_bytes(b'actual saved prefab')
        tiles = []
        for x in range(2):
            for z in range(2):
                path = base / self.folder / 'Terrain' / ('Kingdom ' + str(x) + ' ' + str(z) + '.asset')
                path.write_bytes(b'saved terrain'); Path(str(path) + '.meta').write_bytes(b'guid metadata')
                tiles.append(dict(path=path.relative_to(self.clean).as_posix(), resolution=513, ceilingSamples=0,
                                  maximum=494, above200=100000, steeper65=100, sourceSteeper65=20000))
        self.receipt = dict(status='MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW',
                            folder='Assets/TruckDragonKingdom/' + self.folder,
                            prefab='Assets/TruckDragonKingdom/' + self.folder + '/Vista Regional Foundation.prefab',
                            sourceBytesPreserved=True, temporaryObjectsRemoved=True, validationIssues=[],
                            terrains=tiles, centers=[dict() for _ in range(9)], heightCallbacks=4)

    def select(self):
        path = self.clean / 'Assets/TruckDragonKingdom' / self.folder / 'Foundation Receipt.json'
        path.write_text(json.dumps(self.receipt), encoding='utf-8')
        return rounded_folders(self.clean, self.candidate)

    def test_only_passed_measured_candidate_and_its_private_graphs_are_selected(self):
        self.assertEqual([self.folder, self.private], self.select())

    def test_failed_native_gate_and_outside_terrain_path_block_retention(self):
        self.receipt['validationIssues'] = ['Actual height ceiling clipped.']
        with self.assertRaises(AssertionError): self.select()
        self.receipt['validationIssues'] = []
        self.receipt['terrains'][0]['path'] = 'Assets/Other/terrain.asset'
        with self.assertRaises(AssertionError): self.select()

    def test_missing_highlands_or_no_slope_improvement_is_not_promoted(self):
        for tile in self.receipt['terrains']: tile['maximum'] = 250
        with self.assertRaises(AssertionError): self.select()
        for tile in self.receipt['terrains']: tile['maximum'] = 494; tile['steeper65'] = tile['sourceSteeper65']
        with self.assertRaises(AssertionError): self.select()


if __name__ == '__main__':
    unittest.main()
