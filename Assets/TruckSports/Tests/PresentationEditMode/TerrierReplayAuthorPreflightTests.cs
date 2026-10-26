#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class TerrierReplayAuthorPreflightTests
{
    static bool Match(Transform[] source,Transform[] ghost)=> (bool)Type.GetType("LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring, Assembly-CSharp-Editor",true).GetMethod("TerrierOriginalReplayChannelsMatch").Invoke(null,new object[]{source,ghost});
    [Test]public void OriginalInstantiateRootAliasPreservesAllThirtyFourExactChildMeshChannels()
    {
        var source=new GameObject("NWH GR3D visual geometry");GameObject ghost=null;var mesh=new Mesh();
        try
        {
            mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up};mesh.triangles=new[]{0,1,2};for(int index=0;index<33;index++){var part=new GameObject("Original part "+index,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(source.transform,false);part.GetComponent<MeshFilter>().sharedMesh=mesh;}
            ghost=UnityEngine.Object.Instantiate(source);var a=source.GetComponentsInChildren<Transform>(true);var b=ghost.GetComponentsInChildren<Transform>(true);Assert.That(b[0].name,Is.EqualTo(a[0].name+"(Clone)"));Assert.That(Match(a,b),Is.True);b[0].name=a[0].name;Assert.That(Match(a,b),Is.True);
            b[0].name="Unexpected ghost root";Assert.That(Match(a,b),Is.False);b[0].name=a[0].name+"(Clone)";b[1].name=a[1].name+"(Clone)";Assert.That(Match(a,b),Is.False);b[1].name=a[1].name;
            var alternate=UnityEngine.Object.Instantiate(mesh);try{b[17].GetComponent<MeshFilter>().sharedMesh=alternate;Assert.That(Match(a,b),Is.False,"Equal geometry cannot replace actual original source mesh identity.");b[17].GetComponent<MeshFilter>().sharedMesh=mesh;}finally{UnityEngine.Object.DestroyImmediate(alternate);}
            b[18].SetParent(b[17],false);Assert.That(Match(a,b),Is.False,"Changing parent frame must refuse even when names/order/meshes match.");Assert.That(Match(a,b.Take(33).ToArray()),Is.False);
        }
        finally{if(ghost!=null)UnityEngine.Object.DestroyImmediate(ghost);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(mesh);}
    }
}
#endif
