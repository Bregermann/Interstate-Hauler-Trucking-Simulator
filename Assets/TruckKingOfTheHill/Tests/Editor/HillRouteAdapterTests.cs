using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace LWS.TruckSports.Tests
{
    public sealed class HillRouteAdapterTests
    {
        [Test] public void MountainWorldOriginStartsNearItsActualPlateauInsteadOfSkippingAcrossVoid()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity");
            var matchType=Type.GetType("LWS.TruckKingOfTheHill.HillMatch, Assembly-CSharp",true);var match=Resources.FindObjectsOfTypeAll(matchType).Cast<Component>().First(c=>c.gameObject.scene==scene);
            var graph=matchType.GetField("roads").GetValue(match);var sites=(Vector3[])matchType.GetField("sites").GetValue(match);var start=sites[1]+new Vector3(-25,0,10);
            var routeType=Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp",true);var tuningType=routeType.GetConstructors().Single(c=>c.GetParameters().Length==1).GetParameters()[0].ParameterType;var tuning=Activator.CreateInstance(tuningType);tuningType.GetField("routeConnectorToleranceMeters").SetValue(tuning,.5f);var route=Activator.CreateInstance(routeType,new[]{tuning});var api=routeType.GetMethods().Single(m=>m.Name=="Update"&&m.GetParameters()[0].ParameterType==graph.GetType());
            Assert.That((bool)api.Invoke(route,new object[]{graph,start,sites[4],1,0f,true,Vector3.zero}),Is.True);
            var points=(System.Collections.Generic.List<Vector3>)routeType.GetField("points").GetValue(route);Assert.That(points.Count,Is.GreaterThan(1));Assert.That(Vector3.Distance(start,points[0]),Is.LessThan(40),"A truck near the outside edge must first join its nearby physical approach, not skip directly to a distant heading-aligned node.");
        }
        [Test] public void AllTwentyPhysicalPlateauRoutesRemainConnectedWithTightJunctionTolerance()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity");var matchType=Type.GetType("LWS.TruckKingOfTheHill.HillMatch, Assembly-CSharp",true);var match=Resources.FindObjectsOfTypeAll(matchType).Cast<Component>().First(c=>c.gameObject.scene==scene);var graph=matchType.GetField("roads").GetValue(match);var sites=(Vector3[])matchType.GetField("sites").GetValue(match);
            var routeType=Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp",true);var tuningType=routeType.GetConstructors().Single(c=>c.GetParameters().Length==1).GetParameters()[0].ParameterType;var api=routeType.GetMethods().Single(m=>m.Name=="Update"&&m.GetParameters()[0].ParameterType==graph.GetType());
            for(int a=0;a<5;a++)for(int b=0;b<5;b++)if(a!=b){var tuning=Activator.CreateInstance(tuningType);tuningType.GetField("routeConnectorToleranceMeters").SetValue(tuning,.5f);var route=Activator.CreateInstance(routeType,new[]{tuning});Assert.That((bool)api.Invoke(route,new object[]{graph,sites[a],sites[b],1,0f,true,Vector3.zero}),Is.True);var points=(System.Collections.Generic.List<Vector3>)routeType.GetField("points").GetValue(route);Assert.That(points.Count,Is.GreaterThan(2),a+" to "+b);Assert.That(Vector3.Distance(points[points.Count-1],sites[b]),Is.LessThan(1));for(int i=1;i<points.Count;i++)Assert.That(Vector3.Distance(points[i-1],points[i]),Is.LessThanOrEqualTo(23),"Route must follow the authored dense bridge samples without a long implicit air connector.");}
        }
        [Test] public void ExistingDistrictAndNewGraphApisProduceIdenticalRoutes()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity");
            var type = Type.GetType("LWS.TruckKingOfTheHill.HillMatch, Assembly-CSharp", true); var match = Resources.FindObjectsOfTypeAll(type).Cast<Component>().First(c => c.gameObject.scene == scene);
            var graph = type.GetField("roads").GetValue(match); var sites = (Vector3[])type.GetField("sites").GetValue(match);
            var routeType = Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp", true); var districtType = Type.GetType("LWS.TruckFareFight.FareFightDistrict, Assembly-CSharp", true); var district = ScriptableObject.CreateInstance(districtType); districtType.GetField("graph").SetValue(district, graph);
            try
            {
                foreach (var pair in new[] { new Vector2Int(0, 1), new Vector2Int(1, 4), new Vector2Int(3, 2) })
                {
                    var first = Activator.CreateInstance(routeType); var second = Activator.CreateInstance(routeType); var heading = (sites[pair.y] - sites[pair.x]).normalized;
                    var tail = new object[] { sites[pair.x], sites[pair.y], 1, 0f, true, heading };
                    var oldArgs = new object[] { district }.Concat(tail).ToArray(); var newArgs = new object[] { graph }.Concat(tail).ToArray();
                    var oldApi = routeType.GetMethods().Single(m => m.Name == "Update" && m.GetParameters()[0].ParameterType == districtType); var newApi = routeType.GetMethods().Single(m => m.Name == "Update" && m.GetParameters()[0].ParameterType == graph.GetType());
                    Assert.That((bool)oldApi.Invoke(first, oldArgs), Is.True); Assert.That((bool)newApi.Invoke(second, newArgs), Is.True);
                    var a = (System.Collections.Generic.List<Vector3>)routeType.GetField("points").GetValue(first); var b = (System.Collections.Generic.List<Vector3>)routeType.GetField("points").GetValue(second);
                    Assert.That(a.Count, Is.GreaterThan(1)); CollectionAssert.AreEqual(a, b, "District caller behavior must remain unchanged."); Assert.That(routeType.GetProperty("Distance").GetValue(first), Is.EqualTo(routeType.GetProperty("Distance").GetValue(second)));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(district); }
        }
    }
}
