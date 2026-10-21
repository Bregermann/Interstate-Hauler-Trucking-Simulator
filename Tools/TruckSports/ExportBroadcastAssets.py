"""Preserve only explicitly authored broadcast assets, never vendor/host settings."""
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLEAN = ROOT / ".sports-validation/TruckSportsPublicValidation"
LOG = ROOT / "Logs/TruckSports/PlaytestRepair"
baseline = {p["path"] for p in json.loads((LOG / "host-pending-baseline.json").read_text())}
report = (CLEAN / "Logs/TruckSports/PlaytestRepair/camera-scene-bindings.txt").read_text(encoding="utf-8-sig")
bound = [line.split(" | ")[0] for line in report.splitlines() if " | BOUND | " in line]
assert len(bound) == 33, bound
scene_paths = []
for scene in bound:
    paths = list((CLEAN / "Assets").glob("Truck*/Scenes/" + scene + ".unity"))
    assert len(paths) == 1, (scene, paths)
    scene_paths.append(paths[0].relative_to(CLEAN).as_posix())
paths = scene_paths + [
    "Assets/TruckSports/Resources/Broadcast objective compass.prefab",
    "Assets/TruckSports/Resources/Broadcast objective compass.prefab.meta",
    "Assets/TruckSports/Settings/Keepaway broadcast framing.asset",
    "Assets/TruckSports/Settings/Keepaway broadcast framing.asset.meta",
]
for item in json.loads((LOG / "camera-source-sync.json").read_text()):
    relative = item["path"] + ".meta"
    source, target = CLEAN / relative, ROOT / relative
    assert source.exists(), relative
    if target.exists():
        def guid(path):
            return re.search(rb"^guid: ([a-f0-9]+)", path.read_bytes(), re.M).group(1)
        assert guid(source) == guid(target), "Preserve existing source GUID: " + relative
    else:
        paths.append(relative)
for relative in paths:
    assert relative not in baseline, "Do not overwrite pre-existing pending work: " + relative
    source, target = CLEAN / relative, ROOT / relative
    assert source.is_file(), relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
items = [dict(path=p, sha256=hashlib.sha256((ROOT / p).read_bytes()).hexdigest()) for p in paths]
(LOG / "camera-exported-assets.json").write_text(json.dumps(items, indent=2))
shutil.copy2(CLEAN / "Logs/TruckSports/PlaytestRepair/camera-scene-bindings.txt", LOG / "camera-scene-bindings.txt")
print("Preserved", len(bound), "bound scenes and", len(paths)-len(bound), "owned profile/prefab/source metas; no unrelated asset/settings export.")
