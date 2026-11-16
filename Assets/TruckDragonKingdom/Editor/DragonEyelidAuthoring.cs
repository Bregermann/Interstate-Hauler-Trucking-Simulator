#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // Explicit missing visible art, fitted to actual painted-eye UV surfaces.
    // Raw source meshes/rigs/materials/atlas and existing clips are untouched.
    public static class DragonEyelidAuthoring
    {
        const string SpeciesId="dragon-inferno";
        const string Original=DragonSpeciesAuthoring.DragonRoot+"Dragon Inferno Cute Series/";
        const string Private="Assets/TruckDragonKingdom/PrivateLicensedDragons/DerivedEyelids";
        const string Animal="Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon Animal.prefab";
        const string AtlasSha="e184e816f6a80056fe57935775457c4cfc68defd912a944c6470806092a8f4b6";
        const int Columns=20,Rows=6;
        static readonly float[] Frames={10,25,50,75,100};
        // Measured outline of the actual atlas eye (pixels, origin top-left),
        // expanded6% to cover its painted dark rim as well as iris/white.
        static readonly Vector2[] OutlinePixels={new Vector2(1516,576),new Vector2(1611,537),new Vector2(1632,529),new Vector2(1662,522),new Vector2(1707,516),new Vector2(1709,537),new Vector2(1707,564),new Vector2(1702,591),new Vector2(1683,623),new Vector2(1658,645),new Vector2(1627,659),new Vector2(1601,664),new Vector2(1568,662),new Vector2(1547,657),new Vector2(1532,643),new Vector2(1520,622),new Vector2(1516,603)};
        [Serializable] sealed class Receipt
        {
            public string scope,sourceMesh,sourceAtlas,atlasSha256,configuredPrefab;
            public string[] generatedPaths,generatedSha256,sourcePaths,sourceBefore,sourceAfter;
            public float rigidHeadMaximumError;
            public int sourceBlendShapes,sourceMaterials,vertices,triangles;
            public bool originalSourcesPreserved;
        }
        sealed class Surface
        {
            public Vector2[] uv;
            public Vector3[] positions,normals;
            public float[] modelX;
            public int[] indices;
            public Surface(GameObject model,SkinnedMeshRenderer skin,Transform head,Vector2[] textureCoordinates)
            {
                uv=textureCoordinates;var baked=new Mesh();
                try
                {
                    skin.BakeMesh(baked);indices=baked.triangles;
                    if(baked.vertexCount!=uv.Length)throw new InvalidOperationException("Actual skin and atlas vertex streams differ.");
                    var points=baked.vertices;var sourceNormals=baked.normals;
                    if(sourceNormals.Length!=points.Length){baked.RecalculateNormals();sourceNormals=baked.normals;}
                    positions=new Vector3[points.Length];normals=new Vector3[points.Length];modelX=new float[points.Length];
                    for(int index=0;index<points.Length;index++)
                    {
                        var world=skin.transform.TransformPoint(points[index]);positions[index]=head.InverseTransformPoint(world);modelX[index]=model.transform.InverseTransformPoint(world).x;
                        normals[index]=head.InverseTransformDirection(skin.transform.TransformDirection(sourceNormals[index])).normalized;
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(baked);}
            }
            public void Sample(Vector2 point,int side,out Vector3 position,out Vector3 normal)
            {
                float best=float.NegativeInfinity;position=normal=default;bool found=false;
                for(int triangle=0;triangle<indices.Length;triangle+=3)
                {
                    int a=indices[triangle],b=indices[triangle+1],c=indices[triangle+2];Vector2 x=uv[b]-uv[a],y=uv[c]-uv[a],p=point-uv[a];
                    float determinant=x.x*y.y-x.y*y.x;if(Mathf.Abs(determinant)<.0000001f)continue;
                    float wb=(p.x*y.y-p.y*y.x)/determinant,wc=(x.x*p.y-x.y*p.x)/determinant,wa=1-wb-wc;
                    if(wa<-.0001f||wb<-.0001f||wc<-.0001f)continue;
                    float lateral=(modelX[a]*wa+modelX[b]*wb+modelX[c]*wc)*side;if(lateral<.01f||lateral<best)continue;
                    best=lateral;position=positions[a]*wa+positions[b]*wb+positions[c]*wc;normal=(normals[a]*wa+normals[b]*wb+normals[c]*wc).normalized;found=true;
                }
                if(!found||normal.sqrMagnitude<.9f)throw new InvalidOperationException("Actual eye UV surface missing: side="+side+" uv="+point.ToString("F6")+". No substitute geometry was placed.");
            }
        }
        sealed class Vertex
        {
            public float u,from,to;
            public bool crease;
            public Vector2 tint;
        }
        public static GameObject InstallInfernoEyelids()=>Upgrade(false);
        public static GameObject RegenerateInfernoEyelids()=>Upgrade(true);
        static GameObject Upgrade(bool regenerate)
        {
            Guard();var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);
            if(catalog==null||!catalog.TryGet(SpeciesId,out var species))throw new InvalidOperationException("Actual Inferno catalog required.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Animal);if(prefab==null)throw new InvalidOperationException("Existing project-owned configured Inferno prefab required.");
            if(regenerate)Generate(species,true);
            var contents=PrefabUtility.LoadPrefabContents(Animal);
            try{InstallOnConfiguredInstance(contents,species);return PrefabUtility.SaveAsPrefabAsset(contents,Animal);}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        // Safe hook for the existing explicit configured-prefab factory. It
        // does nothing for other species and preserves an existing setup.
        public static void InstallOnConfiguredInstance(GameObject contents,DragonSpeciesDefinition species)
        {
            Guard();if(species==null||species.id!=SpeciesId)return;
            var motor=contents.GetComponentInChildren<DragonAnimalMotor>(true);if(motor==null)throw new InvalidOperationException("Configured actual motor required before fitting eyelids.");
            var existing=motor.GetComponent<DragonEyelidPresentation>();if(existing!=null){existing.ValidateBindings();return;}
            if(!species.TryGetAnimation(DragonAnimationAction.Sleep,out var sleep))throw new InvalidOperationException("Actual selected Sleep motion required.");
            Generate(species,false);
            var head=motor.GetComponentsInChildren<Transform>(true).Single(bone=>bone.name=="RigHead");
            var body=contents.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(skin=>AssetDatabase.GetAssetPath(skin.sharedMesh)==Original+"FBX/Dragon Inferno.FBX");
            if(body.sharedMaterials.Length!=1||body.sharedMaterial==null)throw new InvalidOperationException("Actual one-material Inferno skin required.");
            var presentation=motor.gameObject.AddComponent<DragonEyelidPresentation>();presentation.motor=motor;presentation.actualHead=head;presentation.sleepingClips=new[]{sleep.clip};
            presentation.lids=new SkinnedMeshRenderer[2];presentation.creases=new SkinnedMeshRenderer[2];
            for(int index=0;index<2;index++)
            {
                string side=index==0?"Left":"Right";
                presentation.lids[index]=Renderer(head,side+" actual surface eyelids",AssetDatabase.LoadAssetAtPath<Mesh>(Private+"/Inferno "+side+" eyelids.asset"),body.sharedMaterial);
                presentation.creases[index]=Renderer(head,side+" closed eyelid crease",AssetDatabase.LoadAssetAtPath<Mesh>(Private+"/Inferno "+side+" crease.asset"),body.sharedMaterial);
            }
            presentation.ValidateBindings();EditorUtility.SetDirty(presentation);
        }
        static SkinnedMeshRenderer Renderer(Transform head,string name,Mesh mesh,Material material)
        {
            if(mesh==null)throw new InvalidOperationException("Actual authored eyelid mesh required: "+name);
            var go=new GameObject("LWS "+name);go.transform.SetParent(head,false);go.layer=head.gameObject.layer;
            var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterial=material;skin.bones=new[]{head};skin.rootBone=head;skin.localBounds=mesh.bounds;skin.updateWhenOffscreen=false;skin.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;skin.receiveShadows=true;skin.enabled=false;return skin;
        }
        static void Generate(DragonSpeciesDefinition species,bool replace)
        {
            string[] assets={Private+"/Inferno Left eyelids.asset",Private+"/Inferno Left crease.asset",Private+"/Inferno Right eyelids.asset",Private+"/Inferno Right crease.asset"};
            if(!replace&&assets.All(path=>AssetDatabase.LoadAssetAtPath<Mesh>(path)!=null))return;
            if(!replace&&assets.Any(File.Exists))throw new InvalidOperationException("Partial existing eyelid assets retained; use the explicit selected Regenerate method after review.");
            string[] sources={Original+"Prefabs/Dragon Inferno.prefab",Original+"FBX/Dragon Inferno.FBX",Original+"Materials/Dragon Inferno.mat",Original+"Textures/Dragon Inferno.psd"};var before=sources.Select(Hash).ToArray();
            if(before[3]!=AtlasSha)throw new InvalidOperationException("The reviewed actual Inferno atlas bytes changed; re-audit UV placement rather than hiding unrelated art.");
            var model=UnityEngine.Object.Instantiate(species.visualPrefab);model.hideFlags=HideFlags.HideAndDontSave;model.SetActive(false);var created=new List<Mesh>();
            try
            {
                var head=model.GetComponentsInChildren<Transform>(true).Single(bone=>bone.name=="RigHead");var skin=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                if(skin.sharedMesh.blendShapeCount!=0||skin.sharedMaterials.Length!=1)throw new InvalidOperationException("Re-audit changed source eye/skin layout before authoring.");
                Vector2[] uv;
                using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(skin.sharedMesh))
                {using(var values=new NativeArray<Vector2>(snapshot[0].vertexCount,Allocator.Temp)){snapshot[0].GetUVs(0,values);uv=values.ToArray();}}
                var surface=new Surface(model,skin,head,uv);Quaternion reference=head.rotation;float maximum=0;
                try
                {
                    head.rotation=Quaternion.AngleAxis(10,model.transform.up)*head.rotation;var probe=new Surface(model,skin,head,uv);
                    foreach(int side in new[]{-1,1})for(int sample=1;sample<8;sample++)
                    {
                        float u=Mathf.Lerp(Umin,Umax,sample/8f);Range(u,out var low,out var high);var point=new Vector2(u,Mathf.Lerp(low,high,.5f));
                        surface.Sample(point,side,out var a,out _);probe.Sample(point,side,out var b,out _);maximum=Mathf.Max(maximum,Vector3.Distance(a,b));
                    }
                }
                finally{head.rotation=reference;}
                if(maximum>.003f)throw new InvalidOperationException("Actual painted-eye surface is not rigid enough to the real head for this fitted lid: maximumlocalerror="+maximum);
                foreach(int side in new[]{-1,1}){created.Add(Build(surface,side,false));created.Add(Build(surface,side,true));}
                Directory.CreateDirectory(Private);AssetDatabase.Refresh();
                for(int index=0;index<assets.Length;index++)
                {
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(assets[index]);
                    if(existing==null){AssetDatabase.CreateAsset(created[index],assets[index]);created[index]=null;}
                    else{EditorUtility.CopySerialized(created[index],existing);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);}
                }
                var after=sources.Select(Hash).ToArray();bool preserved=before.SequenceEqual(after);if(!preserved)throw new InvalidOperationException("Original owned sources changed during eyelid generation.");
                var meshes=assets.Select(AssetDatabase.LoadAssetAtPath<Mesh>).ToArray();
                const string report="Documentation/TruckSports/DragonKingdom/EyelidAuthoringReceipt.json";Directory.CreateDirectory(Path.GetDirectoryName(report));
                File.WriteAllText(report,JsonUtility.ToJson(new Receipt{scope="Actual source-surface geometry generated explicitly; no native playback/render/occlusion acceptance inferred. New real mesh morphs, no invented original eye bone/clip channel.",sourceMesh=sources[1],sourceAtlas=sources[3],atlasSha256=before[3],configuredPrefab=Animal,generatedPaths=assets,generatedSha256=assets.Select(Hash).ToArray(),sourcePaths=sources,sourceBefore=before,sourceAfter=after,originalSourcesPreserved=preserved,rigidHeadMaximumError=maximum,sourceBlendShapes=skin.sharedMesh.blendShapeCount,sourceMaterials=skin.sharedMaterials.Length,vertices=meshes.Sum(mesh=>mesh.vertexCount),triangles=meshes.Sum(mesh=>mesh.triangles.Length/3)},true));
            }
            finally{foreach(var mesh in created)if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(model);}
        }
        static float Umin=>Boundary().Min(point=>point.x);
        static float Umax=>Boundary().Max(point=>point.x);
        static Vector2[] Boundary()=>OutlinePixels.Select(point=>{point=new Vector2(1612.5f,590)+(point-new Vector2(1612.5f,590))*1.06f;return new Vector2(point.x/2048,1-point.y/2048);}).ToArray();
        static void Range(float u,out float low,out float high)
        {
            var polygon=Boundary();low=float.PositiveInfinity;high=float.NegativeInfinity;
            for(int index=0;index<polygon.Length;index++)
            {
                var a=polygon[index];var b=polygon[(index+1)%polygon.Length];if(u<Mathf.Min(a.x,b.x)-.000001f||u>Mathf.Max(a.x,b.x)+.000001f)continue;
                if(Mathf.Abs(a.x-b.x)<.000001f){low=Mathf.Min(low,a.y,b.y);high=Mathf.Max(high,a.y,b.y);continue;}
                float v=Mathf.Lerp(a.y,b.y,Mathf.InverseLerp(a.x,b.x,u));low=Mathf.Min(low,v);high=Mathf.Max(high,v);
            }
            if(!float.IsFinite(low)||!float.IsFinite(high)||high<=low)throw new InvalidOperationException("Actual eye outline has no finite vertical span at "+u);
        }
        static Mesh Build(Surface surface,int side,bool crease)
        {
            var vertices=new List<Vertex>();var indices=new List<int>();
            int rows=crease?1:Rows,grids=crease?1:2;
            for(int half=0;half<grids;half++)
            {
                int first=vertices.Count;
                for(int column=0;column<=Columns;column++)
                {
                    float t=(column+.001f)/(Columns+.002f),u=Mathf.Lerp(Umin,Umax,t);Range(u,out var low,out var high);float seam=Mathf.Lerp(low,high,.44f)+Mathf.Sin(t*Mathf.PI)*.0015f;
                    for(int row=0;row<=rows;row++)
                    {
                        float v=row/(float)rows;
                        vertices.Add(new Vertex{u=u,from=crease?seam:(half==0?high:low),to=crease?(row==0?-.0008f:.0008f):Mathf.Lerp(half==0?high:low,seam,v),crease=crease,tint=crease?new Vector2(.76171875f,.6791992f):new Vector2(u-.12f,Mathf.Lerp(half==0?high:low,seam,v))});
                    }
                }
                for(int column=0;column<Columns;column++)for(int row=0;row<rows;row++)
                {int a=first+column*(rows+1)+row,b=a+rows+1;indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            }
            var positions=new Vector3[vertices.Count];var normals=new Vector3[vertices.Count];var texture=new Vector2[vertices.Count];
            Evaluate(surface,side,vertices,0,positions,normals);var closed=new Vector3[vertices.Count];var closedNormals=new Vector3[vertices.Count];Evaluate(surface,side,vertices,1,closed,closedNormals);
            for(int triangle=0;triangle<indices.Count;triangle+=3)
            {
                int a=indices[triangle],b=indices[triangle+1],c=indices[triangle+2];
                if(Vector3.Dot(Vector3.Cross(closed[b]-closed[a],closed[c]-closed[a]),closedNormals[a]+closedNormals[b]+closedNormals[c])<0){indices[triangle+1]=c;indices[triangle+2]=b;}
            }
            for(int index=0;index<vertices.Count;index++)texture[index]=vertices[index].tint;
            var mesh=new Mesh{name="Inferno "+(side<0?"Left":"Right")+(crease?" genuine closed crease":" fitted upper and lower eyelids")};mesh.vertices=positions;mesh.normals=normals;mesh.uv=texture;mesh.triangles=indices.ToArray();mesh.bindposes=new[]{Matrix4x4.identity};mesh.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=0,weight0=1},vertices.Count).ToArray();
            var bounds=new Bounds(positions[0],Vector3.zero);foreach(var point in positions)bounds.Encapsulate(point);foreach(var point in closed)bounds.Encapsulate(point);bounds.Expand(.02f);mesh.bounds=bounds;
            foreach(float frame in Frames)
            {
                var points=new Vector3[vertices.Count];var directions=new Vector3[vertices.Count];Evaluate(surface,side,vertices,frame/100,points,directions);
                for(int index=0;index<points.Length;index++){points[index]-=positions[index];directions[index]-=normals[index];}
                mesh.AddBlendShapeFrame(DragonEyelidPresentation.ClosureShape,frame,points,directions,null);
            }
            return mesh;
        }
        static void Evaluate(Surface surface,int side,List<Vertex> vertices,float closure,Vector3[] positions,Vector3[] normals)
        {
            for(int index=0;index<vertices.Count;index++)
            {
                var vertex=vertices[index];float v=vertex.crease?vertex.from+vertex.to*Mathf.Lerp(.001f,1,closure):Mathf.Lerp(vertex.from,vertex.to,Mathf.Lerp(.006f,1,closure));
                surface.Sample(new Vector2(vertex.u,v),side,out var point,out var normal);positions[index]=point+normal*(vertex.crease?.0065f:.0045f);normals[index]=normal;
            }
        }
        static string Hash(string path){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
        static void Guard(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Explicit stopped configured-prefab eyelid authoring required.");}
    }
}
#endif
