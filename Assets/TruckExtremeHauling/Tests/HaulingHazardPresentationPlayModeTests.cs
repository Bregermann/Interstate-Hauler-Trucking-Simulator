#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class HaulingHazardPresentationPlayModeTests
    {
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(target);
        static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target,string name,params object[] arguments)=>target.GetType().GetMethod(name).Invoke(target,arguments);

        [UnityTest]
        public IEnumerator AuthoredActualIceFailureDrivesPooledCuesAndFreshRematch()
        {
            var lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"HaulingActualHazardCues"});
            float scale=Time.timeScale;Component match=null;
            try
            {
                Time.timeScale=1;
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckExtremeHauling.ThinIceMatch"),FindObjectsSortMode.None)[0];
                var presentation=match.GetComponent(Runtime("LWS.TruckExtremeHauling.HaulingHazardPresentation"));
                Assert.That(presentation,Is.Not.Null,"Use the actual saved presentation; no test-time installation or emission.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);
                match.GetType().GetField("option").SetValue(match,3);Call(match,"NormalizeSetup");Call(match,"AllBots");
                var rescue=Field(match,"rescue");var incident=Field(rescue,"incidentIce");
                Assert.That((bool)Call(match,"StartMatch"),Is.True);string first=(string)Property(match,"MatchId");
                Time.timeScale=3;float deadline=Time.realtimeSinceStartup+12;
                while(((int)Property(presentation,"ActualBreakBursts")==0||(int)Property(presentation,"ActualWaterEntries")==0)&&Time.realtimeSinceStartup<deadline)yield return null;
                Time.timeScale=1;
                Assert.That((bool)Property(incident,"Broken"),Is.True,"Only genuine supported-load failure may trigger a break presentation.");
                Assert.That((int)Property(presentation,"ActualBreakBursts"),Is.GreaterThan(0));
                Assert.That((int)Property(presentation,"ActualWaterEntries"),Is.GreaterThan(0),"Actual falling body must enter the vendor water-effect bounds.");
                var effects=(Component)Field(presentation,"waterEffects");Assert.That((int)Property(effects,"Entries"),Is.GreaterThan(0));
                var pool=(ParticleSystem[])Field(presentation,"iceChips");Assert.That(pool.Length,Is.EqualTo(4));
                foreach(var emitter in pool)Assert.That(emitter.main.maxParticles,Is.LessThanOrEqualTo(128));
                foreach(var audio in (AudioSource[])Field(effects,"audio"))Assert.That(audio.clip.name,Is.EqualTo("WeatherMakerWaterSound_Splash"));
                Call(match,"SetupScreen");yield return null;
                foreach(var line in (LineRenderer[])Field(presentation,"crackLines"))Assert.That(line.enabled,Is.False);
                Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;
                Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(first));
                Assert.That((int)Property(presentation,"ActualBreakBursts"),Is.Zero,"The new match must not inherit previous break events.");
                Assert.That((int)Property(presentation,"ActualWaterEntries"),Is.Zero);
                TestContext.WriteLine("HAULING_PRESENTATION actual_load_failure=true actual_water_entry=true bounded_existing_pools=true rematch=fresh software_ai_only=true listening=unverified");
            }
            finally{if(match!=null)Call(match,"SetupScreen");Time.timeScale=scale;lease.Dispose();}
        }
    }
}
#endif
