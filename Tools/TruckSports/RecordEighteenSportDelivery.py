"""Record real final expansion evidence. Never infer a pass from an existing script."""
import collections, datetime, hashlib, json, re, shutil, subprocess, xml.etree.ElementTree as ET
from pathlib import Path

R=Path(__file__).resolve().parents[2]
C=R/'.sports-validation/TruckSportsPublicValidation'
L=R/'Logs/TruckSports/EighteenSports'
B=R/'Builds/TruckSports'
E=R/'Documentation/TruckSports/Evidence/2026-10-20-Expansion/Eighteen'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p): return json.loads(p.read_text(encoding='utf-8-sig'))
stamp=datetime.datetime.now(datetime.timezone.utc).isoformat()
dll=sha(B/'TruckSports_Data/Managed/Assembly-CSharp.dll')
receipts=load(L/'windows-verified-processes.json')
assert {v['probe'] for v in receipts}=={'frog','avalanche','ninja','hide-adapters','snow','menu'}
assert all(v['exit']==0 and v['errors']==0 and v['completedEvidence'] and v['unchangedManaged'] and v['managedSha256']==dll for v in receipts), receipts
native=ET.parse(L/'eighteen-final-editmode.xml').getroot()
assert int(native.get('passed'))>=331 and native.get('failed')=='0'
progressProcesses=load(L/'progress-recovery-processes.json')
assert len(progressProcesses)==7 and all(v['exit']==0 for v in progressProcesses),progressProcesses
wallProcesses=load(L/'ninja-wall-correction-processes.json')
assert len(wallProcesses)==2 and all(v['exit']==0 for v in wallProcesses),wallProcesses
hudProcesses=load(L/'hud-counter-native-processes.json')
assert len(hudProcesses)==1 and hudProcesses[0]['exit']==0,hudProcesses
previewProcesses=load(L/'windows-preview-integration-processes.json')
assert len(previewProcesses)==2 and all(v['exit']==0 for v in previewProcesses),previewProcesses
previewProof=load(L/'windows-preview-preservation.json')
assert previewProof['managedAfter']==dll,previewProof
assert previewProof['managedBefore']==dll or set(previewProof['retestedModes'])=={'frog','avalanche','ninja','hide-adapters','snow','menu'},previewProof
correctionProcesses=load(L/'eighteen-ai-corrections-processes.json')
assert len(correctionProcesses)==8 and all(v['exit']==0 for v in correctionProcesses),correctionProcesses
ownedSources=[]
for sport in ['TruckFrogger','TruckAvalancheEscape','TruckNinjaWarrior']:
 rootSet={p.relative_to(R).as_posix() for p in (R/'Assets'/sport).rglob('*') if p.suffix in ['.cs','.asmdef']}
 cleanSet={p.relative_to(C).as_posix() for p in (C/'Assets'/sport).rglob('*') if p.suffix in ['.cs','.asmdef']}
 assert rootSet==cleanSet,(sport,rootSet^cleanSet)
 for path in (R/'Assets'/sport).rglob('*'):
  if path.suffix not in ['.cs','.asmdef']:continue
  relative=path.relative_to(R).as_posix();assert (C/relative).is_file() and sha(path)==sha(C/relative),relative
  ownedSources.append(relative)
tracked=set(subprocess.check_output(['git','ls-files','-z'],cwd=R).decode('utf-8').split('\0'))
allowed={v['path'] for v in load(L/'scoped-payload.json')}
compiledSources=[p for p in allowed if p.startswith('Assets/') and Path(p).suffix in ['.cs','.asmdef']]
assert all((C/p).is_file() and sha(R/p)==sha(C/p) for p in compiledSources),'Scoped compiled source differs from the delivery source'
dependencies=(C/'Logs/TruckSports/EighteenSports/final-three-asset-dependencies.txt').read_text(encoding='utf-8-sig').splitlines()
missing=[p for p in dependencies if p not in tracked|allowed or not (R/p).exists()]
assert not missing,('Unpublished dependencies',missing)
uiProcesses=load(L/'eighteen-final-ui-processes.json')
assert len(uiProcesses)==2 and all(v['exit']==0 for v in uiProcesses),uiProcesses
nativeProcesses=load(L/'eighteen-routes-repair-processes.json')
assert len(nativeProcesses)>=7 and all(v['exit']==0 for v in nativeProcesses),nativeProcesses
proof=load(L/'sixteen-prior-fifteen-source-preservation.json')
changed=[p for p,h in proof.items() if not (C/p).is_file() or sha(C/p)!=h]
assert not changed,changed
summary=(C/'Logs/TruckSports/build-summary.txt').read_text(encoding='utf-8-sig')
assert summary.startswith('Succeeded') and 'Errors: 0' in summary
scenes=summary.split('Scenes: ',1)[1].strip().split(', ')
assert len(scenes)==60 and scenes[0]=='Assets/TruckSports/Scenes/TruckSports.unity'
baseline=load(R/'Logs/TruckSports/EssentialRelease/baseline.json')
logoProof=load(L/'original-logo-preservation.json')
if isinstance(logoProof,list):logoProof={item['path']:item['sha256'] for item in logoProof}
assert isinstance(logoProof,dict)
if 'hashes' in logoProof:logoProof=logoProof['hashes']
preservation={}
for label,entries in [('protected',baseline['protected']),('settings',baseline['settings']),('logos',logoProof)]:
 differences=[p for p,h in entries.items() if not (R/p).is_file() or sha(R/p)!=h]
 assert not differences,(label,differences)
 preservation[label]={'checked':len(entries),'changed':differences}
(L/'eighteen-host-preservation.json').write_text(json.dumps(preservation,indent=2))
E.mkdir(parents=True,exist_ok=True)
for folder in ['Frog','Avalanche','Ninja','HideAdapters','Snow']:
 source=L/folder
 for p in source.iterdir():
  if p.is_file() and (p.name in ['result.txt','performance.json'] or p.suffix=='.png'):
   shutil.copy2(p,E/(folder+'-'+p.name))
shutil.copy2(R/'Logs/TruckSports/PresentationSprint/Menu/result.txt',E/'menu-result.txt')
for frame in (R/'Logs/TruckSports/PresentationSprint/Menu').glob('*.png'):shutil.copy2(frame,E/('Menu-'+frame.name))
for name in ['eighteen-final-editmode.xml','eighteen-host-preservation.json']:
 shutil.copy2(L/name,E/name)
for name in ['avalanche-staging-surface-audit.txt','avalanche-repaired-staging-surface-audit.txt','eighteen-routes-repair-processes.json','eighteen-final-ui-processes.json','eighteen-ai-corrections-processes.json','avalanche-offset-before-fix.xml','windows-eighteen-before-ai-fixes-processes.json','before-warped-wall-processes.json','ninja-before-warped-wall-result.txt','ninja-wall-before-fix-result.txt','ninja-wall-after-surface-fix-result.txt','ninja-wall-correction-processes.json']:
 shutil.copy2(L/name,E/name)
for name in ['hud-counter-native-processes.json','windows-preview-integration-processes.json','windows-preview-preservation.json','windows-before-hud-counter.json']:shutil.copy2(L/name,E/name)
for name in ['progress-recovery-processes.json','windows-before-progress-recovery.json','before-progress-preview-processes.json']:shutil.copy2(L/name,E/name)
for name in ['ninja-contact-before-fix.xml','progress-before-centre-landing-processes.json','ninja-before-centre-landing-result.txt','ninja-centre-first-native-result.txt','ninja-centre-repeat-native-result.txt','centre-before-contact-audit-processes.json','ninja-articulated-first-native-result.txt']:shutil.copy2(L/name,E/name)
for name in ['ninja-static-contact-before-fix.xml','articulated-before-static-audit-processes.json','ninja-articulated-repeat-native-result.txt','ninja-clearance-first-native-result.txt']:shutil.copy2(L/name,E/name)
shutil.copy2(L/'BeforeProgressRecovery/Ninja/result.txt',E/'Ninja-before-progress-recovery-result.txt')
shutil.copy2(C/'Logs/TruckSports/build-summary.txt',E/'build-summary.txt')
(L/'windows-eighteen-final-processes.json').write_text(json.dumps(receipts,indent=2))
shutil.copy2(L/'windows-eighteen-final-processes.json',E/'windows-processes.json')
(E/'prior-fifteen-source-preservation.json').write_text(json.dumps({'checked':len(proof),'changed':changed},indent=2))
for mode in ['frog','avalanche','ninja']:
 p=L/(mode+'-before-delivery-native-result.txt')
 if p.exists():shutil.copy2(p,E/(mode+'-preceding-native-result.txt'))
for probe in ['Frog','Avalanche','Ninja']:shutil.copy2(C/'Logs/TruckSports/EighteenSports'/probe/'result.txt',E/(probe+'-latest-editor-gameplay-result.txt'))

detail={
 'EX04':('TruckFrogger','Frog','Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch.',
 'Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis.',
 'Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween.'),
 'EX07':('TruckAvalancheEscape','Avalanche','Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch.',
 'Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles.',
 'Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators.'),
 'EX05':('TruckNinjaWarrior','Ninja','Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch.',
 'Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority.',
 'Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay.')}

for rid,extra in [('EX07',' The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention.'),('EX05',' A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact.')]:
 sport,probe,features,cause,assets=detail[rid];detail[rid]=(sport,probe,features,cause+extra,assets)

sport,probe,features,cause,assets=detail['EX05'];detail['EX05']=(sport,probe,features,cause+' Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact.',assets)

ledger=R/'TRUCK_SPORTS_ACCEPTANCE_LEDGER.md'
text=ledger.read_text(encoding='utf-8')
text=text.replace('The new18-sport expansion is now active, in the original request\'s recommended vertical-slice order.','All18requested expansion sports are implemented and integrated in the60-scene Windows build. The individual entries retain automated evidence separately from outstanding human, physical-device, listening and extended-soak acceptance.')
for rid,(sport,probe,features,cause,assets) in detail.items():
 receipt=next(v for v in receipts if v['probe']==probe.lower())
 artwork=R/'Tools/TruckSports/ArtworkSources'/(sport+'.json')
 metadata=load(artwork);metadata['status']='Imported original PNG; actual saved-scene setup/info/venue and Windows selection-menu rendering verified. Gameplay preview is an actual Windows capture.';metadata['verification']=dict(utc=stamp,managedSha256=dll,menuChecks=next(v['checks'] for v in receipts if v['probe']=='menu'));artwork.write_text(json.dumps(metadata,indent=2),encoding='utf-8')
 for suffix in ['CORE','AI','ENV','LOGO','PREVIEW','SETUP','HUD','FX','RESULTS','QA']:
  pattern=r'(#### '+rid+'-'+suffix+r'\n)(.*?)(?=\n#### |\n### |\Z)'
  m=re.search(pattern,text,re.S);assert m,(rid,suffix)
  values={'Status':'IMPLEMENTED / UNVERIFIED','Relevant Unity Scene':f'Assets/{sport}/Scenes/{sport}.unity','Relevant Scripts':f'Assets/{sport}/Scripts/, Editor/ and Tests/Editor/.','Prefabs or Assets':assets,'Implementation Evidence':'Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/.','Implementation Notes':features+' Root causes: '+cause,'Automated Verification':f'Final native EditMode{native.get("passed")}/{native.get("passed")}passed. Actual final Windows{probe}{receipt["checks"]}/0; same DLL{dll}; shared adapter, all 18 snow and menu checks recorded in windows-processes.json.','Runtime Evidence':f'Evidence/2026-10-20-Expansion/Eighteen/{probe}-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions.','Screenshot/Video Evidence':f'Evidence/2026-10-20-Expansion/Eighteen/{probe}-*.png; actual Windows composed-frame720p/1080p captures.','Human Verification':'Not human-playtested or physically controlled in this acceptance pass.','Known Issues':'Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun.','Last Verification Date':stamp,'Remaining Work':'Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry.'}
  if suffix in ['LOGO','PREVIEW']:
   values['Status']='VERIFIED COMPLETE';values['Remaining Work']='Optional human aesthetic review. Original imported PNG and actual gameplay preview were rendered and checked in the built menu.'
  block=m.group(2)
  for field,value in values.items():
   block,n=re.subn(r'\| '+re.escape(field)+r' \|[^\n]*','| '+field+' | '+value+' |',block);assert n==1,(rid,suffix,field)
  text=text[:m.start(2)]+block+text[m.end(2):]
ledger.write_text(text,encoding='utf-8')
expansionBlocks=list(re.finditer(r'#### (EX\d{2}-\w+)\n(.*?)(?=\n#### |\n### |\Z)',text,re.S))
assert len(expansionBlocks)==180
counts=collections.Counter(re.search(r'\| Status \| ([^|]+?) \|',m.group(2)).group(1) for m in expansionBlocks)
perf={mode:load(L/mode/'performance.json') for mode in ['Frog','Avalanche','Ninja']}
assert all(v['platform']=='WindowsPlayer' and v['samples']>0 for v in perf.values())
validation={'scope':'All 18 requested expansion sports implemented; automated acceptance and outstanding human/hardware acceptance are distinguished. Historical unrelated backlog is not declared complete.','verifiedUTC':stamp,'scenes':60,'unity':'6000.4.10f1','managedSha256':dll,'native':{'passed':int(native.get('passed')),'failed':0},'windows':receipts,'priorFifteenSourceFilesUnchanged':len(proof),'hostPreservation':preservation,'humanVerified':False,'physicalDevices':False,'listeningAcceptance':False,'performance':perf,'ledgerStatusCounts':dict(counts)}
(B/'BUILD-VALIDATION.json').write_text(json.dumps(validation,indent=2))
(E/'delivery-validation.json').write_text(json.dumps(validation,indent=2))
(B/'SCENES.txt').write_text('\n'.join(scenes)+'\n')
items=[]
for p in sorted(B.rglob('*')):
 if not p.is_file() or (p.parent==B and p.name in ['BUILD-INFO.md','BUILD-INFO.txt','BUILD-INFORMATION.txt','BuildInfo.txt','BUILD-MANIFEST.sha256','BUILD-VALIDATION.json','SCENES.txt','GIT-DELIVERY.txt']) or 'DoNotShip' in str(p) or 'BurstDebug' in str(p):continue
 items.append({'path':p.relative_to(B).as_posix(),'bytes':p.stat().st_size,'sha256':sha(p)})
(L/'eighteen-runtime-files.json').write_text(json.dumps(items,indent=2))
(B/'BUILD-MANIFEST.sha256').write_text('\n'.join(v['sha256']+'  '+v['path'] for v in items)+'\n')

body=f'''# Truck Sports: Party! — eighteen-sport expansion delivery

Recorded {stamp}. Windows x64 Development/Mono/D3D11, Unity6000.4.10f1. All 18 requested expansion sports have saved playable scenes, original imported PNG logos, real gameplay previews and menu integration. The combined executable contains 60 scenes and starts at TruckSports. This is an implementation and automated-acceptance delivery, not a claim of human-certified release readiness or completion of the archived unrelated backlog.

Build: Builds/TruckSports/TruckSports.exe, with all companion files. Managed SHA256: {dll}. Runtime manifest: {len(items)} files / {sum(v['bytes'] for v in items):,}bytes. The previously accepted57-scene build is preserved at .sports-validation/Recovery/Expansion-Fifteen-Sport-57-Scene-Verified. The final accepted build is backed up separately after verification.

## Final verification

- Native EditMode: {native.get('passed')}/{native.get('passed')}passed; includes all 18 rule suites, saved-scene bindings, jump regression and warped-wall seam regression. XML is provided. Runtime tests below are explicit Editor PlayMode and Windows gameplay probes, not a fabricated NUnit PlayMode XML result.
'''
for v in receipts:body+=f'- Actual Windows {v["probe"]}: {v["checks"]}checks,0errors, exit0,{v["seconds"]}seconds; unchanged identical managed binary.\n'
body+=f'''- The earlier 15 sport-owned sources/scenes/assemblies: {len(proof)}files checked unchanged. Their previously recorded complete-match tests remain historical evidence, not a claim that every old test ran again on the new binary.
- Shared route/default-preserving tuning, HUD/camera/input adapters and snow accumulation were rechecked where genuinely affected. All18snow scenes and the complete registered menu are exercised by the final receipts.
- Host preservation: {json.dumps(preservation)}. No original logo or protected Taxi/vendor file was changed by this delivery.
- Actual Windows composed frames at720p/1080p, replay/cab/result evidence, imported logo/preview and native receipts are in Evidence/2026-10-20-Expansion/Eighteen/.

## Per-sport implementation and evidence

The first 15 are preserved working slices. Exact earlier tests and limitations are retained in the corresponding delivery reports; they are not retrospectively relabelled human-verified.

| Sport | Implemented gameplay | Preserved acceptance reference |
|---|---|---|
| Red Light, Green Light | Twelve NWH trucks, laugh-warning red/green authority, genuine movement attribution, physical destruction and finish/results | EighteenSportsDelivery.md |
| Hot Potato | Physical possession/pass cooldown, finite fuse, actual truck destruction, scoring and rematches | EighteenSportsDelivery.md |
| Balloon Battle | Physical per-balloon hit attribution and one-time pops, attacks, elimination, cameras and match flow | EighteenSportsDelivery.md |
| Whack-a-Mole | Finite timed targets, physical valid-window hits, fair AI, scoring and rounds | EighteenSportsDelivery.md |
| King of the Hill | Moving/contested territory, authoritative possession time, actual truck competition and compact bottom-row HUD | EighteenSportsFiveSportDelivery.md; Evidence/HillHud/ |
| Seesaw | True weight-driven platforms, balance/elimination, bounded physical recovery, results/review | EighteenSportsSixSportDelivery.md |
| Floor Is Lava | Physical collapsing platforms, survival/ramming, lava/destruction, deterministic resolution and rematches | EighteenSportsSevenSportDelivery.md |
| Tetherball | Physical constrained ball and correct directional winding, competing trucks, score and replay | EighteenSportsNineSportDelivery.md |
| Jousting | Physical lance/contact attribution, damage, rounds and fair AI approaches | EighteenSportsNineSportDelivery.md |
| Magnet Mayhem | Bounded polarity forces, real occlusion and hauling, settled bay scoring,40physical scrap objects | EighteenSportsTenSportDelivery.md |
| Volleyball | Actual giant-ball contacts, teams/three-touch rules, jumps, net crossing and match flow | EighteenSportsTwelveSportDelivery.md |
| Freeze Tag | Physical tag/rescue, true frozen state, fair teams and match resolution | EighteenSportsTwelveSportDelivery.md |
| Hungry Hungry Haulers | Physical scoop/hopper and deposited-ball scoring, shared input/cab/AI, finite collection and rematch | EighteenSportsFourteenSportDelivery.md |
| Sheep Herding | Actual animated flock threat/escape and full-body pen scoring; no proximity points | EighteenSportsFourteenSportDelivery.md |
| Hide & Seek | Eight trucks, stealth engine/lights/restart, fair LOS/audio clues, physical tags, found spectators and role rotation | EighteenSportsFifteenSportDelivery.md |
'''
for rid,(sport,probe,features,cause,assets) in detail.items():
 body+=f'\n### {probe} ({rid})\n\n{features}\n\nScene: Assets/{sport}/Scenes/{sport}.unity. Scripts/authoring/tests: Assets/{sport}/. Original artwork: Assets/TruckSports/Art/Logos/{sport}.png; real preview beside it in Art/Previews/.\n\nRoot causes repaired: {cause}\n\nReused assets: {assets}\n'
body+='\n## Actual Windows gameplay frames\n\n![Frogger](Evidence/2026-10-20-Expansion/Eighteen/Frog-challenge-1-1080.png)\n\n![Avalanche Escape](Evidence/2026-10-20-Expansion/Eighteen/Avalanche-mountain-1080.png)\n\n![Ninja Warrior](Evidence/2026-10-20-Expansion/Eighteen/Ninja-course-1080.png)\n'
body+='\n## Built menu evidence\n\n![Windows sport-selection showcase](Evidence/2026-10-20-Expansion/Eighteen/Menu-showcase-1920x1080.png)\n\nAll eight collection pages, title screen,720p/1080p/1440p and ultrawide captures are retained beside this image. These are actual composed Windows frames from the final menu acceptance.\n'
body+='\n## Measured development-build performance\n\nUnity ProfilerRecorder and FrameTimingManager telemetry during normal-speed live play, after warmup; capture pauses excluded. This is the current workstation/development build, not a controlled hardware benchmark. This measures normal-speed AI-only broadcast play in clear weather and the neutral burial fixture; it is not a four-viewport severe-weather benchmark. A zero counter sample count means unavailable, never zero cost. Complete JSON, including sample counts and memory, is preserved.\n\n| Mode | Frames | Frame mean/p95 ms | CPU mean/p95 ms | Physics mean/p95 ms | GPU samples | Peak allocated MiB |\n|---|---:|---:|---:|---:|---:|---:|\n'
for mode,p in perf.items():body+=f'| {mode} | {p["samples"]} | {p["frameMeanMs"]:.2f}/{p["frameP95Ms"]:.2f} | {p["cpuMeanMs"]:.2f}/{p["cpuP95Ms"]:.2f} | {p["physicsMeanMs"]:.2f}/{p["physicsP95Ms"]:.2f} | {p["gpuSamples"]} | {p["peakAllocatedBytes"]/1048576:.1f} |\n'
body+=f'''
## Windows-discovered AI and artwork corrections

The first complete Windows pass was retained with its failures: Avalanche41checks/1error, Ninja32checks/2errors, menu384checks/1error. Avalanche's broad-road AI reached its15m-offset waypoint but checked advancement against the road centre; the new focused test reproduced the stuck index on the old code. Advancement now measures the actual assigned waypoint, and checkpoint recovery repairs genuine vehicle damage through NWH's public API. Ninja's original fixed takeoff points launched too early when real forward speed was lower. Takeoffs now approach the actual tilting-deck edges, charge the jump before contact, and use the existing permitted-landing-body navigation API. The first safe landing remains separated by a real13.5m jump; renderer/collider agreement has a saved-scene regression test. Avalanche's imported setup logo existed but used a different object name; it now follows the shared authored artwork convention. Final receipts above are the post-correction Windows results, not the earlier failed attempts.

## Round reset presentation correction

Visual review caught an old SPLASH +5 announcement persisting after a fresh Ninja round because the match clock reset while its transient-message expiry did not. Ninja and Avalanche now clear announcement text, its expiry and old feedback voices in BeginGame. The final Windows probes explicitly check fresh-round messages; preceding native gameplay receipts are retained separately and are not represented as post-fix UI verification.

Frogger's final-approach crossing counter is also clamped to the actual gate total, preventing5/4after the fourth gate. The final HUD change was compiled and the native suite rerun, then actual Windows screenshots were installed as menu previews. Because this source change produced a new managed binary, all six Windows suites were rerun on that final binary rather than reusing the earlier gameplay receipts. The exact before/after hashes and preview-integration preservation record are supplied.

The latest Editor gameplay-probe receipts are labelled separately. Those physical gameplay runs preceded the final one-line Frogger HUD edit; the324-test EditMode suite and all final Windows gameplay checks were rerun after that edit. No earlier Editor result is represented as a post-edit human or hardware playtest.

## Intermittent beached-truck recovery correction

The next complete Windows repetition caught a genuine Ninja failure despite a prior passing match: the truck remained near the second tilting step for over100seconds. Suspension contact jitter repeatedly reset its instantaneous-speed stall timer. Recovery now measures meaningful body displacement, counting real driving and vertical jumps while ignoring small vibration. Four EditMode regressions cover jitter, movement, jumping and checkpoint reset. A new explicitly constrained real-NWH beached-truck fixture verifies bounded recovery, exactly one five-second penalty, an upright return to the earned checkpoint and no invented progress; temporary fixture constraints are restored. The full-course and Windows acceptance assertions remain strict. The failed4870binary result is retained separately from the final pass.

The measured first-jump arc also landed near the moving deck's far edge; its input takeoff now targets the middle. A separate saved-scene collision audit reproduced a1m penetration between the starting deck and first moving step, plus world-anchored moving decks intersecting their static supports. The start pad retains its spawn and flat surface but ends before the moving step. Four joint supports are now kinematic connected bodies: the joints disable collision only with their own mechanically interlocking supports, while trucks still collide normally with both. No global collision layer, jump force, scoring shortcut or vendor source was changed. Two initially failing saved-scene regressions now pass, and actual gameplay checks that unloaded steps remain level at their anchors before the truck arrives. Consecutive complete physical attempts and final Windows acceptance are recorded separately.

The whole-platform contact audit additionally measured1.1m seesaw penetration into each adjacent static pad. The pads now leave genuine clearances at both ends, retain the earned scoring/checkpoint planes, and provide safe full-truck recovery at the checkpoint itself. AI uses its existing charged jump for the seesaw departure. The additional regression checks every weighted deck against all unconnected static geometry; the live unloaded-platform check includes the seesaw as well as all three steps. No overlapping scenery is silently ignored by a global layer change.

## Honest remaining acceptance

Physical gamepads/wheels, listening, subjective balance and human visual/playtest approval were not performed. Software-created input devices exercise ownership and camera/control integration, not hardware certification. Extended multi-round alternating championships, Easy/Normal balance across every course, severe-weather replay readability and long soak/memory behavior are not exhaustively certified. Raw performance evidence above must be reviewed on target hardware. These are individually retained against the permanent IDs; no failed test is hidden by deleting its assertion.

Ledger status counts for these 180 expansion requirements: {json.dumps(dict(counts))}. IMPLEMENTED / UNVERIFIED entries intentionally remain distinct from objective VERIFIED COMPLETE and human approval. The archived historical backlog is not silently reopened or declared finished.

## Vendor accounting

VENDOR ASSETS AUDITED: NWH public vehicle/wheel/damage APIs, UTS public traffic/controller/path APIs, EasyRoads/LWS authoring/navigation, Sky Tow/Vista Arctic terrain and installed city/industrial/rock/pine inventories, Weather Maker/Weatherade, Cinemachine, Heat/TMP/DOTween, existing crowd/water/input/crew/replay systems.

VENDOR ASSETS USED: full NWH tractors/cabs; UTS real traffic; existing EasyRoads/world baking and LWS route adapter; owned clones of purchased terrain, buildings, containers, rocks and foliage; existing Fishing URP water; Weather Maker effects/audio and Weatherade snow/trace material APIs; Cinemachine, existing Heat/TMP/DOTween and Sports review.

RELEVANT ASSETS NOT USED: installed inventory had no complete matching Frogger freight train/container machinery, competitive avalanche game authority or six-obstacle truck-Ninja sport. Those game-specific structures/semantics were authored. Scene Streamer is unnecessary for bounded single-scene courses and was not duplicated. No replacement vehicle, traffic, weather or UI framework was created.

CUSTOM SYSTEMS CREATED: finite Frog/Avalanche/Ninja rules and AI coordination; seeded physical obstacle scheduling and whole-body gates; actual-contact snow exposure/burial; NWH suspension/jump/checkpoint adaptation; owned course/environment composition and original PNGs; focused acceptance probes and wrappers over Unity profiler APIs.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Exact build report

{summary}
'''
for old,new in [('Unity6000.4.10f1','Unity 6000.4.10f1'),('accepted57-scene','accepted 57-scene'),('scenes and starts at TruckSports','scenes and starts at the Truck Sports menu'),('All18requested','All 18 requested'),('All18snow','All 18 snow'),('all18snow','all 18 snow'),('real13.5m','real 13.5 m'),('assigned15m','assigned 15 m'),('from12m/s to0','from 12 m/s to 0'),('at720p/1080p','at 720p/1080p'),('Native EditMode '+native.get('passed')+'/'+native.get('passed')+'passed','Native EditMode '+native.get('passed')+'/'+native.get('passed')+' passed')]:body=body.replace(old,new)
(R/'Documentation/TruckSports/EighteenSportsCompleteDelivery.md').write_text(body,encoding='utf-8')
for p in [R/'Assets/TruckSports/BUILD-INFO.md']+[B/n for n in ['BUILD-INFO.md','BUILD-INFO.txt','BUILD-INFORMATION.txt','BuildInfo.txt']]:p.write_text(body,encoding='utf-8')
progress=R/'Documentation/TruckSports/EighteenSportsProgress.md'
if not (L/'pre-complete-progress.md').exists():shutil.copy2(progress,L/'pre-complete-progress.md')
progress.write_text('# Eighteen-sport expansion — current delivery\n\nAll18requested modes are implemented and integrated in the60-scene Windows build. See [the complete delivery and actual verification](EighteenSportsCompleteDelivery.md) and the root permanent acceptance ledger.\n\nNative EditMode '+native.get('passed')+'/'+native.get('passed')+'passed. Actual Windows receipts, same-binary hashes, performance telemetry and retained human/hardware limitations are recorded in the delivery. No claim of human-certified release readiness.\n\nPrevious milestones remain preserved in EighteenSportsDelivery.md and the Five/Six/Seven/Nine/Ten/Twelve/Fourteen/FifteenSportDelivery reports, their evidence and Git history. The previous accumulated progress text is additionally retained locally in Logs/TruckSports/EighteenSports/pre-complete-progress.md.\n',encoding='utf-8')
print('Recorded actual full18implementation,60saved scenes, exact receipts and outstanding human acceptance. No invented verification.')
