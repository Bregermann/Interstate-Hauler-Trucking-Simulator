import unittest
import contextlib
import json
import tempfile
import types
from pathlib import Path
from unittest.mock import patch
import numpy as np
from generate_dialogue import digest, normalize_trim, safe_id, run


class AdapterTests(unittest.TestCase):
    def test_hash_includes_semantics(self):
        value = {"text": "Hello", "voice": "one", "emotion": "neutral", "reference": "abc", "version": 1}
        for field in value:
            self.assertNotEqual(digest(value), digest({**value, field: "changed"}))
        self.assertEqual(digest(value), digest(dict(reversed(list(value.items())))))

    def test_reject_path_identifiers(self):
        for value in ("../escape", "C:\\bad", "", "..", "a/b", "CON", "a.", "LPT1.txt"):
            with self.assertRaises(ValueError):
                safe_id(value)
        self.assertEqual(safe_id("p001.hello"), "p001.hello")

    def test_trim_and_normalize_without_mutating_source(self):
        source = np.concatenate([np.zeros(1000), np.full(500, .2), np.zeros(1000)]).astype(np.float32)
        copy = source.copy()
        result = normalize_trim(source, 1000)
        self.assertEqual(len(result), 660)
        self.assertAlmostEqual(float(result.max()), .89, places=5)
        np.testing.assert_array_equal(copy, source)

    def test_silence_is_failure(self):
        with self.assertRaises(ValueError):
            normalize_trim(np.zeros(100), 1000)


class QueueTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.project = Path(self.temp.name) / "project"
        self.work = Path(self.temp.name) / "work"
        self.project.mkdir()
        (self.work / "app").mkdir(parents=True)
        (self.work / "app/engine.py").touch()
        self.job = self.project / "queue.json"
        self.reference = Path(self.temp.name) / "reference.wav"
        self.reference.write_bytes(b"unchanged source")
        self.calls = []
        calls = self.calls

        class Engine:
            model = None
            def generate(self, text, options, seed, progress):
                calls.append(text)
                return np.full(100, .2), 1000, {"test": True}

        def reference(path, model):
            if not path.exists():
                raise ValueError("Missing reference")
            return {"path": str(path), "sha256": digest(path.read_bytes().hex())}

        def wav(path, audio, rate, replace):
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(audio.tobytes())

        self.storage = types.SimpleNamespace(sha256=lambda p: digest(p.read_bytes().hex()), operation_lock=contextlib.nullcontext,
            atomic_json=lambda path, value: path.write_text(json.dumps(value), encoding="utf-8"))
        modules = {
            "app.engine": types.SimpleNamespace(Engine=Engine, source_identity=lambda: {"version": "test"}),
            "app.config": types.SimpleNamespace(parameters=lambda model, preset: {"temperature": .8}),
            "app.audio": types.SimpleNamespace(validate_reference=reference, atomic_wav=wav),
            "app.storage": self.storage,
        }
        self.patch = patch.dict("sys.modules", modules)
        self.patch.start()
        self.addCleanup(self.patch.stop)
        self.line = {"passengerId": "p001", "lineId": "greeting", "text": "Hello", "referencePath": str(self.reference)}

    def execute(self, lines=None, plan=False):
        self.job.write_text(json.dumps({"items": lines or [self.line]}), encoding="utf-8")
        code = run(self.job, self.work, self.project, plan)
        return code, json.loads(self.job.with_suffix(".result.json").read_text(encoding="utf-8"))

    def test_cached_resume_does_not_regenerate(self):
        _, first = self.execute()
        _, second = self.execute()
        self.assertEqual(second["items"][0]["status"], "cached")
        self.assertEqual(len(self.calls), 1)
        self.assertEqual(self.reference.read_bytes(), b"unchanged source")
        self.assertEqual(first["items"][0]["hash"], second["items"][0]["hash"])

    def test_plan_does_not_generate(self):
        _, result = self.execute(plan=True)
        self.assertEqual(result["items"][0]["status"], "planned")
        self.assertEqual(self.calls, [])

    def test_failed_line_does_not_stop_next(self):
        code, result = self.execute([{**self.line, "lineId": "bad", "referencePath": "missing.wav"}, self.line])
        self.assertEqual(code, 1)
        self.assertEqual([i["status"] for i in result["items"]], ["failed", "complete"])

    def test_changed_reference_invalidates_cache(self):
        _, first = self.execute()
        self.reference.write_bytes(b"new recording")
        _, second = self.execute()
        self.assertNotEqual(first["items"][0]["hash"], second["items"][0]["hash"])
        self.assertEqual(len(self.calls), 2)

    def test_corrupt_manifest_or_wav_is_not_cached(self):
        _, result = self.execute()
        wav = Path(result["items"][0]["outputPath"])
        wav.with_suffix(".json").write_text("broken")
        self.execute()
        wav.write_bytes(b"corrupt")
        self.execute()
        self.assertEqual(len(self.calls), 3)

    def test_cancel_then_resume(self):
        self.job.with_suffix(".cancel").touch()
        _, result = self.execute()
        self.assertEqual(result["status"], "cancelled")
        self.assertEqual(self.calls, [])
        self.job.with_suffix(".cancel").unlink()
        _, result = self.execute()
        self.assertEqual(result["status"], "complete")

    def test_busy_external_engine_reports_recoverable_error(self):
        def busy():
            raise RuntimeError("Another narration operation is running")
        self.storage.operation_lock = busy
        code, result = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(result["status"], "needs_attention")
        self.assertIn("Another narration", result["error"])


if __name__ == "__main__":
    unittest.main()
