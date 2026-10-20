"""Export only new sport-owned authored assets from the isolated Unity project.

Runtime source remains authoritative in the host checkout. Never export host
settings, vendor files, unrelated scenes, or an old deleted source file.
"""
import shutil
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / ".sports-validation/TruckSportsPublicValidation"
count = 0
def copy(relative):
    global count
    source, target = SOURCE / relative, ROOT / relative
    if not source.is_file():
        return
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists() or source.read_bytes() != target.read_bytes():
        shutil.copy2(source, target)
        count += 1
for sport in ("TruckRedLightGreenLight", "TruckHotPotato", "TruckBalloonBattle", "TruckWhackAMole", "TruckKingOfTheHill", "TruckSeesaw", "TruckFloorIsLava", "TruckTetherball", "TruckJousting", "TruckMagnetMayhem"):
    folder = Path("Assets") / sport
    copy(str(folder) + ".meta")
    for path in (SOURCE / folder).rglob("*"):
        if path.is_file() and path.suffix not in (".cs", ".asmdef") and "RedLightReview" not in path.name:
            copy(path.relative_to(SOURCE))
    for section in ("Logos", "Previews"):
        for suffix in (".png", ".png.meta"):
            copy(f"Assets/TruckSports/Art/{section}/{sport}{suffix}")
    for suffix in (".asset", ".asset.meta"):
        copy(f"Assets/TruckSports/Rendering/{sport} snow pipeline{suffix}")
for script in ("SportsRecordedReview", "SportsExpansionCameraAcceptance", "SportsExpansionViewHud", "SportsExpansionSpectators", "SportsExpansionSnowProbe"):
    copy(f"Assets/TruckSports/Scripts/{script}.cs.meta")
for path in ("Assets/TruckSports/Editor/SportsDrivingSurfaceAuthoring.cs.meta", "Assets/TruckSports/Editor/ExpansionSnowJobs.cs.meta", "Assets/TruckSports/Scenes/TruckSports.unity", "Assets/TruckSports/Art/Logos/Catalogue.json"):
    copy(path)
print(f"Exported {count} scoped assets/metas; runtime/editor source and unrelated files preserved.")
