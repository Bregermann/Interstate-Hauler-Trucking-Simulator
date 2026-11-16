from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import RunDragonKingdomValidation as dragon


class NativeMetadataGuardTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='SportsNativeMetadata-')
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name) / 'NativeTest.cs'
        self.source.write_text('// Native fixture\n', encoding='utf-8')
        self.meta = Path(str(self.source) + '.meta')

    def test_existing_valid_guid_and_bytes_are_preserved(self):
        data = b'fileFormatVersion: 2\r\nguid: a758379e79a4ac7a79fb26b61314dd92\r\n'
        self.meta.write_bytes(data)
        self.assertEqual(self.meta, dragon.meta_for(self.source))
        self.assertEqual(data, self.meta.read_bytes())

    def test_thirty_three_digit_guid_rejects_without_repair_or_launch(self):
        data = b'fileFormatVersion: 2\nguid: aa758379e79a4ac7a79fb26b61314dd92\n'
        self.meta.write_bytes(data)
        with self.assertRaisesRegex(ValueError, '32-hex Unity GUID'):
            dragon.meta_for(self.source)
        self.assertEqual(data, self.meta.read_bytes())

    def test_escaped_newlines_and_duplicate_guids_are_rejected(self):
        for data in (
            b'fileFormatVersion: 2\\nguid: a758379e79a4ac7a79fb26b61314dd92\\n',
            b'guid: a758379e79a4ac7a79fb26b61314dd92\nguid: b758379e79a4ac7a79fb26b61314dd92\n',
        ):
            with self.subTest(data=data):
                self.meta.write_bytes(data)
                with self.assertRaisesRegex(ValueError, '32-hex Unity GUID'):
                    dragon.meta_for(self.source)
                self.assertEqual(data, self.meta.read_bytes())

    def test_missing_code_metadata_gets_one_valid_persistent_guid(self):
        self.assertEqual(self.meta, dragon.meta_for(self.source))
        data = self.meta.read_bytes()
        self.assertEqual(self.meta, dragon.meta_for(self.source))
        self.assertEqual(data, self.meta.read_bytes())


if __name__ == '__main__':
    unittest.main()
