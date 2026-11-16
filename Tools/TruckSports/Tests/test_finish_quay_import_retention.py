"""Synthetic exact-byte guards only; no fixture is native/world acceptance."""
from pathlib import Path
import copy
import hashlib
import json
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain


class QuayNativeImportRetentionTests(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory(prefix='ExactQuayImportFixture-');self.addCleanup(temp.cleanup)
        self.clean=Path(temp.name);self.prior={};self.changes=[];materials={};metadata={}
        self.models=[dict(path=p,signature=s,persistentAssets=a,meshes=1,clips=0,hierarchyTransforms=t)for p,s,a,t in retain.FINISH_QUAY_MODELS]
        for index,path in enumerate(retain.FINISH_QUAY_METADATA):
            raw=('Fixture authored metadata raw '+str(index)).encode();native=raw+b' native bookkeeping'
            asset=path[:-5];self.put(asset,('Fixture unchanged source content '+str(index)).encode());self.prior[asset]=self.sha(asset)
            self.put(path,native);metadata[path]=(self.digest(raw),self.digest(native));self.prior[path]=self.digest(raw)
            self.changes.append(dict(path=path,beforeSha256=self.digest(raw),afterSha256=self.digest(native),rule='EXACT_FIVE_NATIVE_IMPORTER_BOOKKEEPING_RECIPES_ALL_AUTHORED_SETTINGS_AND_FILE_IDS_PRESERVED',authoredDataPreserved=True,nativeVersionLocalId=''))
        self.prefix=b'%YAML 1.1\n';self.body=b'--- !u!21 &2100000\nMaterial:\n  fixtureAuthoredValue: 41\n';self.version=b'--- !u!114 &-987654321\nMonoBehaviour:\n  version: 10\n'
        for index,path in enumerate(retain.FINISH_QUAY_MATERIALS):
            raw=self.prefix+self.body if path==retain.FINISH_QUAY_CRACKS else ('Fixture raw FVP '+str(index)).encode()
            native=raw+self.version if path==retain.FINISH_QUAY_CRACKS else raw+b' exact native defaults'
            self.put(path,native);self.prior[path]=self.digest(raw);self.put(path+'.meta',('fileFormatVersion: 2\nguid: '+f'{index+1:032x}'+'\n').encode());self.prior[path+'.meta']=self.sha(path+'.meta')
            materials[path]=(self.digest(raw),None if path==retain.FINISH_QUAY_CRACKS else self.digest(native))
            self.changes.append(dict(path=path,beforeSha256=self.digest(raw),afterSha256=self.digest(native),rule='OWNED_MATERIAL_BODY_BYTE_EXACT_ONLY_PUBLIC_ASSETVERSION10_NEW_HEADER_ID'if path==retain.FINISH_QUAY_CRACKS else'EXACT_THREE_FVP_NATIVE_BYTES_PROTECTED_AUTHORED_FIELDS_PUBLIC_COLOR_CONVERSIONS_ONLY',authoredDataPreserved=True,nativeVersionLocalId='-987654321'if path==retain.FINISH_QUAY_CRACKS else''))
        self.known=retain.FINISH_PRIVATE+'Fixture existing approved19 material.mat';self.put(self.known,b'Fixture existing native material')
        self.existing=dict(path=self.known,originalSha256=self.digest(b'Fixture existing original material'),importedSha256=self.sha(self.known))
        for path,key in ((retain.FINISH_IMPORT_PROVENANCE,'FINISH_IMPORT_PROVENANCE_SHA'),(retain.FINISH_ASSEMBLY_PROVENANCE,'FINISH_ASSEMBLY_PROVENANCE_SHA')):
            self.put(path,json.dumps(dict(bindings=[self.existing])).encode());self.prior[path]=self.sha(path);self.patch(key,self.sha(path))
        self.patch('FINISH_QUAY_MATERIALS',materials);self.patch('FINISH_QUAY_METADATA',metadata)
        self.patch('FINISH_QUAY_VERSION_SHA',self.digest(self.version.replace(b'&-987654321',b'&0')))
        self.proof=dict(scope=retain.FINISH_QUAY_SCOPE,passed=True,allUnknownChangesRejected=True,protectedModelIdentitiesExact=True,nonMaterialSourceBytesExact=True,watchedPayloads=207,protectedModels=3,sourceFilesWatched=300,protectedOtherSourceFiles=93,changes=[],models=copy.deepcopy(self.models))
    def patch(self,name,value):p=patch.object(retain,name,value);p.start();self.addCleanup(p.stop)
    @staticmethod
    def digest(data):return hashlib.sha256(data).hexdigest()
    def put(self,path,data):p=self.clean/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
    def sha(self,path):return self.digest((self.clean/path).read_bytes())
    def check(self):return retain._finish_quay_imports(self.clean,dict(nativeImportPreflight=self.proof),self.prior)
    def test_all_nine_native_states_accept_even_if_settled_before_wrapper(self):
        before=copy.deepcopy(self.prior);self.assertIs(self.proof,self.check());self.assertEqual(before,self.prior)
    def test_explicit_exact_nine_changes_and_existing_approved_material_accept(self):
        self.proof['changes']=self.changes+[dict(path=self.known,beforeSha256=self.existing['originalSha256'],afterSha256=self.existing['importedSha256'],rule='EXISTING_EXACT_19_OR_51_MATERIAL_PROVENANCE_UNCHANGED',authoredDataPreserved=True)]
        self.check()
    def test_already_native_intake_is_allowed_only_for_exact_nine(self):
        for path in retain.FINISH_QUAY_MATERIALS|retain.FINISH_QUAY_METADATA:self.prior[path]=self.sha(path)
        self.check()
    def test_missing_scope_false_native_flags_and_incomplete_accounting_reject(self):
        original=copy.deepcopy(self.proof)
        for key,value in [('scope','Wrong scope'),('passed',False),('allUnknownChangesRejected',False),('protectedModelIdentitiesExact',False),('nonMaterialSourceBytesExact',False),('protectedModels',2),('watchedPayloads',0),('sourceFilesWatched',299),('protectedOtherSourceFiles',True)]:
            self.proof=copy.deepcopy(original);self.proof[key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
        with self.assertRaises(AssertionError):retain._finish_quay_imports(self.clean,{},self.prior)
    def test_three_complete_actual_native_models_accept_zero_clips(self):
        self.assertEqual([0,0,0],[row['clips']for row in self.check()['models']])
    def test_model_signatures_counts_paths_and_order_are_exact(self):
        original=copy.deepcopy(self.models)
        for key,value in [('path','Assets/Unknown.FBX'),('signature','0'*64),('persistentAssets',171),('meshes',0),('clips',1),('hierarchyTransforms',83)]:
            self.proof['models']=copy.deepcopy(original);self.proof['models'][0][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
        for models in [original[::-1],original[:2],original+[original[0]]]:
            self.proof['models']=models
            with self.assertRaises(AssertionError):self.check()
    def test_each_metadata_pair_rejects_actual_source_content_drift(self):
        for path in retain.FINISH_QUAY_METADATA:
            asset=path[:-5];original=(self.clean/asset).read_bytes();self.put(asset,original+b' changed geometry or shader source')
            with self.subTest(path=path),self.assertRaises(AssertionError):self.check()
            self.put(asset,original)
    def test_each_exact_native_metadata_and_material_byte_hash_is_required(self):
        for path in retain.FINISH_QUAY_METADATA|retain.FINISH_QUAY_MATERIALS:
            original=(self.clean/path).read_bytes();self.put(path,original+b' unknown change')
            with self.subTest(path=path),self.assertRaises(AssertionError):self.check()
            self.put(path,original)
    def test_material_guid_metadata_cannot_change(self):
        for path in retain.FINISH_QUAY_MATERIALS:
            meta=path+'.meta';original=(self.clean/meta).read_bytes();self.put(meta,original+b' unknown metadata')
            with self.subTest(path=path),self.assertRaises(AssertionError):self.check()
            self.put(meta,original)
    def test_unknown_raw_intake_is_not_waived(self):
        for path in retain.FINISH_QUAY_METADATA|retain.FINISH_QUAY_MATERIALS:
            original=self.prior[path];self.prior[path]='0'*64
            with self.subTest(path=path),self.assertRaises(AssertionError):self.check()
            self.prior[path]=original
    def test_only_added_version_header_can_vary_and_no_extra_document(self):
        path=retain.FINISH_QUAY_CRACKS;original=(self.clean/path).read_bytes()
        self.put(path,original.replace(b'&-987654321',b'&987654321'));self.check()
        for data in [original.replace(b'41',b'42'),original.replace(b'version: 10',b'version: 9'),original.replace(b'&-987654321',b'&0'),original.replace(b'&-987654321',b'&2100000'),original+b'--- !u!114 &123\nExtra: 1\n']:
            self.put(path,data)
            with self.subTest(data=data),self.assertRaises(AssertionError):self.check()
    def test_unknown_duplicate_wrong_rule_stale_after_and_unprotected_delta_reject(self):
        valid=copy.deepcopy(self.changes[0])
        for key,value in [('path','Assets/Unknown.mat'),('rule','GUID_ONLY_WAIVER'),('beforeSha256','0'*64),('afterSha256','0'*64),('authoredDataPreserved',False),('nativeVersionLocalId','123')]:
            row=copy.deepcopy(valid);row[key]=value;self.proof['changes']=[row]
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
        self.proof['changes']=[valid,valid]
        with self.assertRaises(AssertionError):self.check()
    def test_added_version_witness_id_must_match_actual_document(self):
        row=copy.deepcopy(self.changes[5]);self.assertEqual(retain.FINISH_QUAY_CRACKS,row['path']);row['nativeVersionLocalId']='987654321';self.proof['changes']=[row]
        with self.assertRaises(AssertionError):self.check()
    def test_old_provenance_tables_are_not_replaced_by_new_rule(self):
        path=retain.FINISH_IMPORT_PROVENANCE;self.put(path,b'Unknown old table')
        with self.assertRaises(AssertionError):self.check()
    def test_exact_source_validator_still_rejects_native_metadata_without_waiver(self):
        path=next(p for p in retain.FINISH_QUAY_METADATA if p.startswith(retain.FINISH_PRIVATE));asset=path[:-5]
        with self.assertRaises(AssertionError):retain._finish_source(self.clean,self.prior,asset,self.prior[asset],self.prior[path],'1'*32)


if __name__=='__main__':unittest.main()
