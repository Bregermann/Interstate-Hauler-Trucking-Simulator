"""One-time capture of the explicitly authorized October20 expansion.

Only requirements/source documents are created here, never scenes or game state.
Do not rerun over a ledger that already contains these permanent IDs.
"""
from pathlib import Path
import hashlib,re
r=Path(__file__).resolve().parents[2]
source=Path('C:/Users/Jeremy/.codex/attachments/fac7b13b-e218-4db4-9796-c28e2a078f6b/Pasted text.txt')
requests=r/'Documentation/TruckSports/Requests';requests.mkdir(exist_ok=True)
raw=source.read_bytes();(requests/'2026-10-20-Eighteen-Sports.md').write_bytes(raw)
addendum='''# All18sports: UI and custom logo addendum

For EVERY new sport, match or exceed the best existing Truck Sports interfaces, particularly Musical Parking. Gameplay completion is not presentation completion.

1. Unique original custom PNG logo/icon, consistent with Truck Sports: Party!.
2. Actual gameplay screenshot in its main-menu card.
3. Polished setup: match options, AI difficulty, names, weather and camera choices.
4. Complete gameplay HUD: attractive panels, truck portraits, scores, timers, progress and mode information.
5. Custom sport-appropriate countdown/start presentation.
6. Animated scoring feedback for points, eliminations, bonuses and achievements.
7. Polished victory/defeat/results/rematch with names, placements and useful statistics.
8. Readable actual-device prompts, controller support and appropriate split screens.
9. Cohesive typography, colors, effects and animation across all18.

Reuse polished shared UI. No placeholder text, primitive menus, debug HUDs or generic icons as final delivery. Create/import/assign actual reusable image assets and inspect their renders. Separate acceptance entries for every game's logo, screenshot, setup, gameplay HUD, scoring effects and results.
'''
(requests/'2026-10-20-Eighteen-Sports-UI-Addendum.md').write_text(addendum,encoding='utf-8')
spec=raw.decode('utf-8-sig'); games=re.findall(r'^# GAME (\d+) — (.*?)\r?\n(.*?)(?=^# GAME \d+ —|^# SECTION 2)',spec,re.M|re.S);assert len(games)==18
keys=['HideSeek','RedLightGreenLight','HotPotato','Frogger','NinjaWarrior','FloorIsLava','AvalancheEscape','FreezeTag','Seesaw','Volleyball','BalloonBattle','MagnetMayhem','Tetherball','Jousting','HungryHaulers','WhackAMole','KingOfTheHill','SheepHerding']
ledger=r/'TRUCK_SPORTS_ACCEPTANCE_LEDGER.md';text=ledger.read_text(encoding='utf-8');assert '### EX01-CORE' not in text
text=text.replace('| Status | VERIFIED COMPLETE |','| Status | VERIFIED |').replace('| Status | IMPLEMENTED / UNVERIFIED |','| Status | IMPLEMENTED BUT UNVERIFIED |')
text=text.replace('Statuses: MISSING; PARTIAL; IMPLEMENTED / UNVERIFIED; VERIFIED COMPLETE; BLOCKED.','Statuses: MISSING; PARTIAL; IMPLEMENTED BUT UNVERIFIED; VERIFIED; BLOCKED. October20 expansion standardizes the two equivalent prior labels without changing their evidence or human-verification meaning.')
text=text.replace('Current authorization: Dock Diving, Poker, original logos for every registered sport, and the main menu overhaul.','Current authorization: completed presentation sprint and Git reconciliation, followed by the18new sports and their complete UI/logo addendum.')
at=text.index('## Active work');end=text.index('Historical baseline:',at)
text=text[:at]+'''## Active work

Git reconciliation is verified (LD-004); public main is synchronized and local/private work is recoverable. The new18-sport expansion is now active, in the original request's recommended vertical-slice order. Source requirements and UI addendum are preserved in Documentation/TruckSports/Requests/. Earlier focused-sprint implementation/evidence remains recorded; its human/device/listening limits have not disappeared.

Expansion order: Red Light Green Light, Hot Potato, Balloon Battle, Whack-a-Mole, King of the Hill; Seesaw, Floor Is Lava, Tetherball, Jousting, Magnet Mayhem; Volleyball, Freeze Tag, Hungry Hungry Haulers, Sheep Herding; Hide and Seek, Frogger, Avalanche Escape, Ninja Warrior. Do not create18empty scenes or count registry entries as gameplay.

'''+text[end:]
sections=['\n## Eighteen-sport expansion — authorized October20\n\nSource SHA256: `'+hashlib.sha256(raw).hexdigest()+'`. Original wording: [master request](Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md), [UI addendum](Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports-UI-Addendum.md). The new status spelling is equivalent to prior evidence-qualified statuses; existing IDs are unchanged.\n']
for (number,title,body),key in zip(games,keys):
 prefix='EX'+number.zfill(2);folder='Truck'+key;scene='Assets/'+folder+'/Scenes/'+folder+'.unity'
 acceptance=body.split('## Acceptance',1)[-1].split('\n---',1)[0].strip();acceptance=' '.join(acceptance.split()).replace('|','/')
 section_anchor='game-'+number+'--'+title.lower().replace(' & ','--').replace(',','').replace(' ','-')
 original='Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME '+number+' ('+title+'), including all confirmed design decisions.'
 items=[
 ('CORE','Complete playable rules and genuine physical gameplay. '+original,acceptance),
 ('AI','Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. '+original,'Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores.'),
 ('ENV','Detailed sport-specific modeled environment using audited owned assets. '+original,'Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection.'),
 ('LOGO','Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction.','Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p.'),
 ('PREVIEW','Actual representative gameplay screenshot in the menu selection card.','Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance.'),
 ('SETUP','Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership.','Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work.'),
 ('HUD','Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start.','Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output.'),
 ('FX','Animated scoring, eliminations, bonuses and notable-event feedback.','Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up.'),
 ('RESULTS','Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return.','Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up.'),
 ('QA','Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions.','Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations.')]
 sections.append('\n### '+prefix+' — '+title+'\n')
 for suffix,request,criteria in items:
  rid=prefix+'-'+suffix
  fields=[('Requirement ID',rid),('Sport/System',title),('Original Request',request),('Acceptance Criteria',criteria),('Status','MISSING'),('Relevant Unity Scene',scene+' (not yet created)'),('Relevant Scripts','Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding.'),('Prefabs or Assets','Audit appropriate installed vendor assets; no placeholder delivery.'),('Implementation Evidence','None yet.'),('Implementation Notes','Keep one game-rule authority; reuse input, identity, weather, cameras and UI.'),('Test Procedure',criteria),('Automated Verification','Not executed.'),('Runtime Evidence','Not executed.'),('Screenshot/Video Evidence','None yet.'),('Human Verification','No human playtest of this new sport.'),('Known Issues','Requirement not implemented.'),('Last Verification Date','None'),('Remaining Work','Implement, integrate and execute this acceptance requirement.')]
  sections.append('\n#### '+rid+'\n\n| Field | Record |\n|---|---|\n'+'\n'.join('| '+a+' | '+b+' |' for a,b in fields)+'\n')
text+='\n'.join(sections)
ledger.write_text(text,encoding='utf-8');print('Captured18original sport specs and180permanent implementation/presentation requirements; no scene completion claimed.')
