#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LWS.TruckPetPals;
using static WildernessNativeScope;
public sealed class PetDirtRebindNativeTests
{
    [UnityTest,Timeout(50000)]public IEnumerator ActualSevenMiniatureReplacementsRebindDirtToCurrentArtAcrossDeferredDestroyAndWash()
    {
        Assert.That(LogAssert.ignoreFailingMessages,Is.False,"Native Error/Exception failure policy must stay enabled.");
        LogAssert.Expect(LogType.Log,new Regex(@"^SPORTS_PROBE_NATIVE_INPUT_MUTED count=\d+$"));
        using(var scope=new WildernessNativeScope("pet-current-dirt-model"))
        {
            yield return scope.Load("TruckPetPals","LWS.TruckPetPals.PetPalsMatch");scope.Configure(1,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(.5f);
            Call(scope.match,"Pause",0,false);yield return null;var pet=Components(Get(scope.match,"companions"))[0];var record=(PetRecord)Get(pet,"Record");Assert.That(record.kind,Is.EqualTo(PetKind.MiniatureTruck));
            var visual=Get(pet,"Visual");var art=(Component)Get(visual,"miniature");var catalog=Resources.Load("SportsTruckAppearanceCatalog");var bodies=(Array)Get(catalog,"bodies");Assert.That(bodies.Length,Is.EqualTo(7));
            var block=new MaterialPropertyBlock();var identity=new Color(.23f,.72f,.91f,1);int painted=0,protectedSlots=0;
            foreach(object body in bodies)
            {
                string id=(string)Get(body,"id");var old=(GameObject)Get(art,"CurrentModel");Call(art,"SelectBody",id,identity);var current=(GameObject)Get(art,"CurrentModel");Assert.That(current,Is.Not.SameAs(old));
                // Controlled presentation input only. This does not exercise or
                // grant earned care/training progress; original full tests do.
                record.cleanliness=20;var renderers=current.GetComponentsInChildren<Renderer>(true);var materials=renderers.Select(r=>r.sharedMaterials).ToArray();var originalBlocks=renderers.Select(r=>Enumerable.Range(0,r.sharedMaterials.Length).Select(i=>{r.GetPropertyBlock(block,i);return block.GetColor("_BaseColor");}).ToArray()).ToArray();
                Call(pet,"PresentDirt");var cached=(Renderer[])Get(visual,"dirtSurfaces");CollectionAssert.AreEquivalent(renderers,cached);Assert.That(cached.All(r=>r!=null&&r.transform.IsChildOf(current.transform)),Is.True,"Retired deferred-destroy assembly renderers cannot enter the current dirt cache.");
                var paint=(bool[][])Get(pet,"paintSlots");var clean=(Color[][])Get(pet,"cleanColors");
                for(int r=0;r<cached.Length;r++)for(int slot=0;slot<paint[r].Length;slot++)
                {
                    cached[r].GetPropertyBlock(block,slot);
                    if(paint[r][slot]){painted++;var expected=Color.Lerp(new Color(.19f,.11f,.055f),clean[r][slot],.2f);AssertColor(block.GetColor("_BaseColor"),expected);}
                    else{protectedSlots++;int index=Array.IndexOf(renderers,cached[r]);Assert.That(block.GetColor("_BaseColor"),Is.EqualTo(originalBlocks[index][slot]),"Glass/chrome/eyes/nonpaint slots retain their original MPB color.");}
                }
                record.cleanliness=100;Set(pet,"nextDirty",0f);Call(pet,"PresentDirt");
                for(int r=0;r<cached.Length;r++)for(int slot=0;slot<paint[r].Length;slot++)if(paint[r][slot]){cached[r].GetPropertyBlock(block,slot);AssertColor(block.GetColor("_BaseColor"),clean[r][slot]);}
                for(int r=0;r<renderers.Length;r++)CollectionAssert.AreEqual(materials[r],renderers[r].sharedMaterials,"Dirt/wash does not replace shared vendor materials.");
                yield return null;yield return null;Assert.That(old==null,Is.True,"The original runtime Destroy boundary really elapsed.");Call(pet,"PresentDirt");Assert.That(((Renderer[])Get(visual,"dirtSurfaces")).All(r=>r!=null),Is.True);Assert.That(LogAssert.ignoreFailingMessages,Is.False,"Normal vendor/input lifecycle logs do not disable Error/Exception failures.");
            }
            Assert.That(painted,Is.GreaterThan(7));Assert.That(protectedSlots,Is.GreaterThan(0));TestContext.WriteLine("PET_DIRT_REBIND_PASS actual7bodies/deferredDestroy/washMPBs,painted="+painted+" protected="+protectedSlots+"; controlled presentation fixture, no earned-care/Windows claim.");
        }
    }
    static void AssertColor(Color a,Color b){Assert.That(a.r,Is.EqualTo(b.r).Within(.000002f));Assert.That(a.g,Is.EqualTo(b.g).Within(.000002f));Assert.That(a.b,Is.EqualTo(b.b).Within(.000002f));Assert.That(a.a,Is.EqualTo(b.a).Within(.000002f));}
}
#endif
