"""Promote the verified camera/playtest milestone without losing the prior build."""
import hashlib
import argparse
import json
import re
import shutil
import subprocess
import time
from pathlib import Path
import sys
sys.dont_write_bytecode = True
from BuildIdentity import snapshot

ROOT = Path(__file__).resolve().parents[2]
LOG = ROOT / "Logs/TruckSports/PlaytestRepair"
EXPECTED = "c8b0b9c1aed92af5778f7bb0be10fbb3950be7b75d5a6dfa8e9be8fb62c64890"


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sha", default=EXPECTED)
    parser.add_argument("--content-sha", required=True, help="Exact full executable/dependency/asset fingerprint")
    parser.add_argument("--run", action="append", help="Exact completed acceptance run Name:CheckCount")
    args = parser.parse_args()
    assert re.fullmatch(r"[a-f0-9]{64}", args.sha)
    expected = args.sha
    assert re.fullmatch(r"[a-f0-9]{64}", args.content_sha)
    assert snapshot(ROOT / "Builds/TruckSportsCamera")[0] == args.content_sha
    running = subprocess.check_output([
        "powershell", "-NoProfile", "-Command",
        "Get-Process | Where-Object { $_.ProcessName -in @('Unity','TruckSports') } | Select-Object -ExpandProperty Id",
    ], text=True).strip()
    if running:
        raise RuntimeError("Wait for the owned Editor/player to close before promotion: " + running)
    runs = [("FinalCamera33", 693), ("FinalSkyGateNormals", 10), ("FinalFareCabViews", 30), ("FinalMineProgressHUD", 52)]
    if args.run:
        assert all(re.fullmatch(r"[A-Za-z0-9_-]+:[1-9][0-9]*", item) for item in args.run)
        runs = [(item.rsplit(":", 1)[0], int(item.rsplit(":", 1)[1])) for item in args.run]
    else:
        assert expected == EXPECTED, "A different candidate needs its exact completed acceptance receipts."
    for name, checks in runs:
        receipt = json.loads((LOG / name / "process.json").read_text())
        assert receipt["managedSha256"] == expected and receipt["exit"] == 0
        assert receipt.get("buildContentSha256") == args.content_sha and receipt.get("unchangedBuildContent") is True, "Fresh acceptance for the exact code AND asset payload is required"
        assert receipt["unchangedManaged"] and receipt["completedEvidence"] and receipt["checks"] == checks and receipt["errors"] == 0
    source = ROOT / "Builds/TruckSportsCamera"
    destination = ROOT / "Builds/TruckSports"
    staged = ROOT / "Builds/TruckSportsVerifiedCameraStage"
    backup = ROOT / (".sports-validation/Recovery/AcceptedBuildBeforeCamera-" + time.strftime("%Y%m%d-%H%M%S"))
    # Every move is restricted to exact, resolved descendants of this workspace.
    for target in (source, destination, staged, backup):
        assert target.resolve().is_relative_to(ROOT.resolve()) and target.resolve() != ROOT.resolve()
    assert not staged.exists() and not backup.exists()
    assert digest(source / "TruckSports_Data/Managed/Assembly-CSharp.dll") == expected
    shutil.copytree(source, staged)
    files = list(source.rglob("*"))
    count = 0
    for file in files:
        if file.is_file():
            assert digest(file) == digest(staged / file.relative_to(source)), file
            count += 1
    backup.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        destination.rename(backup)
    try:
        staged.rename(destination)
    except BaseException:
        if backup.exists() and not destination.exists():
            backup.rename(destination)
        raise
    assert digest(destination / "TruckSports_Data/Managed/Assembly-CSharp.dll") == expected
    assert snapshot(destination)[0] == args.content_sha
    receipt = dict(managedSha256=expected, buildContentSha256=args.content_sha, verifiedFiles=count, destination=str(destination), priorBuild=str(backup), source=str(source), acceptanceRuns=runs)
    (LOG / ("canonical-build-promotion-" + args.content_sha + ".json")).write_text(json.dumps(receipt, indent=2))
    (LOG / "canonical-build-promotion.json").write_text(json.dumps(receipt, indent=2))
    print(json.dumps(receipt), flush=True)


if __name__ == "__main__":
    main()
