#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SnakeFirstHitchGeometryTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static Transform Anchor(GameObject root,string wrapper)=> (Transform)Get(Get(root.GetComponentsInChildren<MonoBehaviour>(true).Single(component=>component.GetType().Name==wrapper),"module"),"attachmentPoint");
    static object Sample(object history,float distance){object[] args={distance,null};Assert.That(history.GetType().GetMethod("TryBehind").Invoke(history,args),Is.True);return args[1];}
    [TestCase(0,false)]
    [TestCase(90,false)]
    [TestCase(180,false)]
    [TestCase(90,true)]
    public void LeadKingpinStaysAtVendorHitchWhileLaterPathPosesRemainUnchanged(int turn,bool airborne)
    {
        GameObject tractor=null,trailer=null,root=null;
        try
        {
            tractor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab"));
            trailer=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab"));
            tractor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);trailer.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            Transform hitch=Anchor(tractor,"TrailerHitchModuleWrapper"),kingpin=Anchor(trailer,"TrailerModuleWrapper");
            var renderers=trailer.GetComponentsInChildren<MeshFilter>(true).Select(filter=>filter.GetComponent<Renderer>()).Where(renderer=>renderer!=null).ToArray();
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            Vector3 localPin=Vector3.up*1.6f+Vector3.Scale(kingpin.position-bounds.center,new Vector3(2.6f/bounds.size.x,3.2f/bounds.size.y,13.5f/bounds.size.z));
            root=new GameObject("Explicit synthetic hitch geometry fixture");var type=Type.GetType("LWS.TruckSnake.SnakeConvoy, Assembly-CSharp",true);var convoy=root.AddComponent(type);
            type.GetField("tractor").SetValue(convoy,tractor.GetComponent(Type.GetType("LWS.TruckSports.SportsEventTruck, Assembly-CSharp",true)));
            // The old runtime has neither field: it should fail the same anchor
            // invariant rather than failing because a new field is unavailable.
            type.GetField("firstTractorAttachment")?.SetValue(convoy,hitch);type.GetField("firstTrailerAttachment")?.SetValue(convoy,localPin);
            var history=Get(convoy,"history");Vector3 oldOffset=(Vector3)Get(convoy,"tractorHitch");
            history.GetType().GetMethod("Reset").Invoke(history,new object[]{tractor.transform.TransformPoint(oldOffset),Quaternion.identity,120f});
            for(int i=1;i<=40;i++){
                float fraction=i/40f,angle=turn*fraction;float radians=angle*Mathf.Deg2Rad;
                tractor.transform.SetPositionAndRotation(turn==0?new Vector3(0,0,30*fraction):new Vector3(8*(1-Mathf.Cos(radians)),airborne?8*Mathf.Sin(fraction*Mathf.PI*.8f):0,8*Mathf.Sin(radians)),Quaternion.Euler(airborne?-15*Mathf.Sin(fraction*Mathf.PI):0,angle,0));
                history.GetType().GetMethod("Append").Invoke(history,new object[]{tractor.transform.TransformPoint(oldOffset),tractor.transform.rotation,0f});
            }
            var pose=type.GetMethod("Pose",Any);object[] first={0,Vector3.zero,Quaternion.identity};Assert.That(pose.Invoke(convoy,first),Is.True);
            Vector3 actual=(Vector3)first[1]+(Quaternion)first[2]*localPin;float gap=Vector3.Distance(actual,hitch.position);
            TestContext.WriteLine("SYNTHETIC_HITCH turn="+turn+" airborne="+airborne+" gapMetres="+gap+" localPin="+localPin+" vendorHitch="+tractor.transform.InverseTransformPoint(hitch.position));
            Assert.That(gap,Is.LessThan(.001f),"The first trailer's actual scaled vendor kingpin must coincide with the tractor's authored fifth wheel.");
            for(int index=1;index<=3;index++){
                float center=(float)Get(convoy,"frontOffset")+index*(float)Get(convoy,"spacing");var sample=Sample(history,center);var front=Sample(history,Mathf.Max(0,center-5));var back=Sample(history,center+5);
                Vector3 up=(Quaternion)Get(sample,"rotation")*Vector3.up;if(Vector3.Dot(up,Vector3.up)<.4f)up=Vector3.up;
                Quaternion rotation=Quaternion.LookRotation((Vector3)Get(front,"position")-(Vector3)Get(back,"position"),up);Vector3 position=(Vector3)Get(sample,"position")-Vector3.up*.9f;
                object[] later={index,Vector3.zero,Quaternion.identity};Assert.That(pose.Invoke(convoy,later),Is.True);
                Assert.That(Vector3.Distance((Vector3)later[1],position),Is.LessThan(.0001f));Assert.That(Quaternion.Angle((Quaternion)later[2],rotation),Is.LessThan(.001f),"Later trailers retain their original historical path poses.");
            }
        }
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(tractor!=null)UnityEngine.Object.DestroyImmediate(tractor);if(trailer!=null)UnityEngine.Object.DestroyImmediate(trailer);}
    }
}
#endif
