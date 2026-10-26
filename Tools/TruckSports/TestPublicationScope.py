import unittest
import os
import subprocess
from pathlib import Path
import tempfile
from types import SimpleNamespace
from unittest.mock import patch
from PublishScopedSports import (check_whitespace, validate_public_path, scoped_commit,
                                expected_local_changed_paths, preserve_local_reviewed_paths)


class PublicationScopeTests(unittest.TestCase):
    def test_private_derivatives_and_unrelated_source_cannot_enter_reviewed_payload(self):
        for path in [
            "Assets/TruckSports/Customization/PrivateLicensedFragments/semi.standard.asset",
            "Assets/TruckSports/Customization/PrivateLicensedFragments.meta",
            "Assets/TruckDockDiving/Customization/PrivateLicensedSplitBodies/novelty.icecream.asset",
            "Assets/TruckDockDiving/Customization/PrivateLicensedSplitBodies.meta",
            "Assets/TruckBeerPong/Carnival/Dance.anim",
            "Assets/TruckBeerPong/Carnival/Dance.anim.meta",
            "Assets/TruckBeerPong/Carnival/Tailgate humanoid throw.FBX",
            "Assets/TruckBeerPong/Carnival/Tailgate humanoid throw.FBX.meta",
            "Assets/TruckCornhole/Carnival/Yard Wave.anim",
            "Assets/TruckCornhole/Carnival/Yard Wave.anim.meta",
            "Assets/TruckSports/Customization/Meshes/truck.fire owned participant livery.asset",
            "Assets/TruckSports/Customization/Meshes/novelty.icecream owned participant livery.asset.meta",
            "Assets/TruckTaxi/Scripts/Taxi.cs",
            "Assets/TruckSports/../TruckTaxi/Taxi.cs",
            "Logs/TruckSports/private-process.log",
        ]:
            with self.subTest(path=path), self.assertRaises(AssertionError):
                validate_public_path(path)

    def test_owned_adapters_configuration_and_evidence_remain_publishable(self):
        for path in [
            ".gitignore",
            "TRUCK_SPORTS_ACCEPTANCE_LEDGER.md",
            "Assets/TruckSports/Scripts/SportsTruckAppearanceHost.cs",
            "Assets/TruckDockDiving/Scripts/DockAppearanceSplitRecipe.cs",
            "Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset",
            "Documentation/TruckSports/Evidence/2026-10-25-Carnival/snow.png",
            "Tools/TruckSports/PublishScopedSports.py",
        ]:
            with self.subTest(path=path):
                validate_public_path(path)


class NativeUnityWhitespaceTests(unittest.TestCase):
    header = b"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"

    def review(self, relative, added, header=None):
        data = (self.header if header is None else header) + b"--- !u!1 &1\nGameObject:\n  m_Name: \n"
        with tempfile.TemporaryDirectory(prefix="sports-whitespace-") as directory:
            repo = Path(directory)
            asset = repo / relative
            asset.parent.mkdir(parents=True)
            asset.write_bytes(data)
            result = SimpleNamespace(returncode=2, stdout=relative + ":6: trailing whitespace.\n" + added + "\n", stderr="")
            with patch("PublishScopedSports.subprocess.run", return_value=result):
                review = check_whitespace(repo, "reviewed-parent", [relative], {})
            self.assertEqual(asset.read_bytes(), data, "The check must preserve exact native asset bytes")
            return review

    def test_native_prefab_empty_scalar_is_reviewed_without_rewriting(self):
        self.assertEqual(self.review("Assets/TruckSports/Menu.prefab", "+  m_Name: ")["unityEmptyFields"], 1)

    def test_native_controller_empty_scalar_is_reviewed(self):
        self.assertEqual(self.review("Assets/TruckBaseJumping/Flight.controller", "+  m_Tag: ")["unityEmptyFields"], 1)

    def test_native_scene_empty_sequence_item_scalar_is_reviewed(self):
        self.assertEqual(self.review("Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity", "+    - participantId: ")["unityEmptyFields"], 1)

    def test_native_crlf_header_is_supported(self):
        self.assertEqual(self.review("Assets/TruckSports/Flight.controller", "+  m_Tag: ", self.header.replace(b"\n", b"\r\n"))["unityEmptyFields"], 1)

    def test_other_whitespace_and_non_native_or_private_content_stay_rejected(self):
        cases = [
            ("Assets/TruckSports/Scripts/Owned.cs", "+// comment ", self.header),
            ("Assets/TruckSports/Scripts/Owned.cs", "+  m_Name: ", self.header),
            ("Assets/TruckSports/Menu.prefab", "+  m_Name: Winner ", self.header),
            ("Assets/TruckSports/Flight.controller", "+arbitrary trailing text ", self.header),
            ("Assets/TruckSports/Menu.prefab", "+  m_Name: ", b"not native YAML\n"),
            ("Assets/TruckSports/Menu.prefab", "+  m_Offset: {x: 0, y: 1} ", self.header),
            ("Assets/TruckSports/Flight.controller", "+  m_Tag:  ", self.header),
            ("Assets/TruckSports/Menu.prefab", "+  m_Name: ", b"%YAML arbitrary\n"),
            ("Assets/Vendor/Menu.prefab", "+  m_Name: ", self.header),
            ("Assets/TruckSports/Customization/PrivateLicensedFragments/Menu.prefab", "+  m_Name: ", self.header),
            ("Assets/TruckTaxi/Flight.controller", "+  m_Tag: ", self.header),
            ("Assets/TruckPedestrianPlowing/Flight.controller", "+  m_Tag: ", self.header),
            ("Assets/TruckSports/Menu.prefab", "+    - participantId: ", self.header),
            ("Assets/TruckSports/Menu.prefab", "+  [complex, key]: ", self.header),
            ("Assets/TruckSports/Scenes/Owned.unity", "+    - participantId: value ", self.header),
        ]
        for relative, added, header in cases:
            with self.subTest(relative=relative, added=added, header=header), self.assertRaises(AssertionError):
                self.review(relative, added, header)


class PrivateCheckpointPreservationTests(unittest.TestCase):
    paths = ["Assets/TruckSports/Scripts/Already.cs", "Assets/TruckSports/Scripts/Later.cs"]
    private_path = "Assets/UnrelatedPrivate/secret.txt"

    def setUp(self):
        # An inherited index/worktree override must never point a test at the
        # user's repository. Every Git write below belongs to this temp repo.
        environment = os.environ.copy()
        for key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_COMMON_DIR", "GIT_INDEX_FILE",
                    "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES"):
            environment.pop(key, None)
        environment["GIT_OPTIONAL_LOCKS"] = "0"
        self.environment = patch.dict(os.environ, environment, clear=True)
        self.environment.start()
        self.addCleanup(self.environment.stop)
        self.directory = tempfile.TemporaryDirectory(prefix="sports-private-checkpoint-")
        self.addCleanup(self.directory.cleanup)
        self.repo = Path(self.directory.name)
        self.git("init")
        for name, value in (("user.name", "Scope Test"), ("user.email", "scope@example.invalid"),
                            ("commit.gpgsign", "false"), ("core.autocrlf", "input")):
            self.git("config", name, value)
        hooks = self.repo / "no-hooks"
        hooks.mkdir()
        self.git("config", "core.hooksPath", str(hooks))
        attributes = self.repo / "empty-global-attributes"
        attributes.write_bytes(b"")
        self.git("config", "core.attributesFile", str(attributes))
        self.write(".gitattributes", b"*.cs text eol=lf\n")
        for path in self.paths:
            self.write(path, b"// public original\n")
        self.git("add", "--", ".gitattributes", *self.paths)
        self.git("commit", "-m", "Public base")
        self.public_parent = self.git("rev-parse", "HEAD").decode().strip()

    def git(self, *args):
        return subprocess.check_output(["git", *args], cwd=self.repo, stderr=subprocess.STDOUT)

    def write(self, relative, data):
        path = self.repo / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)

    def prepare(self, both_already_preserved):
        self.git("checkout", "-b", "private-checkpoint")
        self.write(self.paths[0], b"// reviewed change\n")
        if both_already_preserved:
            self.write(self.paths[1], b"// reviewed change\n")
        self.write(self.private_path, b"checkpoint private data\n")
        self.git("add", "--", *self.paths, self.private_path)
        self.git("commit", "-m", "Private preservation checkpoint")
        private_parent = self.git("rev-parse", "HEAD").decode().strip()
        # Different raw line endings must compare as canonical tree content.
        for path in self.paths:
            self.write(path, b"// reviewed change\r\n")
        self.write(self.private_path, b"uncommitted private data\n")
        index_before = (self.repo / ".git/index").read_bytes()
        entries_before = self.git("ls-files", "--stage", "-z")
        scratch = self.repo / "public-scratch"
        public_commit = scoped_commit(self.repo, self.public_parent, self.paths, "Reviewed patch", scratch)
        self.assertEqual(self.git("rev-parse", "HEAD").decode().strip(), private_parent)
        self.assertEqual((self.repo / ".git/index").read_bytes(), index_before)
        self.assertEqual(self.git("ls-files", "--stage", "-z"), entries_before)
        public_delta = set(self.git("diff", "--name-only", self.public_parent, public_commit).decode().splitlines())
        self.assertEqual(public_delta, set(self.paths))
        self.assertNotIn(self.private_path, self.git("ls-tree", "-r", "--name-only", public_commit).decode().splitlines())
        return private_parent, public_commit, scratch

    def preserve(self, private_parent, public_commit, scratch):
        expected = expected_local_changed_paths(self.repo, private_parent, public_commit, self.paths)
        local_commit = preserve_local_reviewed_paths(self.repo, private_parent, self.paths, expected,
                                                     scratch / "paths.nul", scratch / "message.txt")
        self.assertEqual((self.repo / self.private_path).read_bytes(), b"uncommitted private data\n")
        self.assertEqual(self.git("show", local_commit + ":" + self.private_path), b"checkpoint private data\n")
        for path in self.paths:
            self.assertEqual(self.git("show", local_commit + ":" + path), self.git("show", public_commit + ":" + path))
        self.assertFalse(self.git("diff", "--cached", "--name-only").strip())
        return expected, local_commit

    def test_public_scope_and_private_local_delta_are_distinct(self):
        private_parent, public_commit, scratch = self.prepare(False)
        expected, local_commit = self.preserve(private_parent, public_commit, scratch)
        self.assertEqual(expected, {self.paths[1]})
        self.assertNotEqual(local_commit, private_parent)
        self.assertEqual(set(self.git("diff", "--name-only", private_parent, local_commit).decode().splitlines()), expected)
        self.assertEqual(self.git("rev-parse", local_commit + "^" ).decode().strip(), private_parent)

    def test_fully_preserved_scope_keeps_private_parent_without_empty_commit(self):
        private_parent, public_commit, scratch = self.prepare(True)
        expected, local_commit = self.preserve(private_parent, public_commit, scratch)
        self.assertEqual(expected, set())
        self.assertEqual(local_commit, private_parent)
        self.assertEqual(self.git("rev-parse", "HEAD").decode().strip(), private_parent)

    def test_outside_expected_local_path_is_rejected_before_staging(self):
        private_parent, _, scratch = self.prepare(False)
        before = (self.repo / ".git/index").read_bytes()
        with self.assertRaises(AssertionError):
            preserve_local_reviewed_paths(self.repo, private_parent, self.paths, {self.private_path},
                                          scratch / "paths.nul", scratch / "message.txt")
        self.assertEqual((self.repo / ".git/index").read_bytes(), before)


if __name__ == "__main__":
    unittest.main()
