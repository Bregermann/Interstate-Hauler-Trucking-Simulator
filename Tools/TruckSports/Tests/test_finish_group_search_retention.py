"""Synthetic bounded search provenance tests, never native geometry approval."""
import copy
import hashlib
import math
from pathlib import Path
import sys
import tempfile
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain

class _AuthoredGroupSearchFixture:
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);self.clean=Path(temp.name)
        self.definition='Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset';self.put(self.definition,b'Fixture immutable INPUT definition');self.put(self.definition+'.meta',b'fileFormatVersion: 2\nguid: 11111111111111111111111111111111\n')
        self.prior={p:self.sha(p)for p in (self.definition,self.definition+'.meta')};self.ridge=dict(inputDefinition=self.definition,inputDefinitionGuid='1'*32,inputDefinitionSha256=self.sha(self.definition),inputDefinitionMetaSha256=self.sha(self.definition+'.meta'))
    def put(self,p,data):q=self.clean/p;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data)
    def sha(self,p):return hashlib.sha256((self.clean/p).read_bytes()).hexdigest()
    @staticmethod
    def vector(values):return dict(zip('xyz',values))
    @classmethod
    def bounds(cls,center,extent):return dict(m_Center=cls.vector(center),m_Extent=cls.vector(extent))
    def group(self,index,trial):
        plan=retain.FINISH_SCHEMA3_GROUPS[index];name,region,layout,scale,yaw=plan;hint=retain.FINISH_SCHEMA3_GROUP_HINTS[index];attempts=[]
        for i in range(trial+1):
            o=i//960;c=i%960;parcel=c>=192;r=math.sqrt(55**2+(128**2-55**2)*((c-192)/767))if parcel else 0 if c==0 else 4*math.sqrt(c)
            attempts.append(dict(trial=i,orientationIndex=o,parentYaw=yaw+o*90,phase=('WHOLE_PARENT_QUARTER_TURN_'if o else'')+('SAME_VILLAGE_55_TO_128M_PARCEL'if parcel else'ORIGINAL_NEAR_HINT_192'),distanceFromHint=r,refinementSeedTrial=-1,refinementOffset=self.vector((0,0,0)),rootPosition=self.vector((hint[0]+r,5,hint[2])),bounds=self.bounds((hint[0]+r,6,hint[2]),(3,2,3)),rejectedReason=''if i==trial else'ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE'))
        radius=attempts[-1]['distanceFromHint'];angle=math.radians(yaw+(trial//960)*90)/2
        return plan,dict(self.ridge,name=name,region=region,trial=trial,naturalMode=layout==1,solidColliders=5,position=self.vector((hint[0]+radius,5,hint[2])),scale=self.vector((scale,)*3),rotation=dict(zip('xyzw',(0,math.sin(angle),0,math.cos(angle)))),hint=self.vector(hint),bounds=self.bounds((hint[0]+radius,6,hint[2]),(3,2,3)),regionBounds=self.bounds(hint,(400,200,400)),trials=attempts,refinementSeeds=[])
    def check(self,plan,group):return retain._finish_schema3_group_search(self.clean,self.prior,group,plan,self.ridge)

class AuthoredGroupSearchRetentionTests(_AuthoredGroupSearchFixture,unittest.TestCase):
    def test_all_five_original_searches_accept(self):
        for index in range(5):
            plan,group=self.group(index,191)
            with self.subTest(index=index):self.assertEqual(0,self.check(plan,group))
    def test_only_two_village_courts_accept_parcel_and_whole_parent_quarter_turn(self):
        for index in (0,1):
            for trial in (192,959,960,1152,1919):
                plan,group=self.group(index,trial)
                with self.subTest(index=index,trial=trial):self.assertEqual(trial//960,self.check(plan,group))
    def test_harbor_and_both_natural_groups_cannot_gain_extended_trials(self):
        for index in (2,3,4):
            for trial in (192,960):
                plan,group=self.group(index,trial)
                with self.subTest(index=index,trial=trial),self.assertRaises(AssertionError):self.check(plan,group)
    def test_village_trial_limits_remain_bounded(self):
        for trial in (-1,1920,2880):
            plan,group=self.group(0,0);group['trial']=trial
            with self.subTest(trial=trial),self.assertRaises(AssertionError):self.check(plan,group)
    def test_natural_classification_and_actual_collision_required(self):
        for index in range(5):
            plan,original=self.group(index,0)
            for key,value in [('naturalMode',not original['naturalMode']),('naturalMode',1),('solidColliders',0),('solidColliders',True)]:
                group=copy.deepcopy(original);group[key]=value
                with self.subTest(index=index,key=key),self.assertRaises(AssertionError):self.check(plan,group)
    def test_group_and_ridge_input_definition_tuples_must_match(self):
        for key in self.ridge:
            plan,group=self.group(0,0);group[key]='Assets/Unknown.asset'if key=='inputDefinition'else'0'*len(group[key])
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check(plan,group)
    def test_input_definition_body_and_meta_drift_reject(self):
        for path in self.prior:
            original=(self.clean/path).read_bytes();self.put(path,original+b' drift');plan,group=self.group(0,0)
            with self.subTest(path=path),self.assertRaises(AssertionError):self.check(plan,group)
            self.put(path,original)
    def test_all_trial_indices_phases_parent_yaws_and_distances_are_exact(self):
        plan,original=self.group(1,1152)
        for key,value in [('trial',0),('orientationIndex',0),('parentYaw',270),('phase','GENERIC_1920_TRIAL_WAIVER'),('distanceFromHint',128),('distanceFromHint',float('nan'))]:
            group=copy.deepcopy(original);group['trials'][-1][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check(plan,group)
    def test_reordered_duplicate_and_missing_trial_history_reject(self):
        plan,original=self.group(0,192)
        for rows in (original['trials'][:-1],original['trials'][::-1],original['trials'][:-1]+[original['trials'][0]]):
            group=copy.deepcopy(original);group['trials']=rows
            with self.assertRaises(AssertionError):self.check(plan,group)
    def test_unknown_failed_trials_and_premature_acceptance_reject(self):
        plan,original=self.group(0,192)
        for index,reason in [(0,None),(1,''),(20,'UNKNOWN_RELAXED_CLEARANCE'),(192,'ACTUAL_WING_CLEARANCE')]:
            group=copy.deepcopy(original);group['trials'][index]['rejectedReason']=reason
            with self.subTest(index=index,reason=reason),self.assertRaises(AssertionError):self.check(plan,group)
    def test_final_rotation_can_only_be_the_recorded_whole_parent_turn(self):
        plan,group=self.group(0,1152);angle=math.radians(270)/2;group['rotation']=dict(zip('xyzw',(0,math.sin(angle),0,math.cos(angle))))
        with self.assertRaises(AssertionError):self.check(plan,group)
    def test_extended_parcel_cannot_leave_original_input_region(self):
        plan,group=self.group(0,959);group['regionBounds']=self.bounds(retain.FINISH_SCHEMA3_GROUP_HINTS[0],(100,200,100))
        with self.assertRaises(AssertionError):self.check(plan,group)
    def test_original_hint_scale_and_actual_saved_distance_cannot_drift(self):
        plan,original=self.group(0,1152)
        for key,value in [('hint',self.vector((0,0,0))),('scale',self.vector((2,2,2))),('bounds',self.bounds((0,0,0),(3,2,3)))]:
            group=copy.deepcopy(original);group[key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check(plan,group)

class AuthoredGroupLocalRefinementRetentionTests(_AuthoredGroupSearchFixture,unittest.TestCase):
    def refined(self,last_offset=0,two_seeds=False):
        plan,group=self.group(0,1919);group['trials'][-1]['rejectedReason']='ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE';ids=[42,1200]if two_seeds else[42]
        for number in ids:
            witness=group['trials'][number];center=witness['bounds']['m_Center'];seed=dict(trial=number,orientation=number//960,center=self.vector((center['x'],0,center['z'])));group['refinementSeeds'].append(seed)
            witness.update(rejectedReason='ACTUAL_OTHER_ART_COLLIDER_OVERLAP',samples=49,notDrySamples=0,underFreeboardSamples=0,range=.4,floorPoints=[dict(point=self.vector((center['x']+x*.1,5,center['z']+z*.1)),classifiedDry=True,freeboardPass=True)for x in range(7)for z in range(7)],blocker=dict(kind='NATIVE_OTHER_ART_COLLIDER',path='Fixture original peer collider',bounds=self.bounds((center['x'],6,center['z']),(1,2,1))))
        for j in range(last_offset+1):
            seed=group['refinementSeeds'][j//196];offset=retain.FINISH_SCHEMA3_REFINEMENT_OFFSETS[j%196];center=seed['center'];position=(center['x']+offset[0],5,center['z']+offset[2]);hint=retain.FINISH_SCHEMA3_GROUP_HINTS[0];distance=math.hypot(position[0]-hint[0],position[2]-hint[2])
            group['trials'].append(dict(trial=1920+j,orientationIndex=seed['orientation'],parentYaw=90+seed['orientation']*90,phase='NATIVE_FLAT_PEER_PARCEL_HALF_METRE_LOCAL_REFINEMENT',distanceFromHint=distance,refinementSeedTrial=seed['trial'],refinementOffset=self.vector(offset),rootPosition=self.vector(position),bounds=self.bounds((position[0],6,position[2]),(3,2,3)),rejectedReason=''if j==last_offset else'ACTUAL_OTHER_ART_COLLIDER_OVERLAP'))
        last=group['trials'][-1];group.update(trial=last['trial'],position=last['rootPosition'],bounds=last['bounds']);angle=math.radians(last['parentYaw'])/2;group['rotation']=dict(zip('xyzw',(0,math.sin(angle),0,math.cos(angle))))
        return plan,group
    def test_all_196_offsets_are_nonzero_exact_half_metre_bounded_and_ordered(self):
        offsets=retain.FINISH_SCHEMA3_REFINEMENT_OFFSETS;self.assertEqual(196,len(offsets));self.assertEqual(196,len(set(offsets)));self.assertTrue(all(y==0 and 0<x*x+z*z<=16 and x*2==int(x*2)and z*2==int(z*2)for x,y,z in offsets));self.assertEqual(offsets,tuple(sorted(offsets,key=lambda p:(p[0]*p[0]+p[2]*p[2],p[0],p[2]))))
    def test_native_flat_peer_seed_first_and_last_offset_accept(self):
        for last in (0,195,196,391):
            plan,group=self.refined(last,two_seeds=last>=196)
            with self.subTest(last=last):self.assertEqual(0 if last<196 else 1,self.check(plan,group))
    def test_earlier_seed_requires_all_49_dry_freeboard_points_and_flat_range(self):
        plan,original=self.refined()
        for key,value in [('samples',48),('notDrySamples',1),('underFreeboardSamples',1),('range',.6001),('range',float('nan')),('rejectedReason','ACTUAL_WING_CLEARANCE')]:
            group=copy.deepcopy(original);group['trials'][42][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check(plan,group)
        for change in ('wet','missing','duplicate'):
            group=copy.deepcopy(original);floor=group['trials'][42]['floorPoints']
            if change=='wet':floor[0]['classifiedDry']=False
            elif change=='missing':floor.pop()
            else:floor[-1]=floor[0]
            with self.subTest(change=change),self.assertRaises(AssertionError):self.check(plan,group)
    def test_seed_requires_actual_named_peer_not_any_other_failure(self):
        plan,original=self.refined()
        for blocker in (None,dict(kind='ROAD',path='Fake',bounds=self.bounds((0,0,0),(1,1,1))),dict(kind='NATIVE_OTHER_ART_COLLIDER',path='',bounds=self.bounds((0,0,0),(1,1,1)))):
            group=copy.deepcopy(original);group['trials'][42]['blocker']=blocker
            with self.assertRaises(AssertionError):self.check(plan,group)
    def test_missing_reordered_duplicate_and_more_than_16_seeds_reject(self):
        plan,original=self.refined(196,True)
        for seeds in ([],original['refinementSeeds'][::-1],[original['refinementSeeds'][0]]*2,[original['refinementSeeds'][0]]*17):
            group=copy.deepcopy(original);group['refinementSeeds']=seeds
            with self.assertRaises(AssertionError):self.check(plan,group)
    def test_arbitrary_seed_center_trial_or_orientation_reject(self):
        plan,original=self.refined()
        for key,value in [('trial',1920),('trial',43),('orientation',1),('center',self.vector((0,0,0))),('center',self.vector((-500,1,-280)))]:
            group=copy.deepcopy(original);group['refinementSeeds'][0][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check(plan,group)
    def test_wrong_seed_offset_order_zero_large_offset_and_phase_reject(self):
        plan,original=self.refined()
        for key,value in [('refinementSeedTrial',43),('refinementOffset',self.vector((0,0,0))),('refinementOffset',self.vector((.5,0,0))),('refinementOffset',self.vector((4.5,0,0))),('phase','GENERIC_RANDOM_LOCAL_SEARCH')]:
            group=copy.deepcopy(original);group['trials'][-1][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check(plan,group)
    def test_actual_refined_candidate_must_match_measured_seed_plus_offset(self):
        plan,group=self.refined();group['trials'][-1]['bounds']['m_Center']['x']+=.1
        with self.assertRaises(AssertionError):self.check(plan,group)
    def test_unseeded_later_trial_and_nonvillage_refinement_reject(self):
        plan,group=self.refined();group['refinementSeeds']=[]
        with self.assertRaises(AssertionError):self.check(plan,group)
        for index in (2,3,4):
            other,_=self.group(index,0)
            with self.subTest(index=index),self.assertRaises(AssertionError):self.check(other,group)
    def test_native_base_trials_cannot_claim_refinement(self):
        plan,group=self.group(0,1919);group['trials'][100]['refinementSeedTrial']=42
        with self.assertRaises(AssertionError):self.check(plan,group)

if __name__=='__main__':unittest.main()
