"""Scoped sequential validation; keep the accepted build until camera acceptance passes."""
import argparse
import hashlib
import json
import shutil
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / ".sports-validation/TruckSportsPublicValidation"
LOG = ROOT / "Logs/TruckSports/PlaytestRepair"
UNITY = "C:/Program Files/Unity/Hub/Editor/6000.4.10f1/Editor/Unity.exe"


def sync():
    baseline = {p["path"] for p in json.loads((LOG / "host-pending-baseline.json").read_text())}
    changed = set(filter(None, (subprocess.check_output(["git", "diff", "--name-only", "-z", "HEAD"], cwd=ROOT, stderr=subprocess.PIPE) + subprocess.check_output(["git", "ls-files", "--others", "--exclude-standard", "-z"], cwd=ROOT)).decode().split("\0")))
    sources = sorted(p for p in changed - baseline if p.startswith("Assets/Truck") and p.endswith(".cs"))
    for relative in sources:
        source, target = ROOT / relative, CLEAN / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
    (LOG / "camera-source-sync.json").write_text(json.dumps([dict(path=p, sha256=hashlib.sha256((ROOT / p).read_bytes()).hexdigest()) for p in sources], indent=2))
    print("Synced", len(sources), "scoped sources; excluded pre-existing pending work.", flush=True)


def run(name, arguments):
    started = time.monotonic()
    previous_log = LOG / (name + ".log")
    if previous_log.exists():
        history = LOG / "History"
        history.mkdir(exist_ok=True)
        shutil.copy2(previous_log, history / (name + "-" + str(time.time_ns()) + ".log"))
    command = [UNITY, "-batchmode", "-projectPath", str(CLEAN), *arguments, "-logFile", str(LOG / (name + ".log"))]
    process = subprocess.Popen(command, cwd=ROOT)
    print(name, "Editor PID", process.pid, flush=True)
    code = process.wait()
    receipt = dict(job=name, pid=process.pid, exit=code, seconds=round(time.monotonic()-started, 2))
    path = LOG / "camera-validation-processes.json"
    records = json.loads(path.read_text()) if path.exists() else []
    records.append(receipt)
    path.write_text(json.dumps(records, indent=2))
    print(json.dumps(receipt), flush=True)
    if code:
        raise SystemExit(code)
    if name == "camera-build":
        dll = ROOT / "Builds/TruckSportsCamera/TruckSports_Data/Managed/Assembly-CSharp.dll"
        digest = hashlib.sha256(dll.read_bytes()).hexdigest()
        shutil.copy2(LOG / "camera-source-sync.json", LOG / ("build-" + digest + "-sources.json"))
        print("Retained exact build/source snapshot", digest, flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("stage", choices=["bind", "repair-author", "sky-author", "fare-author", "mine-author", "tests", "build", "sync"])
    parser.add_argument("--test-filter", default="LWS.TruckSports.Tests.BroadcastReadabilityTests")
    parser.add_argument("--test-name", default="camera-editmode")
    parser.add_argument("--test-platform", choices=["EditMode", "PlayMode"], default="EditMode")
    args = parser.parse_args()
    LOG.mkdir(parents=True, exist_ok=True)
    # Call only when this project has no other owned Editor/player. The caller
    # establishes the exclusive turn; no process-killing or project-lock bypass.
    sync()
    if args.stage == "bind":
        run("camera-bind", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsBroadcastCameraAuthoring.Bind"])
    elif args.stage == "repair-author":
        run("repair-authoring", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsPlaytestRepairAuthoring.Apply"])
    elif args.stage == "sky-author":
        run("skytow-repair-authoring", ["-quit", "-executeMethod", "LWS.TruckSkyTow.Editor.SkyTowRepairAuthoring.Upgrade"])
    elif args.stage == "fare-author":
        run("farefight-repair-authoring", ["-quit", "-executeMethod", "LWS.TruckFareFight.Editor.FareFightRepairAuthoring.Upgrade"])
    elif args.stage == "mine-author":
        run("minefield-repair-authoring", ["-quit", "-executeMethod", "LWS.TruckMinefield.Editor.MinefieldPlaytestRepairAuthoring.Upgrade"])
    elif args.stage == "tests":
        run(args.test_name, ["-runTests", "-testPlatform", args.test_platform, "-testFilter", args.test_filter, "-testResults", str(LOG / (args.test_name+".xml"))])
    elif args.stage == "build":
        run("camera-build", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsBuilder.BuildFocused", "-tsBuildOutput", "Builds/TruckSportsCamera/TruckSports.exe"])


if __name__ == "__main__":
    main()
