"""Scoped sequential validation; keep the accepted build until camera acceptance passes."""
import argparse
import hashlib
import json
import shutil
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / ".sports-validation/TruckSportsPublicValidation"
LOG = ROOT / "Logs/TruckSports/PlaytestRepair"
UNITY = "C:/Program Files/Unity/Hub/Editor/6000.4.10f1/Editor/Unity.exe"


def sync():
    baseline = {p["path"] for p in json.loads((LOG / "host-pending-baseline.json").read_text())}
    changed = set(filter(None, (subprocess.check_output(["git", "diff", "--name-only", "-z", "HEAD"], cwd=ROOT, stderr=subprocess.PIPE) + subprocess.check_output(["git", "ls-files", "--others", "--exclude-standard", "-z"], cwd=ROOT)).decode().split("\0")))
    # A scoped milestone commit must not make the next build forget which
    # already-tested repair sources it contains. Retain immutable prior inputs.
    known = set()
    for manifest in LOG.glob("build-*-sources.json"):
        known.update(v["path"] for v in json.loads(manifest.read_text()))
    sources = sorted(p for p in (changed | known) - baseline if p.startswith("Assets/Truck") and p.endswith(".cs"))
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
    previous_xml = LOG / (name + ".xml")
    if previous_xml.exists():
        history = LOG / "History"
        history.mkdir(exist_ok=True)
        shutil.copy2(previous_xml, history / (name + "-" + str(time.time_ns()) + ".xml"))
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
    parser.add_argument("stage", choices=["bind", "repair-author", "sky-author", "fare-author", "mine-author", "trailer-audit", "trailer-author", "field-audit", "field-holder", "field-audio", "keepaway-arena", "tests", "build", "sync"])
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
    elif args.stage == "trailer-audit":
        run("trailer-geometry-audit", ["-quit", "-executeMethod", "LWS.TruckTrailerPlug.Editor.TrailerPlugRepairAuthoring.Audit"])
    elif args.stage == "trailer-author":
        run("trailer-rig-repair-authoring", ["-quit", "-executeMethod", "LWS.TruckTrailerPlug.Editor.TrailerPlugRepairAuthoring.Upgrade"])
    elif args.stage == "field-audit":
        run("field-goal-audio-holder-audit", ["-quit", "-executeMethod", "LWS.TruckFieldGoal.Editor.FieldGoalPlaytestRepairAuthoring.Audit"])
    elif args.stage == "field-holder":
        run("field-goal-holder-repair", ["-quit", "-executeMethod", "LWS.TruckFieldGoal.Editor.FieldGoalPlaytestRepairAuthoring.UpgradeHolder"])
    elif args.stage == "field-audio":
        run("field-goal-audio-repair", ["-quit", "-executeMethod", "LWS.TruckFieldGoal.Editor.FieldGoalPlaytestRepairAuthoring.UpgradeAudio"])
    elif args.stage == "keepaway-arena":
        # Author with the installed owned tool without adding its pending vendor
        # source to this task's public Git payload. Public authoring references
        # it optionally through reflection; saved precut meshes need no slicer.
        vendor = ROOT / "Assets/CoreCollapse/EzySlice"
        copied = []
        for source in vendor.rglob("*"):
            if source.is_file() and (source.suffix == ".cs" or source.name.endswith(".meta")):
                relative = source.relative_to(ROOT)
                target = CLEAN / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
                copied.append(dict(path=relative.as_posix(), sha256=hashlib.sha256(source.read_bytes()).hexdigest()))
        (LOG / "keepaway-private-authoring-tool.json").write_text(json.dumps(copied, indent=2))
        run("keepaway-arena-repair", ["-quit", "-executeMethod", "LWS.TruckKeepaway.Editor.KeepawayArenaRepairAuthoring.Upgrade"])
        shutil.copy2(CLEAN / "Logs/TruckSports/PlaytestRepair/keepaway-arena-migration.txt", LOG / "keepaway-arena-migration.txt")
    elif args.stage == "tests":
        run(args.test_name, ["-runTests", "-testPlatform", args.test_platform, "-testFilter", args.test_filter, "-testResults", str(LOG / (args.test_name+".xml"))])
        result = ET.parse(LOG / (args.test_name + ".xml")).getroot()
        assert int(result.attrib.get("passed", 0)) > 0 and int(result.attrib.get("failed", 0)) == 0, result.attrib
        print("Native test result", result.attrib, flush=True)
    elif args.stage == "build":
        run("camera-build", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsBuilder.BuildFocused", "-tsBuildOutput", "Builds/TruckSportsCamera/TruckSports.exe"])


if __name__ == "__main__":
    main()
