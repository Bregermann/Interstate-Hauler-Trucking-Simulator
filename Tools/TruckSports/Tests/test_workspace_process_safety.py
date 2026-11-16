"""Keep unrelated Editor workers out of this workspace's exclusive lease."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from WorkspaceProcessSafety import workspace_process


class WorkspaceProcessSafetyTests(unittest.TestCase):
    root = Path(__file__).resolve().parents[3]

    def editor(self, pid, project=None, parent=0):
        return dict(ProcessId=pid, ParentProcessId=parent, Name='Unity.exe',
                    ExecutablePath='C:/Program Files/Unity/Editor/Unity.exe',
                    CommandLine='' if project is None else f'Unity.exe -projectPath "{project}"')

    def test_explicit_workspace_project_blocks(self):
        self.assertTrue(workspace_process(self.editor(1, self.root / '.sports-validation/Example'), self.root))

    def test_external_editor_and_its_import_worker_do_not_block(self):
        parent = self.editor(1, self.root.parent / 'External capture')
        worker = self.editor(2, parent=1)
        processes = {1: parent, 2: worker}
        self.assertFalse(workspace_process(parent, self.root, processes))
        self.assertFalse(workspace_process(worker, self.root, processes))

    def test_workspace_editor_worker_still_blocks(self):
        parent = self.editor(1, self.root)
        worker = self.editor(2, parent=1)
        self.assertTrue(workspace_process(worker, self.root, {1: parent, 2: worker}))

    def test_workers_explicit_project_overrides_ancestor(self):
        parent = self.editor(1, self.root.parent / 'External capture')
        worker = self.editor(2, self.root, parent=1)
        self.assertTrue(workspace_process(worker, self.root, {1: parent, 2: worker}))

    def test_unknown_editor_and_missing_parent_still_block(self):
        self.assertTrue(workspace_process(self.editor(2, parent=1), self.root, {}))

    def test_cycle_without_project_still_blocks(self):
        one, two = self.editor(1, parent=2), self.editor(2, parent=1)
        self.assertTrue(workspace_process(one, self.root, {1: one, 2: two}))


if __name__ == '__main__':
    unittest.main()
