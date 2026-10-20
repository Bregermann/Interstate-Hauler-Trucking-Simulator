"""Explicit publication allowlist; never stage the shared checkout wholesale."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, stderr=subprocess.PIPE)
parser = argparse.ArgumentParser()
parser.add_argument("--stage", action="store_true")
args = parser.parse_args()
folders = [f"Assets/{name}/" for name in ("TruckRedLightGreenLight", "TruckHotPotato", "TruckBalloonBattle", "TruckWhackAMole", "TruckKingOfTheHill", "TruckSeesaw", "TruckFloorIsLava", "TruckTetherball", "TruckJousting", "TruckMagnetMayhem", "TruckVolleyball", "TruckFreezeTag", "TruckHungryHaulers", "TruckSheepHerding")]
folders += ["Documentation/TruckSports/Evidence/2026-10-20-Expansion/"]
files = {"TRUCK_SPORTS_ACCEPTANCE_LEDGER.md", "Assets/TruckSports/BUILD-INFO.md", "Assets/TruckSports/Scenes/TruckSports.unity", "Assets/TruckSports/Art/Logos/Catalogue.json"}
for name in ("TruckRedLightGreenLight", "TruckHotPotato", "TruckBalloonBattle", "TruckWhackAMole", "TruckKingOfTheHill", "TruckSeesaw", "TruckFloorIsLava", "TruckTetherball", "TruckJousting", "TruckMagnetMayhem", "TruckVolleyball", "TruckFreezeTag", "TruckHungryHaulers", "TruckSheepHerding"):
    files.add(f"Assets/{name}.meta")
    for section in ("Logos", "Previews"):
        for suffix in (".png", ".png.meta"):
            files.add(f"Assets/TruckSports/Art/{section}/{name}{suffix}")
    for suffix in (".asset", ".asset.meta"):
        files.add(f"Assets/TruckSports/Rendering/{name} snow pipeline{suffix}")
    files.add(f"Tools/TruckSports/ArtworkSources/{name}.json")
for name in ("SportsBuilder", "SportsMenuArtAuthoring", "SportsNewEntriesAuthoring"):
    files.add(f"Assets/TruckSports/Editor/{name}.cs")
for name in ("SportsEventSession", "SportsBroadcastScores", "SportsCameraFit", "SportsFrontEnd", "SportsNavigation", "SportsMenuPresentationProbe", "SportsWindowCapture", "SportsRecordedReview", "SportsExpansionCameraAcceptance", "SportsExpansionViewHud", "SportsExpansionSpectators", "SportsExpansionSnowProbe"):
    files.add(f"Assets/TruckSports/Scripts/{name}.cs")
    if name in ("SportsRecordedReview", "SportsExpansionCameraAcceptance", "SportsExpansionViewHud", "SportsExpansionSpectators", "SportsExpansionSnowProbe"):
        files.add(f"Assets/TruckSports/Scripts/{name}.cs.meta")
files.add("Assets/TruckSports/Tests/PresentationEditMode/PresentationTests.cs")
files.add("Assets/TruckFareFight/Scripts/FareFightRoute.cs")
for name in ("SportsDrivingSurfaceAuthoring", "ExpansionSnowJobs"):
    files.add(f"Assets/TruckSports/Editor/{name}.cs")
    files.add(f"Assets/TruckSports/Editor/{name}.cs.meta")
for name in ("EighteenSportsProgress.md", "EighteenSportsVendorAudit.md", "EighteenSportsDelivery.md", "EighteenSportsFiveSportDelivery.md", "EighteenSportsSixSportDelivery.md", "EighteenSportsSevenSportDelivery.md", "EighteenSportsNineSportDelivery.md", "EighteenSportsTenSportDelivery.md", "EighteenSportsTwelveSportDelivery.md", "EighteenSportsFourteenSportDelivery.md"):
    files.add("Documentation/TruckSports/" + name)
for name in ("2026-10-20-Eighteen-Sports.md", "2026-10-20-Eighteen-Sports-UI-Addendum.md"):
    files.add("Documentation/TruckSports/Requests/" + name)
for name in ("CaptureEighteenSportExpansion.py", "RunExpansionWindowsAcceptance.py", "UpdateExpansionEvidence.py", "SyncExpansionAssets.py", "PrepareExpansionScope.py"):
    files.add("Tools/TruckSports/" + name)
changed = set(filter(None, (git("diff", "--name-only", "-z", "HEAD") + git("ls-files", "--others", "--exclude-standard", "-z")).decode("utf-8").split("\0")))
selected = sorted(path for path in changed if path in files or any(path.startswith(folder) for folder in folders))
if any("RedLightReview" in path for path in selected):
    raise RuntimeError("Retired duplicate replay adapter must not be republished")
payload = [{"path": path, "bytes": (ROOT / path).stat().st_size if (ROOT / path).is_file() else 0, "sha256": hashlib.sha256((ROOT / path).read_bytes()).hexdigest() if (ROOT / path).is_file() else "DELETED"} for path in selected]
log = ROOT / "Logs/TruckSports/EighteenSports"
(log / "scoped-payload.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
pathspec = log / "scoped-pathspec.nul"
pathspec.write_bytes(b"\0".join(path.encode("utf-8") for path in selected) + b"\0")
print(f"Scoped {len(selected)} files, {sum(item['bytes'] for item in payload):,} bytes; {len(changed)-len(selected)} pending files outside this delivery excluded.")
if args.stage:
    existing = set(filter(None, git("diff", "--cached", "--name-only", "-z").decode("utf-8").split("\0")))
    if existing - set(selected):
        raise RuntimeError("Index contains unrelated staged changes; preserve them")
    operation = subprocess.run(["git", "add", "--pathspec-from-file=" + str(pathspec), "--pathspec-file-nul"], cwd=ROOT, capture_output=True)
    (log / "scoped-stage.log").write_bytes(operation.stdout + operation.stderr)
    operation.check_returncode()
    staged = set(filter(None, git("diff", "--cached", "--name-only", "-z").decode("utf-8").split("\0")))
    if staged != set(selected):
        raise RuntimeError("Index differs from explicit publication allowlist")
    print("Staged exact allowlist. No unrelated assets or private history included.")
