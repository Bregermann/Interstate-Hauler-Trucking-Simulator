"""Run one owned Windows acceptance process and retain its exact build receipt."""
import argparse
import hashlib
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

DRAGON_SCENES = {'TruckDragonRiders', 'TruckHoardHeist', 'TruckDragonEggDelivery', 'TruckDragonEscape'}

def dragon_terminal(evidence, pid, extra_args):
    """Validate this player's own terminal JSON, never an unrelated old receipt."""
    data = json.loads(evidence)
    if data.get('platform') != 'WindowsPlayer' or data.get('pid') != pid:
        raise ValueError('Dragon acceptance requires the actual owned Windows process receipt.')
    if not str(data.get('scope', '')).startswith('ACTUAL_WINDOWS_AI_OBSERVATION_'):
        raise ValueError('Unknown Dragon acceptance scope.')
    requested = DRAGON_SCENES
    if '-tsDragon08Scenes' in extra_args:
        at = extra_args.index('-tsDragon08Scenes')
        requested = set(extra_args[at + 1].split(','))
    if not requested or not requested.issubset(DRAGON_SCENES):
        raise ValueError('Use actual registered Dragon scene names.')
    visits = data.get('visits', [])
    if len(visits) != len(requested) or {visit.get('scene') for visit in visits} != requested:
        raise ValueError('Dragon receipt does not cover every requested scene exactly once.')
    complete = '-tsDragon08Complete' in extra_args
    if data.get('completeRequested') != complete or data.get('splitAIViews') != ('-tsDragon08AIViews' in extra_args):
        raise ValueError('Dragon receipt does not match requested match/camera scope.')
    checks, errors = data.get('checks'), data.get('failures')
    if type(checks) is not int or checks <= 0 or type(errors) is not int or errors < 0:
        raise ValueError('Dragon receipt requires real executed check counts.')
    if errors == 0:
        if not all(visit.get('rematch') is True for visit in visits):
            raise ValueError('Missing ordinary rematch acceptance.')
        if complete and not all(visit.get('finished') is True and visit.get('physicalOutcomes', 0) > 0
                                and visit.get('stopReason') == 'ORDINARY_RESULTS' for visit in visits):
            raise ValueError('A deadline or no physical outcome is not a completed Dragon match.')
        if data.get('captures', 0) < len(visits) * 2:
            raise ValueError('Actual Windows images are required for each requested scene.')
    return checks, errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("name")
    parser.add_argument("--scenes")
    parser.add_argument("--flag", default="-tsBroadcastReadability")
    parser.add_argument("--output-flag", default="-tsBroadcastOutput")
    parser.add_argument("--timeout", type=int, default=1200)
    parser.add_argument("--mode")
    parser.add_argument("--graphics-api", choices=["d3d11", "d3d12"], default="d3d11", help="Explicit Windows rendering backend for the recorded acceptance or diagnostic run.")
    parser.add_argument("--result-file")
    parser.add_argument("--extra-arg", action="append", default=[])
    parser.add_argument("--legacy-errors-only", action="store_true", help="Accept an explicit legacy ERRORS=N. footer and count its actual PASS/FAIL check lines.")
    args = parser.parse_args()
    exe = ROOT / "Builds/TruckSportsCamera/TruckSports.exe"
    dll = exe.parent / "TruckSports_Data/Managed/Assembly-CSharp.dll"
    digest = hashlib.sha256(dll.read_bytes()).hexdigest()
    content, _ = snapshot(exe.parent)
    folder = ROOT / "Logs/TruckSports/PlaytestRepair" / args.name
    if args.flag == '-tsDragon08' and folder.exists():
        raise ValueError('Preserve earlier Dragon Windows evidence; choose a fresh run name.')
    folder.mkdir(parents=True, exist_ok=True)
    command = [str(exe), args.flag]
    if args.mode:
        command += [args.mode]
    command += args.extra_arg
    command += [args.output_flag, str(folder), "-force-" + args.graphics_api, "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-logFile", str(folder / "player.log")]
    if args.scenes:
        command += ["-tsBroadcastScenes", args.scenes]
    info = subprocess.STARTUPINFO()
    info.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    # Unity's graphics swap chain needs a visible game window for real captures.
    # This is the requested player acceptance run, not a background service.
    info.wShowWindow = 1
    start = time.monotonic()
    wall_start = time.time()
    process = subprocess.Popen(command, cwd=ROOT, startupinfo=info)
    print("Owned Windows player", process.pid, "managed", digest, "full content", content, flush=True)
    try:
        code = process.wait(timeout=args.timeout)
    except subprocess.TimeoutExpired:
        process.terminate()
        code = process.wait(timeout=30)
        print("Owned acceptance process exceeded its watchdog", flush=True)
    result = (ROOT / args.result_file).resolve() if args.result_file else folder / ("result.json" if args.flag == '-tsDragon08' else "result.txt")
    if not result.is_relative_to(ROOT):
        raise ValueError("Acceptance result must remain inside this workspace")
    evidence = result.read_text(encoding="utf-8-sig") if result.is_file() and result.stat().st_mtime >= wall_start else ""
    terminal = re.search(r"CHECKS=(\d+)\s+ERRORS=(\d+)", evidence)
    legacy = re.search(r"ERRORS=(\d+)\. (?:Actual NWH/PhysX|Generated ramp and real NWH/PhysX flights|Core-Collapse native physics/support slice|Software-injected sources and native physical workload|Separate pure rule fixtures and actual native NWH/PhysX full autonomous matches|Actual PhysX/NWH-disabled gravity platforms and AI weight controls|Labelled initial poses/velocities followed by actual PhysX triggers|Automated posed fixtures and actual Unity runtime)", evidence) if args.legacy_errors_only and not terminal else None
    if args.legacy_errors_only and not terminal and not legacy:
        # Accept only the explicitly requested existing probe's own terminal
        # signature. Never treat an arbitrary ERRORS=0 line as completion.
        protected_footers = {
            "-tsCornholeHole": r"(?m)^ERRORS=(\d+) CHECKS=(\d+) PID=(\d+)\. Real PhysX drops through unchanged opening; injected launch setup, not human throws\.$",
            "-tsPong": r"ERRORS=(\d+)\. Native physical NWH approach, charge/release, once-only spring, hollow cup contacts and penetration\. No human/hardware/GPU acceptance claim\.",
            "-tsParking": r"ERRORS=(\d+)\. Actual NWH/PhysX, autonomous AI matches and native music/pause observation; no hardware/human or subjective audio validation\.",
            "-tsBuzkashi": r"ERRORS=(\d+)\. Native NWH/PhysX six-truck five-minute play, physical cargo constraint and complete match; no human/hardware validation\.",
            "-tsDodgeProbe": r"ERRORS=(\d+)\. Rule fixtures labelled separately from actual NWH/PhysX trampoline, strike and ball-contact matches\. No injected outcomes, human/hardware or rendered performance claim\.",
            "-tsBog": r"ERRORS=(\d+)\. Native NWH/PhysX buoyancy/drag/air/dive race series; no hardware/human playtest or frame-rate benchmark\.",
            "-tsFishing": r"ERRORS=(\d+)\. Native fixed-step generated-fish/swept-net gameplay with autonomous AI; rule fixtures are separate\.",
            "-tsLogProbe": r"ERRORS=(\d+)\. Actual vendor buoyancy and NWH/PhysX match simulation, no injected results\.",
            "-tsSkeeSoloResults": r"ERRORS=(\d+)\. Actual saved NWH/EasyRoads/PhysX ramp simulation and AI matches; no injected scores\.",
            "-tsSkee": r"ERRORS=(\d+)\. Actual saved NWH/EasyRoads/PhysX ramp simulation and AI matches; no injected scores\.",
            "-tsSwing": r"ERRORS=(\d+)\. Actual PhysX pendulum torque/joints, inherited release and first contact; no human/hardware validation\.",
            "-tsSwingFocused": r"ERRORS=(\d+)\. Real PhysX joints, torque, release velocity, flight and contact; synthetic commands/virtual sources; camera-only renders\.",
            "-tsLegacyLook": r"ERRORS=(\d+)\. Native saved modes and injected pad/mouse input, steering/cursor policies and controlled UI renders;",
            "-tsWallProduction": r"ERRORS=(\d+)\. Labelled spawn fixture, injected keyboard opening and actual NWH/PhysX 4v4 match/volleys, controlled UI captures\.",
            "-tsPongFocused": r"ERRORS=(\d+)\. Physical seeded fixtures and actual injected human controls are distinguished above;",
            "-tsSumo": r"ERRORS=(\d+)\. Native NWH/PhysX match simulation; no human, hardware, audio or sustained GPU acceptance claimed\.",
        }
        signature = protected_footers.get(args.flag)
        if signature:
            legacy = re.search(signature, evidence)
            if legacy and args.flag == "-tsCornholeHole":
                observed_checks = len(re.findall(r"^(?:PASS|FAIL) ", evidence, re.MULTILINE))
                if int(legacy[2]) != observed_checks or int(legacy[3]) != process.pid:
                    legacy = None
    checks = int(terminal[1]) if terminal else len(re.findall(r"^(?:PASS|FAIL) ", evidence, re.MULTILINE)) if legacy else None
    errors = int(terminal[2]) if terminal else int(legacy[1]) if legacy else None
    completed = bool(terminal) or bool(legacy and checks)
    if args.flag == '-tsDragon08' and evidence:
        checks, errors = dragon_terminal(evidence, process.pid, args.extra_arg)
        completed = True
    # These original probes predate CHECKS=/ERRORS= footers. Require their
    # specific terminal evidence, real process exit and failure-free transcript;
    # an arbitrary PASS or an old results file cannot establish completion.
    if args.legacy_errors_only and not completed and evidence:
        actual_checks = len(re.findall(r"^(?:PASS|FAIL) ", evidence, re.MULTILINE))
        actual_errors = len(re.findall(r"^(?:FAIL |ERROR |RUNTIME ERROR |INTERRUPTED)", evidence, re.MULTILINE))
        if args.flag in {"-tsCheese", "-tsCheeseGuards", "-tsCheeseForecast"}:
            count = re.search(r"^CHECKS (\d+)$", evidence, re.MULTILINE)
            player_log = (folder / "player.log").read_text(encoding="utf-8-sig", errors="replace")
            completed = bool(count and int(count[1]) == actual_checks and "CHEESE_SMOKE_FINISHED code=0" in player_log and code == 0)
        elif args.flag == "-tsCurling":
            completed = evidence.rstrip().endswith("PASS Curling returns cleanly to collection") and code == 0
        elif args.flag == "-tsBase":
            completed = evidence.rstrip().endswith("PASS Base Jumping returns cleanly and scoped weather pipeline restores") and code == 0
        if completed:
            checks, errors = actual_checks, actual_errors
    if args.result_file and evidence:
        shutil.copy2(result, folder / "result.txt")
    receipt = dict(pid=process.pid, exit=code, seconds=round(time.monotonic()-start, 2), requestedGraphicsAPI=args.graphics_api, managedSha256=digest, buildContentSha256=content, unchangedBuildContent=snapshot(exe.parent)[0]==content, unchangedManaged=hashlib.sha256(dll.read_bytes()).hexdigest()==digest, completedEvidence=completed, checks=checks, errors=errors, legacyErrorsOnly=bool(args.legacy_errors_only and not terminal and completed))
    (folder / "process.json").write_text(json.dumps(receipt, indent=2))
    print(json.dumps(receipt), flush=True)
    if evidence:
        print("\n".join(evidence.splitlines()[-3:]), flush=True)
    raise SystemExit(code or (0 if completed and errors==0 and receipt["unchangedManaged"] and receipt["unchangedBuildContent"] else 1))


if __name__ == "__main__":
    main()
