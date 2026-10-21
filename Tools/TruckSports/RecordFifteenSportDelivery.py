"""Record the actual fifteen-sport acceptance without rewriting prior history/IDs."""
import hashlib,json,re,shutil,xml.etree.ElementTree as ET
from pathlib import Path
R=Path(__file__).resolve().parents[2]; C=R/".sports-validation/TruckSportsPublicValidation"; L=R/"Logs/TruckSports/EighteenSports"; B=R/"Builds/TruckSports"; E=R/"Documentation/TruckSports/Evidence/2026-10-20-Expansion/Fifteen"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
dll=sha(B/"TruckSports_Data/Managed/Assembly-CSharp.dll")
receipts=json.loads((L/"windows-verified-processes.json").read_text())
assert {v["probe"] for v in receipts}=={"hide","hide-adapters","snow","menu"}
assert all(v["exit"]==0 and v["errors"]==0 and v["completedEvidence"] and v["unchangedManaged"] and v["managedSha256"]==dll for v in receipts)
native=ET.parse(L/"fifteen-first-editmode.xml").getroot();assert native.attrib["passed"]=="253" and native.attrib["failed"]=="0"
proof=json.loads((L/"fifteen-prior-fourteen-source-preservation.json").read_text());changed=[p for p,h in proof.items() if sha(C/p)!=h];assert not changed,changed
summary=(C/"Logs/TruckSports/build-summary.txt").read_text();assert summary.startswith("Succeeded") and "Errors: 0" in summary
scenes=summary.split("Scenes: ",1)[1].strip().split(", ");assert len(scenes)==57
E.mkdir(parents=True,exist_ok=True)
for folder,parts in {"Hide":["result.txt","gameplay-720.png","gameplay-1080.png","four-cabs-720.png","four-cabs-1080.png","replay-720.png"],"HideAdapters":["result.txt"],"Snow":["result.txt","TruckHideAndSeek-snow-720.png","TruckHideAndSeek-clear-after-snow-720.png"]}.items():
 for part in parts:shutil.copy2(L/folder/part,E/(folder+"-"+part))
shutil.copy2(R/"Logs/TruckSports/PresentationSprint/Menu/result.txt",E/"menu-result.txt")
shutil.copy2(L/"fifteen-first-editmode.xml",E/"native-editmode.xml")
shutil.copy2(C/"Logs/TruckSports/build-summary.txt",E/"build-summary.txt")
(L/"windows-fifteen-final-processes.json").write_text(json.dumps(receipts,indent=2));shutil.copy2(L/"windows-fifteen-final-processes.json",E/"windows-processes.json")
(E/"prior-fourteen-source-preservation.json").write_text(json.dumps({"checked":len(proof),"changed":changed},indent=2))
# Explicitly keep implementation, measured automated acceptance and human review distinct.
ledger=R/"TRUCK_SPORTS_ACCEPTANCE_LEDGER.md";text=ledger.read_text(encoding="utf-8")
for suffix in ["CORE","AI","ENV","LOGO","PREVIEW","SETUP","HUD","FX","RESULTS","QA"]:
 pattern=r"(#### EX01-"+suffix+r"\n)(.*?)(?=\n#### |\n### |\Z)";m=re.search(pattern,text,re.S);assert m,suffix
 values={"Status":"IMPLEMENTED / UNVERIFIED","Relevant Unity Scene":"Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity","Relevant Scripts":"Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters.","Prefabs or Assets":"Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG.","Implementation Evidence":"Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/.","Implementation Notes":"Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch.","Automated Verification":f"Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 {dll}.","Runtime Evidence":"Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed.","Screenshot/Video Evidence":"Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow.","Known Issues":"No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up.","Last Verification Date":"2026-10-20","Remaining Work":"Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness."}
 if suffix in ["LOGO","PREVIEW"]:values["Status"]="VERIFIED COMPLETE";values["Remaining Work"]="Human aesthetic review; PNG and actual gameplay preview are installed and rendered in Windows."
 block=m.group(2)
 for field,value in values.items():
  block,n=re.subn(r"\| "+re.escape(field)+r" \|[^\n]*","| "+field+" | "+value+" |",block);assert n==1,(suffix,field)
 text=text[:m.start(2)]+block+text[m.end(2):]
ledger.write_text(text,encoding="utf-8")
validation={"milestone":"fifteen of eighteen; Frogger, Avalanche Escape and Ninja Warrior remain in development","scenes":57,"unity":"6000.4.10f1","managedSha256":dll,"native":{"passed":253,"failed":0},"windows":receipts,"priorFourteenSourceFilesUnchanged":len(proof),"humanVerified":False,"physicalDevices":False,"listeningAcceptance":False,"isolatedProfiling":False}
(B/"BUILD-VALIDATION.json").write_text(json.dumps(validation,indent=2));(B/"SCENES.txt").write_text("\n".join(scenes)+"\n")
# Companion manifests exclude generated handoff text to avoid self-referential hashes.
items=[]
for p in sorted(B.rglob("*")):
 if not p.is_file() or (p.parent==B and p.name in ["BUILD-INFO.md","BUILD-INFO.txt","BUILD-INFORMATION.txt","BuildInfo.txt","BUILD-MANIFEST.sha256","BUILD-VALIDATION.json","SCENES.txt","GIT-DELIVERY.txt"]) or "DoNotShip" in str(p) or "BurstDebug" in str(p):continue
 items.append({"path":p.relative_to(B).as_posix(),"bytes":p.stat().st_size,"sha256":sha(p)})
manifest=L/"fifteen-runtime-files.json";manifest.write_text(json.dumps(items,indent=2));(B/"BUILD-MANIFEST.sha256").write_text("\n".join(v["sha256"]+"  "+v["path"] for v in items)+"\n")
body=f"""# Truck Sports — fifteen playable expansion additions; work continues

Windows x64 Development/Mono/D3D11; Unity6000.4.10f1.57saved scenes; startup TruckSports. Managed SHA256 {dll}.

This milestone adds actual Truck Hide & Seek. Frogger, Avalanche Escape and Ninja Warrior remain unfinished; local drafts are not playable-delivery claims. The earlier fourteen modes remain included and their169mode source/scene files were checked unchanged. Shared adapters use opt-in/default-preserving fields; actual Hauler/Herd camera/input regression42/0passed in Editor and Windows.

Native rules253/253passed. Actual Windows Hide47/0, adapter42/0, fifteen-scene snow/road-track48/0 and menu365/0passed on this exactDLL; all exit0. Complete default8AI round, actual AI physical tag, walls/LOS, engine shutdown/restart/headlights, duplicate-contact lock,1–4software humans/cabs, CNP ownership, role resets, replay and rematches are exercised. Full normal8-round rotation, physical controllers/wheels, subjective listening, human balancing and isolated profiling are not certified.

Fixes with preserved failure evidence: rebuilt CNP indicator references caused MissingReferenceExceptions; current hierarchy is retagged after vendor updates. Weather Maker night competed with an unmanaged scene sun; bind its real celestial sun and disable only the extra light. Converted warehouse materials had empty maps; owned material bindings use purchased source textures. Old floating loading fixtures now have actual wall/pole supports. The first Windows snow pass revealed bare cloned roads and omitted road tire contacts and stale authored coverage after native play; runtime road material clones, appropriate asphalt roughness and an actual Lit-clear/Weatherade-snow presentation switch prevent clear-weather snow. Actual renderer references and a snow-to-clear rematch are checked; authored material coverage remains zero. Only owned road materials and the opt-in Sports contact adapter were repaired, reusing the existing Weatherade trace API. Old modes retain their defaults. No Taxi or vendor source modified.

Actual gameplay/cab/replay/snow frames and receipts are in Documentation/TruckSports/Evidence/2026-10-20-Expansion/Fifteen/. Build companion manifest {len(items)}files / {sum(v['bytes'] for v in items):,}bytes. Backup: .sports-validation/Recovery/Expansion-Fifteen-Sport-57-Scene-Verified. GIT-DELIVERY.txt is updated only after the exact scoped commit and both remote refs are verified.

VENDOR ASSETS AUDITED: NWH powertrain/lights/spatial sound and damage APIs; Vista/EasyRoads world; Hivemind modular warehouse/source maps; Leartes garage props/source maps; ShipContainerPack; Compass Navigator Pro; Heat/TMP/DOTween; Weather Maker/Weatherade; existing LWS/FareFightRoute/input/crew/review.
VENDOR ASSETS USED: full NWH tractors/cabs and public engine/light/damage APIs; existing Vista TerrainData and EasyRoads/LWS graph; real Hivemind open hangar modules and Leartes furnishings; ShipContainerPack; stock CNP broad artificial-sector POIs; Weather Maker fixed-night profile and celestial light; Weatherade snow/trace API; established Heat/TMP/DOTween UI.
RELEVANT ASSETS NOT USED: raised MegaCity warehouse door model was physically unsuitable for truck access; reused purchased open modular hangars instead. No installed hide/tag/role authority exists.
CUSTOM SYSTEMS CREATED: game rules, observation/knowledge and fair AI coordination, bounded broad clues, engine-toggle semantics, owned industrial composition and original PNG, focused acceptance probes. Existing vendor vehicle/navigation/UI/weather frameworks remain authoritative.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

Build report:
{summary}
"""
(R/"Documentation/TruckSports/EighteenSportsFifteenSportDelivery.md").write_text(body,encoding="utf-8")
for p in [R/"Assets/TruckSports/BUILD-INFO.md"]+[B/n for n in ["BUILD-INFO.md","BUILD-INFO.txt","BUILD-INFORMATION.txt","BuildInfo.txt"]]:p.write_text(body,encoding="utf-8")
with (R/"Documentation/TruckSports/EighteenSportsProgress.md").open("a",encoding="utf-8") as f:f.write("\n\n"+body)
print("Recorded actual fifteen-sport receipt, permanent EX01 IDs and complete runtime manifest; remaining three not declared complete.")
