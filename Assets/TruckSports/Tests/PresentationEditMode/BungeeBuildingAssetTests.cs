#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BungeeBuildingAssetTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static Type Type(string name)=>System.Type.GetType(name+", Assembly-CSharp",true);
    [Test]
    public void SavedVariantsPreserveEveryStructuralBudgetColliderAndSupportLink()
    {
        const string folder="Assets/TruckBungeeDemolition/Prefabs/";
        foreach(var pair in new[]{new[]{"Ten Storey Destructible Tower","Owned hotel demolition tower v2"},new[]{"Ten Additional Reachable Buildings","Owned city demolition annex v2"}})
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(folder+pair[0]+".prefab");var variant=AssetDatabase.LoadAssetAtPath<GameObject>(folder+pair[1]+".prefab");Assert.That(variant,Is.Not.Null);
            var moduleType=Type("LWS.TruckBungeeDemolition.BungeeModule");var originals=original.GetComponentsInChildren(moduleType,true);var copies=variant.GetComponentsInChildren(moduleType,true);Assert.That(copies.Length,Is.EqualTo(originals.Length));
            var supportType=Type("Assets.CoreCollapse.Scripts.Structural.Support");var supports=original.GetComponentsInChildren(supportType,true);var cloned=variant.GetComponentsInChildren(supportType,true);
            for(int i=0;i<originals.Length;i++)
            {
                Assert.That(Field(copies[i],"budget"),Is.EqualTo(Field(originals[i],"budget")));Assert.That(Field(copies[i],"level"),Is.EqualTo(Field(originals[i],"level")));
                Assert.That(copies[i].GetComponent<BoxCollider>().size,Is.EqualTo(originals[i].GetComponent<BoxCollider>().size));Assert.That(copies[i].transform.localPosition,Is.EqualTo(originals[i].transform.localPosition));
                var a=new SerializedObject((UnityEngine.Object)Field(originals[i],"core"));var b=new SerializedObject((UnityEngine.Object)Field(copies[i],"core"));Assert.That(b.FindProperty("startingHp").floatValue,Is.EqualTo(a.FindProperty("startingHp").floatValue));
                a=new SerializedObject(supports[i]);b=new SerializedObject(cloned[i]);
                foreach(var key in new[]{"supportCapacity","supportWeight"})Assert.That(b.FindProperty(key).floatValue,Is.EqualTo(a.FindProperty(key).floatValue));
                var oldLinks=a.FindProperty("supportedBy");var newLinks=b.FindProperty("supportedBy");Assert.That(newLinks.arraySize,Is.EqualTo(oldLinks.arraySize));
                for(int j=0;j<oldLinks.arraySize;j++)Assert.That(Array.IndexOf(cloned,newLinks.GetArrayElementAtIndex(j).objectReferenceValue),Is.EqualTo(Array.IndexOf(supports,oldLinks.GetArrayElementAtIndex(j).objectReferenceValue)));
            }
        }
    }

}
#endif
