#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit diagnostic only. The installed EasyRoads API and real native
    // terrain writes establish which adjacent span each marker flag controls.
    public static class DragonRoadDeformationProbe
    {
        [Serializable] sealed class Sample { public int disabledMarker; public float[] x, nativeTerrainHeight; }
        [Serializable] sealed class Receipt { public string scope="ISOLATED_NATIVE_EASYROADS_MARKER_SPAN_DIAGNOSTIC"; public float roadHeight=12; public Sample[] cases; }
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonReceipt");
            if(at<0||at+1>=args.Length)throw new InvalidOperationException("A unique diagnostic receipt is required.");
            string output=Path.GetFullPath(args[at+1]);if(File.Exists(output))throw new IOException("Preserve prior probe evidence.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes before the isolated probe.");
            var setup=EditorSceneManager.GetSceneManagerSetup();var facts=new List<Sample>();
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                string folder="Assets/TruckDragonKingdom/Tests/Generated/DeformationProbe/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                foreach(int disabled in new[]{-1,1,2})
                {
                    var data=new TerrainData{heightmapResolution=257,size=new Vector3(240,64,160),name="Actual EasyRoads probe "+disabled};
                    AssetDatabase.CreateAsset(data,folder+"/Terrain-"+disabled+".asset");
                    var terrainObject=Terrain.CreateTerrainGameObject(data);var terrain=terrainObject.GetComponent<Terrain>();terrain.Flush();
                    var networkPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");
                    if(networkPrefab==null)throw new InvalidOperationException("Installed EasyRoads network prefab required.");
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(networkPrefab);
                    try
                    {
                        var owner=instance.GetComponent<ERModularBase>();owner.RoadNetworkInit();var network=new ERRoadNetwork(owner);
                        var markers=new[]{20f,60f,100f,140f,180f}.Select(x=>new Vector3(x,12,80)).ToArray();
                        var road=network.CreateRoad("Actual native marker span "+disabled,new ERRoadType{roadTypeName="Diagnostic span",roadWidth=12,hasMeshCollider=true},markers,Enumerable.Repeat(ERMarkerControlType.StraightXZ,markers.Length).ToArray());
                        road.SetTerrainDeformation(true,false);road.SnapToTerrain(false,false);
                        if(disabled>=0)road.SetTerrainDeformation(disabled,false);
                        for(int marker=0;marker<markers.Length;marker++){road.SetIndent(4,marker);road.SetSurrounding(6,marker);}
                        road.Refresh();network.BuildRoadNetwork(false,false,false);terrain.Flush();Physics.SyncTransforms();
                        var x=new[]{30f,40f,50f,70f,80f,90f,110f,120f,130f,150f,160f,170f};
                        facts.Add(new Sample{disabledMarker=disabled,x=x,nativeTerrainHeight=x.Select(value=>data.GetInterpolatedHeight(value/240,80f/160)).ToArray()});
                        EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(terrainObject);}
                }
                Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(new Receipt{cases=facts.ToArray()},true));
                if(facts[0].nativeTerrainHeight.Any(height=>height<10))throw new InvalidOperationException("The all-enabled control failed to prove actual native road grading; inspect the retained receipt before interpreting marker spans.");
                Debug.Log("DRAGON_EASYROADS_NATIVE_SPANS "+File.ReadAllText(output));
            }
            finally
            {
                if(setup.Length==0||setup.Length==1&&string.IsNullOrEmpty(setup[0].path))EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                else EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}
#endif
