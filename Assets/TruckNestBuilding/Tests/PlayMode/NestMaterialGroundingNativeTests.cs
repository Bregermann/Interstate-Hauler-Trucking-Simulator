#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed partial class NestNativeTests
{
    static bool GenuineRoot(Component part)
    {
        var site=(Component)Get(part,"Site");return site!=null&&(bool)Call(site,"Rooted",part);
    }
    [UnityTest,Timeout(45000)]public IEnumerator ActualLooseGroundQueryMembershipRestoresRootedStructuresAndExactOwnershipAcrossPauseSetRebuildAndRematch()
    {
        using(var scope=new Scope("actual-mode-owned-ground-membership-v10"))
        {
            yield return scope.Load();var match=scope.match;Set(Get(match,"setup"),"count",4);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;yield return null;Call(match,"SetupScreen");
            var builders=Components(Get(match,"builders"));var trucks=builders.Select(b=>(Component)Get(b,"truck")).ToArray();var wheels=trucks.SelectMany(t=>Components(Get(t,"wheels"))).ToArray();Assert.That(wheels.Length,Is.EqualTo(16));
            var parts=Components(Get(match,"materials"));var shapes=parts.Select(p=>(Collider)Get(p,"shape")).ToArray();var hulls=trucks.Select(t=>(Collider)Get(t,"shape")).ToArray();
            var originalSets=wheels.Select(w=>(HashSet<Collider>)Get(w,"vehicleColliders")).ToArray();Assert.That(originalSets.All(s=>s!=null),Is.True,"Actual vendor Start must initialize all16 original query sets.");
            bool hadMaterial=originalSets[0].Contains(shapes[0]);var unrelated=(Collider)Get(Components(Get(match,"sites"))[0],"deck");bool hadOther=originalSets[1].Contains(unrelated);
            originalSets[0].Add(shapes[0]);originalSets[1].Add(unrelated);var baseline=originalSets.Select(s=>s.ToArray()).ToArray();var masks=wheels.Select(w=>(LayerMask)Get(w,"layerMask")).ToArray();var forces=wheels.Select(w=>(float)Get(w,"otherBodyForceScale")).ToArray();var layers=shapes.Select(s=>s.gameObject.layer).ToArray();var matrix=new bool[32,32];for(int a=0;a<32;a++)for(int b=0;b<32;b++)matrix[a,b]=UnityEngine.Physics.GetIgnoreLayerCollision(a,b);
            var pairs=hulls.Select(h=>shapes.Select(s=>UnityEngine.Physics.GetIgnoreCollision(h,s)).ToArray()).ToArray();
            Action membership=()=>
            {
                for(int i=0;i<wheels.Length;i++)
                {
                    var current=(HashSet<Collider>)Get(wheels[i],"vehicleColliders");Assert.That(current,Is.Not.Null);
                    for(int p=0;p<parts.Length;p++)Assert.That(current.Contains(shapes[p]),Is.EqualTo(!GenuineRoot(parts[p])||baseline[i].Contains(shapes[p])),"Only mode-added loose/carried supplies are query-excluded; real rooted structure and preexisting intent preserved.");
                    Assert.That((LayerMask)Get(wheels[i],"layerMask"),Is.EqualTo(masks[i]));Assert.That((float)Get(wheels[i],"otherBodyForceScale"),Is.EqualTo(forces[i]));
                    var hit=(Collider)Get(wheels[i],"HitCollider");if((bool)Get(wheels[i],"IsGrounded")&&hit!=null){int p=Array.IndexOf(shapes,hit);if(p>=0)Assert.That(GenuineRoot(parts[p]),Is.True,"Actual wheel cast cannot treat a loose supply as suspension ground.");}
                }
            };
            try
            {
                Set(Get(match,"setup"),"count",4);Set(match,"option",0);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=12;yield return Advance(scope,.12f);membership();
                // Controlled test-only metadata replacement models the vendor's
                // documented Initialize set rebuild; no collider/pose/force change.
                var replacement=new HashSet<Collider>(baseline[0]);wheels[0].GetType().GetField("vehicleColliders").SetValue(wheels[0],replacement);yield return Advance(scope,.08f);CollectionAssert.AreEquivalent(baseline[0],originalSets[0]);membership();
                var site=Components(Get(match,"sites"))[0];float deadline=Time.realtimeSinceStartup+12;
                while((int)Get(site,"RootedCount")<2&&Time.realtimeSinceStartup<deadline){membership();yield return null;}
                Assert.That((int)Get(site,"RootedCount"),Is.GreaterThanOrEqualTo(2),"Ordinary four-AI physical drops/contact/rooting must exercise the real loose-to-structure membership transition.");membership();
                var pausedMembership=wheels.Select(w=>((HashSet<Collider>)Get(w,"vehicleColliders")).ToArray()).ToArray();Call(match,"Pause",0,false);yield return new WaitForSecondsRealtime(.08f);for(int i=0;i<wheels.Length;i++)CollectionAssert.AreEquivalent(pausedMembership[i],(HashSet<Collider>)Get(wheels[i],"vehicleColliders"));Call(match,"Resume");Call(match,"SetupScreen");
                for(int i=0;i<wheels.Length;i++)CollectionAssert.AreEquivalent(baseline[i],(HashSet<Collider>)Get(wheels[i],"vehicleColliders"),"Exit restores only owned additions; original/native/unrelated members remain.");
                Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return Advance(scope,.1f);membership();Call(match,"SetupScreen");
                for(int i=0;i<wheels.Length;i++)CollectionAssert.AreEquivalent(baseline[i],(HashSet<Collider>)Get(wheels[i],"vehicleColliders"));
                CollectionAssert.AreEqual(layers,shapes.Select(s=>s.gameObject.layer).ToArray());for(int a=0;a<32;a++)for(int b=0;b<32;b++)Assert.That(UnityEngine.Physics.GetIgnoreLayerCollision(a,b),Is.EqualTo(matrix[a,b]));for(int h=0;h<hulls.Length;h++)for(int p=0;p<shapes.Length;p++)Assert.That(UnityEngine.Physics.GetIgnoreCollision(hulls[h],shapes[p]),Is.EqualTo(pairs[h][p]),"Hull/native cargo collisions remain physical.");
                TestContext.WriteLine("NEST_QUERY_MEMBERSHIP_PASS actual16wheels/48materials natural2root transitions; originalsets/setreplacement/pause/rematch restored, originalmasks/reactionScale/layers/hullpairs/globalmatrix exact; no poses/outcomes injected.");
            }
            finally
            {
                Call(match,"SetupScreen");var current=(HashSet<Collider>)Get(wheels[0],"vehicleColliders");if(!hadMaterial){originalSets[0].Remove(shapes[0]);current.Remove(shapes[0]);}if(!hadOther)originalSets[1].Remove(unrelated);wheels[0].GetType().GetField("vehicleColliders").SetValue(wheels[0],originalSets[0]);
            }
        }
    }
}
#endif
