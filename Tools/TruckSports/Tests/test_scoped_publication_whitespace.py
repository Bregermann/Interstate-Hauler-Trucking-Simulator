import hashlib
from pathlib import Path
import sys
import tempfile
import types
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import PublishScopedSports as publication


class ExactWhitespaceReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.path = 'Assets/TruckExample/Editor/Owned.cs'
        target = self.root / self.path
        target.parent.mkdir(parents=True)
        target.write_bytes(b'class Owned {}\n\n')
        self.item = dict(path=self.path, line=2, kind='new blank line at EOF', added=None,
                         sha256=hashlib.sha256(target.read_bytes()).hexdigest(), reason='Exact built source, one nonsemantic final blank line.')

    def finding(self, item=None):
        return publication.reviewed_whitespace_finding(self.root, self.path, 2,
            'new blank line at EOF', None, [item or self.item])

    def check(self, stdout, review=None):
        result = types.SimpleNamespace(returncode=2, stderr='', stdout=stdout)
        with patch.object(publication.subprocess, 'run', return_value=result):
            return publication.check_whitespace(self.root, 'parent', [self.path], {}, review)

    def test_exact_hash_and_finding_are_accepted(self):
        self.assertTrue(self.finding())

    def test_changed_source_is_rejected(self):
        (self.root / self.path).write_bytes(b'class Changed {}\n\n')
        self.assertFalse(self.finding())

    def test_different_line_or_added_text_is_rejected(self):
        self.assertFalse(self.finding(dict(self.item, line=3)))
        self.assertFalse(self.finding(dict(self.item, added='+ other')))

    def test_ambiguous_duplicate_review_is_rejected(self):
        self.assertFalse(publication.reviewed_whitespace_finding(self.root, self.path, 2,
            'new blank line at EOF', None, [self.item, self.item]))

    def test_unreviewed_blank_eof_stays_rejected(self):
        with self.assertRaisesRegex(AssertionError, 'Unreviewed whitespace'):
            self.check(self.path + ':2: new blank line at EOF.\n')

    def test_single_line_eof_does_not_shift_following_native_field_pairs(self):
        self.path = 'Assets/TruckExample/Owned.cs.meta'
        target = self.root / self.path
        target.write_bytes(b'fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\nuserData: \n\n')
        item = dict(self.item, path=self.path, line=4, sha256=hashlib.sha256(target.read_bytes()).hexdigest())
        result = self.check(self.path + ':4: new blank line at EOF.\n' + self.path + ':3: trailing whitespace.\n+  userData: \n', [item])
        self.assertEqual(result['explicitlyReviewedFindings'], 1)
        self.assertEqual(result['unityEmptyFields'], 1)

    def test_review_does_not_cover_an_additional_unreviewed_error(self):
        with self.assertRaisesRegex(AssertionError, 'Unreviewed whitespace'):
            self.check(self.path + ':2: new blank line at EOF.\n' + self.path + ':1: trailing whitespace.\n+class Owned {} \n', [self.item])

    def test_exact_trailing_space_is_independent_of_eof_review(self):
        item = dict(self.item, line=1, kind='trailing whitespace', added='+class Owned {} ')
        result = self.check(self.path + ':1: trailing whitespace.\n+class Owned {} \n', [item])
        self.assertEqual(result['explicitlyReviewedFindings'], 1)


if __name__ == '__main__':
    unittest.main()
