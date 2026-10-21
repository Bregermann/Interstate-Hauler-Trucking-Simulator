using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class SkyTowGateRepairTests
{
    [Test]public void SavedCourseSeparatesOrdinaryAndPrecisionGatesAndUsesTrailingPath()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene("Assets/TruckSkyTow/Scenes/TruckSkyTow.unity");
            var courseType=Type.GetType("LWS.TruckSkyTow.SkyTowCourse, Assembly-CSharp",true);
            var course=UnityEngine.Object.FindObjectsByType(courseType,FindObjectsSortMode.None).Single();
            Assert.That((bool)courseType.GetField("followDelayedTowPath").GetValue(course),Is.True);
            Assert.That((float)courseType.GetField("duration").GetValue(course),Is.InRange(110,130));
            var rings=(Array)courseType.GetField("rings").GetValue(course);Assert.That(rings.Length,Is.EqualTo(144));
            foreach(Component ring in rings)
            {
                var type=ring.GetType();bool bonus=(bool)type.GetField("bonus").GetValue(ring);float radius=(float)type.GetField("radius").GetValue(ring);
                Assert.That(radius,Is.EqualTo(bonus?4.8f:8).Within(.01f));
                var mesh=ring.GetComponent<MeshFilter>().sharedMesh;Assert.That(mesh.subMeshCount,Is.EqualTo(4));Assert.That(mesh.vertexCount,Is.GreaterThan(1500));
                Assert.That(mesh.normals.All(n=>float.IsFinite(n.x)&&float.IsFinite(n.y)&&float.IsFinite(n.z)&&n.sqrMagnitude>.5f),Is.True,"Every gate face needs a valid normal for HDR lighting: "+mesh.name);
            }
            // On a real course bend the nominal truck follows the earlier
            // authored tangent, rather than rotating29m around the current boat.
            float time=35,cruise=(float)courseType.GetProperty("Cruise").GetValue(course),delay=29/cruise;
            object[] earlier={time-delay,0,Vector3.zero,Quaternion.identity};courseType.GetMethod("Frame").Invoke(course,earlier);
            object[] nominal={time,0,Vector3.zero,Quaternion.identity};courseType.GetMethod("NominalFrame").Invoke(course,nominal);
            Assert.That(Vector3.Distance((Vector3)nominal[2],(Vector3)earlier[2]+Vector3.up*18),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle((Quaternion)nominal[3],(Quaternion)earlier[3]),Is.LessThan(.001f));
        }
        finally{if(previous.Length>0)EditorSceneManager.RestoreSceneManagerSetup(previous);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
    }
}
