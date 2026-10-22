#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BaseFlareSmokeTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    [UnityTest]
    public IEnumerator AllFourExistingFlaresReuseNativeSmokeAndClearTheSamePoolOnRematch()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"BaseFlareSmoke"}))
        {
            Component match=null;
            try
            {
                const string owned="Assets/TruckBaseJumping/Prefabs/Flare Smoke.prefab",vendor="Assets/NWH/Vehicle Physics 2/Effects/Prefabs/ExhaustSmoke.prefab";
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(owned),Is.Not.Null);Assert.That(AssetDatabase.GetDependencies(owned),Does.Contain(vendor),"Reuse the installed NWH particle prefab through an owned variant.");
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBaseJumping.BaseJumpMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
                var field=Field(match,"skyField");var bodies=((Array)Field(field,"bodies")).Cast<object>().ToArray();Assert.That(bodies.Length,Is.EqualTo(24));var flares=bodies.Where(b=>(int)Field(b,"kind")==2).ToArray();Assert.That(flares.Length,Is.EqualTo(4));var identities=flares.Select(f=>((Component)f).GetInstanceID()).ToArray();
                foreach(var flare in flares)
                {
                    var smoke=(ParticleSystem)Field(flare,"smoke");Assert.That(smoke,Is.Not.Null);Assert.That(smoke.main.simulationSpace,Is.EqualTo(ParticleSystemSimulationSpace.World));Assert.That(smoke.main.maxParticles,Is.LessThanOrEqualTo(96));Assert.That(smoke.collision.enabled,Is.False);var material=smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial;Assert.That(material,Is.Not.Null);Assert.That(material.shader.isSupported,Is.True);Assert.That(material.mainTexture,Is.Not.Null);
                    var sheet=smoke.textureSheetAnimation;Assert.That(sheet.enabled,Is.True,"The installed atlas must not be shown in full on each particle.");Assert.That(sheet.numTilesX,Is.EqualTo(6));Assert.That(sheet.numTilesY,Is.EqualTo(5));Assert.That(sheet.animation,Is.EqualTo(ParticleSystemAnimationType.WholeSheet));Assert.That(sheet.timeMode,Is.EqualTo(ParticleSystemAnimationTimeMode.Lifetime));Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(Color.white));Assert.That(material.IsKeywordEnabled("_COLORADDSUBDIFF_ON"),Is.False);
                    Assert.That(material.GetFloat("_BlendModePreserveSpecular"),Is.Zero,"Zero-alpha smoke must not leave a glossy billboard silhouette.");Assert.That(material.GetFloat("_SrcBlend"),Is.EqualTo((float)UnityEngine.Rendering.BlendMode.SrcAlpha));Assert.That(material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"),Is.False);
                    Assert.That((bool)field.GetType().GetMethod("Spawn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(field,new[]{flare}),Is.True);
                }
                yield return new WaitForSeconds(.35f);
                foreach(var flare in flares){var smoke=(ParticleSystem)Field(flare,"smoke");Assert.That(smoke.particleCount,Is.GreaterThan(0),"Actual native particle update must emit visible smoke after a pooled flare starts.");TestContext.WriteLine("Native flare="+((Component)flare).name+" particles="+smoke.particleCount+" material="+smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial.name);}
                Call(field,"ResetField",123);foreach(var flare in flares){var smoke=(ParticleSystem)Field(flare,"smoke");Assert.That(smoke.particleCount,Is.Zero);Assert.That(smoke.gameObject.activeInHierarchy,Is.False);}
                Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That(flares.Select(f=>((Component)f).GetInstanceID()),Is.EqualTo(identities),"Rematch reuses the same four emitters instead of leaking another crowd of particle objects.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");}
        }
    }
}
#endif
