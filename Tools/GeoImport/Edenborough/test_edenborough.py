import hashlib
import json
import pathlib
import tempfile
import unittest

import edenborough


OSM = """<osm version="0.6">
<node id="1" lon="-3.1900" lat="55.9490"/>
<node id="2" lon="-3.1900" lat="55.9500"/>
<node id="3" lon="-3.1890" lat="55.9500"/>
<node id="4" lon="-3.1910" lat="55.9500"/>
<way id="10"><nd ref="1"/><nd ref="2"/><tag k="highway" v="residential"/>
<tag k="name" v="Test Street"/></way>
<way id="20"><nd ref="2"/><nd ref="3"/><tag k="highway" v="primary"/>
<tag k="oneway" v="yes"/><tag k="lanes" v="2"/></way>
<way id="30"><nd ref="2"/><nd ref="4"/><tag k="highway" v="footway"/></way>
<relation id="40"><member type="way" ref="10" role="from"/>
<member type="node" ref="2" role="via"/>
<member type="way" ref="20" role="to"/>
<tag k="type" v="restriction"/><tag k="restriction" v="no_right_turn"/></relation>
</osm>"""


class EdenboroughImportTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.path = pathlib.Path(self.temp.name) / "fixture.osm"
        self.path.write_text(OSM, encoding="utf-8")
        self.data = edenborough.normalize(self.path, edenborough.config())

    def tearDown(self):
        self.temp.cleanup()

    def test_origin_metric_and_real_source_provenance(self):
        self.assertEqual(edenborough.metric(-3.19, 55.95, edenborough.config()), [0.0, 0.0])
        self.assertEqual(self.data["source"]["source_sha256"],
                         hashlib.sha256(self.path.read_bytes()).hexdigest())
        self.assertEqual(self.data["source"]["license"], "ODbL 1.0")

    def test_left_hand_lanes_and_non_drivable_path(self):
        self.assertEqual(self.data["counts"]["road_ways"], 2)
        self.assertEqual(self.data["counts"]["lanes"], 4)
        self.assertTrue(any(m["class"] == "footway" and not m["drivable"]
                            for m in self.data["map"]))
        north = next(l for l in self.data["lanes"]
                     if l["osm_way"] == 10 and l["from"] == 1)
        south = next(l for l in self.data["lanes"]
                     if l["osm_way"] == 10 and l["from"] == 2)
        self.assertLess(north["points"][0]["x"], 0)
        self.assertGreater(south["points"][0]["x"], 0)

    def test_oneway_and_turn_restriction(self):
        self.assertFalse(any(l["osm_way"] == 20 and l["from"] == 3
                             for l in self.data["lanes"]))
        self.assertFalse(any(t for t in self.data["turns"]
                             if "osm-w10" in t["from"] and "osm-w20" in t["to"]))
        self.assertEqual(edenborough.validate_graph(self.data), [])

    def test_missing_turn_lane_is_rejected(self):
        self.data["turns"].append({"id": "bad", "from": "missing", "to": "missing",
                                   "points": [], "region": "R_0_0"})
        self.assertTrue(any("missing lane" in error
                            for error in edenborough.validate_graph(self.data)))

    def test_way_tags_are_preserved_for_vendor_adapter(self):
        road = next(r for r in self.data["roads"] if r["osm_way"] == 20)
        tags = {entry["key"]: entry["value"] for entry in road["tags"]}
        self.assertEqual(tags, {"highway": "primary", "oneway": "yes", "lanes": "2"})
        self.assertEqual(road["id"], "osm-w20")

    def test_prototype_is_real_osm_derived(self):
        output = edenborough.OUTPUT
        if not output.exists():
            self.skipTest("Run build to create the prototype artifact")
        artifact = json.loads(output.read_text(encoding="utf-8"))
        self.assertEqual(artifact["source"]["source_url"],
                         edenborough.config()["source_url"])
        self.assertGreater(artifact["counts"]["road_ways"], 200)
        self.assertTrue(artifact["left_hand_traffic"])
        cached = edenborough.CACHE / "old-town-east.osm"
        if cached.exists():
            digest = hashlib.sha256(cached.read_bytes()).hexdigest()
            self.assertEqual(artifact["source"]["source_sha256"], digest)


if __name__ == "__main__":
    unittest.main()
