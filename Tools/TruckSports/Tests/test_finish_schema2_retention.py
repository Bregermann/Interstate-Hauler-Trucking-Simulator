from pathlib import Path
import copy
import hashlib
import json
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain
import test_finish_candidate_retention as finish_fixture
import test_coast_candidate_retention as coast_fixture


def vector(x=0,y=0,z=0):return dict(x=x,y=y,z=z)
def rotation():return dict(x=0,y=0,z=0,w=1)
def bounds(center=None,extent=None):return dict(m_Center=center or vector(),m_Extent=extent or vector(1,1,1))
def inline(value):return '{'+', '.join(k+': '+str(v) for k,v in value.items())+'}'


def direct(go,transform,name,position=None,parent=0,children=(),scale=None,quaternion=None):
    listing='\n'+'\n'.join('  - {fileID: '+str(x)+'}' for x in children) if children else ' []'
    return (f'--- !u!1 &{go}\nGameObject:\n  m_Name: {name}\n'
            f'--- !u!4 &{transform}\nTransform:\n  m_GameObject: {{fileID: {go}}}\n'
            f'  m_LocalPosition: {inline(position or vector())}\n  m_LocalRotation: {inline(quaternion or rotation())}\n'
            f'  m_LocalScale: {inline(scale or vector(1,1,1))}\n  m_Children:{listing}\n  m_Father: {{fileID: {parent}}}\n')


def instance(transform,identity,guid,parent,name,position,quaternion,scale):
    modifications=[]
    for field,values in [('m_LocalPosition',position),('m_LocalRotation',quaternion),('m_LocalScale',scale)]:
        for key,value in values.items():
            modifications.append(f'    - target: {{fileID: 20, guid: {guid}, type: 3}}\n      propertyPath: {field}.{key}\n      value: {value}\n      objectReference: {{fileID: 0}}\n')
    modifications.append(f'    - target: {{fileID: 10, guid: {guid}, type: 3}}\n      propertyPath: m_Name\n      value: {name}\n      objectReference: {{fileID: 0}}\n')
    return (f'--- !u!4 &{transform} stripped\nTransform:\n  m_CorrespondingSourceObject: {{fileID: 20, guid: {guid}, type: 3}}\n  m_PrefabInstance: {{fileID: {identity}}}\n'
            f'--- !u!1001 &{identity}\nPrefabInstance:\n  m_Modification:\n    m_TransformParent: {{fileID: {parent}}}\n    m_Modifications:\n'+''.join(modifications)+
            f'    m_AddedGameObjects: []\n  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}\n')


def material_pair(number):
    count=41 if number<9 else 40
    shader='{fileID: 4800000, guid: '+format(9000,'032x')+', type: 3}'
    props=[dict(kind='Shader',name='m_Shader',sourceValue=shader)]
    lines=['  m_Shader: '+shader,'  m_CustomRenderQueue: '+('2050' if number==0 else '2000'),'  m_SavedProperties:']
    for i in range(count-1-(1 if number==0 else 0)):
        props.append(dict(kind='Scalar',name='_Value'+str(i),sourceValue=str(i)));lines.append('    - _Value'+str(i)+': '+str(i))
    converted=[]
    if number==0:
        base='{r: 1, g: 0.8, b: 0.2, a: 1}'
        props.append(dict(kind='Color',name='_BaseColor',sourceValue=base));lines.append('    - _BaseColor: '+base)
        lines+=['    - _Color: {r: 0.5, g: 0.5, b: 0.5, a: 1}','    - _EmissionColor: {r: 2, g: 1.8666667, b: 0.9490196, a: 1}']
        converted=[dict(kind='Color',name='_Color',sourceValue='{r: 0.5, g: 0.5, b: 0.5, a: 1}',importedValue=base,reason='Inactive legacy alias'),dict(kind='Color',name='_EmissionColor',sourceValue='{r: 2, g: 1.8666667, b: 0.9490196, a: 1}',importedValue='{r: 4.594794, g: 3.9477253, b: 0.8879232, a: 1}',reason='URP HDR linear compatibility')]
    raw=('%YAML 1.1\nMaterial:\n  serializedVersion: 1\n'+'\n'.join(lines)+'\n').encode()
    imported=raw.decode().replace('serializedVersion: 1','serializedVersion: 10')
    for item in converted:imported=imported.replace('    - '+item['name']+': '+item['sourceValue'],'    - '+item['name']+': '+item['importedValue'])
    if number==0:imported=imported.replace('m_CustomRenderQueue: 2050','m_CustomRenderQueue: 2000')
    return raw,imported.encode(),props,converted


class FinishSchema2RetentionTests(unittest.TestCase):
    def setUp(self):
        self.seed=finish_fixture.FinishCandidateRetentionTests('runTest');self.seed.setUp();self.addCleanup(self.seed.doCleanups)
        self.clean=self.seed.clean;self.prior=self.seed.prior;self.receipt=self.seed.receipt;self.finish=self.receipt['finish']
        self.finish.update(compositionSchema=2,traderShips=1)
        self.dependencies=[];self.sources=[];self.material_bindings=[]
        for i in range(103):
            path=retain.FINISH_PRIVATE+('TestArtist/Prefab_'+str(i)+'.prefab' if i<28 else 'TestArtist/'+('M_CLR_yellow_E' if i==28 else 'Material_'+str(i))+'.mat' if i<47 else 'TestArtist/Dependency_'+str(i)+'.asset')
            data=('%YAML 1.1\n'+direct(10,20,'Prefab_'+str(i))).encode() if i<28 else ('Genuine fixture dependency '+str(i)).encode()
            converted=None
            if 28<=i<47:data,converted,protected,changes=material_pair(i-28)
            self.frozen(path,data,1000+i)
            row=dict(path=path,guid=format(1000+i,'032x'),sha256=self.hash(path),metaSha256=self.hash(path+'.meta'));self.dependencies.append(row)
            if i<28:self.sources.append(row)
            if converted is not None:
                snapshot=retain.FINISH_PRIVATE+'DerivedVillageLayout/OriginalMaterialSources/'+row['guid']+'.bytes';self.frozen(snapshot,data,5000+i)
                self.seed.put(path,converted)
                self.material_bindings.append(dict(path=path,guid=row['guid'],originalSha256=row['sha256'],importedSha256=self.hash(path),metaSha256=row['metaSha256'],snapshot=snapshot,snapshotSha256=self.hash(snapshot),snapshotMetaSha256=self.hash(snapshot+'.meta'),snapshotGuid=format(5000+i,'032x'),protectedProperties=protected,convertedProperties=changes,originalRenderQueue=2050 if i==28 else 2000,importedRenderQueue=2000))
        upgrader='Packages/com.unity.render-pipelines.universal/Editor/AssetPostProcessors/MaterialPostprocessor.cs';self.seed.put(upgrader,b'Actual native fixture public material upgrader v10')
        self.seed.put('Packages/com.unity.render-pipelines.universal/package.json',json.dumps(dict(name='com.unity.render-pipelines.universal',version='17.4.0')).encode());self.seed.put('ProjectSettings/ProjectVersion.txt',b'm_EditorVersion: 6000.4.10f1\n')
        provenance=dict(schemaVersion=1,scope='EXACT_V201_PUBLIC_URP_NATIVE_IMPORT_PROVENANCE_NOT_GENERAL_MATERIAL_WAIVER',editorVersion='6000.4.10f1',urpVersion='17.4.0',upgraderPackage='com.unity.render-pipelines.universal',upgraderRelativePath='Editor/AssetPostProcessors/MaterialPostprocessor.cs',upgraderSourceSha256=self.hash(upgrader),nativeUpgraderFromVersion=1,nativeUpgraderToVersion=10,sourceNativeRun='V201',bindings=self.material_bindings)
        self.frozen(retain.FINISH_IMPORT_PROVENANCE,json.dumps(provenance).encode(),2301)
        self.addCleanup(patch.stopall);patch.object(retain,'FINISH_IMPORT_PROVENANCE_SHA',self.hash(retain.FINISH_IMPORT_PROVENANCE)).start()
        self.rows=[];children=[]
        for i in range(61):
            source=self.sources[i%28];p=vector((i%10)*.4,0,(i//10)*.3)
            row=dict(sceneInstanceId=str(100000+i),prefabGuid=source['guid'],prefab=source['path'],sourceSha256=source['sha256'],sourceMetaSha256=source['metaSha256'],position=p,rotation=rotation(),scale=vector(1,1,1),overrides=[])
            if i<3:
                row['overrides']=[dict(targetId=str((10+i)^937063833075107752),targetGuid=row['prefabGuid'],property='m_BoundingSphereOverride.'+axis,value=0,sourceValue='NaN' if axis=='x' else '0',sourceValueFloatBits='0000c07f' if axis=='x' else '00000000',state='INACTIVE_SOURCE_LIGHT_BOUND_OVERRIDE',applied=False,sourceComponentGuid='3dc4d6bb2162865469ab19bc08d79628',sourceComponentId='937063833075107752',nestedSourceInstanceId=str(10+i),sourceComponentType='UnityEngine.Light') for axis in 'xyzw']
            self.rows.append(row)
            children.append(dict(name='Artist source '+row['sceneInstanceId']+' Prefab_'+str(i%28),sceneInstanceId=row['sceneInstanceId'],source=row['prefab'],guid=row['prefabGuid'],sourceSha256=row['sourceSha256'],sourceMetaSha256=row['sourceMetaSha256'],localPosition=p.copy(),localRotation=rotation(),localScale=vector(1,1,1),actualBounds=bounds(vector(-480-p['x']*1.75,2,-285-p['z']*1.75)),sourceOverrides=len(row['overrides']),appliedSourceOverrides=0,inactiveSourceOverrides=len(row['overrides']),overrideEvidence=[dict(**{k:v for k,v in change.items() if k in ('targetId','targetGuid','property','sourceValue','sourceValueFloatBits','state','applied')},authority='ACTUAL_IMPORTED_INHERITED_LIGHT_GUID_LOCAL_ID_AND_PUBLIC_DISABLED_OVERRIDE_CHECK',publicOverrideDisabled=True,useBoundingSphereOverride=False,authoredOriginalOverrideDisabled=True,actualLightType='UnityEngine.Light',actualOriginalSourceGuid=change['sourceComponentGuid'],actualOriginalSourceLocalId=change['sourceComponentId'],actualPersistentGuid=row['prefabGuid'],actualPersistentLocalId=change['targetId'],actualPersistentPrefabId=change['nestedSourceInstanceId'],nativeIdentityMethod='NATIVE_NESTED_TRANSFORM_AND_ORIGINAL_SOURCE_XOR',examinedPersistentComponents=2,targetMatches=1) for change in row['overrides']]))
        layout=dict(schemaVersion=3,scope='Fixture exact original artist composition',sourceScene='OWNED/Tidal Flask Studios/Fantastic Village Pack/scenes/demoscene_village_day.unity',sourceSceneSha256=retain.FINISH_ARTIST_SCENE_SHA,sourceSceneMetaSha256='a'*64,identityParent='2075655012',center=vector(199.45,14.997253,194.77),radius=10,materialImportProvenance=retain.FINISH_IMPORT_PROVENANCE,materialImportProvenanceSha256=self.hash(retain.FINISH_IMPORT_PROVENANCE),instances=self.rows,dependencies=self.dependencies)
        self.frozen(retain.FINISH_ARTIST_LAYOUT,json.dumps(layout).encode(),2001)
        patch.object(retain,'FINISH_ARTIST_LAYOUT_SHA',self.hash(retain.FINISH_ARTIST_LAYOUT)).start()
        self.finish['artistMarket']=dict(scope='ONE_EXACT_SOURCE_AUTHORED_MARKET_GROUP_NOT_ARBITRARY_SCATTER_OR_VISUAL_ACCEPTANCE',layout=retain.FINISH_ARTIST_LAYOUT,layoutSha256=self.hash(retain.FINISH_ARTIST_LAYOUT),layoutGuid=format(2001,'032x'),layoutMetaSha256=self.hash(retain.FINISH_ARTIST_LAYOUT+'.meta'),sourceSceneSha256=retain.FINISH_ARTIST_SCENE_SHA,identityParent='2075655012',position=vector(-480,2,-285),rotation=dict(x=0,y=1,z=0,w=0),scale=vector(1.75,1.75,1.75),bounds=bounds(vector(-480,8,-285),vector(20,8,20)),groups=1,instances=61,prefabs=28,groundSamples=49,trial=0,supportHeightRange=.2,minimumFreeboard=2.5,passed=True,exactSourcePoses=True,exactDependencies=True,roadAndObjectiveBuffersClear=True,children=children,appliedSourceOverrides=0,inactiveSourceOverrides=12,dependencies=[dict(**dep,actualSha256=self.hash(dep['path']),approvedNativeImport=dep['path'].endswith('.mat')) for dep in self.dependencies])
        self.finish['artistMarket']['materialImports']=dict(provenance=retain.FINISH_IMPORT_PROVENANCE,provenanceSha256=self.hash(retain.FINISH_IMPORT_PROVENANCE),provenanceGuid=format(2301,'032x'),provenanceMetaSha256=self.hash(retain.FINISH_IMPORT_PROVENANCE+'.meta'),editorVersion='6000.4.10f1',urpVersion='17.4.0',upgraderSourceSha256=self.hash(upgrader),rawMaterialSnapshots=19,approvedNativeImports=19,exactNonmaterialDependencies=84,exactMetadataAndGuids=103,protectedProperties=769,convertedProperties=2,passed=True,bindings=copy.deepcopy(self.material_bindings))
        self.frozen(retain.FINISH_SHIP,('%YAML 1.1\n'+direct(10,20,'TraderShip')).encode(),2100)
        waterline=[vector(36 if i%2==0 else 44,-2,-10 if i%3 else 10) for i in range(12)]
        self.finish['mooredShip']=dict(scope='ACTUAL_OWNED_TRADER_HULL_SIDE_MOORING_NOT_REPLACEMENT_FERRY_OR_VISUAL_ACCEPTANCE',source=retain.FINISH_SHIP,sourceSha256=self.hash(retain.FINISH_SHIP),sourceMetaSha256=self.hash(retain.FINISH_SHIP+'.meta'),sourceGuid=format(2100,'032x'),bounds=bounds(vector(40,0,0),vector(4,4,16)),position=vector(40,-4,0),rotation=rotation(),scale=vector(1,1,1),seaLevel=-2,keel=-4,hullLength=32,hullHeight=2,hullKeel=-3.2,hullDraft=1.2,waterlineWidth=8,waterlineLength=20,minimumKeelClearance=4,departureLateralClearance=24,depthSamples=25,waterlineIntersections=12,departureSweepSamples=53,passed=True,originalFerryPoseExact=True,waterline=waterline)
        self.finish['sourcePrefabs']=[d['path'] for d in self.sources]+[retain.FINISH_SHIP]
        self.original='%YAML 1.1\n'+direct(90000,90001,'Original kingdom',children=[90003])+direct(90002,90003,'Timber evacuation barge',parent=90001)
        self.canonical='Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab';self.frozen(self.canonical,self.original.encode(),2200)
        self.write_prefab()

    def hash(self,path):return hashlib.sha256((self.clean/path).read_bytes()).hexdigest()
    def frozen(self,path,data,guid):
        self.seed.put(path,data);self.seed.put(path+'.meta',self.seed.meta(guid))
        for name in (path,path+'.meta'):self.prior[name]=self.hash(name)
    def write_prefab(self,order=None):
        order=list(range(61)) if order is None else order
        text=self.original+direct(500,501,retain.FINISH_ARTIST_NAME,vector(-480,2,-285),90001,[6000+i for i in order],vector(1.75,1.75,1.75),dict(x=0,y=1,z=0,w=0))
        for i,row in enumerate(self.rows):text+=instance(6000+i,7000+i,row['prefabGuid'],501,self.finish['artistMarket']['children'][i]['name'],row['position'],row['rotation'],row['scale'])
        ship=self.finish['mooredShip'];text+=instance(8000,8001,ship['sourceGuid'],90001,'Actual owned side-moored trader ship',ship['position'],ship['rotation'],ship['scale'])
        text+='  moltenMesh: {fileID: 4300000, guid: '+format(2,'032x')+', type: 2}\n  moltenMaterial: {fileID: 2100000, guid: '+format(3,'032x')+', type: 2}\n'
        self.seed.put(self.seed.prefab,text.encode())
    def select(self):return self.seed.select()
    def test_schema2_accepts_exact_frozen_sources_and_selects_only_original_four_outputs(self):
        selected={p.relative_to(self.clean).as_posix() for p in self.select()}
        self.assertEqual({p+s for p in (self.seed.mesh,self.seed.material) for s in ('','.meta')},selected)
    def test_missing_artist_or_ship_proof_rejects(self):
        for field in ('artistMarket','mooredShip'):
            old=self.finish.pop(field)
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            self.finish[field]=old
    def test_unknown_schema_and_implicit_legacy_with_new_roots_reject(self):
        for value in (1,3,None,True,'2',0):
            self.finish['compositionSchema']=value
            with self.subTest(value=value),self.assertRaises(AssertionError):self.select()
    def test_legacy_existing_counts_cannot_be_reduced_by_schema2(self):
        self.finish['underbrush']=139
        with self.assertRaisesRegex(AssertionError,'All334'):self.select()
    def test_ordered_or_duplicate_artist_children_reject(self):
        rows=self.finish['artistMarket']['children'];original=copy.deepcopy(rows)
        for value in ([rows[1],rows[0]]+rows[2:],rows[:-1]+[rows[0]],rows[:-1]):
            self.finish['artistMarket']['children']=value
            with self.subTest(count=len(value)),self.assertRaises(AssertionError):self.select()
        self.finish['artistMarket']['children']=original
    def test_recorded_local_pose_source_or_override_drift_rejects(self):
        child=self.finish['artistMarket']['children'][0]
        for field,value in [('localPosition',vector(50,0,0)),('source','Assets/Unknown.prefab'),('sourceOverrides',1)]:
            old=child[field];child[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            child[field]=old
    def test_saved_prefab_child_reorder_duplicate_and_extra_root_reject(self):
        for order in ([1,0]+list(range(2,61)),list(range(60))+[0],list(range(61))+[0]):
            self.write_prefab(order)
            with self.subTest(order=order[:2]),self.assertRaises(AssertionError):self.select()
        self.write_prefab()
    def test_saved_prefab_pose_drift_rejects(self):
        p=self.clean/self.seed.prefab;text=p.read_text();p.write_text(text.replace('propertyPath: m_LocalPosition.x\n      value: 0.0','propertyPath: m_LocalPosition.x\n      value: 8.0',1))
        with self.assertRaisesRegex(AssertionError,'saved artist child position'):self.select()
    def test_layout_dependency_and_source_meta_drift_reject(self):
        for path in [retain.FINISH_ARTIST_LAYOUT,retain.FINISH_ARTIST_LAYOUT+'.meta',self.sources[0]['path'],self.sources[0]['path']+'.meta',self.dependencies[-1]['path']]:
            p=self.clean/path;old=p.read_bytes();p.write_bytes(old+b'changed')
            with self.subTest(path=path),self.assertRaisesRegex(AssertionError,'frozen intake'):self.select()
            p.write_bytes(old)
    def test_duplicate_missing_reordered_dependency_or_source_identity_reject(self):
        deps=self.finish['artistMarket']['dependencies'];old=copy.deepcopy(deps)
        for value in (deps[:-1],deps[:-1]+[deps[0]],[deps[1],deps[0]]+deps[2:]):
            self.finish['artistMarket']['dependencies']=value
            with self.subTest(count=len(value)),self.assertRaisesRegex(AssertionError,'103'):self.select()
        self.finish['artistMarket']['dependencies']=old
    def test_exact_dry_support_counts_freeboard_and_flags_reject(self):
        row=self.finish['artistMarket']
        for field,value in [('groundSamples',48),('supportHeightRange',.61),('minimumFreeboard',1.49),('exactSourcePoses',False),('roadAndObjectiveBuffersClear',False)]:
            old=row[field];row[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            row[field]=old
    def test_nonfinite_parent_child_bounds_and_waterline_reject(self):
        for row,key in [(self.finish['artistMarket']['position'],'x'),(self.finish['artistMarket']['children'][0]['localScale'],'z'),(self.finish['artistMarket']['bounds']['m_Extent'],'y'),(self.finish['mooredShip']['waterline'][0],'x')]:
            old=row[key];row[key]=float('nan')
            with self.subTest(key=key),self.assertRaises(AssertionError):self.select()
            row[key]=old
    def test_ship_waterline_depth_departure_source_and_preserved_ferry_gates_reject(self):
        row=self.finish['mooredShip']
        for field,value in [('depthSamples',24),('departureSweepSamples',52),('minimumKeelClearance',2.99),('departureLateralClearance',9.9),('originalFerryPoseExact',False),('waterlineWidth',7),('waterlineIntersections',13),('source','Assets/Other.prefab'),('keel',-3)]:
            old=row[field];row[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            row[field]=old
        row['waterline'][0]['y']=-1
        with self.assertRaisesRegex(AssertionError,'sea level'):self.select()
    def test_actual_ferry_saved_pose_drift_rejects_even_when_boolean_says_preserved(self):
        p=self.clean/self.seed.prefab;text=p.read_text();p.write_text(text.replace('m_Name: Timber evacuation barge\n--- !u!4 &90003\nTransform:\n  m_GameObject: {fileID: 90002}\n  m_LocalPosition: {x: 0, y: 0, z: 0}','m_Name: Timber evacuation barge\n--- !u!4 &90003\nTransform:\n  m_GameObject: {fileID: 90002}\n  m_LocalPosition: {x: 1, y: 0, z: 0}'))
        with self.assertRaisesRegex(AssertionError,'original ferry full position'):self.select()


    def test_native_material_import_proof_flags_counts_and_version_are_exact(self):
        row=self.finish['artistMarket']['materialImports']
        for field,value in [('passed',False),('approvedNativeImports',18),('rawMaterialSnapshots',18),('exactNonmaterialDependencies',85),('exactMetadataAndGuids',102),('protectedProperties',768),('convertedProperties',3),('editorVersion','6000.5.5f1'),('urpVersion','17.4.1')]:
            old=row[field];row[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            row[field]=old
    def test_imported_bytes_snapshots_metadata_and_upgrader_drift_reject(self):
        row=self.material_bindings[0]
        for path in [row['path'],row['path']+'.meta',row['snapshot'],row['snapshot']+'.meta',retain.FINISH_IMPORT_PROVENANCE,'Packages/com.unity.render-pipelines.universal/Editor/AssetPostProcessors/MaterialPostprocessor.cs']:
            p=self.clean/path;old=p.read_bytes();p.write_bytes(old+b'changed')
            with self.subTest(path=path),self.assertRaises(AssertionError):self.select()
            p.write_bytes(old)
    def test_import_recipe_cannot_waive_additional_material_or_edited_protected_properties(self):
        imports=self.finish['artistMarket']['materialImports'];old=copy.deepcopy(imports['bindings'])
        for mode in ('extra','snapshot','property'):
            imports['bindings']=copy.deepcopy(old)
            if mode=='extra':imports['bindings'].append(copy.deepcopy(old[0]))
            if mode=='snapshot':imports['bindings'][0]['snapshot']='Assets/Other.bytes'
            if mode=='property':imports['bindings'][0]['protectedProperties'][0]['sourceValue']='Different authored shader'
            with self.subTest(mode=mode),self.assertRaises(AssertionError):self.select()
        imports['bindings']=old
    def test_dependency_import_flags_and_actual_hash_cannot_mask_nonmaterial_drift(self):
        row=self.finish['artistMarket']['dependencies'][0]
        for field,value in [('approvedNativeImport',True),('actualSha256','f'*64)]:
            old=row[field];row[field]=value
            with self.subTest(field=field),self.assertRaisesRegex(AssertionError,'84 nonmaterial'):self.select()
            row[field]=old
    def test_raw_nan_only_allowed_as_exact_unapplied_native_disabled_light_evidence(self):
        row=self.finish['artistMarket']['children'][0]['overrideEvidence'][0]
        self.assertEqual('NaN',row['sourceValue']);self.select()
        for field,value in [('applied',True),('publicOverrideDisabled',False),('useBoundingSphereOverride',True),('authoredOriginalOverrideDisabled',False),('actualLightType','UnityEngine.MeshRenderer'),('targetMatches',0),('examinedPersistentComponents',0),('authority','YAML_ABSENCE_GUESS'),('sourceValue','0'),('sourceValueFloatBits','ffffffff')]:
            old=row[field];row[field]=value
            with self.subTest(field=field),self.assertRaises(AssertionError):self.select()
            row[field]=old
    def test_csharp_optional_empty_property_fields_are_normalized_without_loosening_values(self):
        for row in self.finish['artistMarket']['materialImports']['bindings']:
            for item in row['protectedProperties']:
                item.setdefault('importedValue','');item.setdefault('reason','')
        self.select()


class FinishSchema2CoastRetentionTests(unittest.TestCase):
    def setUp(self):
        self.seed=coast_fixture.CoastCandidateRetentionTests('runTest');self.seed.setUp();self.addCleanup(self.seed.doCleanups)
        self.seed.receipt['finish']=dict(compositionSchema=2);self.row=self.seed.receipt['outerBand']
        self.row.update(seaRows=79,seaColumns=79,seaVertices=6241,exactStraightSeaGeometry=True,invertedSeaTriangles=0,invalidSeaVertices=0,maximumSeaRowDrift=.0001,minimumSeaTriangleArea=100,actualSeaBounds=bounds(vector(0,-2,800),vector(6200,0,6200)))
    def test_schema2_validates_straight_native_grid_without_selecting_more_files(self):self.assertEqual(11,len(self.seed.select()))
    def test_actual78_by79_native_grid_is_valid_when_count_product_matches(self):
        self.row.update(seaRows=78,seaColumns=79,seaVertices=6162,maximumSeaRowDrift=.00048828125)
        self.assertEqual(11,len(self.seed.select()))
        self.row['seaVertices']=6241
        with self.assertRaisesRegex(AssertionError,'rectangular'):self.seed.select()
    def test_bad_counts_rows_orientation_drift_area_and_finite_bounds_reject(self):
        for field,value in [('seaRows',78),('seaColumns',2),('seaVertices',6242),('exactStraightSeaGeometry',False),('invertedSeaTriangles',1),('invalidSeaVertices',1),('maximumSeaRowDrift',.15),('maximumSeaRowDrift',float('nan')),('minimumSeaTriangleArea',.01),('minimumSeaTriangleArea',float('inf')),('actualSeaBounds',bounds(vector(0,-2,800),vector(6190,0,6200)))]:
            old=self.row[field];self.row[field]=value
            with self.subTest(field=field,value=value),self.assertRaises(AssertionError):self.seed.select()
            self.row[field]=old
    def test_legacy_scope_does_not_silently_reopen_historical_geometry(self):
        self.seed.receipt.pop('finish');self.row['exactStraightSeaGeometry']=False;self.row['seaRows']=0
        self.assertEqual(11,len(self.seed.select()))


if __name__=='__main__':unittest.main()
