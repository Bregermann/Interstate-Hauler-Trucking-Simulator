"""Export only explicitly upgraded, sport-owned assets from the validation project."""
import hashlib
import json
import re
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / ".sports-validation/TruckSportsPublicValidation"
LOG = ROOT / "Logs/TruckSports/PlaytestRepair"
subprocess.run(["C:/Python313/python.exe", "-X", "utf8", str(ROOT / "Tools/TruckSports/ExportBroadcastAssets.py")], cwd=ROOT, check=True)
baseline = {item["path"] for item in json.loads((LOG / "host-pending-baseline.json").read_text())}
paths = ["Assets/TruckFishing/Scenes/TruckFishing.unity"]
paths += ["Assets/TruckFishing/Prefabs/" + name + ".prefab" for name in ["Small orange one-pound fish", "Large blue five-pound tuna", "Poison shark X warning"]]
paths += ["Assets/TruckFishing/Models/" + name + suffix for name in ["Winding playtest riverbanks", "Winding playtest river surface"] for suffix in [".asset", ".asset.meta"]]
paths += ["Assets/TruckSkyTow/Scenes/TruckSkyTow.unity"]
paths += ["Assets/TruckMinefield/Scenes/TruckMinefield.unity"]
billiards_scene = CLEAN / "Assets/TruckBilliards/Scenes/TruckBilliards.unity"
if "LWS.TruckBilliards.BilliardsHudPresentation" in billiards_scene.read_text(encoding="utf-8-sig"):
    paths += ["Assets/TruckBilliards/Scenes/TruckBilliards.unity"]
bowling_scene = CLEAN / "Assets/TruckBowling/Scenes/TruckBowling.unity"
if "LWS.TruckBowling.BowlingHudPresentation" in bowling_scene.read_text(encoding="utf-8-sig"):
    paths += ["Assets/TruckBowling/Scenes/TruckBowling.unity"]
air_scene = CLEAN / "Assets/TruckAirHockey/Scenes/TruckAirHockey.unity"
if "LWS.TruckAirHockey.AirHockeyPresentation" in air_scene.read_text(encoding="utf-8-sig"):
    paths += ["Assets/TruckAirHockey/Scenes/TruckAirHockey.unity"]
front_scene = CLEAN / "Assets/TruckSports/Scenes/TruckSports.unity"
if "Scrap gameplay preview" in front_scene.read_text(encoding="utf-8-sig"):
    paths += ["Assets/TruckSports/Scenes/TruckSports.unity"]
poker_scene = CLEAN / "Assets/TruckPoker/Scenes/TruckPoker.unity"
if "LWS.TruckPoker.PokerTableHud" in poker_scene.read_text(encoding="utf-8-sig"):
    paths += ["Assets/TruckPoker/Scenes/TruckPoker.unity"]
cheese_scene = CLEAN / "Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity"
if "LWS.TruckCheeseRolling.CheeseCatPresentation" in cheese_scene.read_text(encoding="utf-8-sig"):
    paths += ["Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity"]
    paths += ["Assets/TruckCheeseRolling/Prefabs/Animated owned cat.prefab" + suffix for suffix in ["", ".meta"]]
    paths += ["Assets/TruckCheeseRolling/Art/Animals/Scenery cat gestures.controller" + suffix for suffix in ["", ".meta"]]
mine = CLEAN / "Assets/TruckMinefield"
if (mine / "Materials/West meadow and Vista mountains.asset").is_file():
    for name in ["West", "East"]:
        paths += ["Assets/TruckMinefield/Materials/" + name + " meadow and Vista mountains.asset" + suffix for suffix in ["", ".meta"]]
    paths += ["Assets/TruckMinefield/Prefabs/Outer mountain pine.prefab" + suffix for suffix in ["", ".meta"]]
    if (mine / "Materials/Seamless mountain landing runout.asset").is_file():
        paths += ["Assets/TruckMinefield/Materials/Seamless mountain landing runout.asset" + suffix for suffix in ["", ".meta"]]
        for name in ["Range meadow grass", "Range exposed rock"]:
            paths += ["Assets/TruckMinefield/Materials/" + name + ".terrainlayer" + suffix for suffix in ["", ".meta"]]
    if not (ROOT / "Assets/TruckMinefield/Prefabs.meta").exists():
        paths += ["Assets/TruckMinefield/Prefabs.meta"]
paths += ["Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity"]
paths += ["Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity"]
bay_material = "Assets/TruckMusicalParking/Materials/Reflective bay paint.mat"
if (CLEAN / bay_material).is_file():
    paths += [bay_material, bay_material + ".meta"]
bungee = CLEAN / "Assets/TruckBungeeDemolition"
if (bungee / "Prefabs/Owned city demolition annex v2.prefab").is_file():
    paths += ["Assets/TruckBungeeDemolition/Scenes/TruckBungeeDemolition.unity"]
    for name in ["Owned city demolition annex v2", "Owned hotel demolition tower v2"]:
        paths += ["Assets/TruckBungeeDemolition/Prefabs/" + name + ".prefab" + suffix for suffix in ["", ".meta"]]
    paths += ["Assets/TruckBungeeDemolition/Prefabs/Owned city fractures v2.meta"]
    paths += [p.relative_to(CLEAN).as_posix() for p in (bungee / "Prefabs/Owned city fractures v2").glob("*.prefab*") if p.is_file()]
    paths += [p.relative_to(CLEAN).as_posix() for p in (bungee / "Models").glob("Precut v2 *.asset*") if p.is_file()]
snake_guide = CLEAN / "Assets/TruckSnake/Scripts/SnakePickupGuidance.cs.meta"
if snake_guide.is_file():
    guid = re.search(r"^guid: ([a-f0-9]+)", snake_guide.read_text(), re.M).group(1)
    assert guid in (CLEAN / "Assets/TruckSnake/Scenes/TruckSnake.unity").read_text(), "Export only the explicitly integrated Snake scene"
    paths += ["Assets/TruckSnake/Scenes/TruckSnake.unity"]
    paths += ["Assets/TruckSnake/Configuration/World population.asset"]
if (CLEAN / "Logs/TruckSports/PlaytestRepair/wife-checkpoint-guidance.txt").is_file():
    paths += ["Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity"]
if (CLEAN / "Assets/TruckBaseJumping/Prefabs/Flare Smoke.prefab").is_file():
    paths += ["Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity"]
    paths += ["Assets/TruckBaseJumping/Prefabs/Flare Smoke.prefab" + suffix for suffix in ["", ".meta"]]
    if (CLEAN / "Assets/TruckBaseJumping/Materials/Flare smoke.mat").is_file():
        paths += ["Assets/TruckBaseJumping/Materials/Flare smoke.mat" + suffix for suffix in ["", ".meta"]]
    if not (ROOT / "Assets/TruckBaseJumping/Prefabs.meta").exists():
        paths += ["Assets/TruckBaseJumping/Prefabs.meta"]
paths += ["Assets/TruckTrailerPlug/Models/" + name + suffix for name in ["Open Quarry Floor", "Quarry Pedestrian Navigation"] for suffix in [".asset", ".asset.meta"]]
paths += ["Assets/TruckSkyTow/Art/" + name + part + suffix for name in ["Arctic standard gate", "Arctic precision gold gate"] for part in ["", " body", " inset"] for suffix in [".asset", ".asset.meta"]]
paths += ["Assets/TruckSkyTow/Materials/Arctic gate snow chips.mat" + suffix for suffix in ["", ".meta"]]
paths += [str(p.relative_to(CLEAN)).replace("\\", "/") for p in (CLEAN / "Assets/TruckKeepaway/Materials").glob("Expanded *") if p.is_file()]
dock_presentation = CLEAN / "Assets/TruckDockDiving/Presentation"
if dock_presentation.is_dir():
    paths += ["Assets/TruckDockDiving/Scenes/TruckDockDiving.unity"]
    paths += [p.relative_to(CLEAN).as_posix() for p in dock_presentation.rglob("*") if p.is_file()]
    if not (ROOT / "Assets/TruckDockDiving/Presentation.meta").exists():
        paths += ["Assets/TruckDockDiving/Presentation.meta"]
    # These fitted owned timber/fence/building material copies are new to this
    # venue. Leave all existing user-authored Dock materials unchanged.
    paths += [p.relative_to(CLEAN).as_posix() for p in (CLEAN / "Assets/TruckDockDiving/Materials").glob("Imported *") if p.is_file() and not (ROOT / p.relative_to(CLEAN)).exists()]
records = []
for relative in paths:
    assert relative not in baseline, "Preserve unrelated pending work: " + relative
    source, target = CLEAN / relative, ROOT / relative
    assert source.is_file(), relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
    records.append(dict(path=relative, sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
# Keep generated licensed VFX available locally without adding vendor-derived
# source to the public payload. Never replace differing owner edits.
private_vfx = "Assets/TruckCheeseRolling/PrivateLicensedVfx"
private_copies = []
for folder in ["Resources", "Materials"]:
    for source in (CLEAN / private_vfx / folder).rglob("*"):
        if not source.is_file():
            continue
        relative = source.relative_to(CLEAN); target = ROOT / relative
        digest = hashlib.sha256(source.read_bytes()).hexdigest()
        if target.exists() and hashlib.sha256(target.read_bytes()).hexdigest() != digest:
            raise RuntimeError("Preserve differing licensed owner customization: " + str(relative))
        private_copies.append((source, target))
for source, target in private_copies:
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists():
        shutil.copy2(source, target)
(LOG / "playtest-exported-assets.json").write_text(json.dumps(records, indent=2))
print("Preserved", len(paths), "explicit Fishing/Sky Tow/Minefield/Trailer Plug scene/art paths; original vendor assets and unrelated scenes unchanged.")
