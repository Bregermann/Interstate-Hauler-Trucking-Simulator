"""Synthetic bank provenance/ tamper guards; not native physical acceptance."""
import copy
import math
from pathlib import Path
import sys
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import PreserveDragonKingdomAuthoring as retain

class _SameBankFixture:
    @staticmethod
    def vector(v):return dict(zip('xyz',v))
    @classmethod
    def bounds(cls,p,e):return dict(m_Center=cls.vector(p),m_Extent=cls.vector(e))
    def setUp(self):self.bank=self.fixture()
    def fixture(self,selected=None):
        selected=selected or {};parts=[];rows=[];source=retain.FINISH_PRIVATE+'Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Cliffs/'
        for index in range(24):
            trial=selected.get(index,0);hint,point,radius=retain._finish_bank_candidate(index,trial);name='Owned source village bank stone '+str(index+1);height=6+index%3;bounds=self.bounds((point[0],10,point[2]),(3,height/2,4));phase='ORIGINAL_96'if trial<96 else'SAME_BANK_ALONG_20_TO_48M_384'
            parts.append(dict(name=name,region='village',source=source+'RockFormation_0'+str(index%4+1)+'.prefab',accepted=True,failure='',trial=trial,modelHeight=height,maximumAllowedSupportRange=height*.28,primaryGeometryMethod='EXISTING_STATIC_MODEL_BOUNDS_WITH_PRIMARY_MESH_EXPOSURE',hint=self.vector(hint),bounds=bounds,after=self.vector((point[0]+2,10,point[2]+1)),candidateDistanceFromHint=math.hypot(point[0]-hint[0],point[2]-hint[2]),candidatePhase=phase,supportHeightRange=.4))
            for number in range(trial+1):
                _,p,r=retain._finish_bank_candidate(index,number);last=number==trial;rows.append(dict(name=name,trial=number,phase='ORIGINAL_96'if number<96 else'SAME_BANK_ALONG_20_TO_48M_384',point=self.vector(p),oppositeBankTrial=-1,parcelHint=self.vector(hint),refinementSeedTrial=-1,refinementOffset=self.vector((0,0,0)),searchRadius=r,allowedRange=height*.28,halfFootprint=1.8,supportFound=last,boundsMeasured=last,bounds=self.bounds((p[0],10,p[2]),(3,height/2,4)),range=.4 if last else 0,minimumCreekDistance=1 if last else 0,rejectedReason=''if last else'ACTUAL_TERRAIN_SUPPORT_RANGE'))
        return dict(name=retain.FINISH_SCHEMA3_EXTRA[2],scope='SOURCE_OWNED_VISIBLE_STONE_EMBEDDED_AT_RETAINED_TWELVE_METRE_URBAN_BANK_WITH_UNCHANGED_CREEK_TERRAIN_ROADS',stones=24,acceptedParts=24,rejectedParts=0,failures=[],passed=True,creekCoreClear=True,roadsObjectivesAndWingsClear=True,actualMeshSupport=True,parts=parts,trials=rows,refinementSeeds=[],alternateParcels=[],originalAcceptedBeforeAlternate=24)
    def check(self):return retain._finish_schema3_bank(self.bank)

class SameBankPlacementRetentionTests(_SameBankFixture,unittest.TestCase):
    def test_original24_and_exact_extended_boundary_trials_accept(self):
        self.check();self.bank=self.fixture({0:95,1:96,2:479});self.check()
    def test_actual_static_height_base_support_method_and_sources_are_preserved(self):
        original=copy.deepcopy(self.bank)
        for key,value in [('name','Other bank stone'),('region','harbor'),('source','Assets/Unknown.prefab'),('accepted',False),('failure','Unresolved'),('modelHeight',6.01),('maximumAllowedSupportRange',2),('primaryGeometryMethod','PADDED_HEIGHT_WAIVER')]:
            self.bank=copy.deepcopy(original);self.bank['parts'][0][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
    def test_partial_rejected_missing_or_extra_stones_never_accept(self):
        original=copy.deepcopy(self.bank)
        for key,value in [('acceptedParts',23),('rejectedParts',1),('failures',['Unresolved']),('parts',original['parts'][:-1]),('parts',original['parts']+[original['parts'][0]]),('passed',False),('creekCoreClear',False)]:
            self.bank=copy.deepcopy(original);self.bank[key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
    def test_only_exact96_plus384_search_limits_are_supported(self):
        for trial in (-1,480,1000):
            self.bank=self.fixture();self.bank['parts'][0]['trial']=trial
            with self.subTest(trial=trial),self.assertRaises(AssertionError):self.check()
    def test_bank_hints_and_exact_trial_point_radius_phase_cannot_drift(self):
        original=self.fixture({0:100})
        for key,value in [('point',self.vector((0,0,0))),('searchRadius',48),('phase','UNBOUNDED_PLACEMENT'),('trial',0),('name','Wrong stone'),('allowedRange',2),('halfFootprint',5)]:
            self.bank=copy.deepcopy(original);self.bank['trials'][100][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
    def test_final_native_support_creek_and_range_are_required(self):
        for key,value in [('supportFound',False),('boundsMeasured',False),('range',2),('minimumCreekDistance',.249),('rejectedReason','ACTUAL_OTHER_ART_COLLIDER_OVERLAP')]:
            self.bank=self.fixture();self.bank['trials'][0][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
    def test_earlier_failures_must_use_the_actual_unrelaxed_gate_taxonomy(self):
        for value in ('',None,'UNKNOWN_OR_RELAXED_SUPPORT'):
            self.bank=self.fixture({0:1});self.bank['trials'][0]['rejectedReason']=value
            with self.subTest(value=value),self.assertRaises(AssertionError):self.check()
    def test_missing_duplicate_reordered_and_unknown_trial_rows_reject(self):
        original=self.fixture({0:2})
        for rows in (original['trials'][:-1],original['trials']+[original['trials'][-1]],original['trials'][::-1],[original['trials'][0]]+original['trials']):
            self.bank=copy.deepcopy(original);self.bank['trials']=rows
            with self.assertRaises(AssertionError):self.check()
    def test_pose_provenance_uses_actual_model_bounds_center_not_prefab_pivot(self):
        self.check();self.bank['parts'][0]['bounds']['m_Center']['x']+=.02
        with self.assertRaises(AssertionError):self.check()
    def test_nan_and_unknown_support_method_do_not_bypass_range_gate(self):
        self.bank['parts'][0]['maximumAllowedSupportRange']=float('nan')
        with self.assertRaises(AssertionError):self.check()
    def test_original_source_recipe_keeps_fixed_float32_endpoint_values(self):
        # Fixed float32 source-authoring points, independent of temporary fixtures.
        hint,point,radius=retain._finish_bank_candidate(0,0)
        self.assertAlmostEqual(-682.173095703125,hint[0]);self.assertAlmostEqual(-176.08653259277344,hint[2]);self.assertEqual(hint,point);self.assertEqual(0,radius)
        hint,point,radius=retain._finish_bank_candidate(23,479);self.assertEqual(48,radius);self.assertAlmostEqual(-628.6844482421875,point[0]);self.assertAlmostEqual(-506.4615478515625,point[2])

class NearRoadMarginBankRefinementTests(_SameBankFixture,unittest.TestCase):
    def refined(self,last_offset=0):
        bank=self.fixture({0:479});part=bank['parts'][0];name=part['name'];base=bank['trials'][:480];base[-1].update(rejectedReason='ACTUAL_TERRAIN_SUPPORT_RANGE',supportFound=False,boundsMeasured=False,range=0)
        for number,deficit in [(7,.4),(12,.2)]:
            row=base[number];row.update(rejectedReason='ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE',supportFound=True,boundsMeasured=True,range=.4,minimumCreekDistance=1,blocker=dict(kind='ROAD_GRAPH_ENVELOPE',path='Fixture original graph edge',margin=14,distance=14-deficit))
        bank['refinementSeeds']=[dict(name=name,trial=i,point=copy.deepcopy(base[i]['point']))for i in (12,7)]
        additions=[];hint=retain._finish_bank_candidate(0,0)[0]
        for j in range(last_offset+1):
            seed=bank['refinementSeeds'][j//196];delta=retain.FINISH_SCHEMA3_REFINEMENT_OFFSETS[j%196];p=tuple(seed['point'][a]+d for a,d in zip('xyz',delta));inside=retain._finish_bank_inside_fringe(0,hint,p);last=j==last_offset;_,_,radius=retain._finish_bank_candidate(0,seed['trial']);row=copy.deepcopy(base[0]);row.update(trial=480+j,phase='SAME_BANK_NEAR_MARGIN_HALF_METRE_FOUR_METRE_FRINGE',point=self.vector(p),searchRadius=radius,refinementSeedTrial=seed['trial'],refinementOffset=self.vector(delta),supportFound=last and inside,boundsMeasured=last and inside,range=.4 if last else 0,minimumCreekDistance=1 if last else 0,bounds=self.bounds((p[0],10,p[2]),(3,3,4)),rejectedReason=''if last and inside else'ACTUAL_TERRAIN_SUPPORT_RANGE'if inside else'SAME_BANK_LOCAL_REFINEMENT_OUTSIDE_FOUR_METRE_FRINGE');additions.append(row)
        final=additions[-1];part.update(trial=final['trial'],candidatePhase=final['phase'],bounds=final['bounds'],candidateDistanceFromHint=math.hypot(final['point']['x']-hint[0],final['point']['z']-hint[2]))
        bank['trials']=base+additions+bank['trials'][480:];return bank
    def test_ranked_original_near_margin_seeds_and_ordered_local_offsets_accept(self):
        for last in (0,195,196):
            self.bank=self.refined(last)
            with self.subTest(last=last):self.check()
    def test_seed_order_is_smallest_deficit_then_trial_not_arbitrary(self):
        self.bank=self.refined();self.bank['refinementSeeds'].reverse()
        with self.assertRaises(AssertionError):self.check()
        self.bank=self.refined();self.bank['refinementSeeds']=[self.bank['refinementSeeds'][0]]*2
        with self.assertRaises(AssertionError):self.check()
    def test_large_or_zero_margin_deficits_and_non_graph_failures_cannot_seed(self):
        for changes in [dict(margin=14,distance=12.99),dict(margin=14,distance=14),dict(margin=0,distance=-.2),dict(kind='NATIVE_OTHER_ART_COLLIDER'),dict(path='')]:
            self.bank=self.refined();self.bank['trials'][12]['blocker'].update(changes)
            with self.subTest(changes=changes),self.assertRaises(AssertionError):self.check()
    def test_seed_needs_actual_flat_supported_creek_clear_source_query(self):
        for key,value in [('supportFound',False),('boundsMeasured',False),('range',2),('minimumCreekDistance',.249),('rejectedReason','ACTUAL_WING_CLEARANCE')]:
            self.bank=self.refined();self.bank['trials'][12][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
    def test_unknown_missing_and_more_than16_seeds_reject(self):
        for mode in ('unknown','missing','many'):
            self.bank=self.refined()
            if mode=='unknown':self.bank['refinementSeeds'][0]['name']='Unknown bank object'
            elif mode=='missing':self.bank['refinementSeeds']=[]
            else:self.bank['refinementSeeds']*=9
            with self.subTest(mode=mode),self.assertRaises(AssertionError):self.check()
    def test_refined_radius_remains_seed_search_radius_not_local_distance(self):
        self.bank=self.refined();self.bank['trials'][480]['searchRadius']=self.bank['parts'][0]['candidateDistanceFromHint']
        with self.assertRaises(AssertionError):self.check()
    def test_refined_seed_point_offset_phase_and_bounded_count_are_exact(self):
        for key,value in [('refinementSeedTrial',7),('refinementOffset',self.vector((0,0,0))),('refinementOffset',self.vector((4.5,0,0))),('phase','UNBOUNDED_LOCAL_SEARCH'),('point',self.vector((0,0,0)))]:
            self.bank=self.refined();self.bank['trials'][480][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
        self.bank=self.refined();self.bank['parts'][0]['trial']=3616
        with self.assertRaises(AssertionError):self.check()
    def test_seed_original_point_cannot_move_and_base_cannot_claim_refinement(self):
        self.bank=self.refined();self.bank['refinementSeeds'][0]['point']['x']+=.5
        with self.assertRaises(AssertionError):self.check()
        self.bank=self.fixture();self.bank['trials'][0]['refinementSeedTrial']=12
        with self.assertRaises(AssertionError):self.check()
    def test_local_outside_envelope_rejects_before_support_and_cannot_be_accepted(self):
        self.bank=self.refined(195)
        # A valid four-metre offset is inside the approved fringe; it
        # cannot fabricate an outside-fringe skip of its physical checks.
        self.bank['trials'][480]['rejectedReason']='SAME_BANK_LOCAL_REFINEMENT_OUTSIDE_FOUR_METRE_FRINGE'
        with self.assertRaises(AssertionError):self.check()
        h,_,_=retain._finish_bank_candidate(0,0);self.assertFalse(retain._finish_bank_inside_fringe(0,h,(h[0]+60,h[1],h[2])))
    def test_only_exact_four_metre_fringe_limits_are_supported(self):
        h,_,_=retain._finish_bank_candidate(0,0);a=(1/math.sqrt(5),0,-2/math.sqrt(5));out=(2/math.sqrt(5),0,1/math.sqrt(5))
        for longitudinal,outward,expected in [(52,0,True),(-52,0,True),(0,-4,True),(0,12,True),(52.01,0,False),(-52.01,0,False),(0,-4.01,False),(0,12.01,False)]:
            p=tuple(h[i]+a[i]*longitudinal+out[i]*outward for i in range(3))
            with self.subTest(longitudinal=longitudinal,outward=outward):self.assertEqual(expected,retain._finish_bank_inside_fringe(0,h,p))
    def test_previous_local_phase_is_not_silently_reinterpreted_as_fringe(self):
        self.bank=self.refined();self.bank['trials'][480]['phase']='SAME_BANK_NEAR_MARGIN_HALF_METRE_LOCAL_REFINEMENT'
        with self.assertRaises(AssertionError):self.check()

class OppositeSameStationBankRetentionTests(_SameBankFixture,unittest.TestCase):
    def alternate(self,indices=(14,16),radial=0):
        bank=self.fixture({index:479 for index in indices});bank['originalAcceptedBeforeAlternate']=24-len(indices);extra=[]
        for index in indices:
            part=bank['parts'][index];name=part['name'];original,_,_=retain._finish_bank_candidate(index,0);mirrored,_,_=retain._finish_bank_candidate(index,0,True);station,_,_,side=retain._finish_bank_frame(index)
            original_rows=[row for row in bank['trials']if row['name']==name]
            for row in original_rows:row.update(rejectedReason='ACTUAL_TERRAIN_SUPPORT_RANGE',supportFound=False,boundsMeasured=False,range=0)
            parcel=dict(name=name,originalHint=self.vector(original),alternateHint=self.vector(mirrored),station=self.vector(station),originalSide=side,firstTrial=480,attempts=radial+1);bank['alternateParcels'].append(parcel)
            for number in range(radial+1):
                _,point,radius=retain._finish_bank_candidate(index,number,True);last=number==radial;row=copy.deepcopy(original_rows[0]);row.update(trial=480+number,phase='OPPOSITE_BANK_SAME_STATION_0_TO_48M_480',point=self.vector(point),oppositeBankTrial=number,parcelHint=self.vector(mirrored),searchRadius=radius,supportFound=last,boundsMeasured=last,range=.4 if last else 0,minimumCreekDistance=1 if last else 0,rejectedReason=''if last else'ACTUAL_TERRAIN_SUPPORT_RANGE',bounds=self.bounds((point[0],10,point[2]),(3,part['modelHeight']/2,4)));extra.append(row)
            part.update(trial=480+radial,candidatePhase='OPPOSITE_BANK_SAME_STATION_0_TO_48M_480',bounds=row['bounds'],candidateDistanceFromHint=math.hypot(point[0]-original[0],point[2]-original[2]))
        bank['trials'].extend(extra);return bank
    def test_opposite_start_follows_every_ranked_local_seed_offset(self):
        self.bank=self.alternate((14,));name=self.bank['parts'][14]['name'];base=next(row for row in self.bank['trials']if row['name']==name and row['trial']==7);base.update(supportFound=True,boundsMeasured=True,range=.4,minimumCreekDistance=1,rejectedReason='ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE',blocker=dict(kind='ROAD_GRAPH_ENVELOPE',path='Fixture original graph edge',margin=14,distance=13.8));self.bank['refinementSeeds']=[dict(name=name,trial=7,point=copy.deepcopy(base['point']))];local=[]
        for j,delta in enumerate(retain.FINISH_SCHEMA3_REFINEMENT_OFFSETS):
            point=tuple(base['point'][a]+d for a,d in zip('xyz',delta));row=copy.deepcopy(base);row.update(trial=480+j,phase='SAME_BANK_NEAR_MARGIN_HALF_METRE_FOUR_METRE_FRINGE',point=self.vector(point),refinementSeedTrial=7,refinementOffset=self.vector(delta),supportFound=False,boundsMeasured=False,range=0,minimumCreekDistance=0,rejectedReason='ACTUAL_TERRAIN_SUPPORT_RANGE');local.append(row)
        cut=max(i for i,row in enumerate(self.bank['trials'])if row['name']==name and row['trial']<480)+1;self.bank['trials'][cut:cut]=local;opposite=self.bank['trials'][-1];opposite['trial']=676;self.bank['parts'][14]['trial']=676;self.bank['alternateParcels'][0]['firstTrial']=676;self.check()
        self.bank['alternateParcels'][0]['firstTrial']=480
        with self.assertRaises(AssertionError):self.check()
    def test_actual_exhausted_original_instances_can_mirror_same_station(self):
        for indices in [(0,),(12,),(14,),(16,),(23,),(0,12,23),(14,16)]:
            self.bank=self.alternate(indices)
            with self.subTest(indices=indices):self.check()
    def test_opposite480_radial_range_is_still_bounded(self):
        self.bank=self.alternate(radial=479);self.check();self.bank['parts'][14]['trial']=960
        with self.assertRaises(AssertionError):self.check()
    def test_only_exact24_original_source_identities_can_gain_opposite_parcels(self):
        self.bank=self.alternate((12,));self.bank['alternateParcels'][0]['name']='Owned source village bank stone 25'
        with self.assertRaises(AssertionError):self.check()
    def test_all24_declared_exhausted_instances_remain_bounded_and_bijective(self):
        self.bank=self.alternate(tuple(range(24)));self.check();self.bank['alternateParcels'].append(dict(self.bank['alternateParcels'][0],name='Owned source village bank stone 25'));self.bank['originalAcceptedBeforeAlternate']=-1
        with self.assertRaises(AssertionError):self.check()
    def test_original22_or23_acceptance_count_must_match_exact_deferred_names(self):
        self.bank=self.alternate();self.bank['originalAcceptedBeforeAlternate']=23
        with self.assertRaises(AssertionError):self.check()
    def test_every_original_and_local_candidate_must_reject_before_opposite(self):
        self.bank=self.alternate();next(row for row in self.bank['trials']if row['name'].endswith('15')and row['trial']==200)['rejectedReason']=''
        with self.assertRaises(AssertionError):self.check()
    def test_exact_station_hints_side_start_and_attempt_count_cannot_change(self):
        original=self.alternate()
        for key,value in [('station',self.vector((0,0,0))),('originalHint',self.vector((0,0,0))),('alternateHint',self.vector((0,0,0))),('originalSide',1),('firstTrial',481),('attempts',2)]:
            self.bank=copy.deepcopy(original);self.bank['alternateParcels'][0][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
    def test_alternate_trial_point_phase_index_and_parcel_hint_are_exact(self):
        original=self.alternate();start=next(i for i,row in enumerate(original['trials'])if row['phase'].startswith('OPPOSITE_BANK'))
        for key,value in [('point',self.vector((0,0,0))),('phase','ARBITRARY_BANK_RELOCATION'),('oppositeBankTrial',1),('parcelHint',self.vector((0,0,0))),('refinementSeedTrial',7),('refinementOffset',self.vector((.5,0,0)))]:
            self.bank=copy.deepcopy(original);self.bank['trials'][start][key]=value
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
    def test_original_hint_and_distance_are_not_redefined_as_opposite_hint(self):
        self.bank=self.alternate();self.bank['parts'][14]['hint']=self.bank['alternateParcels'][0]['alternateHint']
        with self.assertRaises(AssertionError):self.check()
        self.bank=self.alternate();self.bank['parts'][14]['candidateDistanceFromHint']=0
        with self.assertRaises(AssertionError):self.check()
    def test_deferred_rows_must_follow_all_original24_searches(self):
        self.bank=self.alternate();original=[row for row in self.bank['trials']if not row['phase'].startswith('OPPOSITE_BANK')];deferred=[row for row in self.bank['trials']if row['phase'].startswith('OPPOSITE_BANK')];cut=max(i for i,row in enumerate(original)if row['name'].endswith('15'))+1;self.bank['trials']=original[:cut]+deferred[:1]+original[cut:]+deferred[1:]
        with self.assertRaises(AssertionError):self.check()
    def test_missing_duplicate_unknown_or_unordered_parcels_cannot_accept(self):
        original=self.alternate()
        for parcels in ([],original['alternateParcels'][::-1],original['alternateParcels']+[original['alternateParcels'][0]]):
            self.bank=copy.deepcopy(original);self.bank['alternateParcels']=parcels
            with self.assertRaises(AssertionError):self.check()

if __name__=='__main__':unittest.main()
