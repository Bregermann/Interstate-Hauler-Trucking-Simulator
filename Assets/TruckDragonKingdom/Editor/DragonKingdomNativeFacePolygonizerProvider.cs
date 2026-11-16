#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Polygonizers;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Plain candidate-scoped factory over the installed native polygonizer.
    // Editor assembly behaviours cannot be attached even temporarily. This
    // factory never becomes a component, master setting or serialized object.
    public sealed class DragonKingdomNativeFacePolygonizerProvider
    {
        DiggerSystem owner;Material[] materials;string candidateIdentity;
        public static bool IsOwnedCandidateContext(string terrainPath,string dataPath,string candidateTag)
        {
            if(string.IsNullOrEmpty(candidateTag)||candidateTag.Length>40||candidateTag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')||string.IsNullOrEmpty(terrainPath)||string.IsNullOrEmpty(dataPath))return false;
            string terrain=terrainPath.Replace('\\','/'),data=dataPath.Replace('\\','/');if(terrain.Split('/').Any(s=>s==".."||s==".")||data.Split('/').Any(s=>s==".."||s=="."))return false;
            return terrain.StartsWith("Assets/TruckDragonKingdom/VisualCandidates/"+candidateTag+"/Terrain/",StringComparison.Ordinal)&&data.StartsWith("Assets/DiggerData/Scenes/DK-"+candidateTag+"/",StringComparison.Ordinal);
        }
        public void ConfigureCandidate(DiggerSystem source,string candidateTag)
        {
            if(source==null||source.Terrain==null||!IsOwnedCandidateContext(AssetDatabase.GetAssetPath(source.Terrain.terrainData),source.BasePathData,candidateTag))throw new InvalidOperationException("Exact owned candidate terrain and matching native Digger persistence tag required; canonical/source worlds are forbidden.");
            owner=source;candidateIdentity=candidateTag;materials=source.Materials?.ToArray();Init();
        }
        public void Init()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||owner==null||materials==null||materials.Length==0||materials.Any(m=>m==null)||owner.Materials==null||!materials.SequenceEqual(owner.Materials)||!IsOwnedCandidateContext(AssetDatabase.GetAssetPath(owner.Terrain.terrainData),owner.BasePathData,candidateIdentity))throw new InvalidOperationException("Stopped Editor and configured explicit owned-candidate face-normal factory required.");
        }
        public IPolygonizer NewPolygonizer(DiggerSystem digger)
        {Init();if(digger!=owner)throw new InvalidOperationException("Provider may only repolygonize its explicitly configured candidate Digger system.");return new MarchingCubesPolygonizer(true);}
        public Material[] GetMaterials(){Init();return materials.ToArray();}
    }
}
#endif
