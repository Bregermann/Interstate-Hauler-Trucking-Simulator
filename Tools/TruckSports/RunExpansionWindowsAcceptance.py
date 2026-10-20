"""Explicit scoped build and actual Windows acceptance for completed expansion slices."""
import argparse
import hashlib
import json
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LOG = ROOT / "Logs/TruckSports/EighteenSports"
MODES = {"hot-potato": "-tsHotPotato", "red-light": "-tsRedLight", "balloon": "-tsBalloon", "whack": "-tsWhack", "hill": "-tsHill", "snow": "-tsExpansionSnow", "menu": "-tsRetroMenu"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--skip-build", action="store_true")
    parser.add_argument("--modes", nargs="+", choices=MODES, default=list(MODES))
    parser.add_argument("--unity", default="C:/Program Files/Unity/Hub/Editor/6000.4.10f1/Editor/Unity.exe")
    args = parser.parse_args()
    LOG.mkdir(parents=True, exist_ok=True)
    if not args.skip_build:
        project = ROOT / ".sports-validation/TruckSportsPublicValidation"
        jobs = [
            ("LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Upgrade", "hot-potato-delivery-authoring.log"),
            ("LWS.TruckRedLightGreenLight.Editor.RedLightAuthoring.Upgrade", "red-light-delivery-authoring.log"),
            ("LWS.TruckBalloonBattle.Editor.BalloonAuthoring.Upgrade", "balloon-delivery-authoring.log"),
            ("LWS.TruckKingOfTheHill.Editor.HillIntegration.Menu", "hill-menu-integration.log"),
            ("LWS.TruckWhackAMole.Editor.WhackIntegration.Menu", "whack-menu-integration.log"),
            ("LWS.TruckBalloonBattle.Editor.BalloonIntegration.Menu", "balloon-menu-integration.log"),
            ("LWS.TruckSports.Editor.SportsBuilder.BuildFocused", "windows-build-verified.log"),
        ]
        for method, name in jobs:
            process = subprocess.Popen([args.unity, "-batchmode", "-quit", "-projectPath", str(project), "-executeMethod", method, "-logFile", str(LOG / name)], cwd=ROOT)
            print(method, "PID", process.pid, flush=True)
            code = process.wait()
            print(method, "exit", code, flush=True)
            if code:
                raise SystemExit(code)
    dll = ROOT / "Builds/TruckSports/TruckSports_Data/Managed/Assembly-CSharp.dll"
    digest = hashlib.sha256(dll.read_bytes()).hexdigest()
    results = []
    for mode in args.modes:
        # This is an interactive graphics acceptance player, not a hidden
        # service: hiding its initial swap chain can produce a black first frame.
        info = subprocess.STARTUPINFO()
        info.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        info.wShowWindow = 1
        command = [str(ROOT / "Builds/TruckSports/TruckSports.exe"), "-screen-width", "1366", "-screen-height", "768", "-screen-fullscreen", "0", "-force-d3d11", MODES[mode], "-logFile", str(LOG / (mode + "-windows-verified.log"))]
        started = time.time()
        process = subprocess.Popen(command, cwd=ROOT, startupinfo=info)
        print(mode, "Windows PID", process.pid, flush=True)
        try:
            code = process.wait(timeout=720)
        except subprocess.TimeoutExpired:
            process.terminate()
            process.wait(timeout=30)
            code = 124
        entry = {"probe": mode, "exit": code, "seconds": round(time.time() - started, 2), "managedSha256": digest, "unchangedManaged": hashlib.sha256(dll.read_bytes()).hexdigest() == digest}
        results.append(entry)
        (LOG / "windows-verified-processes.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
        print(json.dumps(entry), flush=True)
    raise SystemExit(0 if all(item["exit"] == 0 and item["unchangedManaged"] for item in results) else 2)


if __name__ == "__main__":
    main()
