"""Synthetic guard fixtures only; none is native Unity acceptance evidence."""
from pathlib import Path
import copy
import hashlib
import json
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import DragonKingdomReaderContinuation as continuation


class ReaderContinuationTests(unittest.TestCase):
    def setUp(self):
        tmp=tempfile.TemporaryDirectory(prefix='ReaderContinuationFixture-');self.addCleanup(tmp.cleanup)
        self.root=Path(tmp.name);self.clean=self.root/'.sports-validation/TruckSportsPublicValidation'
        self.base=self.root/'Logs/TruckSports/DragonKingdom';self.play=self.root/'Logs/TruckSports/PlaytestRepair'
        self.play.mkdir(parents=True);self.candidate='RoundedIntegrated29';self.processes=[]
        self.runs={tag:self.base/tag for tag in ('Physical','Diagnosis','Reader')}
        for run in self.runs.values():(run/'rendered-evidence').mkdir(parents=True)
        self.source='class Fixture\n{\n    static void WriteWorld(){ ActualWriter(); }\n    public static void ValidateSavedActualAuthoredGroups(object world,object finish)\n    { ActualSavedReader(); }\n}\n'
        self.ridge='class Ridge\r\n{\r\n        static void Write(){ var one=renderer.gameObject.activeInHierarchy; var two=renderer.gameObject.activeInHierarchy; }\r\n}\r\n'
        self.original={continuation.ASSEMBLY:self.source,continuation.RIDGE:self.ridge,'Assets/TruckDragonKingdom/Editor/ActualWorldWriter.cs':'exact actual writer fixture','Assets/TruckDragonKingdom/Tests/World/DragonKingdomWorldAdoptionPlanTests.cs':'fixture original reader',continuation.RENDER_FIXTURE:'fixture original render reader'}
        self.archive('Physical',self.original);self.archive('Diagnosis',self.original);self.archive('Reader',self.original)
        candidate='Assets/TruckDragonKingdom/VisualCandidates/'+self.candidate
        digger='Assets/DiggerData/Scenes/DK-'+self.candidate
        self.put(self.clean,candidate+'/Shared Dragon Kingdom.prefab',b'exact saved candidate')
        self.put(self.clean,candidate+'/Integration Receipt.json',b'fixture physical receipt')
        self.put(self.clean,candidate+'.meta',b'candidate folder guid')
        self.put(self.clean,digger+'/real-tile/Meshes/Chunk.asset',b'actual first observed saved mesh')
        self.put(self.clean,digger+'.meta',b'digger folder guid')
        closure=continuation.snapshot_closure(self.clean,self.candidate)
        before=[r for r in closure if r['path'].startswith(candidate+'/')]
        self.meta=candidate+'/Integration Receipt.json.meta'
        self.put(self.clean,self.meta,b'M'*158)
        self.meta_sha=continuation.digest(self.clean/self.meta)
        after=[r for r in continuation.snapshot_closure(self.clean,self.candidate) if r['path'].startswith(candidate+'/')]
        diagnosis=self.runs['Diagnosis']
        self.write(diagnosis/'candidate-before.json',before);self.write(diagnosis/'candidate-after.json',after)
        self.write(diagnosis/'candidate-existing-byte-continuity.json',dict(existingFilesExact=True,changedOrRemoved=[],added=[r for r in after if r['path']==self.meta]))
        self.write(diagnosis/'digger29-after-reader-before-next-intake.json',dict(scope='FIRST_EXPLICIT_DIGGER29_SHA_BASELINE_AFTER_PHYSICAL_V287_AND_READONLY_V289_NOT_RETROACTIVE_PHYSICAL_END_HASH',physicalRun='Physical',readerDiagnosisRun='Diagnosis',files=[r for r in closure if r['path'].startswith('Assets/DiggerData/')]))
        self.put(self.runs['Physical'],'rendered-evidence/regional-world-'+self.candidate+'.json',b'fixture original physical native receipt')
        self.put(self.runs['Physical'],'rendered-evidence/actual-digger-cavern-'+self.candidate+'.json',b'fixture original actual187 columns')
        self.native('Physical',[(continuation.WORLD,'Passed'),(continuation.PLAN,'Failed')],exit_code=2)
        self.native('Diagnosis',[(continuation.PLAN,'Failed'),(continuation.NEGATIVE,'Passed')],exit_code=2)
        self.native('Reader',[(continuation.PLAN,'Passed')])
        self.write(self.runs['Reader']/('rendered-evidence/identity-adoption-plan-'+self.candidate+'.json'),dict(scope='Synthetic helper binding fixture; full production plan validation is separately mandatory'))
        self.patch=patch.object(continuation,'V289_RECEIPT_META_SHA',self.meta_sha);self.patch.start();self.addCleanup(self.patch.stop)
        self.seal=continuation.make_seal(self.root,self.clean,'Physical','Diagnosis',self.candidate)
        self.seal_path=self.base/'continuation-seal.json';self.write(self.seal_path,self.seal)
        self.snapshots()

    @staticmethod
    def write(path,value):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value),encoding='utf-8')
    @staticmethod
    def put(root,path,data):
        target=root/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
    def archive(self,tag,files):
        run=self.runs[tag];rows=[]
        with zipfile.ZipFile(run/'owned-source-freeze.zip','w') as archive:
            for path,data in sorted(files.items()):
                data=data.encode();archive.writestr(path,data);rows.append(dict(path=path,sha256=hashlib.sha256(data).hexdigest()))
                self.put(self.clean,path,data)
        self.write(run/'input-manifest.json',rows)
    def native(self,tag,cases,exit_code=0,filters=None):
        run=self.runs[tag]
        xml='<test-run>'+''.join('<test-case fullname="'+name+'" result="'+status+'"/>' for name,status in cases)+'</test-run>'
        self.put(run,'tests.xml',xml.encode())
        args=['COMMAND LINE ARGUMENTS:','Unity.exe','-testPlatform','EditMode','-testFilter',';'.join(filters or [name for name,_ in cases]),'-testResults',str((run/'tests.xml').resolve()),'-tsDragonOutput',str((run/'rendered-evidence').resolve()),'-tsDragonWorldCandidate',self.candidate]
        self.put(self.play,'dragon08-'+tag+'.log','\n'.join(args).encode())
        self.processes=[p for p in self.processes if p['job']!='dragon08-'+tag]+[dict(job='dragon08-'+tag,pid=123,exit=exit_code,seconds=12,timedOut=False)]
        self.write(self.play/'camera-validation-processes.json',self.processes)
    def snapshots(self):
        closure=continuation.snapshot_closure(self.clean,self.candidate)
        for name in ('reader-closure-before.json','reader-closure-after.json'):self.write(self.runs['Reader']/name,closure)
    def validate(self):return continuation.validate_continuation(self.root,self.clean,self.runs['Physical'],'Reader',self.candidate,self.seal_path)
    def test_separate_genuine_world_pass_and_later_reader_pass_with_honest_late_digger_baseline(self):
        reader,proof=self.validate();self.assertEqual(self.runs['Reader'],reader)
        self.assertTrue(proof['firstDiggerBaselineIsPostDiagnosis'])
        self.assertNotEqual(self.seal['physicalNative']['xmlSha256'],proof['readerNative']['xmlSha256'])
    def test_failed_skipped_absent_duplicate_or_copied_world_reader_rejects(self):
        for cases in ([(continuation.PLAN,'Failed')],[(continuation.PLAN,'Skipped')],[(continuation.NEGATIVE,'Passed')],[(continuation.PLAN,'Passed')]*2,[(continuation.WORLD,'Passed'),(continuation.PLAN,'Passed')]):
            self.native('Reader',cases)
            with self.subTest(cases=cases),self.assertRaises(AssertionError):self.validate()
    def test_reader_command_candidate_filter_and_xml_path_must_bind_actual_run(self):
        log=self.play/'dragon08-Reader.log';original=log.read_text()
        for text in (original.replace(self.candidate,'OtherCandidate'),original.replace(str((self.runs['Reader']/'tests.xml').resolve()),str((self.runs['Physical']/'tests.xml').resolve())),original.replace(continuation.PLAN,continuation.NEGATIVE)):
            log.write_text(text)
            with self.assertRaises(AssertionError):self.validate()
        log.write_text(original)
    def test_missing_timed_out_failed_or_duplicate_reader_process_rejects(self):
        rows=copy.deepcopy(self.processes)
        for replacement in ([],[dict(rows[-1],timedOut=True)],[dict(rows[-1],exit=2)],[rows[-1],rows[-1]]):
            self.write(self.play/'camera-validation-processes.json',rows[:-1]+replacement)
            with self.assertRaises(AssertionError):self.validate()
    def test_candidate_mesh_receipt_meta_and_digger_byte_drift_reject(self):
        for row in self.seal['files']:
            file=self.clean/row['path'];original=file.read_bytes();file.write_bytes(original+b'drift')
            with self.subTest(path=row['path']),self.assertRaises(AssertionError):self.validate()
            file.write_bytes(original)
    def test_new_removed_duplicate_or_different_candidate_files_reject(self):
        row=self.seal['files'][0];file=self.clean/row['path'];original=file.read_bytes();file.unlink()
        with self.assertRaises(AssertionError):self.validate()
        file.write_bytes(original)
        self.put(self.clean,'Assets/DiggerData/Scenes/DK-'+self.candidate+'/Unknown.asset',b'unknown')
        with self.assertRaises(AssertionError):self.validate()
    def test_reader_before_after_snapshots_required_and_exact(self):
        path=self.runs['Reader']/'reader-closure-before.json';original=continuation.load(path)
        for rows in (original[:-1],original+[original[0]],[]):
            self.write(path,rows)
            with self.assertRaises(AssertionError):self.validate()
        path.unlink()
        with self.assertRaises(FileNotFoundError):self.validate()
    def test_original_physical_result_and_receipts_cannot_be_rewritten(self):
        for relative in ('tests.xml','rendered-evidence/regional-world-'+self.candidate+'.json','rendered-evidence/actual-digger-cavern-'+self.candidate+'.json'):
            path=self.runs['Physical']/relative;original=path.read_bytes();path.write_bytes(original+b'drift')
            with self.subTest(path=relative),self.assertRaises((AssertionError,continuation.ET.ParseError)):self.validate()
            path.write_bytes(original)
    def test_late_digger_seal_cannot_be_changed_or_relabelled_as_generation_evidence(self):
        path=self.runs['Diagnosis']/'digger29-after-reader-before-next-intake.json';value=continuation.load(path)
        value['scope']='RETROACTIVE_PHYSICAL_END_HASH';self.write(path,value)
        with self.assertRaises(AssertionError):self.validate()
    def test_changed_writer_layout_or_unknown_source_cannot_hide_behind_reader_continuation(self):
        for path in (continuation.ASSEMBLY,'Assets/TruckDragonKingdom/Editor/ActualWorldWriter.cs'):
            files=dict(self.original);files[path]=files[path].replace('ActualWriter()','ChangedWriter()')+'\nwriter edit'
            self.archive('Reader',files)
            with self.subTest(path=path),self.assertRaises(AssertionError):self.validate()
    def test_exact_existing_reader_method_body_and_exact_diagnostic_dto_can_change(self):
        files=dict(self.original);files[continuation.ASSEMBLY]=files[continuation.ASSEMBLY].replace('ActualSavedReader();','ReadOnlyDiagnosis("}\\\"{"); /* { } */').replace('    public static',continuation.DIAGNOSTIC_DTO+'    public static')
        self.archive('Reader',files);self.assertTrue(self.validate()[1]['sourceChanges'])
    def test_reader_method_signature_or_new_writer_helper_rejects(self):
        for source in (self.source.replace('object finish','object different'),self.source+'\nstatic void NewWorldWriter(){}'):
            files=dict(self.original);files[continuation.ASSEMBLY]=source;self.archive('Reader',files)
            with self.assertRaises(AssertionError):self.validate()
    def test_frozen_manifest_membership_archive_and_current_source_must_be_exact(self):
        files=dict(self.original);files['Assets/UnknownWriter.cs']='unknown';self.archive('Reader',files)
        with self.assertRaises(AssertionError):self.validate()
        self.archive('Reader',self.original)
        file=self.clean/continuation.ASSEMBLY;file.write_text(self.source+'\npost-native edit')
        with self.assertRaises(AssertionError):self.validate()
    def test_seal_cannot_replace_immutable_diagnosis_candidate_baseline(self):
        seal=copy.deepcopy(self.seal);seal['files'][0]['sha256']='f'*64;self.write(self.seal_path,seal)
        with self.assertRaises(AssertionError):self.validate()
    def activation(self):
        files=dict(self.original);files[continuation.RIDGE]=self.ridge.replace('\r\n','\n').replace('renderer.gameObject.activeInHierarchy','RidgeGeometryObjectActive(renderer.transform)').replace('class Ridge\n{\n','class Ridge\n{\n'+continuation.PERSISTENT_ACTIVATION_HELPER)
        self.archive('Reader',files)
        self.native('Reader',[(continuation.PLAN,'Passed'),(continuation.ACTIVATION,'Passed')])
        rows=[dict(branch=name,savedAuthoredActive=active,liveActive=active,savedPrimary=count,livePrimary=count,savedRoles=['actual-source-mesh-and-collider-role-fixture'],liveRoles=['actual-source-mesh-and-collider-role-fixture']) for name,active,count in [('active',True,2),('disabled ancestor/disabled child',False,0),('disabled renderers',True,0),('explicit primary and secondary LOD',True,1)]]
        proof=dict(scope='ACTUAL_SAVED_CANDIDATE_GRASS_PLUS_OWNED_PREFAB_LIVE_EQUIVALENCE_PRIMARY_LOD_AND_ALL_AUTHORED_ACTIVATION_FLAGS_NO_WORLD_ADOPTION',passed=True,sourceBytesExact=True,sceneContextRestored=True,sourcePart='Artist source 4436662 P_ENV_PLANT_grass_village',actualSavedPrimary=2,originalSceneCount=1,rows=rows)
        path=self.runs['Reader']/'rendered-evidence/saved-live-primary-activation.json';self.write(path,proof)
        return files,path,proof
    def test_shared_activation_fix_requires_exact_transform_and_real_equivalence_case(self):
        self.activation();proof=self.validate()[1]
        self.assertTrue(proof['activationEquivalence']['liveGenerationActivationUnchanged'])
        self.assertTrue(next(r for r in proof['sourceChanges'] if r['path']==continuation.RIDGE)['reviewedCrLfToLfNormalization'])
        self.native('Reader',[(continuation.PLAN,'Passed')])
        with self.assertRaises(AssertionError):self.validate()
    def test_shared_helper_cannot_change_live_rule_ancestor_rule_or_writer_tokens(self):
        files,_,_=self.activation();source=files[continuation.RIDGE]
        for bad in (source.replace('return transform.gameObject.activeInHierarchy','return true'),source.replace('if(!ancestor.gameObject.activeSelf)','if(false)'),source+'\nchanged writer',source.replace('var one=','var changed=')):
            files[continuation.RIDGE]=bad;self.archive('Reader',files)
            with self.assertRaises(AssertionError):self.validate()
    def test_equivalence_receipt_requires_all_actual_activation_source_and_cleanup_proofs(self):
        _,path,proof=self.activation()
        for key,value in [('sourceBytesExact',False),('sceneContextRestored',False),('actualSavedPrimary',0),('sourcePart','different source'),('rows',proof['rows'][:-1])]:
            altered=copy.deepcopy(proof);altered[key]=value;self.write(path,altered)
            with self.subTest(key=key),self.assertRaises(AssertionError):self.validate()
        for index in range(4):
            altered=copy.deepcopy(proof);altered['rows'][index]['liveRoles']=['wrong actual source role'];self.write(path,altered)
            with self.assertRaises(AssertionError):self.validate()
    def test_changed_render_fixture_requires_its_actual_selected_pass_in_same_reader(self):
        files=dict(self.original);files[continuation.RENDER_FIXTURE]+='\nread-only streamed SHA and length pin fixture'
        self.archive('Reader',files)
        with self.assertRaises(AssertionError):self.validate()
        self.native('Reader',[(continuation.PLAN,'Passed'),(continuation.RENDER,'Passed')])
        self.assertEqual(2,len(self.validate()[1]['readerNative']['filters']))
        self.native('Reader',[(continuation.PLAN,'Passed'),(continuation.RENDER,'Failed')],exit_code=2)
        with self.assertRaises(AssertionError):self.validate()
    def test_archive_entry_duplicate_or_wrong_manifest_hash_rejects(self):
        files=dict(self.original);files[continuation.ASSEMBLY]=self.source.replace('ActualSavedReader();','ActualSavedReader(); ReadOnlyDetail();')
        self.archive('Reader',files)
        with self.assertWarns(UserWarning),zipfile.ZipFile(self.runs['Reader']/'owned-source-freeze.zip','a') as archive:archive.writestr(continuation.ASSEMBLY,'wrong duplicate entry')
        with self.assertRaises(AssertionError):self.validate()
    def test_seal_capture_rejects_unknown_addition_changed_existing_bytes_or_false_digger_scope(self):
        path=self.runs['Diagnosis']/'candidate-after.json';original=continuation.load(path)
        changed=copy.deepcopy(original);changed[0]['sha256']='e'*64
        for rows in (changed,original+[dict(path='Assets/TruckDragonKingdom/VisualCandidates/'+self.candidate+'/Unknown.asset',length=1,sha256='e'*64)]):
            self.write(path,rows)
            with self.assertRaises(AssertionError):continuation.make_seal(self.root,self.clean,'Physical','Diagnosis',self.candidate)
        self.write(path,original)
        digger=self.runs['Diagnosis']/'digger29-after-reader-before-next-intake.json';value=continuation.load(digger);value['scope']='RETROACTIVE_GENERATION_BASELINE';self.write(digger,value)
        with self.assertRaises(AssertionError):continuation.make_seal(self.root,self.clean,'Physical','Diagnosis',self.candidate)
    def test_passed_xml_without_actual_saved_plan_receipt_is_insufficient(self):
        (self.runs['Reader']/('rendered-evidence/identity-adoption-plan-'+self.candidate+'.json')).unlink()
        with self.assertRaises(AssertionError):self.validate()


if __name__=='__main__':unittest.main()
