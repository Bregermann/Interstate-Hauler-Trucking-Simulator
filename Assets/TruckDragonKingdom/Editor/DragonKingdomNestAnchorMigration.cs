using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Separate semantic-anchor repair. Does not participate in the strictly
    // transform-exact terrain/material appearance migration.
    public static class DragonKingdomNestAnchorMigration
    {
        const string Root="Assets/TruckDragonKingdom/";
        static string Prefab=>SelectedFolder()==null?Root+"Prefabs/Shared Dragon Kingdom.prefab":SelectedFolder()+"/Shared Dragon Kingdom.prefab";
        static string Preview=>SelectedFolder()==null?Root+"Scenes/DragonKingdomPreview.unity":SelectedFolder()+"/DragonKingdomPreview.unity";
        public static string PrefabPath=>Prefab;
        public static string PreviewPath=>Preview;
        [Serializable]public sealed class AnchorEvidence {public string asset,path;public Vector3 before,after;public Bounds actualNestBounds;public bool changed;}
        [Serializable]public sealed class Receipt {public string scope="FOUR_NEST_METADATA_ANCHORS_ONLY_NOT_GEOMETRY_OR_GAMEPLAY_ACCEPTANCE";public AnchorEvidence[] anchors;public bool exactTerrainRoadDefinitionMaterialBytes,allOtherTransformsAndComponents;}
        public static Receipt LastReceipt{get;private set;}

        public static Vector3 MeasuredNestSeat(Transform nest)
        {
            var bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(nest);
            Vector3 result=bounds.center;result.y=bounds.min.y+.1f;
            var ray=new Ray(new Vector3(bounds.center.x,bounds.max.y+2,bounds.center.z),Vector3.down);
            float nearest=float.PositiveInfinity;
            foreach(var collider in nest.GetComponentsInChildren<Collider>(true))
                if(!collider.isTrigger&&collider.Raycast(ray,out var hit,bounds.size.y+4)&&hit.normal.y>.25f&&hit.distance<nearest)
                {nearest=hit.distance;result=hit.point+Vector3.up*.05f;}
            // A hollow ring has no central triangle: its real model minimum is
            // the central seat reference, in world meters, not scaled local1.
            return result;
        }

        [MenuItem("Truck Sports/Dragon Kingdom/Upgrade accepted nest anchors in world meters")]
        public static void UpgradeAcceptedNestAnchors()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            foreach(var p in new[]{Prefab,Preview})if(!File.Exists(p))throw new InvalidOperationException("Accepted world required: "+p);
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.path==Preview||s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Save and close user/preview scenes before isolated anchor migration.");}
            var prior=EditorSceneManager.GetSceneManagerSetup();var evidence=new List<AnchorEvidence>();
            string candidate=SelectedFolder();
            var protectedPaths=Directory.GetFiles(Root+"Terrain","*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Root+"Models","*",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(Root+"Materials","*",SearchOption.AllDirectories))
                .Concat(new[]{Root+"Configuration/Dragon Kingdom.asset"});
            if(candidate!=null)protectedPaths=protectedPaths.Concat(Directory.GetFiles(candidate+"/Terrain","*",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(candidate+"/Materials","*",SearchOption.AllDirectories))
                .Concat(new[]{candidate+"/Dragon Kingdom.asset",Root+"Prefabs/Shared Dragon Kingdom.prefab",Root+"Scenes/DragonKingdomPreview.unity"});
            var files=protectedPaths.Distinct().ToDictionary(p=>p,p=>File.ReadAllBytes(p));
            string output=Output();Directory.CreateDirectory(output);
            foreach(string p in new[]{Prefab,Preview}){string copy=Path.Combine(output,"before-nest-anchors",p.Replace('/',Path.DirectorySeparatorChar));if(!File.Exists(copy)){Directory.CreateDirectory(Path.GetDirectoryName(copy));File.Copy(p,copy);}}
            var prefab=PrefabUtility.LoadPrefabContents(Prefab);
            try{if(Apply(prefab,Prefab,evidence)>0)PrefabUtility.SaveAsPrefabAsset(prefab,Prefab);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);int changed=0;
                foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<DragonKingdomWorld>(true)!=null)changed+=Apply(root,Preview,evidence);
                if(changed>0&&!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Explicit nest anchor preview did not save.");
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(prior);}
            if(files.Any(p=>!File.ReadAllBytes(p.Key).SequenceEqual(p.Value)))throw new InvalidOperationException("Anchor-only repair changed protected terrain/road/definition/material bytes.");
            LastReceipt=new Receipt{anchors=evidence.ToArray(),exactTerrainRoadDefinitionMaterialBytes=true,allOtherTransformsAndComponents=true};
            File.WriteAllText(Path.Combine(output,"accepted-nest-anchors.json"),JsonUtility.ToJson(LastReceipt,true));
        }
        static int Apply(GameObject root,string asset,List<AnchorEvidence> evidence)
        {
            var world=root.GetComponentInChildren<DragonKingdomWorld>(true);var anchors=world.Region("nests").nests;
            if(anchors.Length!=4||anchors.Any(a=>a==null||a.parent==null))throw new InvalidOperationException("Exactly four actual nest anchors required.");
            var allowed=new HashSet<Transform>(anchors);string before=ProtectedHash(root,allowed);int changed=0;
            foreach(var anchor in anchors)
            {
                var nest=anchor.parent;var bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(nest);
                Vector3 previous=anchor.position,target=MeasuredNestSeat(nest),scale=anchor.localScale;Quaternion rotation=anchor.localRotation;
                bool moved=(previous-target).sqrMagnitude>.00000001f;
                if(moved){anchor.position=target;EditorUtility.SetDirty(anchor);changed++;}
                if(anchor.localRotation!=rotation||anchor.localScale!=scale)throw new InvalidOperationException("Anchor orientation/scale changed.");
                if(Mathf.Abs(anchor.position.y-bounds.min.y)>bounds.size.y+.2f)throw new InvalidOperationException("Nest anchor must remain inside actual bird-nest height bounds.");
                evidence.Add(new AnchorEvidence{asset=asset,path=PathOf(anchor),before=previous,after=anchor.position,actualNestBounds=bounds,changed=moved});
            }
            if(ProtectedHash(root,allowed)!=before)throw new InvalidOperationException("Nest metadata repair changed non-anchor transforms/components.");
            return changed;
        }
        static string ProtectedHash(GameObject root,HashSet<Transform> allowed)
        {
            var b=new StringBuilder();foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                if(component==null)throw new InvalidOperationException("Missing accepted component.");
                if(component is Transform t&&allowed.Contains(t))continue;
                b.Append(component.GetType().FullName).Append(EditorJsonUtility.ToJson(component)).Append('\n');
            }
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())));
        }
        static string PathOf(Transform t){string result=t.name;while(t.parent!=null){t=t.parent;result=t.name+"/"+result;}return result;}
        static string Output(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):"Logs/TruckSports/DragonKingdom/NestAnchorMigration";}
        static string SelectedFolder()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");if(at<0||at+1>=args.Length)return null;
            string tag=args[at+1];if(tag.Length==0||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Valid explicit candidate tag required.");
            return Root+"VisualCandidates/"+tag;
        }
    }
}
