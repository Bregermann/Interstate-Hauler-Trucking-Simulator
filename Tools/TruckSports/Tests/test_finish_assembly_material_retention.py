"""Synthetic byte-accounting fixtures; these never claim native/world acceptance."""
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

META_REASON='EXACT_PUBLIC_URP_NATIVE_SERIALIZATION_QUEUE_KEYWORD_PASS_VERSION_MIGRATION'
ADD_REASON='EXACT_NATIVE_PUBLIC_URP_DEFAULT_FIELD_ADDITION'


class Exact51AssemblyMaterialRetentionTests(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory(prefix='Exact51MaterialFixture-');self.addCleanup(temp.cleanup)
        self.clean=Path(temp.name);self.prior={};self.bindings=[];self.dependencies=[];self.layouts=[]
        self.put('ProjectSettings/ProjectVersion.txt',b'm_EditorVersion: 6000.4.10f1\n')
        package='Packages/com.unity.render-pipelines.universal/'
        self.upgrader=package+'Editor/AssetPostProcessors/MaterialPostprocessor.cs';self.put(self.upgrader,b'Fixture public native upgrader')
        self.put(package+'package.json',json.dumps(dict(name='com.unity.render-pipelines.universal',version='17.4.0')).encode())
        shaders=[]
        for index,name in enumerate(('Shaders/Lit.shader','Shaders/Particles/ParticlesUnlit.shader')):
            path=package+name;self.put(path,b'[MainColor] _BaseColor ("Fixture", Color) = (1,1,1,1)');self.put(path+'.meta',self.meta(9000+index))
            shaders.append(dict(package='com.unity.render-pipelines.universal',relativePath=name,sha256=self.sha(path),guid=f'{9000+index:032x}',mainColor='_BaseColor'))
        self.shaders=shaders
        names=['M_CLR_blue.mat','M_CLR_yellow_E.mat','M_FX_steam.mat','M_PROP_paper.mat']
        for i in range(51):
            path=retain.FINISH_PRIVATE+('Plugins/Tidal Flask Studios/Fantastic Village Pack/materials/'+names[i] if i<4 else 'FixtureAssemblies/Material_'+str(i)+'.mat')
            raw,native,protected,converted,added,changes=self.material(i)
            self.frozen(path,raw,1000+i);guid=f'{1000+i:032x}'
            dep=dict(path=path,guid=guid,sha256=self.sha(path),metaSha256=self.sha(path+'.meta'));self.dependencies.append(dep)
            snapshot=retain.FINISH_PRIVATE+'DerivedFinishAssemblies/OriginalMaterialSources/'+guid+'.bytes';self.frozen(snapshot,raw,2000+i)
            self.put(path,native)
            self.bindings.append(dict(path=path,guid=guid,originalSha256=dep['sha256'],importedSha256=self.sha(path),metaSha256=dep['metaSha256'],snapshot=snapshot,snapshotSha256=self.sha(snapshot),snapshotMetaSha256=self.sha(snapshot+'.meta'),snapshotGuid=f'{2000+i:032x}',originalRenderQueue=2000,importedRenderQueue=2001 if i<30 else 2000,protectedProperties=protected,convertedProperties=converted,addedProperties=added,metadataChanges=changes))
        for i in range(267):
            path=retain.FINISH_PRIVATE+'FixtureAssemblies/Original_'+str(i)+('.prefab' if i<58 else '.asset');self.frozen(path,('Fixture original dependency '+str(i)).encode(),4000+i)
            self.dependencies.append(dict(path=path,guid=f'{4000+i:032x}',sha256=self.sha(path),metaSha256=self.sha(path+'.meta')))
        layout_constants=[]
        for i,(path,unused,count,prefabs) in enumerate(retain.FINISH_ASSEMBLY_LAYOUTS):
            rows=[dict(sceneInstanceId=str(10000+j),prefab=self.dependencies[51+j%prefabs]['path']) for j in range(count)]
            originals=[dict(path=b['snapshot'],source=b['path'],sourceGuid=b['guid'],sha256=b['originalSha256'],metaSha256=b['snapshotMetaSha256'],guid=b['snapshotGuid']) for b in self.bindings]
            layout=dict(schemaVersion=1,sourceSceneSha256=str(i+1)*64,instances=rows,dependencies=self.dependencies,originalMaterials=originals)
            self.frozen(path,json.dumps(layout).encode(),6000+i)
            self.layouts.append(dict(path=path,sha256=self.sha(path),metaSha256=self.sha(path+'.meta'),guid=f'{6000+i:032x}',instances=count,sourceSceneSha256=layout['sourceSceneSha256']))
            layout_constants.append((path,self.sha(path),count,prefabs))
        self.proof=dict(schemaVersion=1,scope='EXACT_V228_51_MATERIAL_NATIVE_STAGE_PROVENANCE_NOT_GENERIC_WAIVER_OR_HIERARCHY_VISUAL_ACCEPTANCE',editorVersion='6000.4.10f1',urpVersion='17.4.0',upgraderPackage='com.unity.render-pipelines.universal',upgraderRelativePath='Editor/AssetPostProcessors/MaterialPostprocessor.cs',upgraderSourceSha256=self.sha(self.upgrader),nativeUpgraderFromVersion=1,nativeUpgraderToVersion=10,sourceNativeRun='V228',sourceNativeArchiveSha256=retain.FINISH_ASSEMBLY_ARCHIVE_SHA,sourceNativeCaseStatus='REJECTED_ASSEMBLY_PREFLIGHT',materialStageOnly=True,publicShaderSources=shaders,sourceLayouts=self.layouts,bindings=self.bindings)
        self.frozen(retain.FINISH_ASSEMBLY_PROVENANCE,json.dumps(self.proof).encode(),7000)
        self.evidence=dict(provenance=retain.FINISH_ASSEMBLY_PROVENANCE,provenanceSha256=self.sha(retain.FINISH_ASSEMBLY_PROVENANCE),provenanceGuid=f'{7000:032x}',provenanceMetaSha256=self.sha(retain.FINISH_ASSEMBLY_PROVENANCE+'.meta'),sourceNativeRun='V228',sourceNativeArchiveSha256=retain.FINISH_ASSEMBLY_ARCHIVE_SHA,rawMaterialSnapshots=51,approvedNativeImports=51,protectedProperties=2544,convertedProperties=6,addedProperties=347,metadataChanges=358,exactNonmaterialDependencies=267,exactMetadataAndGuids=318,passed=True,bindings=copy.deepcopy(self.bindings))
        self.addCleanup(patch.stopall)
        patch.object(retain,'FINISH_ASSEMBLY_PROVENANCE_SHA',self.sha(retain.FINISH_ASSEMBLY_PROVENANCE)).start()
        patch.object(retain,'FINISH_ASSEMBLY_LAYOUTS',tuple(layout_constants)).start()

    @staticmethod
    def meta(number):return ('fileFormatVersion: 2\nguid: '+f'{number:032x}'+'\n').encode()
    def put(self,path,data):p=self.clean/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
    def sha(self,path):return hashlib.sha256((self.clean/path).read_bytes()).hexdigest()
    def frozen(self,path,data,guid):
        self.put(path,data);self.put(path+'.meta',self.meta(guid))
        for name in (path,path+'.meta'):self.prior[name]=self.sha(name)
    def select(self):return retain._finish_assembly_material_imports(self.clean,self.prior,self.evidence)
    def material(self,index):
        base='{r: 1, g: 0.8, b: 0.2, a: 1}';alias=index<4;emission=index<2
        converted=[]
        if alias:converted.append(dict(kind='Color',name='_Color',sourceValue='{r: 0.5, g: 0.5, b: 0.5, a: 1}',importedValue=base,reason='PUBLIC_URP_MAINCOLOR_BASECOLOR_TO_LEGACY_COLOR_ALIAS_NO_AUTHORED_BASECOLOR_CHANGE'))
        if emission:converted.append(dict(kind='Color',name='_EmissionColor',sourceValue='{r: 2, g: 1, b: 0, a: 1}',importedValue='{r: 4.594794, g: 1, b: 0, a: 1}',reason='PUBLIC_URP_NATIVE_V1_TO_V10_HDR_EMISSION_COLOR_LINEAR_COMPATIBILITY'))
        protected=[dict(kind='Shader',name='m_Shader',sourceValue='{fileID: 4800000, guid: '+f'{9000:032x}'+', type: 3}'),dict(kind='Texture',name='_Albedo',sourceValue='m_Texture: {fileID: 2800000, guid: '+f'{8000:032x}'+', type: 3}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}'),dict(kind='Int',name='_Integer',sourceValue='1'),dict(kind='Color',name='_BaseColor',sourceValue=base)]
        protected += [dict(kind='Float',name='_Float'+str(i),sourceValue=str(i)) for i in range(46-len(converted))]
        added=[dict(kind='Float',name='_Native'+str(j),sourceValue='<absent>',importedValue=str(j),reason=ADD_REASON) for j in range(12 if index<17 else 11 if index<30 else 0)]
        names=['serializedVersion','m_ShaderKeywords','m_Parent','m_ModifiedSerializedProperties','m_ValidKeywords','m_InvalidKeywords','m_CustomRenderQueue','disabledShaderPasses','m_LockedProperties','m_BuildTextureStacks','m_AllowLocking','URP.AssetVersion.version']
        changes=[dict(name=name,sourceValue='6' if name=='serializedVersion' else '2000' if name=='m_CustomRenderQueue' else '0',importedValue='8' if name=='serializedVersion' else '2001' if name=='m_CustomRenderQueue' else '1',reason=META_REASON) for name in names[:12 if index<28 else 11 if index<30 else 0]]
        def text(native):
            metadata={name:'6' if name=='serializedVersion' else '2000' if name=='m_CustomRenderQueue' else '0' for name in names}
            if native:
                for change in changes:metadata[change['name']]=change['importedValue']
            lines=['%YAML 1.1','--- !u!21 &2100000','Material:']
            for name in names:
                if name!='URP.AssetVersion.version':lines.append('  '+name+': '+metadata[name])
            lines+=['  m_Shader: '+protected[0]['sourceValue'],'  m_SavedProperties:','    serializedVersion: 3']
            rows=protected[1:]+[dict(c,sourceValue=c['importedValue']if native else c['sourceValue'])for c in converted]
            if native:rows+=[dict(c,sourceValue=c['importedValue'])for c in added]
            for section,kind in [('m_TexEnvs','Texture'),('m_Ints','Int'),('m_Floats','Float'),('m_Colors','Color')]:
                lines.append('    '+section+':')
                for prop in rows:
                    if prop['kind']!=kind:continue
                    lines.append('    - '+prop['name']+(':' if kind=='Texture' else ': '+prop['sourceValue']))
                    if kind=='Texture':lines+=['        '+prop['sourceValue'].splitlines()[0]]+prop['sourceValue'].splitlines()[1:]
            lines+=['--- !u!114 &11400000','MonoBehaviour:','  version: '+metadata['URP.AssetVersion.version']]
            return ('\n'.join(lines)+'\n').encode()
        return text(False),text(True),protected,converted,added,changes

    def test_exact51_stage_accepts_all_frozen_sources_without_selecting_output_assets(self):
        self.assertEqual(51,len(self.select()))
    def test_missing_failed_or_incorrect_native_stage_accounting_rejects(self):
        for key,value in [('passed',False),('rawMaterialSnapshots',50),('approvedNativeImports',50),('convertedProperties',7),('addedProperties',346),('metadataChanges',357),('exactNonmaterialDependencies',266),('exactMetadataAndGuids',317),('sourceNativeRun','V226')]:
            original=self.evidence[key];self.evidence[key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.select()
            self.evidence[key]=original
    def test_reordered_duplicate_extra_or_missing_bindings_reject(self):
        old=copy.deepcopy(self.evidence['bindings'])
        for rows in ([old[1],old[0]]+old[2:],old[:-1]+[old[0]],old+[old[0]],old[:-1]):
            self.evidence['bindings']=rows
            with self.subTest(count=len(rows)),self.assertRaises(AssertionError):self.select()
        self.evidence['bindings']=old
    def test_native_bytes_raw_snapshots_and_all_metadata_remain_exact(self):
        first=self.bindings[0]
        for path in (first['path'],first['path']+'.meta',first['snapshot'],first['snapshot']+'.meta',retain.FINISH_ASSEMBLY_PROVENANCE,self.upgrader,self.layouts[0]['path'],self.layouts[0]['path']+'.meta',self.dependencies[-1]['path']):
            file=self.clean/path;old=file.read_bytes();file.write_bytes(old+b'drift')
            with self.subTest(path=path),self.assertRaises(AssertionError):self.select()
            file.write_bytes(old)
    def test_unreviewed_protected_texture_uv_default_or_metadata_recipe_rejects(self):
        original=copy.deepcopy(self.evidence['bindings'])
        for group,field in [('protectedProperties','sourceValue'),('convertedProperties','importedValue'),('addedProperties','reason'),('metadataChanges','importedValue')]:
            self.evidence['bindings']=copy.deepcopy(original);self.evidence['bindings'][0][group][0][field]='unreviewed'
            with self.subTest(group=group),self.assertRaises(AssertionError):self.select()
        self.evidence['bindings']=original
    def test_proof_cannot_relabel_failed_original_hierarchy_case_as_world_acceptance(self):
        path=self.clean/retain.FINISH_ASSEMBLY_PROVENANCE;original=path.read_bytes()
        for key,value in [('materialStageOnly',False),('sourceNativeCaseStatus','Passed')]:
            proof=copy.deepcopy(self.proof);proof[key]=value;path.write_text(json.dumps(proof));digest=self.sha(retain.FINISH_ASSEMBLY_PROVENANCE);self.prior[retain.FINISH_ASSEMBLY_PROVENANCE]=digest;self.evidence['provenanceSha256']=digest
            with patch.object(retain,'FINISH_ASSEMBLY_PROVENANCE_SHA',digest),self.subTest(key=key),self.assertRaises(AssertionError):self.select()
        path.write_bytes(original)
    def test_other_material_with_same_filename_cannot_receive_reviewed_conversion(self):
        self.evidence['bindings'][0]['path']=retain.FINISH_PRIVATE+'Unreviewed/M_CLR_blue.mat'
        with self.assertRaises(AssertionError):self.select()
    def forged_table(self,proof):
        # Defeat the outer table-hash guard deliberately to exercise inner
        # semantic validation, without weakening production constants.
        self.put(retain.FINISH_ASSEMBLY_PROVENANCE,json.dumps(proof).encode());digest=self.sha(retain.FINISH_ASSEMBLY_PROVENANCE)
        self.prior[retain.FINISH_ASSEMBLY_PROVENANCE]=digest;self.evidence['provenanceSha256']=digest;self.evidence['bindings']=copy.deepcopy(proof['bindings'])
        return patch.object(retain,'FINISH_ASSEMBLY_PROVENANCE_SHA',digest)
    def test_duplicate_protected_property_cannot_hide_an_unaccounted_authored_property(self):
        proof=copy.deepcopy(self.proof);props=proof['bindings'][0]['protectedProperties'];props[1]=copy.deepcopy(props[0])
        with self.forged_table(proof),self.assertRaisesRegex(AssertionError,'accounted exactly once'):self.select()
    def test_matching_changed_native_hash_cannot_authorize_wrong_alias_color(self):
        proof=copy.deepcopy(self.proof);binding=proof['bindings'][0];item=binding['convertedProperties'][0]
        path=self.clean/binding['path'];text=path.read_text();wrong='{r: 0.1, g: 0.2, b: 0.3, a: 1}';text=text.replace('    - _Color: '+item['importedValue'],'    - _Color: '+wrong)
        path.write_text(text);item['importedValue']=wrong;binding['importedSha256']=self.sha(binding['path'])
        with self.forged_table(proof),self.assertRaisesRegex(AssertionError,'alias/Color.linear'):self.select()
    def test_matching_changed_native_hash_cannot_hide_unlisted_metadata(self):
        proof=copy.deepcopy(self.proof);binding=proof['bindings'][0];path=self.clean/binding['path'];path.write_text(path.read_text().replace('  m_Shader:','  m_Unreviewed: 1\n  m_Shader:'));binding['importedSha256']=self.sha(binding['path'])
        with self.forged_table(proof),self.assertRaisesRegex(AssertionError,'metadata migration must be accounted'):self.select()
    def test_actual_public_shader_source_and_guid_are_both_required(self):
        path='Packages/com.unity.render-pipelines.universal/'+self.shaders[0]['relativePath']
        for changed in (path,path+'.meta'):
            file=self.clean/changed;old=file.read_bytes();file.write_bytes(b'unreviewed shader or GUID')
            with self.subTest(path=changed),self.assertRaises(AssertionError):self.select()
            file.write_bytes(old)


if __name__=='__main__':unittest.main()
