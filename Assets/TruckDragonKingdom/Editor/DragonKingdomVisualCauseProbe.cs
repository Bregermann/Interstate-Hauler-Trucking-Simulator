#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static class DragonKingdomVisualCauseProbe
    {
        [Serializable]sealed class Pose{public Vector3 cameraPosition=Vector3.zero,cameraEuler=Vector3.zero;}
        [Serializable]sealed class Region{public string id="";public Pose eye=null,overview=null;}
        [Serializable]sealed class Review{public string preview="";public Region[] regions=Array.Empty<Region>();}
        [Serializable]public sealed class Hit{public string collider,type,mesh,sourcePrefab;public Vector3 point,normal;public float distance;public Bounds bounds;public string[] materials;}
        [Serializable]public sealed class RayRecord{public string view;public Vector2 pixel;public Vector3 origin,direction;public Hit[] hits;}
        [Serializable]public sealed class Receipt{public string scope="READ_ONLY_NATIVE_PHYSX_RAYS_FROM_ACTUAL_V154_RENDER_CAMERAS_NOT_REPLACEMENT_IMAGES",candidate,sourceReview;public bool sourceBytesPreserved;public RayRecord[] rays;}
        public static Receipt LastReceipt{get;private set;}
        public static void ProbeSavedReviewViews()
        {
            string tag=Arg("-tsDragonVisualSource","RoundedIntegrated04"),output=Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom/VisualCause");
            if(tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Actual candidate tag required.");
            string fallback=Path.Combine(Directory.GetParent(Path.GetFullPath(output)).Parent.FullName,"NativeWorldReviewV154","rendered-evidence","world-render-review.json");
            string input=Arg("-tsDragonRenderReceipt",fallback),preview="Assets/TruckDragonKingdom/VisualCandidates/"+tag+"/DragonKingdomPreview.unity";
            if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(input)||!File.Exists(preview))throw new InvalidOperationException("Stopped Editor, actual candidate and actual rendered-view receipt required: "+input);
            var review=JsonUtility.FromJson<Review>(File.ReadAllText(input));if(review.preview!=preview||review.regions.Length!=9)throw new InvalidOperationException("The actual nine-view receipt must match the selected candidate.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path)||s.path==preview)throw new InvalidOperationException("Preserve and close user/source scenes before read-only wall diagnosis.");}
            var files=new[]{preview,preview+".meta",input}.ToDictionary(p=>p,p=>Hash(p));var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Camera camera=null;var rows=new List<RayRecord>();
            var receipt=new Receipt{candidate=tag,sourceReview=input};LastReceipt=receipt;
            try
            {
                scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Single);var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                var relevant=new HashSet<Collider>(world.GetComponentsInChildren<Collider>(true));Physics.SyncTransforms();camera=new GameObject("Temporary actual review ray camera").AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.aspect=1280f/720;camera.nearClipPlane=.3f;camera.farClipPlane=6500;
                foreach(var region in review.regions)foreach(bool overview in new[]{false,true})
                {
                    var pose=overview?region.overview:region.eye;camera.transform.SetPositionAndRotation(pose.cameraPosition,Quaternion.Euler(pose.cameraEuler));
                    foreach(float y in new[]{.25f,.5f,.75f})foreach(float x in new[]{.1f,.3f,.5f,.7f,.9f})
                    {
                        var ray=camera.ViewportPointToRay(new Vector3(x,y,0));var hits=Physics.RaycastAll(ray,4000,~0,QueryTriggerInteraction.Ignore).Where(h=>relevant.Contains(h.collider)).OrderBy(h=>h.distance).Take(12).Select(h=>
                        {
                            var renderer=h.collider.GetComponent<Renderer>();string path=h.collider.name;for(var t=h.collider.transform.parent;t!=null;t=t.parent)path=t.name+"/"+path;
                            return new Hit{collider=path,type=h.collider.GetType().Name,mesh=h.collider is MeshCollider m?AssetDatabase.GetAssetPath(m.sharedMesh):h.collider is TerrainCollider terrain?AssetDatabase.GetAssetPath(terrain.terrainData):"",sourcePrefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(h.collider.gameObject),point=h.point,normal=h.normal,distance=h.distance,bounds=h.collider.bounds,materials=renderer==null?Array.Empty<string>():renderer.sharedMaterials.Select(mat=>mat==null?"NULL":AssetDatabase.GetAssetPath(mat)+" | "+mat.shader.name).ToArray()};
                        }).ToArray();
                        rows.Add(new RayRecord{view=region.id+(overview?"-overview":"-eye"),pixel=new Vector2(x*1280,y*720),origin=ray.origin,direction=ray.direction,hits=hits});
                    }
                }
            }
            finally
            {
                if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);
                receipt.rays=rows.ToArray();receipt.sourceBytesPreserved=files.All(p=>Hash(p.Key)==p.Value);Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"visual-cause-"+tag+".json"),JsonUtility.ToJson(receipt,true));
                if(!receipt.sourceBytesPreserved)throw new InvalidOperationException("Read-only visual cause probe changed saved source bytes.");
            }
        }
        static string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
        static string Hash(string file){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file)));}
    }
}
#endif
