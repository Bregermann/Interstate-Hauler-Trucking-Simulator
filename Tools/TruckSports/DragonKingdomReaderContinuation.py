"""Explicit read-only continuation evidence; never generates or copies Unity assets.

The first Digger hash seal is honestly later than physical generation. It does
not replace the physical native receipt or turn a failed reader into a pass.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import xml.etree.ElementTree as ET
import zipfile

WORLD = 'DragonKingdomRoundedWorldIntegrationTests.MeasuredRoundedVistaAndRealDiggerPreserveAcceptedSourcesAndPassCompleteWorldGates'
PLAN = 'DragonKingdomWorldAdoptionPlanTests.ReadOnlyPlanMapsEveryNonobsoleteOriginalObjectWithoutReplacingCanonicalIdentities'
NEGATIVE = 'DragonKingdomWorldAdoptionPlanTests.ActualSavedOuterPaletteRejectsMissingDuplicateOrDriftedCloneProvenanceWithoutWrites'
ACTIVATION = 'DragonKingdomWorldAdoptionPlanTests.SavedPrefabAndLiveInstancePreserveAuthoredPrimaryMeshActivationWithoutWrites'
RENDER = 'DragonKingdomRenderedWorldReviewTests.AcceptedPreviewRendersNineRealRegionsAtEyeLevelAndOverviewWithoutRegeneration'
SCOPE = 'EXPLICIT_POST_DIAGNOSIS_PRE_READER_SEAL_NOT_RETROACTIVE_PHYSICAL_GENERATION_HASH'
V289_RECEIPT_META_SHA = '48f238f9e40a484290be77531aa3e9f487334bd5075638d92475ee9239010c44'
ASSEMBLY = 'Assets/TruckDragonKingdom/Editor/DragonKingdomRegionalWorldIntegration.AssemblyComposition.cs'
RIDGE = 'Assets/TruckDragonKingdom/Editor/DragonKingdomRegionalWorldIntegration.RidgeComposition.cs'
RENDER_FIXTURE = 'Assets/TruckDragonKingdom/Tests/World/DragonKingdomRenderedWorldReviewTests.cs'
PERSISTENT_ACTIVATION_HELPER = ('        // A persistent prefab asset is not enabled in a loaded scene. Its\n'
    '        // authored activation still includes EVERY serialized ancestor flag.\n'
    '        // Live scene objects retain the original native activeInHierarchy rule.\n'
    '        static bool RidgeGeometryObjectActive(Transform transform)\n'
    '        {\n'
    '            if(transform==null)return false;\n'
    '            if(!EditorUtility.IsPersistent(transform))return transform.gameObject.activeInHierarchy;\n'
    '            for(var ancestor=transform;ancestor!=null;ancestor=ancestor.parent)if(!ancestor.gameObject.activeSelf)return false;\n'
    '            return true;\n'
    '        }\n')
READER_FILES = frozenset(('Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAdoptionPlan.cs',
                         'Assets/TruckDragonKingdom/Tests/World/DragonKingdomWorldAdoptionPlanTests.cs', RENDER_FIXTURE))
DIAGNOSTIC_DTO = ('        [Serializable]sealed class SavedSourcePartMismatch\n'
    '        {public string name,group,source,actualName;public string[] fields;public bool actualActive,expectedActive;public Vector3 actualPosition,expectedPosition,actualScale,expectedScale;public Quaternion actualRotation,expectedRotation;public float rotationAngle;public RidgeGeometryBinding[] actualGeometry,expectedGeometry;}\n')


def digest(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def load(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def identifier(value):
    assert isinstance(value, str) and re.fullmatch('[A-Za-z0-9_-]+', value), 'Explicit safe run/candidate identifier required.'
    return value


def rows_map(rows):
    assert isinstance(rows, list) and rows, 'Exact nonempty file membership required.'
    result = {}
    for row in rows:
        path = row.get('path')
        assert isinstance(path, str) and path.startswith('Assets/') and '\\' not in path and '..' not in Path(path).parts
        assert path not in result, 'Duplicate snapshot path: ' + path
        assert type(row.get('length')) is int and row['length'] >= 0
        assert re.fullmatch('[0-9a-f]{64}', str(row.get('sha256')))
        result[path] = dict(path=path, length=row['length'], sha256=row['sha256'])
    return result


def snapshot_closure(clean, candidate):
    """Candidate and its real Digger tree, including both folder metadata files."""
    identifier(candidate); clean = Path(clean); paths = []
    for relative in ('Assets/TruckDragonKingdom/VisualCandidates/' + candidate,
                     'Assets/DiggerData/Scenes/DK-' + candidate):
        folder = clean / relative
        assert folder.is_dir() and Path(str(folder) + '.meta').is_file(), 'Actual candidate/Digger folder and metadata required.'
        paths.extend(p for p in folder.rglob('*') if p.is_file())
        paths.append(Path(str(folder) + '.meta'))
    return [dict(path=p.relative_to(clean).as_posix(), length=p.stat().st_size, sha256=digest(p)) for p in sorted(paths)]


def native_run(root, tag, candidate, required, *, reader=False):
    """Bind XML to the recorded real Editor command and completed process."""
    root = Path(root); identifier(tag); identifier(candidate)
    run = root / 'Logs/TruckSports/DragonKingdom' / tag
    xml = run / 'tests.xml'; tree = ET.parse(xml)
    cases = list(tree.iter('test-case'))
    for name in required:
        found = [c for c in cases if c.get('fullname') == name]
        assert len(found) == 1 and found[0].get('result') == 'Passed', 'Actual native case must pass exactly once: ' + name
    log = root / 'Logs/TruckSports/PlaytestRepair' / ('dragon08-' + tag + '.log')
    text = log.read_text(encoding='utf-8-sig', errors='replace')
    assert text.count('COMMAND LINE ARGUMENTS:') == 1
    lines = text.split('COMMAND LINE ARGUMENTS:', 1)[1].splitlines()
    def argument(name):
        positions = [i for i, line in enumerate(lines) if line.strip() == name]
        assert len(positions) == 1 and positions[0] + 1 < len(lines), 'Actual unique Editor argument required: ' + name
        return lines[positions[0] + 1].strip()
    normal = lambda value: str(value).replace('\\', '/').lower()
    assert normal(argument('-testResults')) == normal(xml.resolve())
    assert normal(argument('-tsDragonOutput')) == normal((run / 'rendered-evidence').resolve())
    assert argument('-tsDragonWorldCandidate') == candidate and argument('-testPlatform') == 'EditMode'
    filters = argument('-testFilter').split(';')
    assert all(name in filters for name in required) and len(filters) == len(set(filters))
    processes = load(root / 'Logs/TruckSports/PlaytestRepair/camera-validation-processes.json')
    completed = [p for p in processes if p.get('job') == 'dragon08-' + tag]
    assert len(completed) == 1 and completed[0].get('timedOut') is False and completed[0].get('seconds', 0) > 0 and completed[0].get('pid', 0) > 0, 'Actual completed native process required.'
    if reader:
        assert WORLD not in filters and all(c.get('result') == 'Passed' for c in cases), 'Continuation reader must pass; it cannot generate a replacement World pass.'
        assert completed[0].get('exit') == 0
        assert set(filters) == {c.get('fullname') for c in cases}, 'Exact selected reader cases, without copied extra cases, required.'
    return dict(tag=tag, xmlSha256=digest(xml), logSha256=digest(log), process=completed[0], filters=filters)


def manifest(run):
    rows = load(Path(run) / 'input-manifest.json'); result = {}
    for row in rows:
        path = row['path']; assert path not in result
        assert re.fullmatch('[0-9a-f]{64}', row['sha256']); result[path] = row['sha256']
    assert result
    return result


def archived_source(run, path, expected):
    with zipfile.ZipFile(Path(run) / 'owned-source-freeze.zip') as archive:
        assert archive.namelist().count(path) == 1
        data = archive.read(path)
    assert hashlib.sha256(data).hexdigest() == expected, 'Frozen source archive differs from its exact native manifest.'
    return data.decode('utf-8-sig')


def mask_method(text, name):
    """Mask only an existing named C# method body; comments/strings cannot close it."""
    pattern = r'(?m)^\s*(?:public |private |internal )?(?:static )?[^\n;={}]+\b' + re.escape(name) + r'\([^\n]*\)\s*\{'
    matches = list(re.finditer(pattern, text)); assert len(matches) == 1, 'Exactly one existing reader method declaration required: ' + name
    start = matches[0].end() - 1; depth = 0; i = start; state = 'code'
    while i < len(text):
        c = text[i]; nxt = text[i:i+2]
        if state == 'line':
            if c == '\n': state = 'code'
        elif state == 'block':
            if nxt == '*/': state = 'code'; i += 1
        elif state in ('string', 'char'):
            if c == '\\': i += 1
            elif c == ('"' if state == 'string' else "'"): state = 'code'
        elif state == 'verbatim':
            if nxt == '""': i += 1
            elif c == '"': state = 'code'
        elif nxt == '//': state = 'line'; i += 1
        elif nxt == '/*': state = 'block'; i += 1
        elif nxt == '@"': state = 'verbatim'; i += 1
        elif c == '"': state = 'string'
        elif c == "'": state = 'char'
        elif c == '{': depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0: return text[:start] + '{/* exact named reader body */}' + text[i+1:]
        i += 1
    raise AssertionError('Unbalanced C# reader body.')


def writer_remainder(path, text):
    if path == ASSEMBLY:
        assert text.count(DIAGNOSTIC_DTO) <= 1
        text = text.replace(DIAGNOSTIC_DTO, '')
        return mask_method(text, 'ValidateSavedActualAuthoredGroups')
    if path == RIDGE:
        # This exact reviewed edit was saved from CRLF to LF. Only line-ending
        # encoding is normalized here; whitespace and all writer tokens remain
        # protected by the exact reverse transform below.
        text = text.replace('\r\n', '\n')
        if 'RidgeGeometryObjectActive' in text:
            assert text.count(PERSISTENT_ACTIVATION_HELPER) == 1, 'Only the exact persistent-asset activation helper is approved.'
            text = text.replace(PERSISTENT_ACTIVATION_HELPER, '')
            assert text.count('RidgeGeometryObjectActive(renderer.transform)') == 2, 'Only the two actual mesh-sampling activation predicates may change.'
            text = text.replace('RidgeGeometryObjectActive(renderer.transform)', 'renderer.gameObject.activeInHierarchy')
        return text
    raise AssertionError('Unreviewed mixed writer/reader source change: ' + path)


def source_continuity(physical_run, reader_run):
    before, after = manifest(physical_run), manifest(reader_run)
    assert set(before) == set(after), 'Continuation cannot add/remove frozen production dependencies.'
    changed = []
    for path in sorted(before):
        if before[path] == after[path]: continue
        # Full frozen-set comparison deliberately includes world data, importer,
        # physics, layout and shader inputs, not just the C# generation entry.
        if path in READER_FILES:
            archived_source(reader_run, path, after[path])
        else:
            original = archived_source(physical_run, path, before[path])
            current = archived_source(reader_run, path, after[path])
            assert writer_remainder(path, original) == writer_remainder(path, current), 'World writer cohort changed: ' + path
        changed.append(dict(path=path, beforeSha256=before[path], afterSha256=after[path],
                            reviewedCrLfToLfNormalization=path == RIDGE))
    return changed


def require_activation_equivalence(reader):
    path = Path(reader)/'rendered-evidence/saved-live-primary-activation.json'; proof = load(path)
    assert proof.get('scope') == 'ACTUAL_SAVED_CANDIDATE_GRASS_PLUS_OWNED_PREFAB_LIVE_EQUIVALENCE_PRIMARY_LOD_AND_ALL_AUTHORED_ACTIVATION_FLAGS_NO_WORLD_ADOPTION'
    assert all(proof.get(k) is True for k in ('passed','sourceBytesExact','sceneContextRestored'))
    assert proof.get('sourcePart') == 'Artist source 4436662 P_ENV_PLANT_grass_village' and proof.get('actualSavedPrimary') == 2
    assert type(proof.get('originalSceneCount')) is int and proof['originalSceneCount'] > 0
    expected = [('active', True, 2), ('disabled ancestor/disabled child', False, 0), ('disabled renderers', True, 0), ('explicit primary and secondary LOD', True, 1)]
    rows = proof.get('rows'); assert isinstance(rows,list) and len(rows) == len(expected)
    for row, (name, active, primary) in zip(rows, expected):
        assert row.get('branch') == name and row.get('savedAuthoredActive') is active and row.get('liveActive') is active
        assert type(row.get('savedPrimary')) is int and row['savedPrimary'] == row.get('livePrimary') == primary
        assert isinstance(row.get('savedRoles'),list) and row['savedRoles'] and row['savedRoles'] == row.get('liveRoles') and all(isinstance(role,str) and role for role in row['savedRoles'])
    return dict(path=path.name,sha256=digest(path),liveGenerationActivationUnchanged=True)


def make_seal(root, clean, physical_tag, diagnosis_tag, candidate):
    """Parent invokes while idle AFTER diagnosis, BEFORE the final reader intake."""
    root = Path(root); clean = Path(clean); identifier(candidate)
    physical = root / 'Logs/TruckSports/DragonKingdom' / identifier(physical_tag)
    diagnosis = root / 'Logs/TruckSports/DragonKingdom' / identifier(diagnosis_tag)
    world = native_run(root, physical_tag, candidate, [WORLD])
    # The diagnosis is explicitly not acceptance; it may have a failed Plan.
    diagnostic = native_run(root, diagnosis_tag, candidate, [NEGATIVE])
    before = rows_map(load(diagnosis / 'candidate-before.json'))
    after = rows_map(load(diagnosis / 'candidate-after.json'))
    added = set(after) - set(before)
    expected_meta = 'Assets/TruckDragonKingdom/VisualCandidates/' + candidate + '/Integration Receipt.json.meta'
    assert added == {expected_meta} and all(after.get(p) == row for p, row in before.items()), 'Diagnosis may add only the explicitly disclosed native receipt metadata; all existing output bytes must remain exact.'
    assert after[expected_meta]['length'] == 158 and after[expected_meta]['sha256'] == V289_RECEIPT_META_SHA, 'Only the actual recorded V289 metadata addition is approved.'
    disclosure = load(diagnosis / 'candidate-existing-byte-continuity.json')
    assert disclosure.get('existingFilesExact') is True and disclosure.get('changedOrRemoved') == [] and disclosure.get('added') == [after[expected_meta]]
    digger_path = diagnosis / 'digger29-after-reader-before-next-intake.json'; digger = load(digger_path)
    assert digger.get('scope') == 'FIRST_EXPLICIT_DIGGER29_SHA_BASELINE_AFTER_PHYSICAL_V287_AND_READONLY_V289_NOT_RETROACTIVE_PHYSICAL_END_HASH'
    assert digger.get('physicalRun') == physical_tag and digger.get('readerDiagnosisRun') == diagnosis_tag
    closure = snapshot_closure(clean, candidate); observed = rows_map(closure)
    assert all(observed.get(p) == row for p, row in after.items())
    actual_digger = {p: row for p, row in observed.items() if p.startswith('Assets/DiggerData/')}
    assert actual_digger == rows_map(digger['files']), 'First explicit post-diagnosis Digger seal differs.'
    changes = source_continuity(physical, diagnosis)
    return dict(scope=SCOPE, candidate=candidate, physicalRun=physical_tag, diagnosisRun=diagnosis_tag,
                physicalNative=world, diagnosisNative=diagnostic, sourceChanges=changes,
                physicalManifestSha256=digest(physical/'input-manifest.json'),
                diagnosisManifestSha256=digest(diagnosis/'input-manifest.json'),
                physicalReceiptSha256=digest(physical/'rendered-evidence'/('regional-world-'+candidate+'.json')),
                physicalDiggerReceiptSha256=digest(physical/'rendered-evidence'/('actual-digger-cavern-'+candidate+'.json')),
                firstDiggerSealSha256=digest(digger_path), files=closure)


def validate_continuation(root, clean, physical_run, reader_tag, candidate, seal_path):
    root = Path(root); physical_run = Path(physical_run); seal = load(seal_path)
    assert seal.get('scope') == SCOPE and seal.get('candidate') == candidate and seal.get('physicalRun') == physical_run.name
    assert identifier(reader_tag) not in (seal['physicalRun'], seal['diagnosisRun']), 'A failed diagnosis cannot become the accepted reader.'
    diagnosis = root/'Logs/TruckSports/DragonKingdom'/seal['diagnosisRun']
    assert digest(physical_run/'input-manifest.json') == seal['physicalManifestSha256'] and digest(diagnosis/'input-manifest.json') == seal['diagnosisManifestSha256']
    assert native_run(root, physical_run.name, candidate, [WORLD]) == seal['physicalNative']
    assert native_run(root, diagnosis.name, candidate, [NEGATIVE]) == seal['diagnosisNative']
    assert digest(physical_run/'rendered-evidence'/('regional-world-'+candidate+'.json')) == seal['physicalReceiptSha256']
    assert digest(physical_run/'rendered-evidence'/('actual-digger-cavern-'+candidate+'.json')) == seal['physicalDiggerReceiptSha256']
    assert digest(diagnosis/'digger29-after-reader-before-next-intake.json') == seal['firstDiggerSealSha256']
    reader = root/'Logs/TruckSports/DragonKingdom'/reader_tag
    changes = source_continuity(physical_run, reader)
    changed_ridge = any(row['path'] == RIDGE for row in changes)
    changed_render = any(row['path'] == RENDER_FIXTURE for row in changes)
    required = [PLAN]+([ACTIVATION] if changed_ridge else [])+([RENDER] if changed_render else [])
    native = native_run(root, reader_tag, candidate, required, reader=True)
    plan_path = reader/'rendered-evidence'/('identity-adoption-plan-'+candidate+'.json')
    assert plan_path.is_file(), 'Actual saved identity-plan receipt required in the passed reader run.'
    activation = require_activation_equivalence(reader) if changed_ridge else None
    expected = rows_map(seal['files'])
    diagnostic_after = rows_map(load(diagnosis/'candidate-after.json'))
    candidate_prefix = 'Assets/TruckDragonKingdom/VisualCandidates/'+candidate+'/'
    assert {p:r for p,r in expected.items() if p.startswith(candidate_prefix)} == diagnostic_after, 'Seal cannot rewrite the recorded existing candidate baseline.'
    digger = load(diagnosis/'digger29-after-reader-before-next-intake.json')
    assert {p:r for p,r in expected.items() if p.startswith('Assets/DiggerData/')} == rows_map(digger['files']), 'Seal cannot rewrite the honest first Digger baseline.'
    for name in ('reader-closure-before.json', 'reader-closure-after.json'):
        assert rows_map(load(reader/name)) == expected, 'Actual final-reader candidate+Digger before/after must match the explicit seal.'
    assert rows_map(snapshot_closure(clean, candidate)) == expected, 'Current candidate/Digger bytes or membership drifted.'
    # Exact frozen current sources are still required; no post-native edits.
    for path, sha in manifest(reader).items():
        if path.endswith(('.cs', '.cs.meta', '.asmdef', '.asmdef.meta')):
            assert (Path(clean)/path).is_file() and digest(Path(clean)/path) == sha, 'Reader source changed after native validation: '+path
    return reader, dict(scope='ACTUAL_PHYSICAL_WORLD_AND_LATER_READ_ONLY_PLAN_WITH_EXPLICIT_IMMUTABLE_OUTPUT_CONTINUITY_NOT_VISUAL_ACCEPTANCE',
                        sealSha256=digest(seal_path), readerNative=native, sourceChanges=changes,
                        planReceiptSha256=digest(plan_path), activationEquivalence=activation,
                        firstDiggerBaselineIsPostDiagnosis=True, files=len(expected))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate', required=True)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--physical-run')
    parser.add_argument('--diagnosis-run')
    parser.add_argument('--snapshot-only', action='store_true')
    args = parser.parse_args(); root = Path(__file__).resolve().parents[2]
    from WorkspaceProcessSafety import require_idle_workspace
    require_idle_workspace(root)
    clean = root/'.sports-validation/TruckSportsPublicValidation'
    assert not args.output.exists() and args.output.resolve().is_relative_to(root/'Logs/TruckSports/DragonKingdom'), 'New ROOT evidence file only.'
    evidence = snapshot_closure(clean,args.candidate) if args.snapshot_only else make_seal(root,clean,args.physical_run,args.diagnosis_run,args.candidate)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(evidence,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(output=str(args.output),sha256=digest(args.output),scope='READ_ONLY_EVIDENCE_CAPTURE_NO_RETENTION')))


if __name__ == '__main__': main()
