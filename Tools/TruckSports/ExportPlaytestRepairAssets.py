"""Export only explicitly upgraded, sport-owned assets from the validation project."""
import hashlib
import json
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
paths += ["Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity"]
paths += ["Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity"]
paths += ["Assets/TruckTrailerPlug/Models/" + name + suffix for name in ["Open Quarry Floor", "Quarry Pedestrian Navigation"] for suffix in [".asset", ".asset.meta"]]
paths += ["Assets/TruckSkyTow/Art/" + name + part + suffix for name in ["Arctic standard gate", "Arctic precision gold gate"] for part in ["", " body", " inset"] for suffix in [".asset", ".asset.meta"]]
paths += ["Assets/TruckSkyTow/Materials/Arctic gate snow chips.mat" + suffix for suffix in ["", ".meta"]]
records = []
for relative in paths:
    assert relative not in baseline, "Preserve unrelated pending work: " + relative
    source, target = CLEAN / relative, ROOT / relative
    assert source.is_file(), relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
    records.append(dict(path=relative, sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
(LOG / "playtest-exported-assets.json").write_text(json.dumps(records, indent=2))
print("Preserved", len(paths), "explicit Fishing/Sky Tow/Minefield/Trailer Plug scene/art paths; original vendor assets and unrelated scenes unchanged.")
