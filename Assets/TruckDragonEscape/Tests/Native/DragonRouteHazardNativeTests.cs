#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DragonRouteHazardNativeTests
{
    GameObject owner;
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    static object Read(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
    [UnityTest] public IEnumerator FourRealPrefracturedStructuresCollapsePhysicallyAndResetWithoutMeshClones()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.DragonRouteHazard, Assembly-CSharp",true);
        foreach(string id in new[]{"village-treefall","burning-forest-bridge","volcanic-rockfall","harbor-market-collapse"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonEscape/Prefabs/"+id+"/Actual owned structure.prefab");Assert.That(prefab,Is.Not.Null,"Explicitly author the real saved hazards first.");
            owner=new GameObject("Actual native CoreCollapse fixture "+id);owner.transform.position=Vector3.up*1000;
            var hazard=owner.AddComponent(type);type.GetField("id").SetValue(hazard,id);type.GetField("intactPrefab").SetValue(hazard,prefab);
            type.GetField("collapseVelocityChange").SetValue(hazard,id=="burning-forest-bridge"?Vector3.zero:new Vector3(-10,1,0));
            for(int round=0;round<2;round++)
            {
                Call(hazard,"ResetDragonMatch",round);yield return null;Assert.That(Read(hazard,"Collapsed"),Is.EqualTo(false));Assert.That(owner.GetComponentsInChildren<Rigidbody>().Length,Is.Zero,"Reset removes the previous real debris batch.");
                Assert.That(Call(hazard,"Trigger"),Is.EqualTo(true));Assert.That(Call(hazard,"Trigger"),Is.EqualTo(false),"One actual collapse per reset.");
                var bodies=owner.GetComponentsInChildren<Rigidbody>();Assert.That(bodies.Length,Is.InRange(2,32));Assert.That(Read(hazard,"Collapsed"),Is.EqualTo(true));
                var positions=bodies.Select(body=>body.position).ToArray();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                Assert.That(bodies.Select((body,index)=>Vector3.Distance(body.position,positions[index])).Max(),Is.GreaterThan(.005f),"Actual dynamic fragments must move under physics.");
                foreach(var filter in owner.GetComponentsInChildren<MeshFilter>())Assert.That(AssetDatabase.Contains(filter.sharedMesh),Is.True,"Runtime must reuse preauthored licensed meshes without allocating leaked slices.");
            }
            UnityEngine.Object.Destroy(owner);yield return null;owner=null;
        }
    }
    [TearDown] public void Cleanup() { if(owner!=null)UnityEngine.Object.DestroyImmediate(owner); }
}
#endif
