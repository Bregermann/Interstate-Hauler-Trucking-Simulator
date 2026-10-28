#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using static WildernessNativeScope;
public sealed class PetMiniatureGeometryNativeTests
{
    [UnityTest,Timeout(45000)]public IEnumerator AllSevenActualMiniatureBodiesKeepLocalGroundFitAndFourRollingGroupsAtArbitraryWorldHeadings()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckPetPals/Prefabs/Animated MiniatureTruck companion art.prefab");Assert.That(source,Is.Not.Null);GameObject instance=null;
        var catalog=(UnityEngine.Object)Resources.Load("SportsTruckAppearanceCatalog");var bodies=(Array)Get(catalog,"bodies");Assert.That(bodies.Length,Is.EqualTo(7));float oldScale=Time.timeScale;Time.timeScale=1;
        try
        {
            instance=UnityEngine.Object.Instantiate(source);instance.transform.position=new Vector3(115,11,-78);var art=instance.GetComponent(Runtime("LWS.TruckPetPals.PetTruckArt"));var assembly=(Transform)Get(art,"assembly");
            foreach(object body in bodies)
            {
                string id=(string)Get(body,"id");var prefab=(GameObject)Get(body,"visualPrefab");var original=prefab.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();var materials=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
                foreach(float yaw in new[]{0f,53f,179f})
                {
                    instance.transform.rotation=Quaternion.Euler(0,yaw,0);Call(art,"SelectBody",id,new Color(.35f,.8f,.6f));var model=(GameObject)Get(art,"currentModel");
                    var bounds=(Bounds)Runtime("LWS.TruckPetPals.PetTruckArt").GetMethod("LocalRenderedBounds").Invoke(null,new object[]{assembly,model.GetComponentsInChildren<Renderer>(true).Where(r=>r.GetComponent<MeshFilter>() is MeshFilter f&&Array.IndexOf(original,f.sharedMesh)>=0).ToArray()});
                    Assert.That(Mathf.Max(bounds.size.x,bounds.size.z),Is.EqualTo(3.2f).Within(.002f),"Physical world yaw cannot enlarge/shrink the pet's local normalization: "+id+"/"+yaw);Assert.That(bounds.min.y,Is.EqualTo(0).Within(.002f));
                    var wheels=(Transform[])Get(art,"wheels");Assert.That(wheels.Length,Is.EqualTo(4));Assert.That(wheels.All(w=>w.GetComponentsInChildren<MeshFilter>().Length>0),Is.True,"Four actual authored tire/rim groups, no invisible wheel stub.");
                    Assert.That(instance.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(instance.GetComponentsInChildren<Collider>(true),Is.Empty);
                }
                var pose=Runtime("LWS.TruckCamping.CampLivingArt").Assembly.GetType("LWS.TruckCamping.CampPose",true);Call(art,"Request",Enum.Parse(pose,"Pee"),1f);float deadline=Time.realtimeSinceStartup+1.5f;while((float)Get(art,"ActualLegLift")<.2f&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Get(art,"ActualLegLift"),Is.GreaterThan(.2f),"Actual wheel geometry raises at its semantic pivot.");
                Call(art,"Request",Enum.Parse(pose,"GrassKick"),0f);yield return null;yield return null;Assert.That((float)Get(art,"ActualKickAngle"),Is.GreaterThan(0));
                CollectionAssert.AreEqual(original,prefab.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray());CollectionAssert.AreEqual(materials,prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray());
            }
            TestContext.WriteLine("PET_MINIATURE actual seven catalog models, 21 world-heading fits, existing4 rolling groups, live raised wheel/backward kicks, original source mesh/material references unchanged. Controlled native geometry scope, no Windows/human claim.");
        }
        finally{Time.timeScale=oldScale;if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);}
    }
}
#endif
