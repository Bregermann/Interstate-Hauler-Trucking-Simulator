#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.InterstateHauler;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Scene-owned variants over the installed material factory. The reusable
    // world, source road material, meshes, colliders and vendor shaders stay intact.
    public static partial class DragonKingdomRoadSnowAuthoring
    {
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        static string Hash(string path){string full=Path.GetFullPath(path);if(Environment.OSVersion.Platform==PlatformID.Win32NT&&!full.StartsWith(@"\\?\",StringComparison.Ordinal))full=@"\\?\"+full;using(var sha=SHA256.Create())using(var file=File.OpenRead(full))return string.Concat(sha.ComputeHash(file).Select(value=>value.ToString("x2")));}
        public static MeshRenderer[] Roads(Scene scene)
        {
            var world=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
            // Exact output contract of SportsEasyRoadsAuthoring.Bake/Persist,
            // also used by Kingdom BakeRoads. Decorative meshes cannot qualify.
            var colliders=world.GetComponentsInChildren<MeshCollider>(true).Where(collider=>collider.enabled&&!collider.isTrigger&&collider.sharedMesh!=null&&collider.GetComponentInParent<Rigidbody>()==null).Where(collider=>
            {
                string path=AssetDatabase.GetAssetPath(collider.sharedMesh);return path.StartsWith("Assets/TruckDragonKingdom/Models/Kingdom ",StringComparison.Ordinal)&&Path.GetFileName(path).Contains(" EasyRoads mesh ")&&path.EndsWith(".asset",StringComparison.Ordinal);
            }).ToArray();
            foreach(var edge in world.definition.graph.edges.Where(edge=>edge.edgeId!="boarding-deck"))
                if(!colliders.Any(collider=>Path.GetFileName(AssetDatabase.GetAssetPath(collider.sharedMesh)).StartsWith("Kingdom "+edge.edgeId+" EasyRoads mesh ",StringComparison.Ordinal)))throw new InvalidOperationException("Actual graph edge baked geometry missing: "+edge.edgeId);
            foreach(var collider in colliders)if(collider.GetComponent<MeshFilter>()?.sharedMesh!=collider.sharedMesh||collider.GetComponent<MeshRenderer>()==null)throw new InvalidOperationException("Actual baked road must retain matching renderer/filter/physical mesh: "+AssetDatabase.GetAssetPath(collider.sharedMesh));
            return colliders.Select(collider=>collider.GetComponent<MeshRenderer>()).Distinct().ToArray();
        }
        public static int BindSceneRoads(SportsEventSession match)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||match==null||!SportsDragonGames.TryFindScene(match.gameObject.scene.name,out _))throw new InvalidOperationException("Explicit stopped authoring of one actual registered Dragon scene required.");
            var snow=match.GetComponent<SportsSnowSurface>();if(snow==null||!snow.traceExposedRoads)throw new InvalidOperationException("Existing real-contact Sports snow bridge must already be configured.");
            var roads=Roads(match.gameObject.scene);if(roads.Length==0)throw new InvalidOperationException("Actual marked kingdom road geometry required.");
            string folder="Assets/"+match.gameObject.scene.name+"/Materials/Kingdom Road Snow";int changed=0;
            var quality=new SerializedObject(snow.surface);var resolution=quality.FindProperty("traceMaskResolution");if(resolution==null)throw new InvalidOperationException("Updated project-owned Weatherade bridge configuration is required.");
            if(resolution.intValue!=4096){resolution.intValue=4096;quality.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(snow.surface);changed++;}
            foreach(var renderer in roads)
            {
                var marker=renderer.GetComponent<LwsRoadSurface>();if(marker==null)
                {
                    string path=AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh),meshGuid=AssetDatabase.AssetPathToGUID(path);var edge=renderer.GetComponentInParent<DragonKingdomWorld>().definition.graph.edges.FirstOrDefault(candidate=>Path.GetFileName(path).StartsWith("Kingdom "+candidate.edgeId+" EasyRoads mesh ",StringComparison.Ordinal));marker=renderer.gameObject.AddComponent<LwsRoadSurface>();marker.Configure(edge?.roadId??"DK_BAKED_"+meshGuid,edge?.segmentId??"DK_BAKED_SURFACE_"+meshGuid,LwsRoadSurfaceType.Gravel,"Actual authored EasyRoads kingdom gravel");EditorUtility.SetDirty(marker);changed++;
                }
                var materials=renderer.sharedMaterials;bool different=false;
                for(int index=0;index<materials.Length;index++)
                {
                    var source=materials[index];if(source==null)throw new InvalidOperationException("Preserve and inspect missing original road material: "+renderer.name);
                    if(LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(source,LwsWeatheradeSurfaceMaterialMode.Snow))continue;
                    string sourcePath=AssetDatabase.GetAssetPath(source),guid=AssetDatabase.AssetPathToGUID(sourcePath);if(string.IsNullOrEmpty(guid))throw new InvalidOperationException("Asset-backed actual road source required: "+renderer.name);
                    Folder(folder);string path=folder+"/"+guid+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null)
                    {
                        string before=Hash(sourcePath),beforeMeta=Hash(sourcePath+".meta");
                        if(!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial(source.name+" ("+match.gameObject.scene.name+" road weather)",source,Color.gray,LwsWeatheradeSurfaceMaterialMode.Snow,out material)||material.shader==null||!material.shader.isSupported)throw new InvalidOperationException("Actual installed supported Weatherade snow shader required.");
                        string map=source.HasProperty("_BaseMap")?"_BaseMap":source.HasProperty("_MainTex")?"_MainTex":null;
                        if(map!=null){material.SetTextureScale("_BaseMap",source.GetTextureScale(map));material.SetTextureOffset("_BaseMap",source.GetTextureOffset(map));}
                        material.SetFloat("_CoverageAmount",0);material.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(material,path);AssetDatabase.SaveAssetIfDirty(material);
                        if(Hash(sourcePath)!=before||Hash(sourcePath+".meta")!=beforeMeta)throw new InvalidOperationException("Original source material changed during scene-only variant authoring.");
                    }
                    if(!LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(material,LwsWeatheradeSurfaceMaterialMode.Snow))throw new InvalidOperationException("Preserve customized incompatible road variant: "+path);
                    materials[index]=material;different=true;
                }
                if(different){renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);changed++;}
            }
            return changed;
        }
        public static Dictionary<string,string> Authority(Scene scene)
        {
            var result=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var root in scene.GetRootGameObjects())foreach(var item in root.GetComponentsInChildren<Component>(true))if(item!=null)
            {
                string id=GlobalObjectId.GetGlobalObjectIdSlow(item).ToString();result.Add(id,item is Transform||item is Rigidbody||item is Collider||item is MeshFilter?EditorJsonUtility.ToJson(item):item.GetType().AssemblyQualifiedName);
            }
            return result;
        }
        public static void UpgradeSavedRoadSnow()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            for(int index=0;index<SceneManager.sceneCount;index++)if(SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Save user scene edits before explicit road weather upgrade.");
            var setup=EditorSceneManager.GetSceneManagerSetup();var receipts=new List<string>();
            string shared=DragonKingdomWorldAuthoring.PrefabPath;string sharedHash=Hash(shared),sharedMeta=Hash(shared+".meta");
            try
            {
                foreach(var entry in SportsDragonGames.All)
                {
                    var scene=EditorSceneManager.OpenScene(entry.ScenePath,OpenSceneMode.Single);string guid=AssetDatabase.AssetPathToGUID(entry.ScenePath),meta=Hash(entry.ScenePath+".meta");var before=Authority(scene);
                    var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).Single();int changed=BindSceneRoads(match);
                    if(changed>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save selected road material overrides.");}
                    var after=Authority(scene);if(before.Any(pair=>!after.TryGetValue(pair.Key,out string value)||value!=pair.Value)||after.Where(pair=>!before.ContainsKey(pair.Key)).Any(pair=>pair.Value!=typeof(LwsRoadSurface).AssemblyQualifiedName))throw new InvalidOperationException("Original component identities/transforms/physics/meshes changed or non-road component added: "+entry.scene);
                    if(AssetDatabase.AssetPathToGUID(entry.ScenePath)!=guid||Hash(entry.ScenePath+".meta")!=meta)throw new InvalidOperationException("Scene GUID or metadata changed.");
                    receipts.Add(entry.scene+" roadRenderers="+Roads(scene).Length+" changed="+changed+" authoritativeComponents="+before.Count+" sceneGuid="+guid+" originalPhysicsMeshesTransformsIds=true");
                }
                if(Hash(shared)!=sharedHash||Hash(shared+".meta")!=sharedMeta)throw new InvalidOperationException("Canonical shared-world source must stay exact.");
                string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"RoadSnow");Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"selected-upgrade.txt"),receipts);Debug.Log(string.Join("\n",receipts));
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
#endif
