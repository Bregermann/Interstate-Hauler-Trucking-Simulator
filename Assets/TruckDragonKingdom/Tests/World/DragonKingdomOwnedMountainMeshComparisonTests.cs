#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonKingdomOwnedMountainMeshComparisonTests
{
    [Serializable]sealed class Comparison
    {
        public bool meshExact,physicalEquivalentAfterOnlyUv4Normalization,inputMeshesUnchanged;
        public string reason,normalizedComparisonFailure,actualMeshPayloadBefore,actualMeshPayloadAfter,savedMeshPayloadBefore,savedMeshPayloadAfter,actualUv4Sha256,savedUv4Sha256;
        public int actualUv4Count,savedUv4Count;
    }
    static Mesh Fixture()
    {
        var mesh=new Mesh{name="Validation only original road quad",hideFlags=HideFlags.HideAndDontSave};
        mesh.vertices=new[]{new Vector3(-1,0,0),new Vector3(1,0,0),new Vector3(-1,1,0),new Vector3(1,1,0)};mesh.normals=Enumerable.Repeat(Vector3.forward,4).ToArray();mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,1),4).ToArray();mesh.colors=new[]{Color.red,Color.green,Color.blue,Color.white};
        for(int channel=0;channel<8;channel++)mesh.SetUVs(channel,new List<Vector4>{new Vector4(0,0,channel,0),new Vector4(1,0,channel,0),new Vector4(0,1,channel,0),new Vector4(1,1,channel,0)});
        mesh.triangles=new[]{0,1,2,2,1,3};mesh.RecalculateBounds();return mesh;
    }
    // Independent complete native fixture payload: actual vertex attributes,
    // all eight UV sets, topology and indices, plus native serialization.
    static byte[] Payload(Mesh mesh)
    {
        using(var stream=new MemoryStream())
        {
            using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                writer.Write(EditorJsonUtility.ToJson(mesh));writer.Write(mesh.vertexCount);writer.Write((int)mesh.indexFormat);writer.Write(mesh.subMeshCount);
                foreach(var attribute in mesh.GetVertexAttributes()){writer.Write((int)attribute.attribute);writer.Write((int)attribute.format);writer.Write(attribute.dimension);writer.Write(attribute.stream);}
                foreach(var p in mesh.vertices){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}foreach(var n in mesh.normals){writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);}foreach(var t in mesh.tangents){writer.Write(t.x);writer.Write(t.y);writer.Write(t.z);writer.Write(t.w);}foreach(var c in mesh.colors){writer.Write(c.r);writer.Write(c.g);writer.Write(c.b);writer.Write(c.a);}
                for(int channel=0;channel<8;channel++){var uv=new List<Vector4>();mesh.GetUVs(channel,uv);writer.Write(uv.Count);foreach(var p in uv){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(p.w);}}
                for(int sub=0;sub<mesh.subMeshCount;sub++){writer.Write((int)mesh.GetTopology(sub));var indices=mesh.GetIndices(sub);writer.Write(indices.Length);foreach(int index in indices)writer.Write(index);}
            }
            return stream.ToArray();
        }
    }
    [TestCase("unchanged",true,true)]
    [TestCase("uv3",false,true)]
    [TestCase("uv3-and-uv7",false,false)]
    [TestCase("uv3-and-position",false,false)]
    [TestCase("uv3-and-indices",false,false)]
    [TestCase("uv3-and-topology",false,false)]
    public void PhysicalComparisonNormalizesOnlyDisposableUv3AndRejectsAllOtherChanges(string mutation,bool fullExact,bool physicalExact)
    {
        Mesh saved=Fixture(),actual=UnityEngine.Object.Instantiate(saved);
        try
        {
            actual.name="Validation only actual SDK quad";
            if(mutation!="unchanged"){var uv=new List<Vector4>();actual.GetUVs(3,uv);uv[0]+=new Vector4(.125f,.5f,1,2);actual.SetUVs(3,uv);}
            if(mutation=="uv3-and-uv7"){var uv=new List<Vector4>();actual.GetUVs(7,uv);uv[1]+=Vector4.one;actual.SetUVs(7,uv);}
            if(mutation=="uv3-and-position"){var vertices=actual.vertices;vertices[0]+=Vector3.right*.125f;actual.vertices=vertices;actual.RecalculateBounds();}
            if(mutation=="uv3-and-indices")actual.triangles=new[]{0,2,1,2,1,3};
            if(mutation=="uv3-and-topology")actual.SetIndices(new[]{0,1,2,3},MeshTopology.Lines,0);
            byte[] actualBefore=Payload(actual),savedBefore=Payload(saved);
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true);var receiptType=type.GetNestedType("OwnedMountainNativeMeshComparison",BindingFlags.Public);var receipt=Activator.CreateInstance(receiptType);
            type.GetMethod("CompareOwnedMountainMeshPayload",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{actual,saved,receipt});var result=JsonUtility.FromJson<Comparison>(JsonUtility.ToJson(receipt));
            Assert.That(result.meshExact,Is.EqualTo(fullExact));Assert.That(result.physicalEquivalentAfterOnlyUv4Normalization,Is.EqualTo(physicalExact));Assert.That(result.inputMeshesUnchanged,Is.True);Assert.That(result.actualMeshPayloadBefore,Is.EqualTo(result.actualMeshPayloadAfter));Assert.That(result.savedMeshPayloadBefore,Is.EqualTo(result.savedMeshPayloadAfter));CollectionAssert.AreEqual(actualBefore,Payload(actual),"Actual grading input was changed.");CollectionAssert.AreEqual(savedBefore,Payload(saved),"Saved render/collision authority was changed.");
            if(mutation=="uv3"||mutation=="uv3-and-uv7"||mutation=="uv3-and-indices"||mutation=="uv3-and-topology"){Assert.That(result.reason,Is.EqualTo("UV channel 3"));Assert.That(result.actualUv4Count,Is.EqualTo(4));Assert.That(result.savedUv4Count,Is.EqualTo(4));Assert.That(result.actualUv4Sha256,Is.Not.EqualTo(result.savedUv4Sha256));}
            if(mutation=="uv3-and-uv7")Assert.That(result.normalizedComparisonFailure,Is.EqualTo("UV channel 7"),"The full comparator must continue beyond the normalized UV3 channel.");
            if(mutation=="uv3-and-indices"||mutation=="uv3-and-topology")Assert.That(result.normalizedComparisonFailure,Does.Contain("topology/indices"),"The full comparator must reach every index and topology after checking all UVs.");
            TestContext.WriteLine("ACTUAL_NATIVE_VALIDATION_MESH "+mutation+" rawFullExact="+result.meshExact+" physicalOnly="+result.physicalEquivalentAfterOnlyUv4Normalization+" originalInputsFullPayloadExact=true NOT_REAL_ROAD_WORLD_OR_RENDER_ACCEPTANCE");
        }
        finally{UnityEngine.Object.DestroyImmediate(actual);UnityEngine.Object.DestroyImmediate(saved);}
    }
}
#endif
