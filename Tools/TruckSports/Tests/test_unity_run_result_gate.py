from pathlib import Path
import tempfile
import sys
import unittest

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
import RunBroadcastValidation as runner


class UnityRunResultGateTests(unittest.TestCase):
    def check_xml(self, text):
        with tempfile.TemporaryDirectory(prefix="SportsTestResultGate-") as folder:
            path = Path(folder) / "result.xml"
            path.write_text(text)
            return runner.require_nonempty_unity_test_result(["-runTests", "-testResults", str(path)])

    def test_editor_authors_are_not_mistaken_for_test_runs(self):
        self.assertIsNone(runner.require_nonempty_unity_test_result(["-quit", "-executeMethod", "Author.Create"]))

    def test_actual_positive_run_counts_executed_cases(self):
        self.assertEqual(3, self.check_xml('<test-run result="Passed" passed="3" failed="0" skipped="0"/>'))

    def test_zero_case_success_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "zero tests"):
            self.check_xml('<test-run result="Passed" passed="0" failed="0" skipped="0" total="0"/>')

    def test_only_skipped_cases_do_not_establish_execution(self):
        with self.assertRaisesRegex(ValueError, "zero tests"):
            self.check_xml('<test-run result="Passed" passed="0" failed="0" skipped="3" total="3"/>')

    def test_tests_require_an_explicit_receipt(self):
        with self.assertRaisesRegex(ValueError, "explicit results"):
            runner.require_nonempty_unity_test_result(["-runTests"])

    def test_recorded_real_dentist_zero_case_receipt_is_rejected(self):
        path = runner.ROOT / "Logs/TruckSports/Impossible07/impossible07-dentist-v1.xml"
        if not path.exists():
            self.skipTest("Local native evidence is intentionally excluded from public source.")
        with self.assertRaisesRegex(ValueError, "zero tests"):
            runner.require_nonempty_unity_test_result(["-runTests", "-testResults", str(path)])


if __name__ == "__main__":
    unittest.main()
