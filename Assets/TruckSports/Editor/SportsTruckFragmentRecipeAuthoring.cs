using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.CoreCollapse.EzySlice;
using Assets.CoreCollapse.EzySlice.Framework;
using UnityEditor;
using UnityEngine;
using Plane=Assets.CoreCollapse.EzySlice.Framework.Plane;

namespace LWS.TruckSports.Editor
{
    public static class SportsTruckFragmentRecipeAuthoring
    {
        public const string Folder="Assets/TruckSports/Customization/PrivateLicensedFragments";
        [MenuItem("Truck Sports/Customizer/Create Missing Vendor Sliced Body Recipes")]
        public static void CreateMissingRecipes()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            if(catalog==null||!catalog.Validate(out _))throw new InvalidOperationException("The existing body catalog and livery must be authored first.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var body in catalog.bodies)Create(body);
            AssetDatabase.SaveAssets();Debug.Log("SPORTS_FRAGMENT_RECIPES_AUTHORED actual vendor EzySlice body surfaces/submeshes; no scene or vehicle physics changed. Native geometry/render proof pending.");
        }
        [MenuItem("Truck Sports/Customizer/Repair Known Incomplete Standard Fragment Recipe")]
        public static void RepairKnownIncompleteStandardRecipe()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            const string expected="6827FBCFE8AF3BE6E8E27A5E827EA7ED0E2816978FEE4918D8368665163E2D71";string path=Folder+"/semi.standard.asset";
            if(!File.Exists(path)||Hash(path)!=expected)throw new InvalidOperationException("Only the exact retained native19 incomplete standard recipe is eligible; custom or changed data is preserved.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var body=catalog!=null?catalog.FindBody("semi.standard"):null;var recipe=AssetDatabase.LoadAssetAtPath<SportsTruckFragmentRecipe>(path);
            if(body==null||recipe==null||recipe.version!=1||recipe.bodyId!="semi.standard"||recipe.sourcePrefab!=body.visualPrefab||recipe.sourceDependencyHash!=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(body.visualPrefab)).ToString()||recipe.parts==null||recipe.parts.Length==0||recipe.parts.Any(part=>part==null||part.mesh!=null)||AssetDatabase.LoadAllAssetsAtPath(path).Any(asset=>asset!=recipe))throw new InvalidOperationException("Incomplete owned recipe signature changed or real/custom subassets exist; preserved for inspection.");
            string guid=AssetDatabase.AssetPathToGUID(path),backup="Library/TruckSports/FragmentRecipeRecovery/semi.standard."+expected+".asset";Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(File.Exists(backup)){if(Hash(backup)!=expected)throw new InvalidOperationException("Existing recovery backup differs; preserved.");}else File.Copy(path,backup);
            if(File.Exists(backup+".meta")){if(File.ReadAllText(backup+".meta")!=File.ReadAllText(path+".meta"))throw new InvalidOperationException("Existing recovery metadata backup differs; preserved.");}else File.Copy(path+".meta",backup+".meta");
            Create(body,recipe);if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Fragment recovery changed the original GUID.");
            Debug.Log("SPORTS_FRAGMENT_KNOWN_PARTIAL_RECOVERED "+path+" preservedGUID="+guid+" exactOriginalBackup="+backup+"; authored geometry validated and independently saved.");
        }
        static string Hash(string path){using(var hash=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","");}
        static void Create(SportsTruckAppearanceCatalog.Body body,SportsTruckFragmentRecipe recovery=null)
        {
            string path=Folder+"/"+body.id+".asset";var existing=AssetDatabase.LoadAssetAtPath<SportsTruckFragmentRecipe>(path);
            string sourceHash=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(body.visualPrefab)).ToString();
            if(existing!=null&&existing!=recovery){if(existing.sourceDependencyHash!=sourceHash||!existing.ValidFor(body,out _))throw new InvalidOperationException("Preserved existing recipe requires explicit inspection after source/body art changes: "+body.id);return;}
            var recipe=ScriptableObject.CreateInstance<SportsTruckFragmentRecipe>();recipe.bodyId=body.id;recipe.sourcePrefab=body.visualPrefab;recipe.sourceDependencyHash=sourceHash;
            var cap=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Owned cut metal.mat");
            if(cap==null){cap=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Owned cut metal"};cap.SetColor("_BaseColor",new Color(.18f,.2f,.21f));cap.SetFloat("_Metallic",.6f);AssetDatabase.CreateAsset(cap,Folder+"/Owned cut metal.mat");}
            recipe.capMaterial=cap;
            var renderers=body.visualPrefab.GetComponentsInChildren<Renderer>(true);var tires=new HashSet<string>(body.wheelRendererNames??Array.Empty<string>());var parts=new List<SportsTruckFragmentRecipe.Part>();var meshes=new List<Mesh>();
            try
            {
                var bounds=body.visualEnvelope;float minX=bounds.min.x,maxX=bounds.max.x,minZ=bounds.min.z,maxZ=bounds.max.z;
                for(int index=0;index<renderers.Length;index++)
                {
                    var renderer=renderers[index];if(!renderer.enabled||tires.Contains(renderer.name))continue;
                    var filter=renderer.GetComponent<MeshFilter>();if(!(renderer is MeshRenderer)||filter==null||filter.sharedMesh==null||!filter.sharedMesh.isReadable||filter.sharedMesh.subMeshCount!=renderer.sharedMaterials.Length)throw new InvalidOperationException("Readable matching mesh/submesh material foundation required: "+body.id+"/"+renderer.name);
                    Matrix4x4 toBody=body.visualPrefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;if(!float.IsFinite(toBody.determinant)||Mathf.Abs(toBody.determinant)<1e-9f)throw new InvalidOperationException("Degenerate body renderer transform: "+renderer.name);
                    // Vendor hull UV/normal generation uses its first submesh.
                    // Slice one original material surface at a time so a region
                    // lacking material0 cannot lose another material's atlas UVs.
                    for(int material=0;material<filter.sharedMesh.subMeshCount;material++)for(int region=0;region<8;region++)
                    {
                        float x0=Mathf.Lerp(minX,maxX,region%2*.5f),x1=Mathf.Lerp(minX,maxX,(region%2+1)*.5f),z0=Mathf.Lerp(minZ,maxZ,region/2*.25f),z1=Mathf.Lerp(minZ,maxZ,(region/2+1)*.25f);
                        Mesh clip=UnityEngine.Object.Instantiate(filter.sharedMesh);
                        var triangles=filter.sharedMesh.GetTriangles(material);clip.subMeshCount=1;clip.SetTriangles(triangles,0);
                        string context=body.id+" renderer="+index+"/"+renderer.name+" source="+AssetDatabase.GetAssetPath(filter.sharedMesh)+" mesh="+filter.sharedMesh.name+" material="+material+" region="+region;string stage="source";
                        try
                        {
                            RemoveDegenerateTriangles(clip,context+" "+stage);
                            ValidateSliceAttributes(clip);
                            stage="x-min";clip=Keep(clip,toBody,new Vector3(x0,0,0),Vector3.right,1,context+" "+stage);if(clip==null)continue;
                            stage="x-max";clip=Keep(clip,toBody,new Vector3(x1,0,0),Vector3.left,1,context+" "+stage);if(clip==null)continue;
                            stage="z-min";clip=Keep(clip,toBody,new Vector3(0,0,z0),Vector3.forward,1,context+" "+stage);if(clip==null)continue;
                            stage="z-max";clip=Keep(clip,toBody,new Vector3(0,0,z1),Vector3.back,1,context+" "+stage);if(clip==null)continue;
                            if(clip.vertexCount<3||clip.GetTriangles(0).Length==0)continue;
                            if(clip.normals.Length!=clip.vertexCount)clip.RecalculateNormals();
                            foreach(var vertex in clip.vertices)if(!float.IsFinite(vertex.x)||!float.IsFinite(vertex.y)||!float.IsFinite(vertex.z))throw new InvalidOperationException("Nonfinite vendor slice geometry.");
                            var normals=clip.normals;foreach(int vertex in clip.triangles)if(!float.IsFinite(normals[vertex].x)||!float.IsFinite(normals[vertex].y)||!float.IsFinite(normals[vertex].z))throw new InvalidOperationException("Nonfinite vendor slice normals.");
                            if(filter.sharedMesh.uv.Length==filter.sharedMesh.vertexCount&&clip.uv.Length!=clip.vertexCount)throw new InvalidOperationException("Vendor slice lost an original material surface's atlas UVs.");
                            clip.name=body.id+" renderer "+index+" material "+material+" region "+region;meshes.Add(clip);parts.Add(new SportsTruckFragmentRecipe.Part{rendererIndex=index,region=region,sourceMesh=filter.sharedMesh,mesh=clip,sourceMaterialCount=renderer.sharedMaterials.Length,sourceMaterialIndex=material});clip=null;
                        }
                        catch(Exception sliceError){throw new InvalidOperationException(context+" plane="+stage+" · "+sliceError.Message,sliceError);}
                        finally{if(clip!=null)UnityEngine.Object.DestroyImmediate(clip);}
                    }
                }
                recipe.parts=parts.ToArray();if(!recipe.ValidFor(body,out var error))throw new InvalidOperationException(body.id+" · "+error);
                if(recovery!=null){string name=recovery.name;EditorUtility.CopySerialized(recipe,recovery);UnityEngine.Object.DestroyImmediate(recipe);recipe=recovery;recipe.name=name;}else AssetDatabase.CreateAsset(recipe,path);
                foreach(var mesh in meshes){AssetDatabase.AddObjectToAsset(mesh,recipe);EditorUtility.SetDirty(mesh);}EditorUtility.SetDirty(recipe);
                // Commit each complete body before moving to the next. A later
                // vendor slice refusal must not strand earlier main assets with
                // null references to never-saved embedded mesh subassets.
                AssetDatabase.SaveAssetIfDirty(recipe);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var saved=AssetDatabase.LoadAssetAtPath<SportsTruckFragmentRecipe>(path);if(saved==null)throw new InvalidOperationException("Saved recipe is missing: "+body.id);if(!saved.ValidFor(body,out var savedError))throw new InvalidOperationException("Saved recipe validation failed: "+body.id+" · "+savedError);
                var embedded=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();if(embedded.Length!=meshes.Count||saved.parts.Any(part=>!embedded.Contains(part.mesh)))throw new InvalidOperationException("Saved recipe is missing embedded mesh data: "+body.id);
                Debug.Log("SPORTS_FRAGMENT_RECIPE_COMMITTED "+body.id+" embeddedMeshes="+embedded.Length+" path="+path);
            }
            catch{foreach(var mesh in meshes)if(mesh!=null&&!AssetDatabase.Contains(mesh))UnityEngine.Object.DestroyImmediate(mesh);if(recipe!=null&&!AssetDatabase.Contains(recipe))UnityEngine.Object.DestroyImmediate(recipe);throw;}
        }
        // Explicit author adapters may reuse the same vendor foundation for a
        // different plane layout. The caller supplies an owned, readable clone
        // with its chosen original material surface in submesh0, and retains
        // responsibility for unsupported source attributes and private export.
        // A replaced/discarded working mesh is consumed; asset meshes refuse.
        public static Mesh ClipOwnedWorkingMesh(Mesh working,Matrix4x4 toBody,Vector3 bodyPoint,Vector3 bodyNormal,int capIndex,string context)
        {
            if(working==null)return null;if(AssetDatabase.Contains(working)||!working.isReadable)throw new InvalidOperationException("Clone the readable source into an owned working mesh before clipping.");RemoveDegenerateTriangles(working,context+" source");ValidateSliceAttributes(working);return Keep(working,toBody,bodyPoint,bodyNormal,capIndex,context);
        }
        static Mesh Keep(Mesh mesh,Matrix4x4 toBody,Vector3 bodyPoint,Vector3 bodyNormal,int capIndex,string context)
        {
            Vector3 point=toBody.inverse.MultiplyPoint3x4(bodyPoint),normal=toBody.transpose.MultiplyVector(bodyNormal).normalized;bool above=false,below=false;
            var vertices=mesh.vertices;foreach(int index in mesh.triangles){float distance=Vector3.Dot(normal,vertices[index]-point);above|=distance>1e-5f;below|=distance< -1e-5f;}
            if(!below)return mesh;if(!above){UnityEngine.Object.DestroyImmediate(mesh);return null;}
            var hull=Slicer.Slice(mesh,new Plane(point,normal),new TextureRegion(0,0,1,1),capIndex);
            if(hull==null||hull.upperHull==null)throw new InvalidOperationException("Vendor slice did not produce a required intersecting body hull.");
            var upper=hull.upperHull;if(hull.lowerHull!=null)UnityEngine.Object.DestroyImmediate(hull.lowerHull);
            try{RemoveDegenerateTriangles(upper,context);RepairGeneratedTangents(upper,context);ValidateSliceAttributes(upper);}
            catch{UnityEngine.Object.DestroyImmediate(upper);throw;}
            UnityEngine.Object.DestroyImmediate(mesh);return upper;
        }
        // EzySlice's barycentric interpolation divides by a triangle's largest
        // projected cross component. Imported zero-area faces and numerical
        // slivers from an earlier cut cannot be interpolated. Remove only those
        // faces on the owned working mesh; never recalculate valid hard normals
        // or replace atlas coordinates/material boundaries.
        static int RemoveDegenerateTriangles(Mesh mesh,string context)
        {
            var vertices=mesh.vertices;var kept=new int[mesh.subMeshCount][];int removed=0;double removedArea=0,totalArea=0;
            for(int submesh=0;submesh<mesh.subMeshCount;submesh++)
            {
                var indices=mesh.GetTriangles(submesh);var valid=new List<int>(indices.Length);
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                    if(!Finite(a)||!Finite(b)||!Finite(c))throw new InvalidOperationException("Nonfinite vendor slice geometry.");
                    double ux=(double)b.x-a.x,uy=(double)b.y-a.y,uz=(double)b.z-a.z,vx=(double)c.x-a.x,vy=(double)c.y-a.y,vz=(double)c.z-a.z;
                    double nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx,area=Math.Sqrt(nx*nx+ny*ny+nz*nz)*.5,edge=Math.Max(ux*ux+uy*uy+uz*uz,vx*vx+vy*vy+vz*vz);totalArea+=area;
                    var projected=Vector3.Cross(b-a,c-a);
                    if(area<=edge*1e-12||projected.x==0f&&projected.y==0f&&projected.z==0f){removed++;removedArea+=area;continue;}
                    valid.Add(indices[i]);valid.Add(indices[i+1]);valid.Add(indices[i+2]);
                }
                kept[submesh]=valid.ToArray();
            }
            if(removed==0)return 0;
            if(removedArea>Math.Max(1e-12,totalArea*1e-8))throw new InvalidOperationException("Degenerate cleanup would discard meaningful original surface area: "+removedArea+"/"+totalArea);
            var oldIndices=kept.SelectMany(indices=>indices).Distinct().ToArray();var remap=new Dictionary<int,int>();for(int i=0;i<oldIndices.Length;i++)remap.Add(oldIndices[i],i);
            var normals=mesh.normals;var tangents=mesh.tangents;var colors=mesh.colors;var channels=new List<Vector4>[8];for(int channel=0;channel<8;channel++){channels[channel]=new List<Vector4>();mesh.GetUVs(channel,channels[channel]);}
            var format=mesh.indexFormat;mesh.Clear();mesh.indexFormat=format;mesh.vertices=oldIndices.Select(index=>vertices[index]).ToArray();
            if(normals.Length==vertices.Length)mesh.normals=oldIndices.Select(index=>normals[index]).ToArray();if(tangents.Length==vertices.Length)mesh.tangents=oldIndices.Select(index=>tangents[index]).ToArray();if(colors.Length==vertices.Length)mesh.colors=oldIndices.Select(index=>colors[index]).ToArray();
            for(int channel=0;channel<8;channel++)if(channels[channel].Count==vertices.Length)mesh.SetUVs(channel,oldIndices.Select(index=>channels[channel][index]).ToList());
            mesh.subMeshCount=kept.Length;for(int submesh=0;submesh<kept.Length;submesh++)mesh.SetTriangles(kept[submesh].Select(index=>remap[index]).ToArray(),submesh);mesh.RecalculateBounds();
            Debug.Log("SPORTS_FRAGMENT_DEGENERATE_CLEANUP "+context+" faces="+removed+" discardedArea="+removedArea.ToString("R")+" totalArea="+totalArea.ToString("R"));return removed;
        }
        static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
        static bool Finite(Vector4 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z)&&float.IsFinite(value.w);
        static int RepairGeneratedTangents(Mesh mesh,string context)
        {
            var original=mesh.tangents;if(original.Length!=mesh.vertexCount)return 0;int invalid=0;foreach(var tangent in original)if(!Finite(tangent))invalid++;if(invalid==0)return 0;
            var normals=mesh.normals;var uv=mesh.uv;
            if(normals.Length!=mesh.vertexCount||uv.Length!=mesh.vertexCount)throw new InvalidOperationException("Generated tangent repair requires unchanged complete normals and atlas coordinates.");
            foreach(int index in mesh.triangles)if(!Finite(normals[index])||!float.IsFinite(uv[index].x)||!float.IsFinite(uv[index].y))throw new InvalidOperationException("Generated tangent repair refused invalid normal/UV inputs.");
            mesh.RecalculateTangents();var repaired=mesh.tangents;
            for(int i=0;i<original.Length;i++){if(Finite(original[i]))repaired[i]=original[i];else if(!Finite(repaired[i]))throw new InvalidOperationException("Unity could not derive a finite tangent for the generated cut surface.");}
            mesh.tangents=repaired;Debug.Log("SPORTS_FRAGMENT_GENERATED_TANGENTS_REPAIRED "+context+" invalidGeneratedValues="+invalid+"; existing finite tangents/normals/UVs unchanged.");return invalid;
        }
        static void ValidateSliceAttributes(Mesh mesh)
        {
            var normals=mesh.normals;var uv=mesh.uv;var tangents=mesh.tangents;
            foreach(int index in mesh.triangles)
            {
                if(normals.Length==mesh.vertexCount&&!Finite(normals[index]))throw new InvalidOperationException("Nonfinite vendor slice normals on a nondegenerate surface.");
                if(uv.Length==mesh.vertexCount&&(!float.IsFinite(uv[index].x)||!float.IsFinite(uv[index].y)))throw new InvalidOperationException("Nonfinite vendor slice atlas coordinates on a nondegenerate surface.");
                if(tangents.Length==mesh.vertexCount&&(!float.IsFinite(tangents[index].x)||!float.IsFinite(tangents[index].y)||!float.IsFinite(tangents[index].z)||!float.IsFinite(tangents[index].w)))throw new InvalidOperationException("Nonfinite vendor slice tangents on a nondegenerate surface.");
            }
        }
    }
}
