using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckDockDiving.Editor
{
    // Explicit one-artifact repair. Does not run on import, open, capture,
    // generation or installation. Root reviews/invokes this independently.
    public static class DockHotdogKnownRecipeRepair
    {
        const string ExpectedAsset="E3941DA51F947306FBB9D4F58288F71F32991ACEB2D57033A845793DAD7CE7A9";
        const string ExpectedMeta="54C5ADD2378E445BABCC5E4E165BC9C6805AF045DA152D56219B3F3010B64394";
        const string ExpectedGuid="aa59d84bd7192fd43b29148b5bb6e41e";
        const string Folder="Assets/TruckDockDiving/Customization/PrivateLicensedSplitBodies",PathName=Folder+"/novelty.hotdog.asset";
        static string Disk(string path)=>System.IO.Path.IsPathRooted(path)?path:System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..",path));
        static string Hash(string path){using(var algorithm=SHA256.Create())using(var stream=File.OpenRead(Disk(path)))return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","");}
        [MenuItem("Truck Sports/Customizer/Dock/Repair ONLY Reviewed Native31 Hotdog Recipe")]
        public static void RepairOnlyReviewedNative31Hotdog()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            for(int index=0;index<UnityEngine.SceneManagement.SceneManager.sceneCount;index++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Preserve dirty scene work before explicit known-recipe repair.");
            if(!File.Exists(Disk(PathName))||!File.Exists(Disk(PathName+".meta"))||Hash(PathName)!=ExpectedAsset||Hash(PathName+".meta")!=ExpectedMeta||AssetDatabase.AssetPathToGUID(PathName)!=ExpectedGuid)throw new InvalidOperationException("Refuse custom, absent, already repaired or changed recipe. Only the reviewed Native31 bytes/meta/GUID are authorized.");
            var catalog=Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");var body=catalog!=null?catalog.FindBody("novelty.hotdog"):null;var old=AssetDatabase.LoadAssetAtPath<DockAppearanceSplitRecipe>(PathName);var settings=AssetDatabase.LoadAssetAtPath<DockAppearanceSplitSettings>(LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.DockPlanePath);
            if(body==null||old==null||settings==null||!old.ValidFor(body)||old.sourceDependencyHash!=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(body.visualPrefab)).ToString())throw new InvalidOperationException("Reviewed source prefab/current valid old recipe/settings required.");
            var plane=settings.planes.Single(item=>item.bodyId==body.id);if(plane.vehiclePlaneY!=old.vehiclePlaneY||plane.canonicalToVehicle!=old.canonicalToVehicle)throw new InvalidOperationException("Artist plane/fit changes require separate review; this repair only removes duplicate vendor-ON ownership.");
            // Stronger than a subobject scan: refuse ANY external asset reference
            // to this recipe path. Native31 stopped before owner installation.
            var dependencyPaths=AssetDatabase.GetAllAssetPaths().Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&path!=PathName&&!AssetDatabase.IsValidFolder(path)).ToArray();
            const int dependencyBatchSize=512;double dependencyProgress=EditorApplication.timeSinceStartup;
            Debug.Log("DOCK_REPAIR_DEPENDENCY_PREFLIGHT start assets="+dependencyPaths.Length+" batchSize="+dependencyBatchSize+"; no asset mutation/backup has begun.");
            for(int first=0;first<dependencyPaths.Length;first+=dependencyBatchSize)
            {
                int count=Math.Min(dependencyBatchSize,dependencyPaths.Length-first);var batch=new string[count];Array.Copy(dependencyPaths,first,batch,0,count);
                if(AssetDatabase.GetDependencies(batch,false).Contains(PathName))
                {
                    foreach(string path in batch)if(AssetDatabase.GetDependencies(path,false).Contains(PathName))throw new InvalidOperationException("External recipe/old-mesh reference retained for review: "+path);
                    throw new InvalidOperationException("Dependency batch reported an unresolved external recipe reference; retained for explicit review: assets "+first+".."+(first+count));
                }
                int completed=first+count;if(first==0||completed==dependencyPaths.Length||completed%(dependencyBatchSize*8)==0||EditorApplication.timeSinceStartup-dependencyProgress>=5)
                {Debug.Log("DOCK_REPAIR_DEPENDENCY_PREFLIGHT checked="+completed+"/"+dependencyPaths.Length+"; no external recipe dependency in completed batches; no asset mutation/backup has begun.");dependencyProgress=EditorApplication.timeSinceStartup;}
            }
            var others=catalog.bodies.Where(item=>item.id!=body.id).SelectMany(item=>new[]{Folder+"/"+item.id+".asset",Folder+"/"+item.id+".asset.meta"}).ToDictionary(path=>path,Hash);
            var replacement=ScriptableObject.CreateInstance<DockAppearanceSplitRecipe>();replacement.name=old.name;replacement.hideFlags=old.hideFlags;replacement.bodyId=old.bodyId;replacement.sourcePrefab=old.sourcePrefab;replacement.sourceDependencyHash=old.sourceDependencyHash;replacement.vehiclePlaneY=old.vehiclePlaneY;replacement.canonicalToVehicle=old.canonicalToVehicle;replacement.capMaterial=old.capMaterial;
            var meshes=new List<Mesh>();bool mutation=false;string backup=null;
            try
            {
                var parts=new List<DockAppearanceSplitRecipe.Part>();var renderers=body.visualPrefab.GetComponentsInChildren<Renderer>(true);var tires=new HashSet<string>(body.wheelRendererNames??Array.Empty<string>());var point=plane.canonicalToVehicle.inverse.MultiplyPoint3x4(new Vector3(0,plane.vehiclePlaneY,0));var normal=plane.canonicalToVehicle.transpose.MultiplyVector(Vector3.up).normalized;
                for(int index=0;index<renderers.Length;index++)
                {
                    var renderer=renderers[index];if(!renderer.enabled||tires.Contains(renderer.name))continue;var filter=renderer.GetComponent<MeshFilter>();if(!(renderer is MeshRenderer)||filter==null||filter.sharedMesh==null||!filter.sharedMesh.isReadable||filter.sharedMesh.subMeshCount!=renderer.sharedMaterials.Length)throw new InvalidOperationException("Unchanged readable source material surfaces required.");var source=filter.sharedMesh;var matrix=body.visualPrefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
                    for(int material=0;material<source.subMeshCount;material++)for(int half=0;half<2;half++)
                    {
                        var mesh=LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.SliceDockSurfaceHalf(source,material,matrix,point,half==1?normal:-normal,half==1);if(mesh==null)continue;mesh.name=body.id+" renderer "+index+" material "+material+(half==1?" upper":" lower")+" reviewed coplanar repair";meshes.Add(mesh);parts.Add(new DockAppearanceSplitRecipe.Part{rendererIndex=index,materialIndex=material,sourceMaterialCount=renderer.sharedMaterials.Length,upper=half==1,sourceMesh=source,mesh=mesh});
                    }
                }
                replacement.parts=parts.ToArray();ValidateAreas(replacement,body);
                string project=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));backup=System.IO.Path.Combine(project,"Logs/TruckSports/PlaytestRepair/DockHotdogRecipeRepair",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(backup);File.Copy(Disk(PathName),System.IO.Path.Combine(backup,"novelty.hotdog.asset"),false);File.Copy(Disk(PathName+".meta"),System.IO.Path.Combine(backup,"novelty.hotdog.asset.meta"),false);
                if(Hash(System.IO.Path.Combine(backup,"novelty.hotdog.asset"))!=ExpectedAsset||Hash(System.IO.Path.Combine(backup,"novelty.hotdog.asset.meta"))!=ExpectedMeta)throw new InvalidOperationException("Full asset/meta backup verification failed before mutation.");
                var oldMeshes=AssetDatabase.LoadAllAssetsAtPath(PathName).OfType<Mesh>().ToArray();mutation=true;
                foreach(var mesh in meshes){AssetDatabase.AddObjectToAsset(mesh,old);EditorUtility.SetDirty(mesh);}EditorUtility.CopySerialized(replacement,old);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);
                var serialized=File.ReadAllText(Disk(PathName));if(System.Text.RegularExpressions.Regex.IsMatch(serialized,@"(?m)^\s+mesh: \{fileID: 0\}"))throw new InvalidOperationException("New embedded mesh references were not persisted; restoring verified backup.");
                foreach(var mesh in meshes)
                {
                    if(!AssetDatabase.Contains(mesh)||AssetDatabase.GetAssetPath(mesh)!=PathName||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id)||guid!=ExpectedGuid)throw new InvalidOperationException("New owned meshes must all be persistent recipe subassets.");
                    if(!System.Text.RegularExpressions.Regex.IsMatch(serialized,@"(?m)^--- !u!43 &"+id+@"\r?$")||!serialized.Contains("mesh: {fileID: "+id+"}"))throw new InvalidOperationException("Every native mesh body and its part reference must be persisted on disk before old owned subassets are removed.");
                }
                // No external dependencies were allowed; old unreferenced owned
                // meshes can now be removed without touching any other asset.
                foreach(var mesh in oldMeshes)UnityEngine.Object.DestroyImmediate(mesh,true);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);AssetDatabase.ImportAsset(PathName,ImportAssetOptions.ForceUpdate);
                var actual=AssetDatabase.LoadAssetAtPath<DockAppearanceSplitRecipe>(PathName);ValidateAreas(actual,body);if(Hash(PathName+".meta")!=ExpectedMeta||AssetDatabase.AssetPathToGUID(PathName)!=ExpectedGuid)throw new InvalidOperationException("Original recipe GUID/meta must stay byte-exact.");foreach(var pair in others)if(Hash(pair.Key)!=pair.Value)throw new InvalidOperationException("Another recipe/meta changed unexpectedly: "+pair.Key);
                File.WriteAllText(System.IO.Path.Combine(backup,"receipt.txt"),"EXPLICIT_REVIEWED_DOCK_HOTDOG_REPAIR\noldAsset="+ExpectedAsset+"\nnewAsset="+Hash(PathName)+"\nmeta="+ExpectedMeta+"\nguid="+ExpectedGuid+"\notherSixRecipesAndMetasByteExact=true\nexternalRecipeOrOldMeshReferences=none\nnewEmbeddedMeshes="+meshes.Count+"\n");Debug.Log("DOCK_KNOWN_HOTDOG_REPAIRED in-place GUID/meta preserved; six other recipes unchanged; full raw licensed backup PRIVATE at "+backup+". Native geometry gate must rerun before owner install.");
            }
            catch
            {
                if(mutation&&backup!=null){File.Copy(System.IO.Path.Combine(backup,"novelty.hotdog.asset"),Disk(PathName),true);File.Copy(System.IO.Path.Combine(backup,"novelty.hotdog.asset.meta"),Disk(PathName+".meta"),true);AssetDatabase.ImportAsset(PathName,ImportAssetOptions.ForceUpdate);Debug.LogWarning("Dock repair failed; exact verified prior asset/meta bytes restored from "+backup);}
                throw;
            }
            finally{foreach(var mesh in meshes)if(mesh!=null&&!AssetDatabase.Contains(mesh))UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(replacement);}
        }
        static double Area(Mesh mesh,int material,Matrix4x4 matrix){var vertices=mesh.vertices;var triangles=mesh.GetTriangles(material);double area=0;for(int face=0;face<triangles.Length;face+=3)area+=Vector3.Cross(matrix.MultiplyVector(vertices[triangles[face+1]]-vertices[triangles[face]]),matrix.MultiplyVector(vertices[triangles[face+2]]-vertices[triangles[face]])).magnitude*.5;return area;}
        static void ValidateAreas(DockAppearanceSplitRecipe recipe,SportsTruckAppearanceCatalog.Body body)
        {
            if(recipe==null||!recipe.ValidFor(body))throw new InvalidOperationException("Complete validated replacement recipe required.");var renderers=body.visualPrefab.GetComponentsInChildren<Renderer>(true);
            for(int index=0;index<renderers.Length;index++)
            {
                var renderer=renderers[index];if(!renderer.enabled||(body.wheelRendererNames??Array.Empty<string>()).Contains(renderer.name))continue;var source=renderer.GetComponent<MeshFilter>().sharedMesh;var matrix=body.visualPrefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
                for(int material=0;material<source.subMeshCount;material++){double original=Area(source,material,matrix);var parts=recipe.parts.Where(part=>part.rendererIndex==index&&part.materialIndex==material).ToArray();double cut=parts.Sum(part=>Area(part.mesh,0,matrix));if(Math.Abs(cut-original)>Math.Max(1e-4,original*.005))throw new InvalidOperationException("Unchanged0.5% source area gate failed: "+renderer.name+" material="+material+" original="+original.ToString("R")+" cut="+cut.ToString("R"));}
            }
        }
    }
}
