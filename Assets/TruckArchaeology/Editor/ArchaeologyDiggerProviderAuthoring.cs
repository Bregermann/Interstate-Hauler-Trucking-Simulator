using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Polygonizers;
using Digger.Modules.Runtime.Sources;
namespace LWS.TruckArchaeology.Editor
{
    // Conditional on actual startup diagnostic confirmation. One component only;
    // caller opens/saves. Never invokes Init, reconstructs data or edits SDK assets.
    public static class ArchaeologyDiggerProviderAuthoring
    {
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("Archaeology provider preflight: "+message);}
        public static ArchaeologyDefaultPolygonizerProvider Install(ArchaeologyMatch match)
        {
            Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped Editor required");
            Require(match!=null&&(match.gameObject.scene.path==ArchaeologyAuthoring.Scene||string.IsNullOrEmpty(match.gameObject.scene.path)),"exact original Archaeology/new unsaved author required");
            var earth=match.earth;Require(earth!=null&&earth.system!=null&&earth.runtime!=null&&earth.terrain!=null,"actual existing vendor bindings required");
            var masters=UnityEngine.Object.FindObjectsByType<DiggerMaster>(FindObjectsInactive.Include,FindObjectsSortMode.None);var systems=UnityEngine.Object.FindObjectsByType<DiggerSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None);var runtimes=UnityEngine.Object.FindObjectsByType<DiggerMasterRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Require(masters.Length==1&&systems.Length==1&&runtimes.Length==1&&systems[0]==earth.system&&runtimes[0]==earth.runtime,"preserve exactly one existing vendor authority each");var master=masters[0];var system=systems[0];
            Require(master.gameObject.scene==match.gameObject.scene&&system.gameObject.scene==match.gameObject.scene&&earth.runtime.gameObject.scene==match.gameObject.scene&&system.transform.parent==earth.terrain.transform&&system.Terrain==earth.terrain,"original same-scene terrain/system/master/runtime required");
            Require(master.SceneDataFolder=="TruckArchaeology"&&system.Guid=="archaeology-desert-07-01"&&master.ChunkSize==33&&master.ResolutionMult==1&&Mathf.Abs(master.VoxelHeight-.25f)<.000001f&&!master.AutoVoxelHeight&&master.ColliderLodIndex==0&&!master.CreateLODs,"preserve original data ID, resolution and collider configuration");
            var serialized=new SerializedObject(system);Require(serialized.FindProperty("master").objectReferenceValue==master&&serialized.FindProperty("basePathData").stringValue.Replace('\\','/')=="Assets/DiggerData/Scenes/TruckArchaeology/archaeology-desert-07-01","preserve existing serialized master/data path without lazy mutation");
            Require(UnityEngine.Object.FindObjectsByType<APolygonizerProvider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"existing/custom/partial provider must be reviewed, never replaced");
            Require(earth.runtime.enablePersistence&&earth.runtime.BufferSize==1&&system.Materials!=null&&system.Materials.Length>0&&system.Materials.All(m=>m!=null&&m.shader!=null),"original configured runtime/materials required");
            var terrainMaterial=earth.terrain.materialTemplate;Require(terrainMaterial!=null&&terrainMaterial.shader!=null&&terrainMaterial.shader.name=="Digger/Terrain/URP/Lit","retain actual original URP cutter material");
            Require(match.trucks.Length==4&&match.equipment.Length==4&&match.artifacts.Length==12&&match.receptions.Length==4,"complete original four-plot venue required");
            // All guards above precede the sole serialized mutation.
            var provider=master.gameObject.AddComponent<ArchaeologyDefaultPolygonizerProvider>();EditorUtility.SetDirty(provider);EditorUtility.SetDirty(master);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
            Debug.Log("ARCHAEOLOGY_DEFAULT_VENDOR_PROVIDER one owned factory component; original Ready predicate, MarchingCubes algorithm, URP materials, terrain/chunks/collider/body data unchanged. Does not Init/open/save; caller owns save and native validation.");return provider;
        }
        [MenuItem("Truck Sports/Alpha 0.7/Repair OPEN Archaeology/Configure actual vendor default provider")]
        public static void InstallOnOpenOriginalScene()
        {
            var matches=UnityEngine.Object.FindObjectsByType<ArchaeologyMatch>(FindObjectsInactive.Include,FindObjectsSortMode.None);Require(matches.Length==1&&matches[0].gameObject.scene.path==ArchaeologyAuthoring.Scene,"open exact saved Archaeology scene required");Install(matches[0]);
        }
    }
}
