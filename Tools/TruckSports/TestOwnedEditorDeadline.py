"""No Unity launch: verify only the already-owned Popen handle can be terminated."""
import subprocess
import unittest
from unittest.mock import Mock
from RunBroadcastValidation import wait_owned_editor


class OwnedEditorDeadlineTests(unittest.TestCase):
    def test_success_keeps_actual_exit_and_never_terminates(self):
        process = Mock()
        process.wait.return_value = 0
        self.assertEqual(wait_owned_editor(process, 60), (0, False))
        process.wait.assert_called_once_with(timeout=60)
        process.terminate.assert_not_called()
        process.kill.assert_not_called()

    def test_timeout_is_failure_even_if_shutdown_returns_zero(self):
        process = Mock()
        process.wait.side_effect = [subprocess.TimeoutExpired("owned test", 60), 0]
        self.assertEqual(wait_owned_editor(process, 60), (0, True))
        process.terminate.assert_called_once_with()
        process.kill.assert_not_called()

    def test_termination_uses_only_owned_handle_and_retains_exit(self):
        process = Mock()
        process.wait.side_effect = [subprocess.TimeoutExpired("owned test", 60), 4294967295]
        self.assertEqual(wait_owned_editor(process, 60), (4294967295, True))
        process.terminate.assert_called_once_with()

    def test_stuck_termination_falls_back_only_on_same_handle(self):
        process = Mock()
        process.wait.side_effect = [subprocess.TimeoutExpired("owned test", 60), subprocess.TimeoutExpired("owned shutdown", 15), -9]
        self.assertEqual(wait_owned_editor(process, 60), (-9, True))
        process.terminate.assert_called_once_with()
        process.kill.assert_called_once_with()
        self.assertEqual(process.wait.call_args_list[-1].kwargs, {"timeout": 15})

    def test_invalid_deadlines_cannot_launch_wait_or_kill(self):
        for deadline in [0, -1, 7201, float("inf"), float("nan"), None, "60"]:
            process = Mock()
            with self.assertRaises(ValueError):
                wait_owned_editor(process, deadline)
            self.assertFalse(process.method_calls)


if __name__ == "__main__":
    unittest.main()
