using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public sealed class FrogSceneTests
{
 Component match;SerializedObject data;
 [OneTimeSetUp]public void Open(){EditorSceneManager.OpenScene("Assets/TruckFrogger/Scenes/TruckFrogger.unity");var type=Type.GetType("LWS.TruckFrogger.FrogMatch, Assembly-CSharp",true);match=Resources.FindObjectsOfTypeAll(type).Cast<Component>().First(c=>c.gameObject.scene.IsValid());data=new SerializedObject(match);}
 [Test]public void EveryAuthoredBehaviourAndSharedPresentationReferenceResolves(){foreach(var root in match.gameObject.scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Assert.Zero(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),t.name);foreach(string name in new[]{"drivingViews","broadcast","atmosphere","menus","feedback"})Assert.NotNull(data.FindProperty(name).objectReferenceValue,name);Assert.NotNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Logos/TruckFrogger.png"));}
 [Test]public void FiveDistinctCoursesHaveOrderedRealCrossingsAndGroundedStarts(){var world=new SerializedObject(data.FindProperty("world").objectReferenceValue);var courses=world.FindProperty("courses");Assert.AreEqual(5,courses.arraySize);for(int i=0;i<5;i++){var c=courses.GetArrayElementAtIndex(i);var root=(Transform)c.FindPropertyRelative("root").objectReferenceValue;bool active=root.gameObject.activeSelf;root.gameObject.SetActive(true);Physics.SyncTransforms();var start=c.FindPropertyRelative("start").vector3Value;Assert.True(Physics.Raycast(start+Vector3.up*10,Vector3.down,out var hit,20,1,QueryTriggerInteraction.Ignore));Assert.Less(Mathf.Abs(hit.point.y),.2f);var gates=c.FindPropertyRelative("gates");Assert.GreaterOrEqual(gates.arraySize,2);float z=float.NegativeInfinity;for(int j=0;j<gates.arraySize;j++){float next=gates.GetArrayElementAtIndex(j).vector3Value.z;Assert.Greater(next,z);z=next;}root.gameObject.SetActive(active);}Assert.AreEqual(8,data.FindProperty("trucks").arraySize);}
 [Test]public void ExistingRouteDriverDefaultsRemainCompatible(){var type=Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp",true);var route=Activator.CreateInstance(type);Assert.AreEqual(1f,type.GetField("speedScale").GetValue(route));Assert.AreEqual(.6f,type.GetField("groundNormalThreshold").GetValue(route));}

}
