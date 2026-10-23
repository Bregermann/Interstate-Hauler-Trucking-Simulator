"""Scoped sequential validation; keep the accepted build until camera acceptance passes."""
import argparse
import hashlib
import json
import shutil
import subprocess
import time
import xml.etree.ElementTree as ET
import zipfile
import sys
sys.dont_write_bytecode = True
from BuildIdentity import snapshot
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / ".sports-validation/TruckSportsPublicValidation"
LOG = ROOT / "Logs/TruckSports/PlaytestRepair"
UNITY = "C:/Program Files/Unity/Hub/Editor/6000.4.10f1/Editor/Unity.exe"


def retain_sources(digest):
    """Keep the exact compiled owned sources, even while ROOT work continues."""
    manifest = LOG / ("build-" + digest + "-sources.json")
    entries = json.loads(manifest.read_text(encoding="utf-8"))
    archive = LOG / ("build-" + digest + "-sources.zip")
    if archive.exists():
        with zipfile.ZipFile(archive) as prior:
            assert prior.read("inputs.json") == manifest.read_bytes(), "Never reuse an archive key for different source inputs"
        return
    payloads = []
    for item in entries:
        relative = Path(item["path"])
        assert relative.as_posix().startswith("Assets/Truck") and relative.suffix == ".cs"
        source = (CLEAN / relative).resolve()
        assert source.is_relative_to(CLEAN.resolve())
        data = source.read_bytes()
        assert hashlib.sha256(data).hexdigest() == item["sha256"], relative
        payloads.append((relative.as_posix(), data))
    with zipfile.ZipFile(archive, "x", compression=zipfile.ZIP_DEFLATED) as output:
        output.writestr("inputs.json", manifest.read_bytes())
        for relative, data in payloads:
            output.writestr(relative, data)
    print("Retained", len(payloads), "exact compiled source files", archive.name, flush=True)


def record_build():
    folder = ROOT / "Builds/TruckSportsCamera"
    managed = hashlib.sha256((folder / "TruckSports_Data/Managed/Assembly-CSharp.dll").read_bytes()).hexdigest()
    content, files = snapshot(folder)
    (LOG / ("build-" + content + "-files.json")).write_text(json.dumps(files, indent=2), encoding="utf-8")
    shutil.copy2(LOG / "camera-source-sync.json", LOG / ("build-" + content + "-sources.json"))
    retain_sources(content)
    identity = dict(managedSha256=managed, buildContentSha256=content, files=len(files))
    (LOG / "current-camera-build.json").write_text(json.dumps(identity, indent=2), encoding="utf-8")
    print("Exact code AND asset build identity", json.dumps(identity), flush=True)


def sync(deferred_path=None):
    baseline = {p["path"] for p in json.loads((LOG / "host-pending-baseline.json").read_text())}
    changed = set(filter(None, (subprocess.check_output(["git", "diff", "--name-only", "-z", "HEAD"], cwd=ROOT, stderr=subprocess.PIPE) + subprocess.check_output(["git", "ls-files", "--others", "--exclude-standard", "-z"], cwd=ROOT)).decode().split("\0")))
    # A scoped milestone commit must not make the next build forget which
    # already-tested repair sources it contains. Retain immutable prior inputs.
    known = set()
    for manifest in LOG.glob("build-*-sources.json"):
        known.update(v["path"] for v in json.loads(manifest.read_text()))
    sources = sorted(p for p in (changed | known) - baseline if p.startswith("Assets/Truck") and p.endswith(".cs"))
    deferred = set(json.loads(Path(deferred_path).read_text())) if deferred_path else set()
    if deferred:
        # Preserve a frozen tested source slice while the user-authorized next
        # feature is being edited in ROOT. Existing deferred inputs must still
        # match the exact prior build; never silently reuse unknown CLEAN code.
        identity = json.loads((LOG / "current-camera-build.json").read_text())
        previous = {item["path"]: item["sha256"] for item in json.loads((LOG / ("build-" + identity["buildContentSha256"] + "-sources.json")).read_text())}
        for relative in deferred:
            if relative in previous:
                assert hashlib.sha256((CLEAN / relative).read_bytes()).hexdigest() == previous[relative], relative
            else:
                assert not (CLEAN / relative).exists(), relative
        sources = [relative for relative in sources if relative not in deferred or relative in previous]
    for relative in sources:
        if relative in deferred:
            continue
        source, target = ROOT / relative, CLEAN / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
    (LOG / "camera-source-sync.json").write_text(json.dumps([dict(path=p, sha256=hashlib.sha256((CLEAN / p).read_bytes()).hexdigest()) for p in sources], indent=2))
    if deferred:
        (LOG / "deferred-source-slice.json").write_text(json.dumps(sorted(deferred), indent=2))
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
        record_build()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("stage", choices=["parking-bays", "hockey-hud", "snake-traffic", "snake-town", "snake-handling", "snake-guidance", "wife-camera", "compass-margin", "bind", "repair-author", "sky-author", "fare-author", "mine-author", "mine-sky", "corn-sky", "trailer-audit", "trailer-author", "field-audit", "field-holder", "field-audio", "keepaway-arena", "base-author", "wife-hud", "time-audit", "time-collection-audit", "time-author", "time-all-author", "time-legacy-audit", "time-legacy-author", "tests", "build", "sync"])
    parser.add_argument("--test-filter", default="LWS.TruckSports.Tests.BroadcastReadabilityTests")
    parser.add_argument("--test-name", default="camera-editmode")
    parser.add_argument("--test-platform", choices=["EditMode", "PlayMode"], default="EditMode")
    parser.add_argument("--defer-sources", help="Explicit owned WIP paths; existing CLEAN inputs must match the preceding frozen build.")
    parser.add_argument("--execute-owned", help="Explicit additional LWS.TruckSports.Editor method for focused additive authoring; use with stage sync.")
    args = parser.parse_args()
    LOG.mkdir(parents=True, exist_ok=True)
    # Call only when this project has no other owned Editor/player. The caller
    # establishes the exclusive turn; no process-killing or project-lock bypass.
    sync(args.defer_sources)
    if args.execute_owned:
        assert args.stage == "sync" and args.execute_owned.startswith("LWS.TruckSports.Editor."), "Only an explicit owned authoring method is accepted here"
        run("score-presentation-authoring", ["-quit", "-executeMethod", args.execute_owned])
        return
    if args.stage == "time-legacy-audit":
        run("sky-legacy-layout-audit", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsEnvironmentAuthoring.AuditLegacy"])
    if args.stage == "parking-bays":
        run("parking-bay-appearance-authoring", ["-quit", "-executeMethod", "LWS.TruckMusicalParking.Editor.ParkingBayAppearanceAuthoring.Upgrade"])
    elif args.stage == "hockey-hud":
        run("hockey-presentation-authoring", ["-quit", "-executeMethod", "LWS.TruckHockey.Editor.HockeyPresentationAuthoring.Upgrade"])
    elif args.stage == "snake-traffic":
        run("snake-original-traffic-lanes-authoring", ["-quit", "-executeMethod", "LWS.TruckSnake.Editor.SnakePlaytestRepairAuthoring.UpgradeTraffic"])
    elif args.stage == "snake-town":
        run("snake-town-infill-authoring", ["-quit", "-executeMethod", "LWS.TruckSnake.Editor.SnakePlaytestRepairAuthoring.UpgradeTown"])
    elif args.stage == "snake-handling":
        run("snake-mode-handling-authoring", ["-quit", "-executeMethod", "LWS.TruckSnake.Editor.SnakePlaytestRepairAuthoring.UpgradeHandling"])
    elif args.stage == "snake-guidance":
        run("snake-pickup-guidance-authoring", ["-quit", "-executeMethod", "LWS.TruckSnake.Editor.SnakePlaytestRepairAuthoring.UpgradeGuidance"])
    elif args.stage == "wife-camera":
        run("wife-start-camera-authoring", ["-quit", "-executeMethod", "LWS.TruckWifeCarrying.Editor.WifeHudRepairAuthoring.UpgradeStartupCamera"])
    elif args.stage == "compass-margin":
        run("camera-compass-margin", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsBroadcastCameraAuthoring.UpgradeCompassMargin"])
    elif args.stage == "bind":
        run("camera-bind", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsBroadcastCameraAuthoring.Bind"])
    elif args.stage == "repair-author":
        run("repair-authoring", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsPlaytestRepairAuthoring.Apply"])
    elif args.stage == "sky-author":
        run("skytow-repair-authoring", ["-quit", "-executeMethod", "LWS.TruckSkyTow.Editor.SkyTowRepairAuthoring.Upgrade"])
    elif args.stage == "wife-hud":
        run("wife-compact-checkpoint-authoring", ["-quit", "-executeMethod", "LWS.TruckWifeCarrying.Editor.WifeHudRepairAuthoring.Upgrade"])
    elif args.stage == "base-author":
        run("base-flare-smoke-authoring", ["-quit", "-executeMethod", "LWS.TruckBaseJumping.Editor.BaseJumpPlaytestRepairAuthoring.Upgrade"])
    elif args.stage == "fare-author":
        run("farefight-repair-authoring", ["-quit", "-executeMethod", "LWS.TruckFareFight.Editor.FareFightRepairAuthoring.Upgrade"])
    elif args.stage == "mine-author":
        run("minefield-repair-authoring", ["-quit", "-executeMethod", "LWS.TruckMinefield.Editor.MinefieldPlaytestRepairAuthoring.Upgrade"])
    elif args.stage == "mine-sky":
        run("minefield-viewport-sky-authoring", ["-quit", "-executeMethod", "LWS.TruckMinefield.Editor.MinefieldPlaytestRepairAuthoring.UpgradeViewportSky"])
    elif args.stage == "corn-sky":
        run("cornhole-viewport-sky-authoring", ["-quit", "-executeMethod", "LWS.TruckCornhole.Editor.CornholeWeatherAuthoring.UpgradeViewportSky"])
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
    elif args.stage == "time-audit":
        run("time-environment-audit", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsEnvironmentAuthoring.Audit"])
    elif args.stage == "time-collection-audit":
        run("time-collection-audit", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsEnvironmentAuthoring.AuditCollection"])
    elif args.stage == "time-author":
        run("time-representative-install", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsEnvironmentAuthoring.InstallRepresentative"])
    elif args.stage == "time-all-author":
        run("time-collection-install", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsEnvironmentAuthoring.InstallApplicable"])
    elif args.stage == "time-legacy-author":
        run("sky-legacy-time-install", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsEnvironmentAuthoring.InstallLegacyTime"])
    elif args.stage == "tests":
        run(args.test_name, ["-runTests", "-testPlatform", args.test_platform, "-testFilter", args.test_filter, "-testResults", str(LOG / (args.test_name+".xml"))])
        result = ET.parse(LOG / (args.test_name + ".xml")).getroot()
        assert int(result.attrib.get("passed", 0)) > 0 and int(result.attrib.get("failed", 0)) == 0, result.attrib
        print("Native test result", result.attrib, flush=True)
    elif args.stage == "build":
        run("camera-build", ["-quit", "-executeMethod", "LWS.TruckSports.Editor.SportsBuilder.BuildFocused", "-tsBuildOutput", "Builds/TruckSportsCamera/TruckSports.exe"])


if __name__ == "__main__":
    main()
