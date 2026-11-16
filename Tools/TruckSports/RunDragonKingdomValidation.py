"""Serialized0.8 native validation; never touches existing scenes or builds.
Only the new DragonKingdom subtree plus the reviewed Animal tag are intaken.
Each attempt retains a unique manifest/log/result; ROOT source may advance
independently after the exact snapshot has been copied into CLEAN.
"""
import argparse, hashlib, json, re, sys, uuid, zipfile
import xml.etree.ElementTree as ET
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/TruckSports'))
import RunBroadcastValidation as runner
from WorkspaceProcessSafety import require_idle_workspace

# Initial attachment is separate from legitimate later AI gate-rim dislodgement.
# This proof retains two real10s launches, strict geometry/visibility and natural flight.
MOUNT_TEST = 'DragonRiderSaddleNativeTests.TwoActualSoftwareHumanStartsKeepOriginalNwhMountedAboveBackThroughNaturalTakeoffCoreFlightAndRematch'

def mount_dependency(path):
    return (path.startswith(('Assets/TruckDragonRiders/Scripts/', 'Assets/TruckDragonRiders/Scenes/',
                            'Assets/TruckDragonRiders/Animations/',
                            'Assets/TruckDragonKingdom/Scripts/', 'Assets/TruckDragonKingdom/Prefabs/Dragons/',
                            'Assets/TruckDragonKingdom/Animations/Authored/'))
            or path in ('Assets/TruckDragonRiders/Animations.meta',
                        'Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset',
                        'Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.cs',
                        'Assets/TruckDragonRiders/Tests/Native/DragonRiderSaddleNativeTests.cs',
                        'Assets/TruckDragonRiders/Tests/Native/DragonRiderSaddleFrameObserver.cs',
                        'Assets/TruckDragonRiders/Tests/Native/LWS.TruckDragonRiders.NativeTests.asmdef',
                        'Assets/TruckSports/Scripts/SportsBroadcastFraming.cs',
                        'Assets/TruckSports/Scripts/SportsProbeInputIsolation.cs'))

def saved_mount_profile_dependencies(root):
    """Only owned saved motion/profile closure; native source import stays native."""
    folder=root/'Assets/TruckDragonRiders/Animations';required=set();owned_hashes={}
    if not folder.is_dir():return required,owned_hashes
    for source in folder.rglob('*'):
        if source.is_file():required.add(source.relative_to(root).as_posix())
    required.add('Assets/TruckDragonRiders/Animations.meta')
    for path in tuple(required):
        if not path.endswith('.meta'):
            if not (root/(path+'.meta')).is_file():raise ValueError('Missing saved animation/profile Unity metadata: '+path)
            required.add(path+'.meta')
    active=any('/Normal Flight V1' in path for path in required)
    if not active:return required,owned_hashes
    for species in ('dragon-inferno','dragon-fire'):
        prefix='Assets/TruckDragonRiders/Animations/'+species+'/Normal Flight V1';metadata=prefix+' Recipe.json';controller=prefix+'.overrideController'
        if not (root/metadata).is_file() or not (root/controller).is_file():raise ValueError('Incomplete saved normal-flight profile: '+species)
        data=json.loads((root/metadata).read_text(encoding='utf-8-sig'))
        if type(data.get('version')) is not int or data['version']!=1 or data.get('species')!=species or data.get('controller')!=controller or not isinstance(data.get('recipe'),str) or not data['recipe']:raise ValueError('Mismatched saved normal-flight profile provenance: '+species)
        rows=data.get('ownedFiles')
        if not isinstance(rows,list) or not rows or len({row.get('path') for row in rows})!=len(rows):raise ValueError('Distinct exact owned saved-profile members required: '+species)
        declared={}
        for row in rows:
            path,digest=row.get('path'),row.get('sha256')
            if not isinstance(path,str) or not path.startswith(prefix) or '\\' in path or '..' in Path(path).parts or not isinstance(digest,str) or not re.fullmatch('[0-9a-fA-F]{64}',digest):raise ValueError('Invalid saved-profile owned path/hash: '+str(path))
            declared[path]=digest.lower()
        actual={path for path in required if path.startswith(prefix) and path not in (metadata,metadata+'.meta')}
        if set(declared)!=actual or controller not in declared:raise ValueError('Missing or extra saved-profile clip/controller/provenance member: '+species)
        bodies={path for path in declared if not path.endswith('.meta')}
        if not any(path.endswith('.anim') for path in bodies) or any(path+'.meta' not in declared for path in bodies) or any(path[:-5] not in bodies for path in declared if path.endswith('.meta')):raise ValueError('Saved-profile body/meta membership differs: '+species)
        # Traverse only these declared owned assets through their actual Unity
        # GUID references. Original/vendor dependencies remain under existing
        # native authoring/import proof; no raw-to-native hash waiver is added.
        by_guid={}
        for path in bodies:
            matches=re.findall(r'^guid: ([0-9a-fA-F]{32})\s*$',(root/(path+'.meta')).read_text(encoding='utf-8-sig'),re.M)
            if len(matches)!=1 or matches[0].lower() in by_guid:raise ValueError('Unique saved-profile Unity GUID metadata required: '+path)
            by_guid[matches[0].lower()]=path
        pending=[controller];visited=set()
        while pending:
            path=pending.pop()
            if path in visited:continue
            visited.add(path)
            if not (root/path).is_file():raise ValueError('Missing saved-profile dependency: '+path)
            refs=re.findall(r'guid:\s*([0-9a-fA-F]{32})',(root/path).read_text(encoding='utf-8-sig'))
            pending.extend(by_guid[g.lower()] for g in refs if g.lower() in by_guid and by_guid[g.lower()] not in visited)
        if visited!=bodies:raise ValueError('Owned saved-profile members do not match the actual controller dependency closure: '+species)
        owned_hashes.update(declared)
    return required,owned_hashes


def require_mount_evidence(directory, root=ROOT):
    """A caller cannot turn the mount prerequisite into an unchecked boolean.

    Consume the real native case and its frozen dependency hashes. A later
    scene, mount, rig or motion change requires another actual mounted run.
    Failed unrelated cases in that same batch remain failures, independently.
    """
    directory = Path(directory).resolve()
    evidence_root = (root / 'Logs/TruckSports/DragonKingdom').resolve()
    if not directory.is_relative_to(evidence_root):
        raise ValueError('Mounted prerequisite must use retained local native evidence.')
    cases = [case for case in ET.parse(directory / 'tests.xml').iter('test-case')
             if case.get('fullname') == MOUNT_TEST]
    if len(cases) != 1 or cases[0].get('result') != 'Passed':
        raise ValueError('Actual two-start mounted-body prerequisite has not passed.')
    manifest = json.loads((directory / 'input-manifest.json').read_text(encoding='utf-8'))
    profile_required,profile_hashes=saved_mount_profile_dependencies(root)
    checked = [fact for fact in manifest if mount_dependency(fact['path']) or fact['path'] in profile_required]
    if len({fact['path'] for fact in checked})!=len(checked):raise ValueError('Duplicate mounted dependency provenance.')
    required = {'Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity',
                'Assets/TruckDragonRiders/Scripts/DragonRiderSaddle.cs',
                'Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset',
                'Assets/TruckDragonRiders/Tests/Native/DragonRiderSaddleNativeTests.cs',
                'Assets/TruckDragonRiders/Tests/Native/DragonRiderSaddleFrameObserver.cs',
                'Assets/TruckDragonRiders/Tests/Native/LWS.TruckDragonRiders.NativeTests.asmdef',
                'Assets/TruckSports/Scripts/SportsBroadcastFraming.cs',
                'Assets/TruckSports/Scripts/SportsProbeInputIsolation.cs'}
    required.update(profile_required)
    if not required.issubset({fact['path'] for fact in checked}):
        raise ValueError('Mounted prerequisite lacks original scene, saddle or catalog provenance.')
    frozen={fact['path']:fact['sha256'].lower() for fact in checked}
    for path,digest in profile_hashes.items():
        if frozen.get(path)!=digest:raise ValueError('Saved-profile owned hash mismatches the actual mounted frozen manifest: '+path)
    for fact in checked:
        source = root / fact['path']
        if not source.is_file() or hashlib.sha256(source.read_bytes()).hexdigest() != fact['sha256']:
            raise ValueError('Mounted prerequisite is stale; rerun actual mount: ' + fact['path'])
    return dict(directory=str(directory), test=MOUNT_TEST, checkedDependencies=len(checked),
                resultSha256=hashlib.sha256((directory / 'tests.xml').read_bytes()).hexdigest())

def meta_for(path):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        # A compilable .cs is still ignored by Unity if its importer GUID is
        # malformed. V164's 33-digit test GUID produced a zero-test run.
        # Reject before launch; never silently replace authored metadata.
        guids = [line.partition(':')[2].strip() for line in meta.read_text(encoding='utf-8-sig').splitlines() if line.startswith('guid:')]
        if len(guids) != 1 or len(guids[0]) != 32 or any(c not in '0123456789abcdefABCDEF' for c in guids[0]):
            raise ValueError('Expected one 32-hex Unity GUID in metadata; preserve and repair explicitly: ' + str(meta))
        return meta
    text = 'fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n'
    if path.is_dir(): text += 'folderAsset: yes\nDefaultImporter:'
    elif path.suffix == '.cs': text += 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {fileID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.asmdef': text += 'AssemblyDefinitionImporter:'
    else: raise RuntimeError('New authored binary asset must receive its real Unity importer metadata: ' + str(path))
    if text.endswith(':'): text += '\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    meta.write_text(text, encoding='utf-8'); return meta

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('tag'); parser.add_argument('--method'); parser.add_argument('--tests')
    parser.add_argument('--platform', choices=['EditMode','PlayMode'], default='EditMode')
    parser.add_argument('--timeout',type=int, default=900)
    parser.add_argument('--world-candidate', help='Explicit isolated kingdom appearance/shoulder candidate tag; never replaces the accepted world.')
    parser.add_argument('--finish-art', action='store_true', help='Explicit richer owned venue art on a fresh full isolated world candidate; never decorates a saved canonical world.')
    parser.add_argument('--riders-mount-evidence', help='Retained passing real two-start mount run with exact current mount, scene, rig and motion hashes; required before Riders weather.')
    parser.add_argument('--surface-source', help='Existing isolated candidate used for vendor road/terrain surface comparison; never mutates the source.')
    parser.add_argument('--vista-candidate', help='Fresh ReviewCandidates child for isolated native regional Vista generation.')
    parser.add_argument('--vista-foundation', help='Existing measured ReviewCandidates child consumed by isolated world integration.')
    parser.add_argument('--cavern-candidate', help='Fresh short ReviewCandidates child for measured vendor cavern overhead mass.')
    parser.add_argument('--rounded-candidate', help='Fresh short ReviewCandidates child for native rounded vendor landform review.')
    parser.add_argument('--outer-source', help='Existing physically accepted isolated world consumed read-only by genuine Vista scenery-band generation.')
    parser.add_argument('--outer-candidate', help='Fresh short ReviewCandidates child for genuine outer Vista tiles; never replaces inner terrain.')
    parser.add_argument('--outer-band', help='Existing measured outer Vista scenery prefab attached only by explicit fresh full-world integration.')
    parser.add_argument('--forest-view-evidence', help='Existing retained world-render-review.json for read-only forest LOD projection.')
    parser.add_argument('--include-mode', action='append', default=[], choices=['TruckDragonEscape','TruckHoardHeist','TruckDragonEggDelivery','TruckDragonRiders'])
    parser.add_argument('--exclude-mayhem', action='store_true', help='Explicitly defer the independent, not-yet-intaken variant source tree.')
    parser.add_argument('--exclude-ambience', action='store_true', help='Explicitly defer the independent living-world integration until its source is ready.')
    args = parser.parse_args()
    mount_evidence = require_mount_evidence(args.riders_mount_evidence) if args.riders_mount_evidence else None
    if args.finish_art:
        assert args.world_candidate and args.tests and 'DragonKingdomRoundedWorldIntegrationTests.' in args.tests, 'Finish art requires explicit full world generation, not a saved-scene mutation.'
        for workspace in (ROOT, runner.CLEAN):
            assert not (workspace/'Assets/TruckDragonKingdom/VisualCandidates'/args.world_candidate).exists(), 'Finish art requires a fresh candidate; preserve existing review outputs.'
    assert args.tag and all(c.isalnum() or c in '-_' for c in args.tag)
    if args.world_candidate:
        assert all(c.isalnum() or c in '-_' for c in args.world_candidate), 'Use a simple candidate identifier, not a path.'
    if args.surface_source:
        assert args.surface_source and all(c.isalnum() or c in '-_' for c in args.surface_source), 'Use a simple existing surface-source candidate identifier.'
        assert (ROOT/'Assets/TruckDragonKingdom/VisualCandidates'/args.surface_source/'Shared Dragon Kingdom.prefab').is_file(), 'Surface comparison requires an existing saved candidate prefab.'
    if args.vista_candidate:
        assert all(c.isalnum() or c in '-_' for c in args.vista_candidate), 'Use a simple Vista candidate identifier, not a path.'
    if args.vista_foundation:
        assert args.vista_foundation and all(c.isalnum() or c in '-_' for c in args.vista_foundation), 'Use a simple measured Vista foundation identifier.'
        assert (ROOT/'Assets/TruckDragonKingdom/ReviewCandidates'/args.vista_foundation/'Vista Regional Foundation.prefab').is_file(), 'Integration requires a real saved measured foundation.'
    if args.cavern_candidate:
        assert len(args.cavern_candidate)<=40 and all(c.isalnum() or c in '-_' for c in args.cavern_candidate), 'Use a fresh short cavern candidate identifier.'
    if args.rounded_candidate:
        assert args.rounded_candidate and len(args.rounded_candidate)<=40 and all(c.isalnum() or c in '-_' for c in args.rounded_candidate), 'Use a fresh short rounded-landform candidate identifier.'
    if args.outer_source or args.outer_candidate:
        assert args.outer_source and args.outer_candidate, 'Outer scenery needs both exact accepted source and fresh candidate identifiers.'
        for tag in (args.outer_source,args.outer_candidate):
            assert len(tag)<=40 and all(c.isalnum() or c in '-_' for c in tag), 'Use simple isolated outer terrain identifiers.'
        assert (ROOT/'Assets/TruckDragonKingdom/VisualCandidates'/args.outer_source/'Shared Dragon Kingdom.prefab').is_file(), 'Preserve an actual physically accepted source before outer generation.'
        for workspace in (ROOT,runner.CLEAN):
            assert not (workspace/'Assets/TruckDragonKingdom/ReviewCandidates'/args.outer_candidate).exists(), 'Preserve existing outer candidates; choose a fresh tag.'
    if args.outer_band:
        assert args.world_candidate and args.finish_art, 'Attach measured scenery only through explicit fresh full-world integration.'
        assert len(args.outer_band)<=40 and all(c.isalnum() or c in '-_' for c in args.outer_band), 'Use an existing measured outer band tag.'
        assert (ROOT/'Assets/TruckDragonKingdom/ReviewCandidates'/args.outer_band/'Outer Vista Band.prefab').is_file(), 'Retain the genuine passed outer prefab/dependencies before integration.'
    if args.forest_view_evidence:
        view_evidence=Path(args.forest_view_evidence).resolve()
        assert view_evidence.is_relative_to(ROOT/'Logs/TruckSports/DragonKingdom') and view_evidence.name=='world-render-review.json' and view_evidence.is_file(), 'Use actual retained native world-camera evidence.'
    assert bool(args.method) != bool(args.tests)
    if args.method:
        assert args.method.startswith('LWS.TruckDragonKingdom.') or args.method in {
            'LWS.TruckHoardHeist.Editor.HoardHeistAuthoring.CreateNewScene',
            'LWS.TruckHoardHeist.Editor.HeistSleepingPeekAuthoring.UpgradeSavedHeistPeekBinding',
            'LWS.TruckHoardHeist.Editor.HeistSleepingVigilExitAuthoring.UpgradeSavedHeistVigilExit',
            'LWS.TruckDragonEggDelivery.Editor.DragonEggDeliveryAuthoring.Create',
            'LWS.TruckDragonEscape.Editor.DragonEscapeAuthoring.Create',
            'LWS.TruckDragonRiders.Editor.DragonRidersAuthoring.Create',
            'LWS.TruckSports.Editor.SportsNewEntriesAuthoring.InstallDragonEntries'
        }, 'Only reviewed explicit expansion authoring entry points are allowed.'
    print(require_idle_workspace(ROOT), flush=True)
    out = ROOT / 'Logs/TruckSports/DragonKingdom' / args.tag
    assert not out.exists(), 'Preserve prior attempts; use a new tag'
    out.mkdir()
    paths = []
    for name in ['TruckDragonKingdom'] + args.include_mode:
        base = ROOT / 'Assets' / name
        assert base.is_dir(), str(base)
        folders = [base] + [p for p in base.rglob('*') if p.is_dir()]
        for folder in folders: meta_for(folder)
        for p in list(base.rglob('*')):
            if p.is_file() and p.suffix in ['.cs','.asmdef']: meta_for(p)
        paths += [p for p in base.rglob('*') if p.is_file()] + [Path(str(base) + '.meta')]
    if args.exclude_mayhem:
        deferred=ROOT/'Assets/TruckDragonKingdom/Mayhem'
        staged=runner.CLEAN/'Assets/TruckDragonKingdom/Mayhem'
        assert not staged.exists() or not any(staged.rglob('*.cs')), 'Do not silently mix previously intaken variant source with an exclusion.'
        paths=[p for p in paths if not p.is_relative_to(deferred) and p!=Path(str(deferred)+'.meta')]
    if args.exclude_ambience:
        deferred=ROOT/'Assets/TruckDragonKingdom/Ambience'
        staged=runner.CLEAN/'Assets/TruckDragonKingdom/Ambience'
        assert not staged.exists() or not any(staged.rglob('*.cs')), 'Do not silently exclude previously intaken ambience source.'
        paths=[p for p in paths if not p.is_relative_to(deferred) and p!=Path(str(deferred)+'.meta')]
    # Reviewed additive public bridge reuses existing time/sky/headlight/setup
    # binding for new authored venues; no legacy migration is invoked.
    bridge = ROOT / 'Assets/TruckSports/Editor/SportsEnvironmentAuthoring.cs'
    paths += [bridge, Path(str(bridge)+'.meta')]
    appearance = ROOT / 'Assets/TruckSports/Editor/SportsTruckAppearanceImpossibleAuthoring.cs'
    paths += [appearance, Path(str(appearance)+'.meta')]
    for relative in ['Assets/TruckFareFight/Scripts/FareFightRoadAccess.cs','Assets/TruckFareFight/Tests/EditMode/FareFightRoadAccessRegressionTests.cs']:
        source=ROOT/relative;paths += [source,meta_for(source)]
    # Reviewed collection dispatch is additive. Actual cards remain gated on
    # all four authored scenes, original logos and genuine gameplay captures.
    for relative in [
        'Assets/TruckSports/Scripts/SportsDragonGames.cs',
        'Assets/TruckSports/Scripts/SportsFrontEnd.cs',
        'Assets/TruckSports/Scripts/SportsNavigation.cs',
        'Assets/TruckSports/Scripts/SportsBroadcastFraming.cs',
        'Assets/TruckSports/Scripts/SportsProbeInputIsolation.cs',
        'Assets/TruckSports/Scripts/SportsCompactViewportHud.cs',
        'Assets/TruckSports/Scripts/SportsDrivingViews.cs',
        'Assets/TruckSports/Scripts/SportsChaosWeather.cs',
        'Assets/TruckSports/Scripts/SportsSnowSurface.cs',
        'Assets/LWS/TruckTaxi/Scripts/TruckTaxiSnowSurface.cs',
        'Assets/TruckSports/Scripts/SportsFinishResultsSource.cs',
        'Assets/TruckSports/Editor/SportsResultsPresentationAuthoring.cs',
        'Assets/TruckSports/Editor/SportsBuilder.cs',
        'Assets/TruckSports/Editor/SportsMenuArtAuthoring.cs',
        'Assets/TruckSports/Editor/SportsDragonMenuAuthoring.cs'
    ]:
        source = ROOT / relative
        paths += [source, meta_for(source)]
    if not args.exclude_mayhem:
        source = ROOT / 'Assets/TruckKeepaway/Scripts/KeepawaySheep.cs'
        paths += [source, Path(str(source)+'.meta')]
        # The optional director lives inside these original sport scenes.
        # Freeze the actual ROOT integrations, not an older untracked CLEAN
        # scene left behind by a prior explicit migration.
        for mode in ['TruckKeepaway','TruckBalloonBattle','TruckCornhole','TruckCheeseRolling']:
            scene=ROOT/'Assets'/mode/'Scenes'/(mode+'.unity')
            assert scene.is_file(), 'Actual existing Mayhem sport scene is required: '+str(scene)
            paths += [scene,meta_for(scene)]
    for mode in ['TruckDragonEscape','TruckHoardHeist','TruckDragonEggDelivery','TruckDragonRiders']:
        logo=ROOT/'Assets/TruckSports/Art/Logos'/(mode+'.png')
        if logo.exists(): paths += [logo,Path(str(logo)+'.meta')]
    paths.append(ROOT / 'ProjectSettings/TagManager.asset')
    facts = []
    for source in sorted(paths):
        relative = source.relative_to(ROOT); data = source.read_bytes(); destination = runner.CLEAN / relative
        before = destination.read_bytes() if destination.exists() else None
        if before != data:
            if before is not None:
                backup = out / 'before' / relative; backup.parent.mkdir(parents=True, exist_ok=True); backup.write_bytes(before)
            destination.parent.mkdir(parents=True, exist_ok=True); destination.write_bytes(data)
        assert source.read_bytes() == data, str(relative) + ' changed during intake'
        facts.append(dict(path=relative.as_posix(), sha256=hashlib.sha256(data).hexdigest(), previousSha256=hashlib.sha256(before).hexdigest() if before is not None else None))
    (out / 'input-manifest.json').write_text(json.dumps(facts,indent=2),encoding='utf-8')
    (out / 'scope.json').write_text(json.dumps(dict(includeModes=args.include_mode,mayhemDeferred=args.exclude_mayhem,ambienceDeferred=args.exclude_ambience),indent=2),encoding='utf-8')
    if mount_evidence:
        (out / 'mounted-prerequisite.json').write_text(json.dumps(mount_evidence,indent=2),encoding='utf-8')
    # Retain exact first-party compilation inputs, even as independent ROOT work
    # advances after this snapshot. Licensed payloads remain private dependencies
    # described by hashes; never duplicate their source into a shareable archive.
    with zipfile.ZipFile(out/'owned-source-freeze.zip','x',compression=zipfile.ZIP_DEFLATED) as archive:
        archive.write(out/'input-manifest.json','input-manifest.json')
        for fact in facts:
            relative=fact['path']
            if 'PrivateLicensed' in relative or '/Animations/Authored/' in relative: continue
            if not relative.endswith(('.cs','.cs.meta','.asmdef','.asmdef.meta')): continue
            payload=(runner.CLEAN/relative).read_bytes()
            assert hashlib.sha256(payload).hexdigest()==fact['sha256'], 'Frozen input changed before launch: '+relative
            archive.writestr(relative,payload)
    print('Frozen new-mode inputs', len(facts), 'files',flush=True)
    if args.method:
        command = ['-quit','-executeMethod',args.method,'-tsDragonReceipt',str(out / 'authoring.json')]
    else:
        command = ['-runTests','-testPlatform',args.platform,'-testFilter',args.tests,'-testResults',str(out / 'tests.xml')]
    command += ['-tsDragonOutput', str(out / 'rendered-evidence')]
    if args.world_candidate: command += ['-tsDragonWorldCandidate',args.world_candidate]
    if args.finish_art: command += ['-tsDragonFinishArt']
    if mount_evidence: command += ['-tsDragonRidersMountAccepted']
    if args.surface_source: command += ['-tsDragonSurfaceSource',args.surface_source]
    if args.vista_candidate: command += ['-tsDragonVistaCandidate','Assets/TruckDragonKingdom/ReviewCandidates/'+args.vista_candidate]
    if args.vista_foundation: command += ['-tsDragonVistaFoundation',args.vista_foundation]
    if args.cavern_candidate: command += ['-tsDragonCavernCandidate',args.cavern_candidate]
    if args.rounded_candidate: command += ['-tsDragonRoundedCandidate',args.rounded_candidate]
    if args.outer_source: command += ['-tsDragonOuterSource',args.outer_source,'-tsDragonOuterCandidate',args.outer_candidate]
    if args.outer_band: command += ['-tsDragonOuterBand',args.outer_band]
    if args.forest_view_evidence: command += ['-tsDragonForestViewEvidence',str(view_evidence)]
    runner.run('dragon08-' + args.tag, command, timeout_seconds=args.timeout)

if __name__ == '__main__': main()
