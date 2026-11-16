from pathlib import Path
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


class ForestOutputRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='SportsForestRetention-')
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name) / 'Root'
        self.clean = Path(temporary.name) / 'Clean'
        self.tag = 'ForestNativeTest'
        self.run = self.root / 'Logs/TruckSports/DragonKingdom' / self.tag
        self.run.mkdir(parents=True)
        self.prior = {}
        self.source_paths = []
        self.source_hashes = []
        self.shader = ['736e744e3b7817e418215dffa87b7088', '46887de279c9ffa44a92290beaaca2b7']
        for index, name in enumerate(('M_ENV_tree_leaves_ancient.mat', 'M_ENV_wood_ancient_01.mat')):
            path = retain.FOREST_VENDOR + 'Materials/' + name
            self.put(self.clean, path, self.material(self.shader[index], 'Source'))
            self.put(self.clean, path + '.meta', self.meta(index + 1))
            for key in (path, path + '.meta'):
                self.source_paths.append(key)
                self.source_hashes.append(digest((self.clean / key).read_bytes()))
        self.evidence = dict(repeatByteExact=True, originalBytesExact=True,
                             scenePrefabBytesExact=True, materialGuidsExact=True,
                             sourcePaths=self.source_paths, sourceHashes=self.source_hashes)
        self.proof_targets = [retain.FOREST_PROOF + '/' + name for name in (
            'Observed original Lit leaf.mat', 'Observed original Lit wood.mat',
            'Configured vendor leaf.mat', 'Configured vendor wood.mat')]
        for index, path in enumerate(self.proof_targets):
            shader = '933532a4fcc9baf4fa0491de14d08ed7' if index < 2 else self.shader[index - 2]
            self.put(self.clean, path, self.material(shader, path))
            self.put(self.clean, path + '.meta', self.meta(20 + index))
        for index, path in enumerate((retain.FOREST_PROOF + '.meta', 'Assets/TruckDragonKingdom/VisualCandidates.meta')):
            self.put(self.clean, path, self.meta(30 + index))

    @staticmethod
    def material(shader, name):
        return ('Material:\n  m_Name: ' + name + '\n  m_Shader: {fileID: 4800000, guid: ' + shader + ', type: 3}\n').encode()

    @staticmethod
    def meta(number):
        return ('fileFormatVersion: 2\nguid: ' + format(number, '032x') + '\n').encode()

    @staticmethod
    def put(base, relative, data):
        path = base / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)

    def receipt(self, scope):
        path = self.run / 'rendered-evidence/ForestMaterialProof' / ('prepared.json' if scope == 'forest-proof' else 'upgraded.json')
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(self.evidence), encoding='utf-8')
        return path

    def select(self, scope='forest-proof'):
        self.receipt(scope)
        return retain.forest_paths(self.clean, self.run, self.prior, scope)

    def execute(self, scope='forest-proof', name='forest-retained', extra=()):
        self.receipt(scope)
        (self.run / 'input-manifest.json').write_text(json.dumps([dict(path=path, sha256=value) for path, value in self.prior.items()]))
        with patch.object(retain, 'ROOT', self.root), patch.object(retain, 'CLEAN', self.clean), patch.object(sys, 'argv', ['retain', self.tag, '--scope', scope, '--receipt-name', name, *extra]):
            retain.main()

    def canonical(self):
        for index, path in enumerate(retain.FOREST_MATERIALS):
            before = self.material('933532a4fcc9baf4fa0491de14d08ed7', 'Original Lit')
            self.put(self.root, path, before)
            self.put(self.root, path + '.meta', self.meta(50 + index))
            self.put(self.clean, path, self.material(self.shader[index], 'Original Lit'))
            self.put(self.clean, path + '.meta', self.meta(50 + index))
            self.prior[path] = digest(before)
            self.prior[path + '.meta'] = digest(self.meta(50 + index))
        kept = 'Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab'
        self.put(self.clean, kept, b'Actual unchanged world fixture')
        self.put(self.clean, kept + '.meta', self.meta(90))
        self.evidence.update(targets=retain.FOREST_MATERIALS[:],
                             before=[self.prior[path] for path in retain.FOREST_MATERIALS],
                             after=[digest((self.clean / path).read_bytes()) for path in retain.FOREST_MATERIALS],
                             preservedPaths=[kept, kept + '.meta'],
                             preservedHashes=[digest((self.clean / path).read_bytes()) for path in (kept, kept + '.meta')])

    def test_proof_is_exact_materials_and_folder_guids_without_candidate_sweep(self):
        self.put(self.clean, retain.FOREST_PROOF + '/OtherCandidate.prefab', b'Not selected')
        self.put(self.clean, retain.FOREST_PROOF + '/Editor.cs', b'Not selected')
        self.put(self.clean, retain.FOREST_PROOF + '/Nested/Unreviewed.mat', b'Not selected')
        selected = {path.relative_to(self.clean).as_posix() for path in self.select()}
        expected = {path + suffix for path in self.proof_targets for suffix in ('', '.meta')}
        expected.update((retain.FOREST_PROOF + '.meta', 'Assets/TruckDragonKingdom/VisualCandidates.meta'))
        self.assertEqual(expected, selected)
        self.execute()
        self.assertFalse((self.root / retain.FOREST_PROOF / 'OtherCandidate.prefab').exists())

    def test_missing_actual_preparation_receipt_rejects(self):
        with self.assertRaises(FileNotFoundError):
            retain.forest_paths(self.clean, self.run, {}, 'forest-proof')

    def test_every_preservation_flag_requires_true_boolean(self):
        for field in ('repeatByteExact', 'originalBytesExact', 'scenePrefabBytesExact', 'materialGuidsExact'):
            for value in (False, 'true', 1, None):
                with self.subTest(field=field, value=value):
                    self.evidence[field] = value
                    with self.assertRaisesRegex(AssertionError, 'assertions required'):
                        self.select()
            self.evidence[field] = True

    def test_missing_meta_or_guid_collision_cannot_be_retained(self):
        path = self.clean / (self.proof_targets[0] + '.meta')
        path.unlink()
        with self.assertRaisesRegex(AssertionError, 'metadata required'):
            self.select()
        path.write_bytes(self.meta(21))
        with self.assertRaisesRegex(AssertionError, 'GUID collision'):
            self.select()

    def test_wrong_material_family_and_stale_vendor_bytes_are_rejected(self):
        path = self.clean / self.proof_targets[2]
        old = path.read_bytes()
        path.write_bytes(self.material('933532a4fcc9baf4fa0491de14d08ed7', 'Still generic'))
        with self.assertRaisesRegex(AssertionError, 'material family'):
            self.select()
        path.write_bytes(old)
        self.put(self.clean, self.source_paths[0], b'Vendor file changed after native receipt')
        with self.assertRaisesRegex(AssertionError, 'preserved bytes differ'):
            self.select()

    def test_raw_source_path_escape_is_rejected(self):
        self.evidence['sourcePaths'] = [retain.FOREST_VENDOR + '../Other/Vendor.mat']
        self.evidence['sourceHashes'] = ['0' * 64]
        with self.assertRaisesRegex(AssertionError, 'scope'):
            self.select()

    def test_canonical_upgrade_selects_only_two_assets_and_preserves_before_bytes(self):
        self.canonical()
        old = {path: (self.root / path).read_bytes() for path in retain.FOREST_MATERIALS}
        self.execute('forest-material')
        for path, before in old.items():
            self.assertEqual((self.clean / path).read_bytes(), (self.root / path).read_bytes())
            self.assertEqual(before, (self.run / 'before-output-retention/forest-retained' / path).read_bytes())
        records = json.loads((self.run / 'forest-retained.json').read_text())
        self.assertEqual(4, len(records))

    def test_concurrent_root_material_blocks_whole_batch_before_any_write(self):
        self.canonical()
        leaf_before = (self.root / retain.FOREST_MATERIALS[0]).read_bytes()
        custom = b'Concurrent ROOT wood material customization'
        self.put(self.root, retain.FOREST_MATERIALS[1], custom)
        with self.assertRaisesRegex(AssertionError, 'concurrent ROOT'):
            self.execute('forest-material')
        self.assertEqual(leaf_before, (self.root / retain.FOREST_MATERIALS[0]).read_bytes())
        self.assertEqual(custom, (self.root / retain.FOREST_MATERIALS[1]).read_bytes())
        self.assertFalse((self.run / 'forest-retained.json').exists())

    def test_original_guid_and_actual_upgraded_hash_are_required(self):
        self.canonical()
        meta = self.clean / (retain.FOREST_MATERIALS[0] + '.meta')
        meta.write_bytes(self.meta(999))
        with self.assertRaisesRegex(AssertionError, 'frozen canonical forest GUID'):
            self.select('forest-material')
        meta.write_bytes(self.meta(50))
        self.evidence['after'][0] = '0' * 64
        with self.assertRaisesRegex(AssertionError, 'upgraded forest material hashes'):
            self.select('forest-material')

    def test_saved_world_preservation_hashes_are_required(self):
        self.canonical()
        self.evidence['preservedHashes'][0] = '0' * 64
        with self.assertRaisesRegex(AssertionError, 'preserved bytes differ'):
            self.select('forest-material')

    def test_receipt_cannot_expand_selected_material_scope(self):
        self.canonical()
        self.evidence['targets'].append('Assets/TruckDragonKingdom/Materials/Unrelated.mat')
        with self.assertRaisesRegex(AssertionError, 'Only the two canonical'):
            self.select('forest-material')

    def test_existing_backup_is_never_overwritten_and_failure_is_atomic(self):
        self.canonical()
        before = (self.root / retain.FOREST_MATERIALS[0]).read_bytes()
        backup = self.run / 'before-output-retention/forest-retained' / retain.FOREST_MATERIALS[1]
        backup.parent.mkdir(parents=True)
        backup.write_bytes(b'Earlier preserved bytes')
        with self.assertRaisesRegex(AssertionError, 'existing forest before bytes'):
            self.execute('forest-material')
        self.assertEqual(b'Earlier preserved bytes', backup.read_bytes())
        self.assertEqual(before, (self.root / retain.FOREST_MATERIALS[0]).read_bytes())

    def test_mode_or_candidate_mixing_is_rejected(self):
        for extra in (('--include-mode', 'TruckHoardHeist'), ('--candidate', 'Unrelated')):
            with self.subTest(extra=extra), self.assertRaisesRegex(AssertionError, 'separately'):
                self.execute(extra=extra)


if __name__ == '__main__':
    unittest.main()
