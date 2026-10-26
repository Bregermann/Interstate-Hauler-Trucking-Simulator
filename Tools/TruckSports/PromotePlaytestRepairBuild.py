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
from WorkspaceProcessSafety import workspace_process

ROOT = Path(__file__).resolve().parents[2]
LOG = ROOT / "Logs/TruckSports/PlaytestRepair"
EXPECTED = "c8b0b9c1aed92af5778f7bb0be10fbb3950be7b75d5a6dfa8e9be8fb62c64890"


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def coordinated_batch_editor(observed, permitted_pid):
    """Validate the one explicitly coordinated, independent build exception."""
    if permitted_pid is None:
        return None
    assert permitted_pid > 0
    for process in observed:
        if process['ProcessId'] != permitted_pid:
            continue
        command = process.get('CommandLine') or ''
        project = re.search(r'-projectPath\s+(?:"([^"]+)"|(\S+))', command, re.I)
        method = re.search(r'-executeMethod\s+(\S+)', command, re.I)
        assert process['Name'] == 'Unity.exe' and project and method, "Only the explicitly coordinated independent batch build may overlap promotion"
        assert Path(project[1] or project[2]).resolve() == ROOT.resolve()
        assert method[1] == 'DeadAir.Editor.DeadAirProductionValidation.BuildWindows'
        assert re.search(r'(?:^|\s)-batchmode(?:\s|$)', command, re.I) and re.search(r'(?:^|\s)-quit(?:\s|$)', command, re.I)
        return dict(pid=process['ProcessId'], project=str(ROOT), entryMethod=method[1])
    return None  # The approved build may have exited; every other process still blocks.


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sha", default=EXPECTED)
    parser.add_argument("--content-sha", required=True, help="Exact full executable/dependency/asset fingerprint")
    parser.add_argument("--run", action="append", help="Exact completed acceptance run Name:CheckCount")
    parser.add_argument("--concurrent-build-editor-pid", type=int, help="Explicit coordinated exception for the independent ROOT DeadAir BuildWindows batch process. Promotion still refuses every workspace player and other Editor.")
    args = parser.parse_args()
    assert re.fullmatch(r"[a-f0-9]{64}", args.sha)
    expected = args.sha
    assert re.fullmatch(r"[a-f0-9]{64}", args.content_sha)
    assert snapshot(ROOT / "Builds/TruckSportsCamera")[0] == args.content_sha
    processes = subprocess.check_output([
        "powershell", "-NoProfile", "-Command",
        "Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('Unity.exe','TruckSports.exe') } | Select-Object ProcessId,Name,ExecutablePath,CommandLine | ConvertTo-Json -Compress",
    ], text=True).strip()
    observed = json.loads(processes) if processes else []
    if isinstance(observed, dict):
        observed = [observed]
    concurrent_build = coordinated_batch_editor(observed, args.concurrent_build_editor_pid)
    # A manually launched archived build elsewhere does not hold these files.
    # Never close it. Unknown paths and other Editors still conservatively block.
    # Only the exact coordinated independent batch-build PID can be exempted.
    running = [p for p in observed if (concurrent_build is None or p['ProcessId'] != concurrent_build['pid']) and workspace_process(p, ROOT)]
    if running:
        raise RuntimeError("Wait for the workspace Editor/player to close before promotion: " + json.dumps([dict(pid=p['ProcessId'], name=p['Name']) for p in running]))
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
    receipt['unrelatedEditorProcessesIgnored'] = [dict(pid=p['ProcessId']) for p in observed if p['Name'] == 'Unity.exe' and not workspace_process(p, ROOT)]
    if concurrent_build is not None:
        receipt['concurrentIndependentBatchBuild'] = concurrent_build
    (LOG / ("canonical-build-promotion-" + args.content_sha + ".json")).write_text(json.dumps(receipt, indent=2))
    (LOG / "canonical-build-promotion.json").write_text(json.dumps(receipt, indent=2))
    print(json.dumps(receipt), flush=True)


if __name__ == "__main__":
    main()
