"""Explicit editor-side OSM import. No Unity runtime dependency or network at play time."""

import argparse
import collections
import datetime
import hashlib
import json
import math
import pathlib
import shutil
import subprocess
import urllib.request
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent
CACHE = ROOT / "cache"
OUTPUT = ROOT / "prototype.json"
ROAD_CLASSES = {
    "motorway", "trunk", "primary", "secondary", "tertiary",
    "residential", "service", "living_street", "unclassified",
    "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link",
}
NO_ACCESS = {"no", "private"}


def config():
    return json.loads((ROOT / "config.json").read_text(encoding="utf-8"))


def inside(lon, lat, cfg):
    polygon = cfg.get("polygon")
    if polygon:
        hit = False
        for a, b in zip(polygon, polygon[1:] + polygon[:1]):
            if (a[1] > lat) != (b[1] > lat):
                x = (b[0] - a[0]) * (lat - a[1]) / (b[1] - a[1]) + a[0]
                if lon < x:
                    hit = not hit
        return hit
    west, south, east, north = cfg["bbox"]
    return west <= lon <= east and south <= lat <= north


def metric(lon, lat, cfg):
    origin = cfg["origin"]
    radius = 6378137.0
    return [round(radius * math.radians(lon - origin["lon"]) *
                  math.cos(math.radians(origin["lat"])), 3),
            round(radius * math.radians(lat - origin["lat"]), 3)]


def parse_osm(path):
    nodes, ways, restrictions = {}, [], []
    for _, elem in ET.iterparse(path, events=("end",)):
        if elem.tag == "node":
            nodes[int(elem.attrib["id"])] = (
                float(elem.attrib["lon"]), float(elem.attrib["lat"]))
            elem.clear()
        elif elem.tag == "way":
            tags = {t.attrib["k"]: t.attrib["v"] for t in elem.findall("tag")}
            refs = [int(n.attrib["ref"]) for n in elem.findall("nd")]
            ways.append((int(elem.attrib["id"]), refs, tags))
            elem.clear()
        elif elem.tag == "relation":
            tags = {t.attrib["k"]: t.attrib["v"] for t in elem.findall("tag")}
            if tags.get("type") == "restriction" and "restriction" in tags:
                members = {m.attrib["role"]: (m.attrib["type"], int(m.attrib["ref"]))
                           for m in elem.findall("member")
                           if m.attrib.get("role") in {"from", "to", "via"}}
                if all(k in members for k in ("from", "to", "via")):
                    restrictions.append((tags, members))
            elem.clear()
    return nodes, ways, restrictions


def source_snapshot(path, cfg):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    if path.name == "old-town-east.osm":
        source_bbox = None
        for _, elem in ET.iterparse(path, events=("start",)):
            if elem.tag == "bounds":
                source_bbox = [float(elem.attrib[k]) for k in (
                    "minlon", "minlat", "maxlon", "maxlat")]
                break
            if elem.tag in {"node", "way"}:
                break
        if source_bbox is None or any(abs(a - b) > 1e-7 for a, b in zip(source_bbox, cfg["bbox"])):
            raise ValueError("Cached OSM bbox differs from config; refresh the source explicitly.")
    return {
        "provider": "OpenStreetMap",
        "source_url": (cfg["source_url"] if path.name == "old-town-east.osm" else
                       cfg["geofabrik_pbf_url"] if path.name == "scotland-latest.osm.pbf" else
                       path.as_uri()),
        "source_file": path.name,
        "source_sha256": digest.hexdigest(),
        "downloaded_utc": datetime.datetime.fromtimestamp(
            path.stat().st_mtime, datetime.timezone.utc).isoformat(),
        "license": "ODbL 1.0",
        "attribution": "© OpenStreetMap contributors",
        "attribution_url": "https://www.openstreetmap.org/copyright",
        "boundary": cfg.get("polygon", cfg["bbox"]),
        "boundary_json": json.dumps(cfg.get("polygon", cfg["bbox"]), separators=(",", ":")),
        "pipeline_version": cfg["pipeline_version"],
    }


def source_path(args):
    if args.source:
        path = pathlib.Path(args.source).resolve()
    else:
        path = CACHE / "old-town-east.osm"
    if not path.exists():
        raise SystemExit("Source absent. Run 'download' or pass --source; no implicit download.")
    if path.suffix not in {".osm", ".pbf"}:
        raise SystemExit("Source must be .osm or .pbf")
    if path.is_relative_to(ROOT.parents[2] / "Assets"):
        raise SystemExit("Raw OSM source must remain outside Unity Assets.")
    return path


def xml_input(path, cfg):
    if path.suffix == ".osm":
        return path
    osmium = shutil.which("osmium")
    if not osmium:
        raise SystemExit("PBF import requires an existing osmium executable; no package is installed automatically.")
    extracted = CACHE / "edinburgh-extract.osm.pbf"
    xml = CACHE / "edinburgh-extract.osm"
    bbox = ",".join(str(x) for x in cfg["bbox"])
    subprocess.run([osmium, "extract", "--bbox", bbox, "--strategy", "complete_ways",
                    "--overwrite", "-o", str(extracted), str(path)], check=True)
    subprocess.run([osmium, "cat", "--output-format", "osm", "--overwrite",
                    "-o", str(xml), str(extracted)], check=True)
    return xml


def lane_counts(tags, oneway):
    def number(key):
        try:
            return max(0, min(8, int(tags.get(key, "0"))))
        except ValueError:
            return 0
    total = number("lanes")
    forward, backward = number("lanes:forward"), number("lanes:backward")
    if oneway:
        return max(1, forward or total or 1), 0
    if forward or backward:
        return max(1, forward), max(1, backward)
    return max(1, (total + 1) // 2), max(1, total // 2) if total else 1


def allowed(tags):
    highway = tags.get("highway", "")
    if highway not in ROAD_CLASSES:
        return False
    if any(tags.get(k) in NO_ACCESS for k in ("access", "vehicle", "motor_vehicle", "motorcar", "hgv")):
        return False
    if highway == "service" and tags.get("service") in {"driveway", "parking_aisle", "emergency_access"}:
        return False
    return True


def normalize(path, cfg):
    nodes, ways, relations = parse_osm(xml_input(path, cfg))
    road_ways = [(wid, refs, tags) for wid, refs, tags in ways if allowed(tags)]
    counts = collections.Counter(n for _, refs, _ in road_ways for n in refs)
    out = {
        "schema": 1, "district_id": cfg["district_id"], "district_name": cfg["district_name"],
        "origin": cfg["origin"], "coordinate_system": "WGS84 local tangent equirectangular; X east, Z north, metres",
        "left_hand_traffic": True, "lane_width_meters": cfg["lane_width_meters"],
        "source": source_snapshot(path, cfg), "roads": [], "lanes": [],
        "turns": [], "rail": [], "water": [], "regions": [], "map": [],
    }
    width = cfg["lane_width_meters"]
    lane_at_node = collections.defaultdict(lambda: {"in": [], "out": []})
    seen_nodes = set()
    road_count = collections.Counter()
    restrictions = collections.defaultdict(list)
    for tags, members in relations:
        if members["from"][0] == "way" and members["to"][0] == "way" and members["via"][0] == "node":
            restrictions[(members["via"][1], members["from"][1])].append(
                (members["to"][1], tags["restriction"]))
    for way_id, refs, tags in ways:
        kind = "road" if "highway" in tags else (
            "rail" if "railway" in tags else "water" if (
                tags.get("waterway") or tags.get("natural") in {"water", "coastline"} or
                tags.get("landuse") == "reservoir") else "")
        if not kind:
            continue
        pairs = [(a, b) for a, b in zip(refs, refs[1:])
                 if a in nodes and b in nodes and
                 inside((nodes[a][0] + nodes[b][0]) / 2,
                        (nodes[a][1] + nodes[b][1]) / 2, cfg)]
        if not pairs:
            continue
        if kind != "road":
            target = out[kind]
            target.append({"osm_way": way_id, "name": tags.get("name", ""),
                           "class": tags.get("railway", tags.get("waterway",
                                    tags.get("natural", tags.get("water", "")))),
                           "points": [metric(*nodes[n], cfg) for n in refs if n in nodes]})
            continue
        if not allowed(tags):
            out["map"].append({"road": f"osm-w{way_id}", "class": tags.get("highway", ""),
                               "name": tags.get("name", ""), "access": tags.get("access", ""),
                               "drivable": False,
                               "points": [metric(*nodes[n], cfg) for n in refs if n in nodes]})
            continue
        highway = tags["highway"]
        road_count[highway] += 1
        one = tags.get("oneway", "")
        reverse = one == "-1"
        oneway = (reverse or one in {"yes", "1", "true"} or
                  tags.get("junction") == "roundabout" or
                  (highway in {"motorway", "motorway_link"} and one not in {"no", "0", "false"}))
        f_count, b_count = lane_counts(tags, oneway)
        if reverse:
            try:
                reverse_count = int(tags.get("lanes:backward", "0") or "0")
            except ValueError:
                reverse_count = 0
            f_count, b_count = 0, max(1, min(8, reverse_count or f_count))
        road = {
            "id": f"osm-w{way_id}", "osm_way": way_id, "name": tags.get("name", ""),
            "class": highway, "oneway": one or ("yes" if oneway else "no"),
            "tags": [{"key": key, "value": value} for key, value in sorted(tags.items())],
            "roundabout": tags.get("junction") == "roundabout",
            "lanes": tags.get("lanes", ""), "lanes_forward": tags.get("lanes:forward", ""),
            "lanes_backward": tags.get("lanes:backward", ""),
            "maxspeed": tags.get("maxspeed", ""), "surface": tags.get("surface", ""),
            "bridge": tags.get("bridge", ""), "tunnel": tags.get("tunnel", ""),
            "layer": tags.get("layer", ""), "access": tags.get("access", ""),
            "vehicle": tags.get("vehicle", ""), "motor_vehicle": tags.get("motor_vehicle", ""),
            "hgv": tags.get("hgv", ""), "service": tags.get("service", ""),
            "width": tags.get("width", ""), "forward_count": f_count,
            "backward_count": b_count, "segments": [],
        }
        for a, b in pairs:
            pa, pb = metric(*nodes[a], cfg), metric(*nodes[b], cfg)
            dx, dz = pb[0] - pa[0], pb[1] - pa[1]
            length = math.hypot(dx, dz)
            if length < 0.25:
                continue
            seen_nodes.update((a, b))
            segment = {"a": a, "b": b, "points": [pa, pb],
                       "region": region_of((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2, cfg)}
            road["segments"].append(segment)
            for direction, count in ((1, f_count), (-1, b_count)):
                if not count:
                    continue
                start, end = (a, b) if direction == 1 else (b, a)
                p0, p1 = (pa, pb) if direction == 1 else (pb, pa)
                nx, nz = -(p1[1] - p0[1]) / length, (p1[0] - p0[0]) / length
                for index in range(count):
                    offset = ((index + .5) * width if not oneway else
                              (index - (count - 1) / 2) * width)
                    lane_id = f"osm-w{way_id}-{a}-{b}-d{direction}-l{index}"
                    lane = {"id": lane_id, "road": road["id"], "osm_way": way_id,
                            "from": start, "to": end, "index": index,
                            "region": segment["region"],
                            "points": [[round(p0[0] + nx * offset, 3), round(p0[1] + nz * offset, 3)],
                                       [round(p1[0] + nx * offset, 3), round(p1[1] + nz * offset, 3)]]}
                    out["lanes"].append(lane)
                    lane_at_node[start]["out"].append(lane)
                    lane_at_node[end]["in"].append(lane)
        if road["segments"]:
            out["roads"].append(road)
            out["map"].append({"road": road["id"], "class": highway, "name": road["name"],
                               "access": road["access"], "drivable": True,
                               "points": [metric(*nodes[n], cfg) for n in refs if n in nodes]})
    for node, lanes in lane_at_node.items():
        for arriving in lanes["in"]:
            for leaving in lanes["out"]:
                if arriving["id"] == leaving["id"] or arriving["from"] == leaving["to"]:
                    continue
                if not legal_turn(node, arriving["osm_way"], leaving["osm_way"], restrictions):
                    continue
                a, b = arriving["points"][-1], leaving["points"][0]
                out["turns"].append({"id": f"{arriving['id']}->{leaving['id']}",
                                     "from": arriving["id"], "to": leaving["id"],
                                     "via": node, "points": [a, b],
                                     "region": leaving["region"]})
    regions = collections.defaultdict(lambda: {"roads": set(), "lanes": 0, "turns": 0})
    for road in out["roads"]:
        for segment in road["segments"]:
            regions[segment["region"]]["roads"].add(road["id"])
    for lane in out["lanes"]:
        regions[lane["region"]]["lanes"] += 1
    for turn in out["turns"]:
        regions[turn["region"]]["turns"] += 1
    size = cfg["chunk_meters"]
    for region_id, data in sorted(regions.items()):
        x, z = (int(v) for v in region_id.split("_")[1:])
        out["regions"].append({"id": region_id, "bounds": [x * size, z * size, size, size],
                               "roads": sorted(data["roads"]), "lanes": data["lanes"],
                               "turns": data["turns"],
                               "scene_name": "Edenborough_" + region_id})
    out["counts"] = {"osm_nodes": len(seen_nodes), "road_ways": len(out["roads"]),
                     "road_segments": sum(len(r["segments"]) for r in out["roads"]),
                     "lanes": len(out["lanes"]), "turns": len(out["turns"]),
                     "rail_ways": len(out["rail"]), "water_ways": len(out["water"]),
                     "regions": len(out["regions"]), "road_classes": dict(sorted(road_count.items()))}
    for road in out["roads"]:
        for segment in road["segments"]:
            segment["points"] = [{"x": p[0], "z": p[1]} for p in segment["points"]]
    for collection in ("lanes", "turns", "rail", "water", "map"):
        for feature in out[collection]:
            feature["points"] = [{"x": p[0], "z": p[1]} for p in feature["points"]]
    return out


def legal_turn(node, from_way, to_way, restrictions):
    entries = restrictions.get((node, from_way), [])
    if any(value.startswith("only_") for _, value in entries):
        return any(target == to_way and value.startswith("only_") for target, value in entries)
    return not any(target == to_way and value.startswith("no_") for target, value in entries)


def region_of(x, z, cfg):
    size = cfg["chunk_meters"]
    return f"R_{math.floor(x / size)}_{math.floor(z / size)}"


def validate_graph(result):
    errors = []
    roads = {road["id"] for road in result["roads"]}
    lanes = {lane["id"] for lane in result["lanes"]}
    if len(lanes) != len(result["lanes"]):
        errors.append("Duplicate lane ID")
    turns = {turn["id"] for turn in result["turns"]}
    if len(turns) != len(result["turns"]):
        errors.append("Duplicate turn ID")
    if not roads or not lanes:
        errors.append("No drivable roads or lanes in selected boundary")
    for lane in result["lanes"]:
        if lane["road"] not in roads:
            errors.append(f"Lane {lane['id']} references a missing road")
        a, b = lane["points"]
        if math.hypot(b["x"] - a["x"], b["z"] - a["z"]) < .25:
            errors.append(f"Lane {lane['id']} has zero length")
    for turn in result["turns"]:
        if turn["from"] not in lanes or turn["to"] not in lanes:
            errors.append(f"Turn {turn['id']} references a missing lane")
        if turn["from"] == turn["to"]:
            errors.append(f"Turn {turn['id']} is a self-loop")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("locate", "download", "extract", "validate",
                                            "build", "preview-classes", "preview-regions", "rebuild-map"))
    parser.add_argument("--source", help="Existing .osm or .pbf outside Assets")
    args = parser.parse_args()
    cfg = config()
    CACHE.mkdir(exist_ok=True)
    if args.command == "locate":
        print(f"Configured small source: {CACHE / 'old-town-east.osm'}")
        print(f"Optional Scotland PBF: {cfg['geofabrik_pbf_url']}")
        return
    if args.command == "download":
        target = CACHE / "old-town-east.osm"
        if target.exists():
            raise SystemExit("Snapshot exists; remove it explicitly before refreshing.")
        request = urllib.request.Request(cfg["source_url"],
                                         headers={"User-Agent": "InterstateHauler-Edenborough-EditorImport/1.0"})
        with urllib.request.urlopen(request, timeout=120) as response, target.open("wb") as file:
            shutil.copyfileobj(response, file)
        print(f"Downloaded {target} ({target.stat().st_size} bytes)")
        return
    path = source_path(args)
    result = normalize(path, cfg)
    errors = validate_graph(result)
    if errors:
        raise SystemExit("Invalid normalized graph: " + "; ".join(errors[:20]))
    if args.command == "validate":
        print(json.dumps(result["counts"], indent=2))
        return
    if args.command == "preview-classes":
        print(json.dumps(result["counts"]["road_classes"], indent=2))
        return
    if args.command == "preview-regions":
        print(json.dumps(result["regions"], indent=2))
        return
    if args.command in {"extract", "build", "rebuild-map"}:
        OUTPUT.write_text(json.dumps(result, separators=(",", ":"), ensure_ascii=False),
                          encoding="utf-8")
        print(f"{OUTPUT}: {json.dumps(result['counts'])}")


if __name__ == "__main__":
    main()
