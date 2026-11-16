import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from RunPlaytestWindowsValidation import dragon_terminal


class DragonWindowsReceiptTests(unittest.TestCase):
    def setUp(self):
        self.data = dict(platform='WindowsPlayer', pid=1234,
                         scope='ACTUAL_WINDOWS_AI_OBSERVATION_NOT_HUMAN_DEVICE_LISTENING_OR_COMPLETE_VARIANT_ACCEPTANCE',
                         completeRequested=True, splitAIViews=False, checks=40, failures=0, captures=10,
                         visits=[dict(scene='TruckDragonEggDelivery', rematch=True, finished=True,
                                      physicalOutcomes=4, stopReason='ORDINARY_RESULTS')])
        self.args = ['-tsDragon08Scenes', 'TruckDragonEggDelivery', '-tsDragon08Complete']

    def check(self):
        return dragon_terminal(json.dumps(self.data), 1234, self.args)

    def test_matching_actual_completed_process_receipt_is_counted(self):
        self.assertEqual((40, 0), self.check())

    def test_other_process_or_editor_receipt_is_rejected(self):
        for key, value in (('pid', 4321), ('platform', 'WindowsEditor')):
            with self.subTest(key=key):
                old = self.data[key]
                self.data[key] = value
                with self.assertRaisesRegex(ValueError, 'owned Windows'):
                    self.check()
                self.data[key] = old

    def test_opening_run_cannot_be_reported_as_completed_match(self):
        self.data['completeRequested'] = False
        with self.assertRaisesRegex(ValueError, 'requested match/camera'):
            self.check()

    def test_timeout_or_no_genuine_physical_outcome_is_rejected(self):
        for key, value in (('physicalOutcomes', 0), ('stopReason', 'OBSERVATION_DEADLINE'), ('finished', False)):
            with self.subTest(key=key):
                visit = self.data['visits'][0]
                old = visit[key]
                visit[key] = value
                with self.assertRaisesRegex(ValueError, 'completed Dragon match'):
                    self.check()
                visit[key] = old

    def test_missing_scene_rematch_or_capture_is_rejected(self):
        self.args = ['-tsDragon08Complete']
        with self.assertRaisesRegex(ValueError, 'every requested scene'):
            self.check()
        self.args = ['-tsDragon08Scenes', 'TruckDragonEggDelivery', '-tsDragon08Complete']
        self.data['visits'][0]['rematch'] = False
        with self.assertRaisesRegex(ValueError, 'rematch'):
            self.check()
        self.data['visits'][0]['rematch'] = True
        self.data['captures'] = 0
        with self.assertRaisesRegex(ValueError, 'images'):
            self.check()

    def test_failed_run_retains_counts_without_becoming_acceptance(self):
        self.data['failures'] = 2
        self.data['visits'][0]['physicalOutcomes'] = 0
        self.assertEqual((40, 2), self.check())


if __name__ == '__main__':
    unittest.main()
