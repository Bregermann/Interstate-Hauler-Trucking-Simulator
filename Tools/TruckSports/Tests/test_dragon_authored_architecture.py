"""Source extraction guards; no synthetic fixture is native/world acceptance."""
from pathlib import Path
import copy
import hashlib
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import DragonKingdomAuthoredArchitecture as art


class AuthoredArchitectureExtractionTests(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory(prefix='AuthoredArchitectureFixture-');self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.owned=self.root/'Owned';self.destination=self.root/'Assets/TruckDragonKingdom/PrivateLicensedEnvironment';self.pack='Fixture Pack'
        self.guid='1234567890abcdef1234567890abcdef';self.material_guid='abcdef1234567890abcdef1234567890'
        self.path='Assets/Plugins/'+self.pack+'/1xWall.prefab';self.material_path='Assets/Plugins/'+self.pack+'/Original.mat'
        self.prefab=('--- !u!1 &1\nGameObject:\n  m_Name: Actual original wall\n--- !u!4 &4\nTransform:\n  m_GameObject: {fileID: 1}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 1, y: 2, z: 3}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Father: {fileID: 0}\n').encode()
        self.scene=('--- !u!1 &10\nGameObject:\n  m_Name: Authored parent\n  m_IsActive: 1\n--- !u!4 &100\nTransform:\n  m_GameObject: {fileID: 10}\n  m_LocalRotation: {x: 0, y: 0.70710677, z: 0, w: 0.70710677}\n  m_LocalPosition: {x: 30, y: -2, z: 80}\n  m_LocalScale: {x: 2, y: 3, z: 4}\n  m_Father: {fileID: 0}\n--- !u!1001 &300\nPrefabInstance:\n  m_Modification:\n    m_TransformParent: {fileID: 100}\n    m_Modifications:\n    - target: {fileID: 4, guid: '+self.guid+', type: 3}\n      propertyPath: m_LocalPosition.x\n      value: -2.5\n      objectReference: {fileID: 0}\n    - target: {fileID: 77, guid: '+self.guid+', type: 3}\n      propertyPath: m_Materials.Array.data[0]\n      value: \n      objectReference: {fileID: 2100000, guid: '+self.material_guid+', type: 2}\n    m_RemovedComponents: []\n  m_SourcePrefab: {fileID: 100100000, guid: '+self.guid+', type: 3}\n').encode()
        self.scene_path=self.owned/self.pack/'Demo.unity';self.scene_path.parent.mkdir(parents=True);self.scene_path.write_bytes(self.scene);Path(str(self.scene_path)+'.meta').write_bytes(b'original demo metadata')
        self.entries={self.path:(self.prefab,b'guid: '+self.guid.encode(),''),self.material_path:(b'actual source material',b'guid: '+self.material_guid.encode(),'')};self.index={self.guid:self.path,self.material_guid:self.material_path}
        self.spec=dict(name='Fixture substantial source guard',pack=self.pack,scene='Demo.unity',sha=art.sha(self.scene),selection='CASTLE_CONNECTED_ARCHITECTURAL_SOURCE_SET',instances=1,prefabs=1)
    def extract(self):return art.extract_composition(self.spec,self.entries,self.index,self.owned,self.destination,self.root)
    def write_scene(self,data):self.scene_path.write_bytes(data);self.spec['sha']=art.sha(data)
    def test_actual_source_local_trs_parent_chain_and_raw_override_records_are_retained(self):
        seeds,layout,raw,meta=self.extract();part=layout['instances'][0];parent=layout['parentTransforms'][0]
        self.assertEqual(sorted((self.path,self.material_path)),seeds)
        self.assertEqual(dict(x=-2.5,y=2,z=3),part['position']);self.assertEqual(dict(x=2,y=3,z=4),parent['scale']);self.assertEqual(30,parent['position']['x']);self.assertAlmostEqual(.70710677,parent['rotation']['y'])
        self.assertEqual(self.scene,raw);self.assertEqual(b'original demo metadata',meta);self.assertEqual(2,len(part['modifications']));self.assertIn('m_RemovedComponents: []',part['serializedInstance'])
        self.assertEqual(hashlib.sha256(part['serializedInstance'].encode()).hexdigest(),part['serializedInstanceSha256'])
        for name in ('nativeOverridesReconciled','nativeMaterialImportsValidated','nativeMeshBoundsMeasured','nativeSupportAndRoadWingBuffersValidated'):self.assertIs(False,layout[name])
    def test_changed_original_demo_requires_separate_recipe_review(self):
        self.scene_path.write_bytes(self.scene+b'changed original')
        with self.assertRaisesRegex(RuntimeError,'Changed owned architecture'):self.extract()
    def test_nonfinite_root_or_parent_trs_rejects(self):
        for old,new in ((b'value: -2.5',b'value: NaN'),(b'x: 30, y: -2',b'x: NaN, y: -2')):
            self.write_scene(self.scene.replace(old,new))
            with self.assertRaisesRegex(RuntimeError,'Nonfinite'):self.extract()
    def test_unknown_material_guid_does_not_become_optional_or_silently_substituted(self):
        self.index.pop(self.material_guid)
        with self.assertRaisesRegex(RuntimeError,'Unknown original authored'):self.extract()
    def test_missing_cyclic_or_stripped_parent_rejects(self):
        for original,replacement in ((b'm_Father: {fileID: 0}',b'm_Father: {fileID: 100}'),(b'm_TransformParent: {fileID: 100}',b'm_TransformParent: {fileID: 999}'),(b'!u!4 &100\n',b'!u!4 &100 stripped\n')):
            self.write_scene(self.scene.replace(original,replacement))
            with self.assertRaises(RuntimeError):self.extract()
    def test_disabled_parent_is_preserved_not_reenabled_by_intake(self):
        self.write_scene(self.scene.replace(b'm_IsActive: 1',b'm_IsActive: 0'));self.assertIs(False,self.extract()[1]['parentTransforms'][0]['active'])
    def test_missing_and_extra_source_members_are_not_accepted_as_complete(self):
        self.spec['instances']=2
        with self.assertRaisesRegex(RuntimeError,'count differs'):self.extract()
        self.spec['instances']=1;self.spec['prefabs']=2
        with self.assertRaisesRegex(RuntimeError,'count differs'):self.extract()
    def test_documentary_nonfinite_override_stays_raw_and_unapplied(self):
        extra=('    - target: {fileID: 88, guid: '+self.guid+', type: 3}\n      propertyPath: m_BoundingSphereOverride.x\n      value: NaN\n      objectReference: {fileID: 0}\n').encode()
        self.write_scene(self.scene.replace(b'    m_RemovedComponents: []',extra+b'    m_RemovedComponents: []'))
        row=self.extract()[1]['instances'][0];self.assertEqual('NaN',row['modifications'][-1]['sourceValue']);self.assertIs(False,row['nativeOverridesReconciled'])
    def test_unparsed_modification_rejects_instead_of_being_dropped(self):
        self.write_scene(self.scene.replace(b'objectReference: {fileID: 0}',b'objectReference: unsupported'))
        with self.assertRaisesRegex(RuntimeError,'Unparsed original modification'):self.extract()
    def test_prefab_root_missing_or_ambiguous_rejects(self):
        for data in (b'--- !u!1 &1\nGameObject:\n  m_Name: missing\n',self.prefab+self.prefab.replace(b'&1\n',b'&2\n').replace(b'&4\n',b'&5\n')):
            self.entries[self.path]=(data,self.entries[self.path][1],'')
            with self.assertRaises(RuntimeError):self.extract()
    def test_extraction_does_not_create_intake_assets_or_modify_vendor_sources(self):
        self.extract();self.assertFalse(self.destination.exists());self.assertEqual(self.scene,self.scene_path.read_bytes());self.assertEqual(self.prefab,self.entries[self.path][0])
    def test_explicit_subsets_keep_canonical_order_and_do_not_extract_other_compositions(self):
        def fake(spec,*args):return [self.path],dict(name=spec['name'],sourceScene='OWNED/Fixture'),b'fixture source',b'fixture meta'
        with patch.object(art,'extract_composition',side_effect=fake)as extraction:
            _,layouts,_=art.owned_architecture_compositions(self.entries,self.index,self.owned,self.destination,self.root,['fishing-warehouse','fortified-quarter'])
            self.assertEqual(['fortified-quarter','fishing-warehouse'],[r['compositionKey']for r in layouts]);self.assertEqual(2,extraction.call_count)
    def test_unknown_empty_or_duplicate_composition_selection_rejects(self):
        for keys in ([],['unreviewed'],['fortified-quarter','fortified-quarter']):
            with self.subTest(keys=keys),self.assertRaises(RuntimeError):art.owned_architecture_compositions(self.entries,self.index,self.owned,self.destination,self.root,keys)


if __name__=='__main__':unittest.main()
