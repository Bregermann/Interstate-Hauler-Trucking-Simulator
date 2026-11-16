"""Synthetic schema3 receipt guards; no fixture is native acceptance evidence."""
from pathlib import Path
import copy
import hashlib
import json
import sys
import tempfile
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain


class _Schema3Fixture:
    def setUp(self):
        temp=tempfile.TemporaryDirectory(prefix='Schema3NativePlanFixture-');self.addCleanup(temp.cleanup)
        self.clean=Path(temp.name);self.run=self.clean/'Run';(self.run/'rendered-evidence').mkdir(parents=True);self.prior={};self.candidate='RoundedIntegrated24'
        self.canonical='Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab';self.prefab='Assets/TruckDragonKingdom/VisualCandidates/'+self.candidate+'/Shared Dragon Kingdom.prefab'
        self.frozen(self.canonical,b'Fixture unchanged original saved prefab',100);self.frozen(self.prefab,b'Fixture separately saved candidate prefab',101)
        self.integration=dict(prefab=self.prefab,bearings=dict(patches=1))
        paths=list(retain.FINISH_SCHEMA3_PLAN_REQUIRED_PATHS)+['Fixture root[0]/Fixture ordinary native-core root '+str(i)+'[0]'for i in range(341)]
        self.plan=dict(scope='READ_ONLY_ORIGINAL_OBJECT_ID_MAPPING_NOT_ADOPTION_OR_VISUAL_APPROVAL',tag=self.candidate,status='MAPPED_ORIGINAL_IDENTITIES_REQUIRES_VISUAL_COURSE_APPROVAL_AND_EXPLICIT_APPLICATION',canonicalPrefabHash=self.sha(self.canonical),candidatePrefabHash=self.sha(self.prefab),canonicalGuid=f'{100:032x}',candidateGuid=f'{101:032x}',originalObjects=600,mappedOriginalObjects=595,allRetainedOriginalObjectsMapped=True,ownedFinishRoots=343,ownedArtistMarketRoots=1,ownedCoastalRoots=1,retainedOriginalIds=['Fixture original '+str(i)for i in range(595)],obsolete=[dict(path='Fixture obsolete '+str(i))for i in range(5)],additions=[dict(path=path)for path in paths])
        self.result('Passed');self.write_plan()
    @staticmethod
    def meta(number):return ('fileFormatVersion: 2\nguid: '+f'{number:032x}'+'\n').encode()
    def put(self,path,data):p=self.clean/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
    def sha(self,path):return hashlib.sha256((self.clean/path).read_bytes()).hexdigest()
    def frozen(self,path,data,guid):
        self.put(path,data);self.put(path+'.meta',self.meta(guid))
        for name in (path,path+'.meta'):self.prior[name]=self.sha(name)
    def result(self,status,duplicate=False):
        case='<test-case fullname="'+retain.FINISH_SCHEMA3_PLAN_TEST+'" result="'+status+'"/>'
        (self.run/'tests.xml').write_text('<test-run>'+case*(2 if duplicate else 1)+'</test-run>')
    def write_plan(self):(self.run/'rendered-evidence'/('identity-adoption-plan-'+self.candidate+'.json')).write_text(json.dumps(self.plan))
    def select(self):return retain.require_schema3_saved_plan(self.clean,self.run,self.candidate,self.integration,self.prior)


class Schema3SavedPlanRetentionTests(_Schema3Fixture,unittest.TestCase):
    def test_exact_saved_plan_accepts_343_plus_artist_and_all_eight_known_new_roots(self):
        self.assertEqual(343,self.select()['ownedFinishRoots'])
    def test_actual_v293_indexed_native_required_paths_are_accepted(self):
        # Copied from the actual passed V293 native Plan; this small fixture is
        # a parser regression, not a substitute for that native acceptance.
        actual=('Emberwick Village[0]/Owned original artist market frontage[0]',
                'Emberwick Village[0]/Owned authored village north garden[0]',
                'Emberwick Village[0]/Owned authored village south garden[0]',
                'Emberwick Village[0]/Owned village stone retaining bank[0]',
                'Lanternwater Harbor[0]/Owned supported harbor merchant quayside[0]',
                'Lanternwater Harbor[0]/Owned supported moored trader cargo[0]',
                'Lanternwater Harbor[0]/Owned authored harbor garden[0]',
                'Whisperwood[0]/Owned authored forest rock grove[0]',
                'Skycradle Cliffs[0]/Owned authored nest rock grove[0]')
        self.plan['additions'][:9]=[dict(path=path)for path in actual];self.write_plan();self.select()
    def test_missing_unexpected_or_noncanonical_sibling_indices_reject(self):
        original=self.plan['additions'][0]['path']
        for bad in (original[:-3],original[:-3]+'[1]',original[:-3]+'[00]',original[:-3]+'[-1]',original.replace('Village[0]','Village[1]'),original.replace('Emberwick Village','Whisperwood'),original+'/',original.replace('/','\\')):
            self.plan['additions'][0]['path']=bad;self.write_plan()
            with self.subTest(path=bad),self.assertRaises(AssertionError):self.select()
    def test_same_known_leaf_with_other_parent_or_sibling_cannot_hide_as_extra_root(self):
        for bad in ('Whisperwood[0]/Owned authored village north garden[0]',retain.FINISH_SCHEMA3_PLAN_REQUIRED_PATHS[0][:-3]+'[1]',retain.FINISH_SCHEMA3_PLAN_REQUIRED_PATHS[0]):
            self.plan['additions'][-1]['path']=bad;self.write_plan()
            with self.subTest(path=bad),self.assertRaises(AssertionError):self.select()
    def test_failed_skipped_missing_duplicate_and_unrelated_native_cases_reject(self):
        for state in ('Failed','Skipped','Inconclusive'):
            self.result(state)
            with self.subTest(state=state),self.assertRaises(AssertionError):self.select()
        self.result('Passed',True)
        with self.assertRaises(AssertionError):self.select()
        (self.run/'tests.xml').write_text('<test-run><test-case fullname="Unrelated.Test" result="Passed"/></test-run>')
        with self.assertRaises(AssertionError):self.select()
    def test_actual_saved_candidate_canonical_and_guid_changes_invalidate_plan(self):
        for path in (self.prefab,self.canonical,self.prefab+'.meta',self.canonical+'.meta'):
            file=self.clean/path;original=file.read_bytes();file.write_bytes(self.meta(9999)if path.endswith('.meta')else original+b'drift')
            with self.subTest(path=path),self.assertRaises(AssertionError):self.select()
            file.write_bytes(original)
    def test_root_count_mapping_and_duplicate_original_identity_drift_reject(self):
        original=copy.deepcopy(self.plan)
        for key,value in [('ownedFinishRoots',335),('ownedArtistMarketRoots',0),('ownedCoastalRoots',0),('allRetainedOriginalObjectsMapped',False),('mappedOriginalObjects',594),('retainedOriginalIds',self.plan['retainedOriginalIds'][:-1]+[self.plan['retainedOriginalIds'][0]])]:
            self.plan=copy.deepcopy(original);self.plan[key]=value;self.write_plan()
            with self.subTest(key=key),self.assertRaises(AssertionError):self.select()
    def test_unknown_extra_root_and_missing_known_source_group_reject(self):
        self.plan['additions'].append(dict(path='Fixture root/Unknown extra'));self.write_plan()
        with self.assertRaises(AssertionError):self.select()
        self.plan['additions'].pop();self.plan['additions'][0]['path']='Fixture root/Unreviewed replacement';self.write_plan()
        with self.assertRaises(AssertionError):self.select()
    def test_only_two_exact_native_sha_encodings_are_allowed(self):
        self.plan['candidatePrefabHash']='-'.join(self.sha(self.prefab)[i:i+2].upper()for i in range(0,64,2));self.write_plan();self.select()
        self.plan['candidatePrefabHash']='--'.join(self.sha(self.prefab)[i:i+2]for i in range(0,64,2));self.write_plan()
        with self.assertRaises(AssertionError):self.select()


class Schema3ActualPaletteRetentionTests(_Schema3Fixture,unittest.TestCase):
    def setUp(self):
        super().setUp();self.folder='Assets/TruckDragonKingdom/VisualCandidates/'+self.candidate+'/Terrain/Palette/'
        source=retain.FINISH_PRIVATE+'Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Terrain/'
        self.layers=[];self.deps=[];self.tiles=[]
        properties=['  m_DiffuseTexture: {fileID: 0}','  m_NormalMapTexture: {fileID: 0}','  m_MaskMapTexture: {fileID: 0}','  m_TileSize: {x: 11, y: 11}','  m_TileOffset: {x: 0, y: 0}','  m_Specular: {r: 0, g: 0, b: 0, a: 0}','  m_Metallic: 0','  m_Smoothness: 0','  m_NormalScale: 1','  m_DiffuseRemapMin: {x: 0, y: 0, z: 0, w: 0}','  m_DiffuseRemapMax: {x: 1, y: 1, z: 1, w: 1}','  m_MaskMapRemapMin: {x: 0, y: 0, z: 0, w: 0}','  m_MaskMapRemapMax: {x: 1, y: 1, z: 1, w: 1}']
        for i,name in enumerate(('GrassGreen','GrassYellow','Dirt','Rock')):
            path=source+name+'.terrainlayer';target=self.folder+name+'.terrainlayer';data=('%YAML 1.1\nTerrainLayer:\n'+'\n'.join(properties)+'\n').encode();self.frozen(path,data,200+i);self.frozen(target,data,300+i)
            self.layers.append(dict(source=path,sourceGuid=f'{200+i:032x}',sourceSha256=self.sha(path),sourceMetaSha256=self.sha(path+'.meta'),candidate=target,candidateGuid=f'{300+i:032x}',candidateSha256=self.sha(target),candidateMetaSha256=self.sha(target+'.meta'),properties='Actual native properties compared separately in C#'))
            self.deps.append(dict(path=path,guid=f'{200+i:032x}',sha256=self.sha(path),metaSha256=self.sha(path+'.meta')))
        refs=[]
        for i in range(16):
            target=self.folder+'Outer '+str(i)+'.asset'if i>=4 else self.folder.replace('/Palette/','/')+'Core '+str(i)+'.asset'
            self.frozen(target,('Fixture terrain physical fields '+str(i)).encode(),400+i);outer=i>=4
            original='Assets/TruckDragonKingdom/ReviewCandidates/FixtureOuter/Terrain/Outer '+str(i)+'.asset'if outer else target
            if outer:self.frozen(original,('Fixture unchanged source data '+str(i)).encode(),500+i)
            self.tiles.append(dict(name='Fixture actual terrain '+str(i),sourceData=original,sourceDataGuid=f'{500+i if outer else 400+i:032x}',sourceDataSha256=self.sha(original),sourceDataMetaSha256=self.sha(original+'.meta'),candidate=target,candidateGuid=f'{400+i:032x}',candidateSha256=self.sha(target),candidateMetaSha256=self.sha(target+'.meta'),heightsBefore='a'*64,heightsAfter='a'*64,alphaBefore='b'*64,alphaAfter='b'*64,holesBefore='c'*64,holesAfter='c'*64,finalHoles='c'*64,externalDataCloned=outer,appearancePreservedGeometryAndAlpha=True,position=dict(x=i*100,y=0,z=0),size=dict(x=800,y=600,z=800),heightResolution=513,alphaWidth=512,alphaHeight=512,holesResolution=512))
            refs+=['  m_TerrainData: {fileID: 15600000, guid: '+f'{400+i:032x}'+', type: 2}']*2
        self.put(self.prefab,('\n'.join(refs)+'\n').encode())
        self.integration['palette']=dict(recipe='source-polyart-authored11m',diagnosticRun='V239c',terrains=16,clonedOuterTerrainData=12,passed=True,appearanceOperationsPreservedGeometryAndAlpha=True,sourceDependencies=self.deps,layers=self.layers,tiles=self.tiles)
    def select(self):return retain._finish_schema3_palette(self.clean,self.candidate,self.integration,self.prior)
    def test_exact_actual_output_hashes_and_32_saved_terrain_collider_bindings_accept(self):
        self.assertEqual(12,len(self.select()))
    def test_output_layer_appearance_data_bytes_meta_and_original_outer_are_frozen(self):
        for path in (self.layers[0]['candidate'],self.layers[0]['candidate']+'.meta',self.tiles[4]['candidate'],self.tiles[4]['sourceData'],self.layers[0]['source']):
            file=self.clean/path;old=file.read_bytes();file.write_bytes(old+b'drift')
            with self.subTest(path=path),self.assertRaises(AssertionError):self.select()
            file.write_bytes(old)
    def test_changed_height_alpha_or_hole_operation_cannot_be_relabelled_appearance(self):
        for key in ('heightsAfter','alphaAfter','holesAfter'):
            old=self.tiles[0][key];self.tiles[0][key]='f'*64
            with self.subTest(key=key),self.assertRaises(AssertionError):self.select()
            self.tiles[0][key]=old
    def test_outer_clone_must_retain_original_data_and_holes(self):
        self.tiles[4]['finalHoles']='e'*64
        with self.assertRaises(AssertionError):self.select()
    def test_missing_or_duplicated_saved_terrain_reference_rejects(self):
        file=self.clean/self.prefab;old=file.read_bytes();file.write_bytes(old+old.splitlines(keepends=True)[0])
        with self.assertRaises(AssertionError):self.select()
    def test_unknown_palette_file_and_wrong_clone_path_reject(self):
        path=self.folder+'Unknown.asset';self.put(path,b'Unknown output')
        with self.assertRaises(AssertionError):self.select()
        (self.clean/path).unlink();self.tiles[4]['candidate']=self.folder+'Unreviewed.asset'
        with self.assertRaises(AssertionError):self.select()


class Schema3AuthoredOverrideRetentionTests(unittest.TestCase):
    def test_actual_pinned_garden33_natural22_expansion_is143_fields_and74_active(self):
        # Actual immutable Garden89 recipe has10 active/3 disabled/20 obsolete;
        # Natural34 has22 active fields. Native composition repeats3 and2.
        self.assertEqual([74,9,60],retain._finish_schema3_override_totals([[10,3,20]]*3+[[22,0,0]]*2,[33]*3+[22]*2))
    def test_omitted_extra_or_redistributed_active_source_field_rejects(self):
        original=[[10,3,20]for _ in range(3)]+[[22,0,0]for _ in range(2)]
        for index,delta in ((0,-1),(0,1),(3,-1),(3,1)):
            rows=copy.deepcopy(original);rows[index][0]+=delta
            with self.subTest(index=index,delta=delta),self.assertRaises(AssertionError):retain._finish_schema3_override_totals(rows,[33]*3+[22]*2)
        rows=copy.deepcopy(original);rows[0][0]-=1;rows[1][0]+=1
        with self.assertRaises(AssertionError):retain._finish_schema3_override_totals(rows,[33]*3+[22]*2)
    def row(self,guid='42f3586992df8ca4d93d43abc56eb109',sha='a'*64,property='m_LocalPosition.x',value='0',target='9180849499835440226'):
        source=dict(sceneInstanceId='12345',prefabGuid=guid,sourceSha256=sha,overrides=[dict(targetGuid=guid,targetId=target,property=property,sourceValue=value,objectReference='fileID: 0')])
        proof=dict(instance='12345',targetGuid=guid,targetId=target,property=property,rawValue=value,state='APPLIED_EXACT_ORIGINAL_AUTHOR_VALUE',applied=True,appliedValueVerified=True,targetMatches=1,nativeIdentityMethod='ACTUAL_PERSISTENT_SOURCE_GUID_LOCAL_ID_AND_EXACT_PREFAB_SIBLING_COMPONENT_SLOT_MAPPING',sourceGlobalId='Actual source identity fixture',instanceGlobalId='Actual instance identity fixture',actualType='UnityEngine.Transform',actualValue=value)
        return source,proof
    def test_real_finite_active_transform_and_readback_are_required(self):
        source,proof=self.row(value='0.25');self.assertEqual([1,0,0],retain._finish_schema3_overrides(source,[proof]))
        for field,value in [('applied',False),('appliedValueVerified',False),('targetMatches',0),('actualValue','NaN'),('actualValue','0.3')]:
            copyproof=dict(proof);copyproof[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):retain._finish_schema3_overrides(source,[copyproof])
    def test_exact_original_material_reference_uses_native_asset_type_two(self):
        source,proof=self.row(property='m_Materials.Array.data[0]');guid='1234567890abcdef1234567890abcdef';source['overrides'][0]['objectReference']='fileID: 2100000, guid: '+guid+', type: 2';proof.update(assignedAssetGuid=guid,assignedAssetLocalId='2100000',actualValue=guid+'/2100000',actualType='UnityEngine.MeshRenderer')
        self.assertEqual([1,0,0],retain._finish_schema3_overrides(source,[proof]));source['overrides'][0]['objectReference']=source['overrides'][0]['objectReference'].replace('type: 2','type: 3')
        with self.assertRaises(AssertionError):retain._finish_schema3_overrides(source,[proof])
    def test_exact_absent_source_field_is_documentary_and_never_a_disabled_light(self):
        source,proof=self.row(guid='ba43096d063f9e141bc66d23a6aad4b6',sha='3a3b5a6f7a64a67e7d99b70ecd9f5d641f12527a64ed1cec258b52bcbab5bf36',property='m_BoundingSphereOverride.x',value='10',target='1439406413011755468')
        proof.update(state='EXACT_OBSOLETE_SOURCE_BOUND_TARGET_ABSENT_IN_NATIVE_HIERARCHY',nativeIdentityMethod='EXACT_PINNED_PREFAB_SHA_AND_COMPLETE_NATIVE_SOURCE_INSTANCE_ID_ABSENCE',targetMatches=0,sourceObjectCount=125,instanceObjectCount=125,missingScripts=0,applied=False,appliedValueVerified=False,disabledPublicLightOverride=False)
        self.assertEqual([0,0,1],retain._finish_schema3_overrides(source,[proof]))
        for field,value in [('applied',True),('targetMatches',1),('sourceObjectCount',124),('disabledPublicLightOverride',True),('state','INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE')]:
            bad=dict(proof);bad[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):retain._finish_schema3_overrides(source,[bad])
        source['sourceSha256']='b'*64
        with self.assertRaises(AssertionError):retain._finish_schema3_overrides(source,[proof])
    def test_disabled_light_requires_exact_real_streetlamp_target(self):
        source,proof=self.row(guid='4b1c95ef468f0ad42b07857ca0836f4f',sha='cd287a962e95c8db6e5ce8e146098d50f76da314963a8c63ee014054c2fa1c39',property='m_BoundingSphereOverride.x',target='3877092548939707763')
        proof.update(state='INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE',actualType='UnityEngine.Light',disabledPublicLightOverride=True,applied=False,appliedValueVerified=False)
        self.assertEqual([0,1,0],retain._finish_schema3_overrides(source,[proof]));proof['disabledPublicLightOverride']=False
        with self.assertRaises(AssertionError):retain._finish_schema3_overrides(source,[proof])
    def test_reordered_wrong_owner_or_unsupported_active_field_rejects(self):
        source,proof=self.row()
        for field,value in [('instance','Other instance'),('rawValue','1'),('property','Unsupported.Field')]:
            bad=dict(proof);bad[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):retain._finish_schema3_overrides(source,[bad])
    def test_omitted_or_extra_native_active_evidence_cannot_change_original_field_count(self):
        source,proof=self.row()
        for evidence in ([],[proof,dict(proof)]):
            with self.assertRaises(AssertionError):retain._finish_schema3_overrides(source,evidence)


class Schema3FullWorldGateTests(_Schema3Fixture,unittest.TestCase):
    def test_plan_alone_cannot_authorize_schema3_world_retention(self):
        with self.assertRaisesRegex(AssertionError,'full native rounded-world'):
            retain._finish_composition_v3(self.clean,self.candidate,self.integration,self.prior,self.run)


class Schema3HarborSupportRetentionTests(unittest.TestCase):
    def setUp(self):
        self.quay=dict(name=retain.FINISH_SCHEMA3_EXTRA[0],passed=True,originalFerryPoseExact=True,departureCorridorClear=True,roadAndObjectiveBuffersClear=True,modules=40,piers=10,sourceBearingStations=2,supportedDecorations=22,decorationFootSamples=198,departureQueries=53,minimumDepartureLateralClearance=11,deckSurface=0,parts=[{}for _ in range(50)],decorations=[{}for _ in range(22)],pierContacts=[],sourceBearingContacts=[],deckContacts=[])
        for x in (674,695):
            for z in (-624,-637,-650,-663,-676,-688):
                p=dict(x=x,y=0,z=z)
                if z==-624:self.quay['sourceBearingContacts'].append(dict(position=p,nativeBed=0,sourceUnderside=-.9,sourceCollider='Fixture actual source dock',passed=True))
                else:self.quay['pierContacts'].append(dict(position=p,nativeBed=-8,postBottom=-8.12,postTop=-2.785,deckUnderside=-2.8,baseCollider='Fixture actual terrain',baseContact=True,deckContact=True))
        for i in range(198):
            self.quay['deckContacts'].append(dict(requested=dict(x=675+i%6,y=.5,z=-630-i%20),actual=dict(x=675+i%6,y=0,z=-630-i%20),collider='Fixture actual walkable BoxCollider',passed=True))
    def select(self):return retain._finish_schema3_quay(self.quay)
    def test_ten_generated_piers_plus_two_native_source_bearings_mean_fifty_parts(self):
        self.select();self.assertEqual(50,len(self.quay['parts']));self.assertEqual(12,self.quay['piers']+self.quay['sourceBearingStations'])
    def test_station_counts_follow_actual_native_classification_without_inventing_parts(self):
        bearing=self.quay['sourceBearingContacts'].pop();self.quay['sourceBearingStations']-=1;self.quay['piers']+=1;self.quay['parts'].append({})
        self.quay['pierContacts'].append(dict(position=bearing['position'],nativeBed=-8,postBottom=-8.12,postTop=bearing['sourceUnderside']+.015,deckUnderside=bearing['sourceUnderside'],baseCollider='Fixture changed native terrain',baseContact=True,deckContact=True))
        self.select()
    def test_twelve_claimed_generated_piers_cannot_also_claim_two_existing_bearings(self):
        self.quay['piers']=12
        with self.assertRaisesRegex(AssertionError,'support stations'):self.select()
    def test_every_measured_source_bearing_needs_a_finite_real_underside_contact(self):
        contact=self.quay['sourceBearingContacts'][0]
        for field,value in [('passed',False),('sourceCollider',''),('sourceUnderside',1),('sourceUnderside',float('nan')),('nativeBed',float('inf'))]:
            original=contact[field];contact[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            contact[field]=original
    def test_source_bearing_count_missing_or_duplicate_station_rejects(self):
        original=copy.deepcopy(self.quay['sourceBearingContacts']);self.quay['sourceBearingContacts']=original[:1]
        with self.assertRaises(AssertionError):self.select()
        self.quay['sourceBearingContacts']=[original[0],copy.deepcopy(original[0])]
        with self.assertRaisesRegex(AssertionError,'station must occur exactly once'):self.select()
    def test_generated_piers_still_need_actual_bed_to_native_underside_joints(self):
        contact=self.quay['pierContacts'][0]
        for field,value in [('baseContact',False),('deckContact',False),('postBottom',-7),('postTop',0),('deckUnderside',float('inf'))]:
            original=contact[field];contact[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            contact[field]=original
    def test_all_twenty_two_decorations_and_198_contacts_remain_mandatory(self):
        for field,value in [('passed',False),('supportedDecorations',21),('decorationFootSamples',197),('deckContacts',self.quay['deckContacts'][:-1])]:
            original=self.quay[field];self.quay[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            self.quay[field]=original
        self.quay['deckContacts'][0]['passed']=False
        with self.assertRaises(AssertionError):self.select()
    def test_actual_walkable_floor_and_decoration_foot_queries_cannot_be_relabelled(self):
        self.quay['deckSurface']=2
        with self.assertRaises(AssertionError):self.select()
        self.quay['deckSurface']=0;self.quay['deckContacts'][0]['actual']['y']=.1
        with self.assertRaisesRegex(AssertionError,'decoration-foot'):self.select()


if __name__=='__main__':unittest.main()
