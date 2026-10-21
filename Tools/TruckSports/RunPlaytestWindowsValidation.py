"""Run one owned Windows acceptance process and retain its exact build receipt."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("name")
    parser.add_argument("--scenes")
    parser.add_argument("--flag", default="-tsBroadcastReadability")
    parser.add_argument("--output-flag", default="-tsBroadcastOutput")
    parser.add_argument("--timeout", type=int, default=1200)
    parser.add_argument("--mode")
    parser.add_argument("--result-file")
    parser.add_argument("--extra-arg", action="append", default=[])
    args = parser.parse_args()
    exe = ROOT / "Builds/TruckSportsCamera/TruckSports.exe"
    dll = exe.parent / "TruckSports_Data/Managed/Assembly-CSharp.dll"
    digest = hashlib.sha256(dll.read_bytes()).hexdigest()
    folder = ROOT / "Logs/TruckSports/PlaytestRepair" / args.name
    folder.mkdir(parents=True, exist_ok=True)
    command = [str(exe), args.flag]
    if args.mode:
        command += [args.mode]
    command += args.extra_arg
    command += [args.output_flag, str(folder), "-force-d3d11", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-logFile", str(folder / "player.log")]
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
    print("Owned Windows player", process.pid, "build", digest, flush=True)
    try:
        code = process.wait(timeout=args.timeout)
    except subprocess.TimeoutExpired:
        process.terminate()
        code = process.wait(timeout=30)
        print("Owned acceptance process exceeded its watchdog", flush=True)
    result = (ROOT / args.result_file).resolve() if args.result_file else folder / "result.txt"
    if not result.is_relative_to(ROOT):
        raise ValueError("Acceptance result must remain inside this workspace")
    evidence = result.read_text(encoding="utf-8-sig") if result.is_file() and result.stat().st_mtime >= wall_start else ""
    terminal = re.search(r"CHECKS=(\d+)\s+ERRORS=(\d+)", evidence)
    if args.result_file and evidence:
        shutil.copy2(result, folder / "result.txt")
    receipt = dict(pid=process.pid, exit=code, seconds=round(time.monotonic()-start, 2), managedSha256=digest, unchangedManaged=hashlib.sha256(dll.read_bytes()).hexdigest()==digest, completedEvidence=bool(terminal), checks=int(terminal[1]) if terminal else None, errors=int(terminal[2]) if terminal else None)
    (folder / "process.json").write_text(json.dumps(receipt, indent=2))
    print(json.dumps(receipt), flush=True)
    if evidence:
        print("\n".join(evidence.splitlines()[-3:]), flush=True)
    raise SystemExit(code or (0 if terminal and int(terminal[2])==0 and receipt["unchangedManaged"] else 1))


if __name__ == "__main__":
    main()
