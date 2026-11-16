from pathlib import Path
import hashlib
import json
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import RunDragonKingdomValidation as dragon


class _MountPrerequisiteFixture:
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='SportsMountPrerequisite-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.run = self.root / 'Logs/TruckSports/DragonKingdom/ActualMountedRun'
        self.run.mkdir(parents=True)
        self.xml = self.run / 'tests.xml'
        self.result('Passed')
        self.facts = []
        for name in ('Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity',
                     'Assets/TruckDragonRiders/Scripts/DragonRiderSaddle.cs',
                     'Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset',
                     'Assets/TruckDragonRiders/Tests/Native/DragonRiderSaddleNativeTests.cs',
                     'Assets/TruckDragonRiders/Tests/Native/DragonRiderSaddleFrameObserver.cs',
                     'Assets/TruckDragonRiders/Tests/Native/LWS.TruckDragonRiders.NativeTests.asmdef',
                     'Assets/TruckSports/Scripts/SportsBroadcastFraming.cs',
                     'Assets/TruckSports/Scripts/SportsProbeInputIsolation.cs'):
            source = self.root / name
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(b'actual frozen input fixture')
            self.facts.append(dict(path=name, sha256=hashlib.sha256(source.read_bytes()).hexdigest()))
        self.manifest()

    def result(self, status):
        self.xml.write_text('<test-run><test-case fullname="' + dragon.MOUNT_TEST + '" result="' + status + '"/></test-run>')

    def manifest(self):
        (self.run / 'input-manifest.json').write_text(json.dumps(self.facts))


class ActualMountPrerequisiteTests(_MountPrerequisiteFixture,unittest.TestCase):
    def test_exact_passed_native_case_with_original_dependencies_authorizes_weather(self):
        receipt = dragon.require_mount_evidence(self.run, self.root)
        self.assertEqual(8, receipt['checkedDependencies'])
        self.assertEqual(dragon.MOUNT_TEST, receipt['test'])

    def test_failed_or_skipped_actual_mount_does_not_authorize_weather(self):
        for status in ('Failed', 'Skipped', 'Inconclusive'):
            with self.subTest(status=status):
                self.result(status)
                with self.assertRaisesRegex(ValueError, 'has not passed'):
                    dragon.require_mount_evidence(self.run, self.root)

    def test_unrelated_pass_is_not_mount_acceptance(self):
        self.xml.write_text('<test-run><test-case fullname="Unrelated.Test" result="Passed"/></test-run>')
        with self.assertRaisesRegex(ValueError, 'has not passed'):
            dragon.require_mount_evidence(self.run, self.root)

    def test_legacy_ai_case_cannot_replace_current_initial_human_proof(self):
        legacy = 'DragonRiderSaddleNativeTests.ActualCalibratedSaddlesKeepOriginalNwhCrewsAboveBackWithBoundedLiveBankAndVisibleBodyAcrossRematch'
        self.xml.write_text('<test-run><test-case fullname="' + legacy + '" result="Passed"/></test-run>')
        with self.assertRaisesRegex(ValueError, 'has not passed'):
            dragon.require_mount_evidence(self.run, self.root)

    def test_changed_software_input_or_shared_camera_invalidates_previous_proof(self):
        for name in ('Assets/TruckSports/Scripts/SportsProbeInputIsolation.cs',
                     'Assets/TruckSports/Scripts/SportsBroadcastFraming.cs',
                     'Assets/TruckDragonRiders/Tests/Native/LWS.TruckDragonRiders.NativeTests.asmdef'):
            source = self.root / name
            original = source.read_bytes()
            source.write_bytes(b'changed dependency')
            with self.assertRaisesRegex(ValueError, 'is stale'):
                dragon.require_mount_evidence(self.run, self.root)
            source.write_bytes(original)

    def test_changed_original_scene_invalidates_previous_acceptance(self):
        (self.root / self.facts[0]['path']).write_bytes(b'new saved scene')
        with self.assertRaisesRegex(ValueError, 'is stale'):
            dragon.require_mount_evidence(self.run, self.root)

    def test_incomplete_manifest_does_not_authorize_weather(self):
        self.facts.pop()
        self.manifest()
        with self.assertRaisesRegex(ValueError, 'lacks original'):
            dragon.require_mount_evidence(self.run, self.root)

    def test_external_receipt_cannot_authorize_weather(self):
        with self.assertRaisesRegex(ValueError, 'retained local'):
            dragon.require_mount_evidence(self.root / 'ExternalReceipt', self.root)


class SavedMotionProfilePrerequisiteTests(_MountPrerequisiteFixture,unittest.TestCase):
    def put(self,path,data):
        p=self.root/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
    def sync(self):
        names={fact['path'] for fact in self.facts};paths=list((self.root/'Assets/TruckDragonRiders/Animations').rglob('*'))+[self.root/'Assets/TruckDragonRiders/Animations.meta']
        for path in paths:
            if path.is_file():
                relative=path.relative_to(self.root).as_posix();fact=dict(path=relative,sha256=hashlib.sha256(path.read_bytes()).hexdigest())
                if relative in names:self.facts[next(i for i,f in enumerate(self.facts)if f['path']==relative)]=fact
                else:self.facts.append(fact);names.add(relative)
        self.manifest()
    def bundles(self,count=3,nested=False):
        self.put('Assets/TruckDragonRiders/Animations.meta',b'fileFormatVersion: 2\nguid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n');self.profiles=[]
        for species_index,species in enumerate(('dragon-inferno','dragon-fire')):
            prefix='Assets/TruckDragonRiders/Animations/'+species+'/Normal Flight V1';controller=prefix+'.overrideController';owned=[];guids=[]
            self.put('Assets/TruckDragonRiders/Animations/'+species+'.meta',('fileFormatVersion: 2\nguid: '+f'{9000+species_index:032x}'+'\n').encode())
            for i in range(count):
                path=prefix+' Pose '+str(i)+'.anim';guid=f'{10000+species_index*100+i:032x}';self.put(path,b'%YAML 1.1\nAnimationClip: {fixture: 1}\n');self.put(path+'.meta',('fileFormatVersion: 2\nguid: '+guid+'\n').encode());owned.extend([path,path+'.meta']);guids.append(guid)
            if nested:
                derivative=prefix+' Transition.overrideController';guid=f'{11000+species_index:032x}';self.put(derivative,('m_Clip: {guid: '+guids[-1]+'}\n').encode());self.put(derivative+'.meta',('fileFormatVersion: 2\nguid: '+guid+'\n').encode());owned.extend([derivative,derivative+'.meta']);guids[-1]=guid
            self.put(controller,('m_Clips:\n'+''.join('  - {guid: '+guid+'}\n'for guid in guids)).encode());self.put(controller+'.meta',('fileFormatVersion: 2\nguid: '+f'{12000+species_index:032x}'+'\n').encode());owned.extend([controller,controller+'.meta'])
            metadata=prefix+' Recipe.json';data=dict(version=1,species=species,controller=controller,recipe='Synthetic native-profile prerequisite fixture',ownedFiles=[dict(path=path,sha256=hashlib.sha256((self.root/path).read_bytes()).hexdigest().upper())for path in owned]);self.put(metadata,json.dumps(data).encode());self.put(metadata+'.meta',('fileFormatVersion: 2\nguid: '+f'{13000+species_index:032x}'+'\n').encode());self.profiles.append((metadata,data))
        self.sync()
    def check(self):return dragon.require_mount_evidence(self.run,self.root)
    def test_current_owned_controllers_clips_provenance_and_metas_authorize_only_exact_frozen_profile(self):
        self.bundles();self.assertGreater(self.check()['checkedDependencies'],8)
    def test_no_fixed_five_clip_limit_and_nested_owned_controller_dependencies_supported(self):
        self.bundles(count=8,nested=True);self.check()
    def test_changed_owned_body_metadata_and_recipe_invalidate_old_actual_mount(self):
        self.bundles();targets=[self.profiles[0][0],self.profiles[0][0]+'.meta',self.profiles[0][1]['controller'],self.profiles[0][1]['controller']+'.meta',self.profiles[0][1]['ownedFiles'][0]['path']]
        for path in targets:
            original=(self.root/path).read_bytes();self.put(path,original+b' changed')
            with self.subTest(path=path),self.assertRaises(ValueError):self.check()
            self.put(path,original)
    def test_missing_clip_or_unfrozen_clip_meta_reject(self):
        self.bundles();path=self.profiles[0][1]['ownedFiles'][0]['path'];original=(self.root/path).read_bytes();(self.root/path).unlink()
        with self.assertRaises(ValueError):self.check()
        self.put(path,original);self.facts=[f for f in self.facts if f['path']!=path+'.meta'];self.manifest()
        with self.assertRaisesRegex(ValueError,'lacks original'):self.check()
    def test_source_metadata_and_declared_owned_hash_mismatch_reject(self):
        self.bundles();metadata,data=self.profiles[0];original=json.loads(json.dumps(data))
        for key,value in [('version',2),('species','unknown-species'),('controller','Assets/Unknown.overrideController')]:
            broken=json.loads(json.dumps(original));broken[key]=value;self.put(metadata,json.dumps(broken).encode());self.sync()
            with self.subTest(key=key),self.assertRaises(ValueError):self.check()
        broken=json.loads(json.dumps(original));broken['ownedFiles'][0]['sha256']='0'*64;self.put(metadata,json.dumps(broken).encode());self.sync()
        with self.assertRaisesRegex(ValueError,'owned hash mismatches'):self.check()
    def test_extra_orphan_owned_motion_is_not_an_actual_controller_dependency(self):
        self.bundles();metadata,data=self.profiles[0];path=data['controller'].replace('.overrideController',' Orphan.anim');self.put(path,b'Unused fixture clip');self.put(path+'.meta',b'fileFormatVersion: 2\nguid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\n');data['ownedFiles'].extend(dict(path=p,sha256=hashlib.sha256((self.root/p).read_bytes()).hexdigest())for p in (path,path+'.meta'));self.put(metadata,json.dumps(data).encode());self.sync()
        with self.assertRaisesRegex(ValueError,'controller dependency closure'):self.check()
    def test_duplicate_owned_clip_guid_rejects(self):
        self.bundles();metadata,data=self.profiles[0];first=data['ownedFiles'][0]['path'];second=data['ownedFiles'][2]['path'];self.put(second+'.meta',(self.root/(first+'.meta')).read_bytes());self.sync()
        with self.assertRaises(ValueError):self.check()
    def test_owned_controller_cannot_drop_clip_references_even_when_frozen_hashes_match(self):
        self.bundles();metadata,data=self.profiles[0];controller=data['controller'];self.put(controller,b'm_Clips: []\n')
        for row in data['ownedFiles']:
            if row['path']==controller:row['sha256']=hashlib.sha256((self.root/controller).read_bytes()).hexdigest()
        self.put(metadata,json.dumps(data).encode());self.sync()
        with self.assertRaisesRegex(ValueError,'controller dependency closure'):self.check()
    def test_unlisted_extra_owned_profile_asset_is_not_silently_accepted(self):
        self.bundles();path=self.profiles[0][1]['controller'].replace('.overrideController',' Extra.anim');self.put(path,b'Unknown new owned profile member');self.put(path+'.meta',b'fileFormatVersion: 2\nguid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\n');self.sync()
        with self.assertRaisesRegex(ValueError,'Missing or extra'):self.check()
    def test_partial_species_bundle_rejects(self):
        self.bundles();(self.root/self.profiles[1][0]).unlink()
        with self.assertRaisesRegex(ValueError,'Incomplete saved'):self.check()
    def test_no_new_bundle_is_needed_to_protect_existing_saved_hover_animation(self):
        path='Assets/TruckDragonRiders/Animations/dragon-fire/Mounted Hover.anim';self.put(path,b'Existing mounted motion');self.put(path+'.meta',b'fileFormatVersion: 2\nguid: cccccccccccccccccccccccccccccccc\n');self.put('Assets/TruckDragonRiders/Animations.meta',b'fileFormatVersion: 2\nguid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n');self.sync();self.check();self.put(path,b'Changed hover')
        with self.assertRaisesRegex(ValueError,'is stale'):self.check()
    def test_new_profile_cannot_be_absent_from_the_original_passed_manifest(self):
        self.bundles();self.facts=self.facts[:8];self.manifest()
        with self.assertRaisesRegex(ValueError,'lacks original'):self.check()
    def test_unknown_external_owned_paths_and_duplicate_frozen_entries_reject(self):
        self.bundles();metadata,data=self.profiles[0];data['ownedFiles'][0]['path']='../External.anim';self.put(metadata,json.dumps(data).encode());self.sync()
        with self.assertRaises(ValueError):self.check()
        self.put(metadata,json.dumps(dict(data,ownedFiles=data['ownedFiles'][1:])).encode());self.sync()
        with self.assertRaises(ValueError):self.check()
    def test_duplicate_mounted_manifest_dependencies_reject(self):
        self.bundles();self.facts.append(self.facts[-1]);self.manifest()
        with self.assertRaisesRegex(ValueError,'Duplicate mounted'):self.check()

if __name__ == '__main__':
    unittest.main()
