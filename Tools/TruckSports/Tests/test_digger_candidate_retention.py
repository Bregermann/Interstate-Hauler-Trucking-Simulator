from pathlib import Path
import copy
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from PreserveDragonKingdomAuthoring import candidate_digger_paths


class DiggerCandidateRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsDiggerRetention-')
        self.addCleanup(temporary.cleanup)
        self.clean = Path(temporary.name)
        self.tag = 'RoundedIntegrated04'
        self.folder = 'Assets/DiggerData/Scenes/DK-' + self.tag
        self.tiles = [self.folder + '/' + character * 32 for character in ('a', 'b')]
        for tile in self.tiles:
            path = self.clean / tile / '.internal' / 'actual-generated.vox'
            path.parent.mkdir(parents=True)
            path.write_bytes(b'actual generated voxel state')
        (self.clean / (self.folder + '.meta')).write_bytes(b'actual scene GUID')
        self.receipt = dict(status='PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW', cavern=dict(
            status='MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW',
            exactHeightmaps=True, outsideHolesExact=True, streamingAssetsUntouched=True,
            pipelineSettingsPreserved=True, installedUrpDetectorSatisfied=True, systems=2,
            generatedMeshColliders=136, roof=[dict() for _ in range(187)], minimumClearance=54.95,
            dataFolder=self.folder, tileDataFolders=self.tiles,
            streamingAssetsFolders=['Assets/StreamingAssets/DiggerData/' + Path(tile).name for tile in self.tiles]))

    def test_only_this_passed_cave_and_its_metadata_are_selected(self):
        paths = candidate_digger_paths(self.clean, self.tag, self.receipt)
        self.assertEqual(3, len(paths))
        self.assertTrue(all(path.relative_to(self.clean).as_posix().startswith(self.folder) for path in paths))

    def test_rejected_or_incomplete_cave_cannot_be_promoted(self):
        for field, value in (('status', 'REJECTED_DIGGER_CAVERN'), ('outsideHolesExact', False), ('roof', [])):
            receipt = copy.deepcopy(self.receipt)
            receipt['cavern'][field] = value
            with self.subTest(field=field), self.assertRaises(AssertionError):
                candidate_digger_paths(self.clean, self.tag, receipt)

    def test_another_scenes_data_or_new_streaming_writes_are_rejected(self):
        self.receipt['cavern']['dataFolder'] = 'Assets/DiggerData/Scenes/TruckArchaeology'
        with self.assertRaises(AssertionError): candidate_digger_paths(self.clean, self.tag, self.receipt)
        self.receipt['cavern']['dataFolder'] = self.folder
        path = self.clean / self.receipt['cavern']['streamingAssetsFolders'][0]
        path.mkdir(parents=True)
        with self.assertRaises(AssertionError): candidate_digger_paths(self.clean, self.tag, self.receipt)

    def test_extra_unrelated_guid_folder_blocks_retention(self):
        (self.clean / self.folder / ('c' * 32)).mkdir()
        with self.assertRaises(AssertionError): candidate_digger_paths(self.clean, self.tag, self.receipt)


if __name__ == '__main__':
    unittest.main()
