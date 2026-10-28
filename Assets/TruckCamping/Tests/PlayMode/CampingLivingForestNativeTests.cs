#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using static WildernessNativeScope;
public sealed class CampingLivingForestNativeTests
{
    [UnityTest,Timeout(45000)]public IEnumerator SavedLivingForestRendersOriginalOneAndFourOwnedViewsInActualWeatherMakerClearDayWithoutReseeding()
    {
        using(var scope=new WildernessNativeScope("camp-living-forest-views"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");var world=Get(scope.match,"world");var terrains=(Terrain[])Get(world,"terrains");var data=terrains.Select(t=>t.terrainData).ToArray();var trees=data.Select(t=>t.treeInstances).ToArray();Assert.That(trees.Sum(t=>t.Length),Is.EqualTo(2297));
            foreach(var tile in data)Assert.That(AssetDatabase.GetAssetPath(tile.treePrototypes[0].prefab),Is.EqualTo("Assets/TruckCamping/Prefabs/Camp Haul solid JC07 pine.prefab"));
            var weather=Get(scope.match,"presentationWeather");var presets=Get(weather,"presets");Call(presets,"Select","9aad5ff5dfa09ce468205f676714f891");Sports("SportsTimeOfDay","Select",Enum.Parse(Runtime("LWS.TruckSports.SportsTimeChoice"),"Day"));
            foreach(int count in new[]{1,4})
            {
                scope.Configure(count);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);Call(Get(scope.match,"drivingViews"),"Select",1);yield return scope.Advance(1);
                Assert.That((string)Get(weather,"PresetId"),Is.EqualTo("9aad5ff5dfa09ce468205f676714f891"));Assert.That(Get(Get(weather,"matchTime"),"Applied").ToString(),Is.EqualTo("Day"));
                foreach(int width in new[]{1280,1920})scope.Capture("camp-solid-jc07-owned-"+count+"-"+width,width,width==1280?720:1080);
                for(int i=0;i<data.Length;i++){Assert.That(terrains[i].terrainData,Is.SameAs(data[i]));CollectionAssert.AreEqual(trees[i],data[i].treeInstances);}
            }
            Assert.That(((Component)world).GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Actual fallen Whitebark pine ",StringComparison.Ordinal)),Is.EqualTo(9),"Original intentional deadwood remains in the shared woodland.");
            TestContext.WriteLine("CAMP_SOLID_PINE_SAVED_VIEWS actual1/4owned720/1080 Clear/Day WM captures; original4TerrainData/2297placements/deadwood unchanged. Rendered beauty/readability requires direct original image review; not a hardware/Windows/performance claim.");
        }
    }
}
#endif
