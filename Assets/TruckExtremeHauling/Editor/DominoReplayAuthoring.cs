#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LWS.TruckSports;
namespace LWS.TruckExtremeHauling.Editor
{
    /// <summary>Explicit saved-scene render-only replay upgrade; no normal-play cloning.</summary>
    public static class DominoReplayAuthoring
    {
        const string RootName="Authored twelve-second domino lane replays";
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Domino collapse replay")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/TruckExtremeHauling/Scenes/DominoDerby.unity")throw new InvalidOperationException("Open the saved Domino Derby scene.");
            var match=UnityEngine.Object.FindObjectsByType<DominoDerbyMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);
            if(match.collapseReplay!=null){Debug.Log("DOMINO_REPLAY_UPGRADE_PRESERVED");return;}
            if(match.piecesPerPlayer!=12 || match.trailers.Length!=48)throw new InvalidOperationException("This explicit upgrade expects four authored twelve-trailer lanes.");
            if(scene.GetRootGameObjects().Any(g=>g.name==RootName))throw new InvalidOperationException("Existing replay root has lost its match binding; repair that binding rather than duplicating ghosts.");
            var root=new GameObject(RootName);var replay=root.AddComponent<DominoCollapseReplay>();replay.captureSeconds=12;replay.lanes=new SportsMotionReplay[4];
            int hierarchyParts=0;
            for(int lane=0;lane<4;lane++)
            {
                var group=new GameObject("Recorded physical lane "+(lane+1));group.transform.SetParent(root.transform,false);
                var record=replay.lanes[lane]=group.AddComponent<SportsMotionReplay>();
                record.capacity=124;record.interval=.1f;record.rolling=false;record.maximumReplayAge=18;record.sources=new Transform[12];record.ghosts=new Transform[12];
                for(int piece=0;piece<12;piece++)
                {
                    var source=match.trailers[lane*12+piece].transform;record.sources[piece]=source;
                    var ghost=SportsReplayVisuals.Copy(source);ghost.transform.SetParent(group.transform,true);record.ghosts[piece]=ghost.transform;
                    CopyLods(source,ghost.transform);
                    foreach(var renderer in ghost.GetComponentsInChildren<Renderer>(true))renderer.shadowCastingMode=ShadowCastingMode.Off;
                    if(ghost.GetComponentsInChildren<Collider>(true).Length!=0 || ghost.GetComponentsInChildren<Rigidbody>(true).Length!=0 || ghost.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new InvalidOperationException("Replay ghosts must contain render geometry, bones and LODs only.");
                    hierarchyParts+=ghost.GetComponentsInChildren<Transform>(true).Length;ghost.SetActive(false);
                }
            }
            match.collapseReplay=replay;EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("DOMINO_COLLAPSE_REPLAY_AUTHORED lanes=4 sourcesPerLane=12 capacity=124 sampleInterval=.1 seconds=12 ghostParts="+hierarchyParts+"; actual collapse-only highlights, LODs, no replay physics. Caller saves and tests.");
        }
        static void CopyLods(Transform source,Transform target)
        {
            var original=source.GetComponentsInChildren<Transform>(true);var copies=target.GetComponentsInChildren<Transform>(true);
            if(original.Length!=copies.Length)throw new InvalidOperationException("Replay render hierarchy must match its authored physical source.");
            var map=new Dictionary<Transform,Transform>();for(int i=0;i<original.Length;i++)map.Add(original[i],copies[i]);
            foreach(var group in source.GetComponentsInChildren<LODGroup>(true))
            {
                var copy=map[group.transform].gameObject.AddComponent<LODGroup>();var lods=group.GetLODs();
                for(int level=0;level<lods.Length;level++)
                {
                    var renderers=lods[level].renderers;var mirrored=new Renderer[renderers.Length];
                    for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)mirrored[i]=(Renderer)map[renderers[i].transform].GetComponent(renderers[i].GetType());
                    lods[level].renderers=mirrored;
                }
                copy.fadeMode=group.fadeMode;copy.animateCrossFading=group.animateCrossFading;copy.SetLODs(lods);copy.localReferencePoint=group.localReferencePoint;copy.size=group.size;copy.enabled=group.enabled;
            }
        }
    }
}
#endif
