"""Record measured expansion evidence without regenerating requirement IDs."""
import re
import shutil
import json
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Documentation/TruckSports/Evidence/2026-10-20-Expansion"
OUT.mkdir(parents=True, exist_ok=True)
LOG = ROOT / "Logs/TruckSports/EighteenSports"
receipt = json.loads((LOG / "windows-verified-processes.json").read_text())
prior = json.loads((LOG / "windows-eight-full-processes.json").read_text())
managed = ROOT / "Builds/TruckSports/TruckSports_Data/Managed/Assembly-CSharp.dll"
digest = hashlib.sha256(managed.read_bytes()).hexdigest()
if {r["probe"] for r in receipt} != {"lava", "tether", "joust", "snow", "menu"} or any(r["exit"] != 0 or not r["completedEvidence"] or r["errors"] != 0 or not r["unchangedManaged"] or r["managedSha256"] != digest for r in receipt):
    raise RuntimeError("Final five affected Windows suites must pass on the current nine-sport build")
if {r["probe"] for r in prior} != {"hot-potato", "red-light", "balloon", "whack", "hill", "seesaw", "lava", "tether", "snow", "menu"} or any(r["exit"] != 0 or not r["completedEvidence"] or r["errors"] != 0 or not r["unchangedManaged"] for r in prior):
    raise RuntimeError("Retain the preceding complete eight-sport Windows acceptance")
clean = ROOT / ".sports-validation/TruckSportsPublicValidation"
preserved = json.loads((LOG / "nine-prior-six-source-preservation.json").read_text())
changed = [p for p,h in preserved.items() if hashlib.sha256((clean/p).read_bytes()).hexdigest() != h]
if changed:
    raise RuntimeError("Previously tested gameplay code/scenes changed: " + repr(changed))
import xml.etree.ElementTree as ET
native = ET.parse(LOG / "nine-editmode.xml").getroot()
if native.get("passed") != "135" or native.get("failed") != "0":
    raise RuntimeError("135 native tests must pass")
(LOG / "nine-preservation-result.json").write_text(json.dumps({"files": len(preserved), "changed": changed},indent=2))

def counts(path):
    match = re.search(r"CHECKS=(\d+) ERRORS=(\d+)", path.read_text())
    if not match or match[2] != "0":
        raise RuntimeError("Acceptance is not a pass: " + str(path))
    return int(match[1])
menu_checks = counts(ROOT / "Logs/TruckSports/PresentationSprint/Menu/result.txt")
for sport in ("HotPotato", "RedLight", "Balloon", "Whack", "Hill", "Seesaw", "Lava", "Tether", "Joust", "Snow"):
    for name in ("result.txt", "gameplay-720.png", "four-cabs-720.png", "replay-720.png", "warning-720.png", "eliminated-spectator-720.png", "decoy-cab-720.png", "quarry-impact-720.png", "lava-impact-720.png"):
        source = LOG / sport / name
        if source.exists():
            shutil.copy2(source, OUT / (sport + "-" + name))
for name in ("windows-verified-processes.json", "windows-eight-full-processes.json", "nine-editmode.xml", "nine-prior-six-source-preservation.json", "nine-preservation-result.json", "camera-before-fix.xml", "lava-grade-before.xml", "lava-grade-after.xml", "eight-editmode-final.xml", "windows-six-initial-processes.json", "windows-six-final-targeted-processes.json", "six-bracing-scene-preservation.json", "red-light-editmode-2.xml", "hot-potato-editmode.xml", "balloon-editmode-final.xml", "whack-editmode.xml", "presentation-editmode-final.xml", "five-editmode-final.xml", "six-editmode-final.xml", "seven-editmode-final.xml", "seven-presentation-editmode.xml", "seven-final-build-summary.txt", "preservation-all.json", "original-logo-preservation.json"):
    if (LOG / name).exists():
        shutil.copy2(LOG / name, OUT / name)
for source in (LOG / "Snow").glob("*-snow-720.png"):
    shutil.copy2(source, OUT / source.name)

ledger = ROOT / "TRUCK_SPORTS_ACCEPTANCE_LEDGER.md"
text = ledger.read_text(encoding="utf-8-sig")
for prefix, folder, native, capture in (("EX02", "TruckRedLightGreenLight", 13, "RedLight"), ("EX03", "TruckHotPotato", 11, "HotPotato"), ("EX11", "TruckBalloonBattle", 13, "Balloon"), ("EX16", "TruckWhackAMole", 12, "Whack"), ("EX17", "TruckKingOfTheHill", 22, "Hill"), ("EX09", "TruckSeesaw", 14, "Seesaw"), ("EX06", "TruckFloorIsLava", 18, "Lava"), ("EX13", "TruckTetherball", 14, "Tether"), ("EX14", "TruckJousting", 15, "Joust")):
    checks = counts(LOG / capture / "result.txt")
    probe = {"HotPotato":"hot-potato","RedLight":"red-light","Balloon":"balloon","Whack":"whack","Hill":"hill","Seesaw":"seesaw","Lava":"lava","Tether":"tether","Joust":"joust"}[capture]
    phase = next((r for r in receipt if r["probe"] == probe),None)
    evidence = phase or next(r for r in prior if r["probe"] == probe)
    tested_hash = evidence["managedSha256"]
    phase_note = "final nine-sport build" if phase else "preceding eight-sport build; original gameplay source/scene preserved exactly in final build"
    humans = "one–two" if prefix in ("EX13","EX14") else "one–four"
    for suffix in ("CORE", "AI", "ENV", "LOGO", "PREVIEW", "SETUP", "HUD", "FX", "RESULTS", "QA"):
        rid = prefix + "-" + suffix
        pattern = r"(#### " + rid + r"\s*\n)(.*?)(?=\n#### |\n### |\Z)"
        match = re.search(pattern, text, re.S)
        if not match:
            raise RuntimeError("Missing permanent requirement " + rid)
        block = match.group(2)
        values = {
            "Status": "IMPLEMENTED / UNVERIFIED" if suffix not in ("LOGO", "PREVIEW") else "VERIFIED COMPLETE",
            "Relevant Unity Scene": f"Assets/{folder}/Scenes/{folder}.unity",
            "Relevant Scripts": f"Assets/{folder}/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators.",
            "Prefabs or Assets": f"Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/{folder}.png; see vendor audit.",
            "Implementation Evidence": f"Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/.",
            "Automated Verification": f"Unity6000.4.10f1 native EditMode {native}/{native}; actual Windows NWH/PhysX acceptance {checks}checks/0errors on {phase_note}, Assembly-CSharp SHA256 {tested_hash}; final common menu{menu_checks}/0 and nine-sport native135/135.",
            "Runtime Evidence": f"Full AI match, actual physical interaction, pause, recorded replay, rematch, {humans} software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification.",
            "Screenshot/Video Evidence": f"Logs/TruckSports/EighteenSports/{capture}/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/.",
            "Human Verification": "Not performed for this new sport. No subjective listening or physical-device acceptance claimed.",
            "Known Issues": "Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Nine-sport explicit snow accumulation and actual NWH tire-contact tracks passed27checks; see its receipt.",
            "Last Verification Date": "2026-10-20",
            "Remaining Work": "Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence.",
        }
        if suffix in ("LOGO", "PREVIEW"):
            values["Remaining Work"] = "Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe."
        if prefix == "EX02" and suffix == "LOGO":
            values["Human Verification"] = "User approved the grungy revision3artwork: 'that one looks good, continue on'. This is artwork approval, not gameplay or hardware verification."
        if prefix == "EX16":
            values["Relevant Scripts"] = "Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud."
            values["Known Issues"] = "Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits."
            values["Implementation Notes"] = "Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch."
        if prefix == "EX17":
            values["Relevant Scripts"] = "Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter."
            values["Implementation Notes"] = "Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review."
        if prefix == "EX09":
            values["Relevant Scripts"] = "Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter."
            values["Implementation Notes"] = "Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation."
            values["Known Issues"] = "Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately."
        if prefix == "EX06":
            values["Relevant Scripts"] = "Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering."
            values["Implementation Notes"] = "Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch."
            values["Known Issues"] = "Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately."
        if prefix == "EX13":
            values["Implementation Notes"] = "Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed."
            values["Known Issues"] = "Human pacing, physical controls, listening and long-run performance remain unverified."
        if prefix == "EX14":
            values["Implementation Notes"] = "Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode."
            values["Known Issues"] = "Human pacing, physical controls, listening and long-run performance remain unverified."
        for field, value in values.items():
            block, count = re.subn(r"\| " + re.escape(field) + r" \|[^\n]*", "| " + field + " | " + value + " |", block)
            if count != 1:
                raise RuntimeError(f"{rid}: missing/duplicated field {field}")
        text = text[:match.start(2)] + block + text[match.end(2):]
text = text.replace("Statuses: MISSING; PARTIAL; IMPLEMENTED BUT UNVERIFIED; VERIFIED; BLOCKED.", "Statuses: MISSING; PARTIAL; IMPLEMENTED / UNVERIFIED; VERIFIED COMPLETE; BLOCKED.")
text = text.replace("| Status | IMPLEMENTED BUT UNVERIFIED |", "| Status | IMPLEMENTED / UNVERIFIED |").replace("| Status | VERIFIED |", "| Status | VERIFIED COMPLETE |")
ledger.write_text(text, encoding="utf-8")
print("Preserved permanent IDs; retained full eight-sport phase and recorded five final nine-sport Windows suites plus135 native tests. Human acceptance remains separate.")
