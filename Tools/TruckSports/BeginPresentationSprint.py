"""One-time, non-destructive requirement capture for the authorized presentation sprint."""
from pathlib import Path
from datetime import datetime, timezone
import json, re, shutil

ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Documentation/TruckSports'
ARCHIVE=DOC/'Archive/2026-10-20-LegacyBacklog'
LOGS=ROOT/'Logs/TruckSports/PresentationSprint'
LOGS.mkdir(parents=True,exist_ok=True)
nav=(ROOT/'Assets/TruckSports/Scripts/SportsNavigation.cs').read_text(encoding='utf-8')
registry=re.findall(r'\b(\w+)="(Truck[^"]+)"',nav.split('public static bool Collection')[0])
registry=[(key,scene) for key,scene in registry if key!='Front']
assert len(registry)==41 and len(set(s for _,s in registry))==41
titles={'Hockey':'TRUCK ICE HOCKEY','Cheese':'HARDCORE TRUCK CHEESE ROLLING','Dock':'TRUCK DOCK DIVING','KeepawayCab':'TRUCK KEEPAWAY CAB CHAOS','PedestrianPlowingCab':'PEDESTRIAN PLOWING CAB CHAOS','PedestrianPlowing':'PEDESTRIAN PLOWING','Bog':'TRUCK BOG SNORKELLING','Bungee':'TRUCK BUNGEE DESTRUCTION','Dodgeball':'TRUCK TRAMPOLINE DODGEBALL','Derby':'TRUCK DEMOLITION DERBY','Tug':'TRUCK TUG OF WAR','Skee':'TRUCK SKEE-BALL','Swing':'TRUCK GIANT SWING','Parking':'TRUCK MUSICAL PARKING','Wife':'TRUCK WIFE CARRYING','Mattress':'TRUCK MATTRESS RACING','Minefield':'TRUCK MINEFIELD','Terrier':'TRUCK TERRIER RACING','BaseJump':'TRUCK BASE JUMPING','SkyTow':'TRUCK SKY TOW RINGS'}
items=[]
for key,scene in registry:
 title=titles.get(key,re.sub(r'(?<=[a-z])(?=[A-Z])',' ',scene).upper())
 items.append({'key':key,'scene':scene,'title':title,'logo':'Assets/TruckSports/Art/Logos/'+scene+'.png'})
(LOGS/'registered-sports.json').write_text(json.dumps(items,indent=2),encoding='utf-8')
ledger=ROOT/'TRUCK_SPORTS_ACCEPTANCE_LEDGER.md'
if ledger.exists():raise SystemExit('Permanent ledger already exists; update it rather than replacing IDs.')
ARCHIVE.mkdir(parents=True,exist_ok=True)
legacy=['EssentialReleaseChecklist.md','PartyMasterLedger.md','GoddessPlaytestLedger.md','FareFightLedger.md','PartyCheckpoint.md']
for name in legacy:
 source=DOC/name;target=ARCHIVE/name
 assert source.resolve().is_relative_to(ROOT) and target.resolve().is_relative_to(ROOT)
 if target.exists():raise SystemExit('Existing archive must not be overwritten: '+str(target))
 shutil.copyfile(source,target)
 source.write_text('# Historical reference — archived October 20, 2026\n\nThe active requirements authority is [TRUCK_SPORTS_ACCEPTANCE_LEDGER.md](../../TRUCK_SPORTS_ACCEPTANCE_LEDGER.md).\n\nThis former backlog is retained unchanged in [the dated archive](Archive/2026-10-20-LegacyBacklog/'+name+'). Its old pending tasks do not automatically enter the current sprint. Relevant known defects and evidence remain available for reference.\n',encoding='utf-8')

lines=['# Truck Sports: Party! — permanent requirements and acceptance ledger','',
'Authoritative active-work record from October 20, 2026. Current authorization: Dock Diving, Poker, original logos for every registered sport, and the main menu overhaul. Historical work is deferred unless the user reactivates it or it directly affects these requirements.','',
'Statuses: MISSING; PARTIAL; IMPLEMENTED / UNVERIFIED; VERIFIED COMPLETE; BLOCKED. Automated completion and human verification are separate. Human verification is never inferred from software input, scripts or screenshots. IDs remain permanent; superseded/cancelled entries retain their record.','',
'## Active work','',
'- Dock Diving: DD-001–DD-005.','- Poker: PK-001–PK-005.','- Sport logos: LG-001–LG-041, dynamically captured from SportsNavigation and checked against the explicit build scene list.','- Menus: MN-001–MN-005.','- Ledger and delivery: LD-001–LD-003.','',
'Historical baseline: [dated backlog archive](Documentation/TruckSports/Archive/2026-10-20-LegacyBacklog/). Existing per-mode implementation and test evidence remains in Documentation/TruckSports. This sprint does not re-import the old reminder list.','']
def requirement(rid,system,request,criteria,status,evidence,remaining):
 lines.extend(['### '+rid+' — '+system,'',
 '| Field | Record |','|---|---|',
 '| Requirement ID | '+rid+' |','| Sport/System | '+system+' |',
 '| Original Request | '+request+' |','| Acceptance Criteria | '+criteria+' |',
 '| Status | '+status+' |','| Implementation Evidence | '+evidence+' |',
 '| Automated Verification | No new sprint acceptance run yet; previous evidence is baseline only. |',
 '| Human Verification | No new human playtest reported for this requirement. |',
 '| Remaining Work | '+remaining+' |',''])
lines+=['## Dock Diving','']
for rid,req,crit,remain in [
 ('DD-001','Complete airborne comic speech bubbles with correct participant ownership.','Visible timed bubble follows the actual airborne truck, uses participant identity/color, stays readable and screen-safe in supported views, and clears on landing/reset.','Inspect existing DockSlang, repair ownership/positioning/timing defects and validate actual Windows frames.'),
 ('DD-002','Complete launch/landing/reaction audio and natural cadence.','Correct event and source position; no rapid duplicate or overlapping reactions; shared volume/mute honored; no audio survives reset or exit.','Audit existing recordings, spatial sources and shared feedback; implement only missing event/ownership coordination.'),
 ('DD-003','Verify complete launches, trajectories, splitting, splashes, scoring and result transitions.','Complete real matches score each attempt once with useful cameras, accurate names/colors and preserved established rules.','Run focused actual matches and repair concrete defects.'),
 ('DD-004','Reliable multiplayer ownership, consecutive rounds, pause and rematches.','All supported participant configurations rotate correctly; independent inputs and results survive pause/reconnect/rematch without stale presentation.','Test supported turn-taking/multiplayer; do not invent a different simultaneous sport.'),
 ('DD-005','Inspect and finish the Dock Diving environment and presentation.','Finished installed-model venue, readable HUD/intro/logo, no missing materials, clipped action or placeholder artifacts in the player.','Inspect saved scene and real rendered flights; targeted environment/presentation corrections only.')]:
 requirement(rid,'Dock Diving',req,crit,'PARTIAL','Assets/TruckDockDiving/Scenes/TruckDockDiving.unity; existing DockMatch, DockSlang, DockReplay and authoring tools.',remain)
lines+=['## Poker','']
for rid,req,crit,remain in [
 ('PK-001','Finish the mobile dealer in the actual restaurant.','Dealer continuously travels a clear route, visibly operates vehicle and hands two face-down cards to each active participant, with correct wait/pause/skip/reset states.','Inspect actual room-route clearance and animations; run complete dealing cycles and repair concrete failures.'),
 ('PK-002','Finish coherent restaurant architecture, furniture, lighting and population.','Owned models and appropriate dressing support table visibility and dealer clearance; no obsolete placeholder surfaces or missing materials.','Inspect current saved room and add/fix only missing presentation.'),
 ('PK-003','Keep private cards private in human, shared and spectator views.','Only the correct owner can peek; moving cards and AI/spectator panes never reveal private values; public showdown follows established rules.','Repeat targeted privacy/render/input tests after room integration.'),
 ('PK-004','Preserve complete existing Hold’em rules and chip accounting.','Legal actions, side pots, ties, eliminations and full tournaments resolve without duplicate deals, lost chips or softlocks.','Run existing rule tests and actual complete tournaments across difficulties/rematches.'),
 ('PK-005','Finish Poker cameras, readable cards/actions/results and controller-compatible UI.','Supported human layouts have useful views and clear actions; dealer/showdown transitions preserve privacy; rematch/exit work.','Inspect actual layouts at common display shapes and test software input/navigation; record hardware limits.')]:
 requirement(rid,'Poker',req,crit,'PARTIAL','Assets/TruckPoker/Scenes/TruckPoker.unity; PokerMobileDealer, PokerPresentation, PokerTable, PokerVenueAuthoring.',remain)
lines+=['## Original sport logos','',
'Each registered scene has an individual art/integration requirement. Variants receive distinct related artwork rather than silently sharing another scene’s identity. Original PNG artwork must be inspected at actual menu size; generated output alone is not completion.','']
for i,item in enumerate(items,1):
 requirement(f'LG-{i:03}',item['title']+' ('+item['scene']+')','Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation.','A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows.','MISSING',item['logo']+' (planned); scene '+item['scene']+'.','Generate original artwork; inspect alpha/lettering/silhouette; import efficiently; integrate and capture actual menu/player evidence.')
lines+=['## Main menu and shared presentation','']
for rid,req,crit in [
 ('MN-001','Overhaul title and sport browsing with prominent selected sport, custom logos, screenshots and concise information.','All registered games remain discoverable and launchable; selected sport has a clear focus state, representative art and responsive polished layout.'),
 ('MN-002','Use current representative actual gameplay screenshots.','Every registered sport has an inspected genuine gameplay capture; changed Dock/Poker/Kickball environments and cameras are represented accurately.'),
 ('MN-003','Preserve and clarify sport setup, Your Crew, names/colors and participant options.','Existing human/AI configuration and identities persist; setup/start/results/return actions remain predictable and correct.'),
 ('MN-004','Complete keyboard/gamepad navigation, ownership and consistent Back/Confirm/Cancel behavior.','No lost/trapped focus; all registered entries/options are reachable; supported reconnect/shared ownership preserved; actual prompts are accurate.'),
 ('MN-005','Polished efficient transitions and screen-compatible UI.','Logo/screenshot aspect preserved, readable focus/description/configuration at720p/1080p/higher/ultrawide and relevant splits; no overlap, broken return transition or unnecessary texture load cost.')]:
 requirement(rid,'Main Menu',req,crit,'PARTIAL','Existing SportsFrontEnd, SportsCatalogue, SportsMenuArtAuthoring, Heat/uGUI, DOTween and Your Crew.', 'Implement targeted presentation over existing actions/configuration; validate rendered layouts and navigation in Windows.')
lines+=['## Ledger and delivery','']
requirement('LD-001','Requirements ledger','Archive the old backlog and make this permanent ledger the active authority without importing unrelated tasks.','Historical records preserved; stable IDs and all nine required fields exist for this sprint; future requests update original entries.','IMPLEMENTED / UNVERIFIED','TRUCK_SPORTS_ACCEPTANCE_LEDGER.md; Documentation/TruckSports/Archive/2026-10-20-LegacyBacklog; Tools/TruckSports/BeginPresentationSprint.py.','Check archive hashes and ID coverage; update workflow references.')
requirement('LD-002','Targeted acceptance','Test relevant complete Dock/Poker matches, logo/menu art, navigation, multiplayer, rematches and actual Windows rendering.','Appropriate targeted EditMode/PlayMode and player checks have exact receipts and visual evidence; no invented listening/hardware/human claims.','MISSING','Planned focused sprint probes over existing systems.','Run acceptance after implementation; record failures against their IDs.')
requirement('LD-003','Build and Git delivery','Deliver one combined Windows build and scoped commits/push, preserving unrelated work.','Builds/TruckSports/TruckSports.exe plus companions includes finished sprint work; settings restored; reviewed source pushed normally; remaining sprint issues explicit.','MISSING','Existing SportsBuilder.Build and audited public Sports-only delivery lineage.','Build after source/art integration; exact-player validation; scoped commit/push and handoff.')
lines+=['## Requirement history','', '- 2026-10-20: User explicitly replaced the historical backlog process and prioritized this sprint. Existing completed features are not automatically reopened.','- Registry snapshot:41 unique sport scenes from current SportsNavigation; final scene-list/menu coverage must agree.','']
ledger.write_text('\n'.join(lines),encoding='utf-8')
print('Captured',len(items),'registered sport scenes; archived',len(legacy),'historical documents; permanent sprint ledger created.')
