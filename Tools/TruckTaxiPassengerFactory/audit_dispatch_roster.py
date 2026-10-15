"""Read-only Unity YAML audit input for the real C# dispatch policy simulation."""
import json
from pathlib import Path

import yaml


class UnityLoader(yaml.SafeLoader):
    pass


UnityLoader.add_multi_constructor(
    "tag:unity3d.com,2011:", lambda loader, suffix, node: loader.construct_mapping(node)
)


def read_asset(path):
    with path.open(encoding="utf-8-sig") as stream:
        return yaml.load(stream, Loader=UnityLoader).get("MonoBehaviour", {})


def main():
    root = Path(__file__).resolve().parents[2]
    taxi = root / "Assets/LWS/TruckTaxi"
    assets = {}
    for folder in (taxi / "Passengers", taxi / "ScriptableObjects"):
        for path in folder.rglob("*.asset"):
            meta = path.with_suffix(path.suffix + ".meta")
            if meta.exists():
                with meta.open(encoding="utf-8-sig") as stream:
                    guid = yaml.safe_load(stream)["guid"]
                assets[guid] = path
    config = read_asset(taxi / "ScriptableObjects/TruckTaxi_DemoConfiguration.asset")
    database = read_asset(assets[config["passengerDatabase"]["guid"]])
    active = [entry.get("guid") for entry in database["passengers"]]
    rows = []
    for guid, path in assets.items():
        data = read_asset(path)
        if "passengerId" not in data or "spawnWeight" not in data:
            continue
        casting_path = assets.get((data.get("casting") or {}).get("guid"))
        casting = read_asset(casting_path) if casting_path else {}
        rows.append(dict(
            id=data.get("passengerId"), name=data.get("passengerName"),
            path=path.relative_to(root).as_posix(), activeCount=active.count(guid),
            available=bool(data.get("available", True)), weight=data.get("spawnWeight", 1),
            rarity=data.get("rarity", 0), districts=data.get("districts") or [],
            archetype=data.get("archetype") or "", traits=data.get("specialTraits") or [],
            dispatchArchetype=data.get("dispatchArchetype") or "",
            gender=data.get("gender", 0), castingGender=casting.get("sexPresentation") or "",
            explicitlyAdult=bool(data.get("explicitlyAdult", False)),
            minimumAge=data.get("minimumAdultAge", 0), optIn=bool(data.get("specialAppreciationEligible", False)),
            chaosAffinity=data.get("chaosAffinity", -1), flirtatious=bool(data.get("flirtatiousPresentation", False)),
        ))
    print(json.dumps(dict(config=config, roster=sorted(rows, key=lambda row: row["id"] or ""))))


if __name__ == "__main__":
    main()
