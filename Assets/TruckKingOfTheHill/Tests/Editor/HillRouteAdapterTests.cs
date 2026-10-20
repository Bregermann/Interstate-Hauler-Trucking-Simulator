using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace LWS.TruckSports.Tests
{
    public sealed class HillRouteAdapterTests
    {
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
