from pathlib import Path
import hashlib
import json
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import RunBroadcastValidation as runner


class ExplicitSourceProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="SportsOwnedSource-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.path = "Assets/TruckDragonKingdom/Scripts/Actor.cs"
        source = self.root / self.path
        source.parent.mkdir(parents=True)
        source.write_bytes(b"// Original project-owned feature\n")
        self.entry = dict(path=self.path, sha256=hashlib.sha256(source.read_bytes()).hexdigest())

    def select(self, entries, baseline=()):
        manifest = self.root / "review.json"
        manifest.write_text(json.dumps(entries))
        with patch.object(runner, "ROOT", self.root):
            return runner.reviewed_additional_sources(manifest, set(baseline))

    def test_committed_owned_code_keeps_explicit_provenance(self):
        self.assertEqual({self.path}, self.select([self.entry]))

    def test_stale_hash_cannot_relabel_changed_code(self):
        (self.root / self.path).write_text("// changed after review")
        with self.assertRaisesRegex(ValueError, "changed after review"):
            self.select([self.entry])

    def test_exact_owned_snow_adapter_can_be_reviewed_without_admitting_other_taxi_sources(self):
        path='Assets/LWS/TruckTaxi/Scripts/TruckTaxiSnowSurface.cs'
        source=self.root/path;source.parent.mkdir(parents=True);source.write_bytes(b'// Explicit shared snow adapter')
        entry=dict(path=path,sha256=hashlib.sha256(source.read_bytes()).hexdigest())
        self.assertEqual({path},self.select([entry]))
        with self.assertRaisesRegex(ValueError,'Invalid'):
            self.select([entry],[path])
        with self.assertRaisesRegex(ValueError,'Invalid'):
            self.select([dict(path='Assets/LWS/TruckTaxi/Scripts/TruckTaxiPassengerRuntime.cs',sha256=entry['sha256'])])

    def shared_sync_fixture(self):
        relative='Assets/LWS/TruckTaxi/Scripts/TruckTaxiSnowSurface.cs'
        source=self.root/relative;source.parent.mkdir(parents=True);source.write_bytes(b'// Reviewed shared vendor adapter revision')
        fact=dict(path=relative,sha256=hashlib.sha256(source.read_bytes()).hexdigest())
        log=self.root/'Logs';log.mkdir();(log/'host-pending-baseline.json').write_text('[]')
        clean=self.root/'ValidationCopy'
        return relative,source,fact,log,clean

    def test_reviewed_committed_shared_adapter_is_copied_and_recorded_in_build_provenance(self):
        relative,source,fact,log,clean=self.shared_sync_fixture()
        review=self.root/'review.json';review.write_text(json.dumps([fact]))
        with patch.object(runner,'ROOT',self.root),patch.object(runner,'LOG',log),patch.object(runner,'CLEAN',clean),patch.object(runner.subprocess,'check_output',return_value=b''):
            runner.sync(additional_sources_path=review)
        self.assertEqual(source.read_bytes(),(clean/relative).read_bytes())
        self.assertEqual([fact],json.loads((log/'camera-source-sync.json').read_text()))

    def test_preserved_shared_build_input_reuses_exact_hash_but_rejects_unreviewed_revision(self):
        relative,source,fact,log,clean=self.shared_sync_fixture()
        (log/'build-prior-sources.json').write_text(json.dumps([fact]))
        with patch.object(runner,'ROOT',self.root),patch.object(runner,'LOG',log),patch.object(runner,'CLEAN',clean),patch.object(runner.subprocess,'check_output',return_value=b''):
            runner.sync()
            saved=(clean/relative).read_bytes()
            source.write_bytes(b'// Changed after previous build; not reviewed yet')
            with self.assertRaisesRegex(ValueError,'Shared adapter changed'):
                runner.sync()
            self.assertEqual(saved,(clean/relative).read_bytes())

    def test_unrelated_pending_work_is_still_excluded(self):
        with self.assertRaisesRegex(ValueError, "Invalid"):
            self.select([self.entry], [self.path])

    def test_licensed_and_path_escape_payloads_are_rejected(self):
        for path in ("Assets/TruckDragonKingdom/PrivateLicensedDragons/Vendor.cs",
                     "Assets/TruckDragonKingdom/../Vendor.cs", "Assets/UTS/Vendor.cs",
                     "Assets/TruckDragonKingdom/Settings.asset", "C:/External.cs"):
            with self.subTest(path=path), self.assertRaises(ValueError):
                self.select([dict(path=path, sha256=self.entry["sha256"])])

    def test_empty_and_duplicate_reviews_are_rejected(self):
        for entries in ([], [self.entry, self.entry]):
            with self.subTest(entries=entries), self.assertRaises(ValueError):
                self.select(entries)


if __name__ == "__main__":
    unittest.main()
