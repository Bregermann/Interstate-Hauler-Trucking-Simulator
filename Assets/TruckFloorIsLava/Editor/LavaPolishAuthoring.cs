using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports.Editor;
namespace LWS.TruckFloorIsLava.Editor
{
    public static class LavaPolishAuthoring
    {
        public static void BasaltRim()
        {
            var scene=EditorSceneManager.OpenScene(LavaAuthoring.Scene);var m=Object.FindAnyObjectByType<LavaMatch>();var terrain=Object.FindAnyObjectByType<Terrain>();
            foreach(string name in new[]{"Installed volcanic rock formations","Installed quarry cliffs and distant scale references"})
            {var old=GameObject.Find(name);if(old!=null)old.SetActive(false);}
            if(GameObject.Find("Terrain-grounded Vista basalt rim formations")==null)
            {
                var root=new GameObject("Terrain-grounded Vista basalt rim formations").transform;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_DesertRock.prefab");
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTargetLaunch/Materials/Weathered basalt formations.mat");
                for(int i=0;i<32;i++)
                {
                    float a=i*2.399963f,radius=365+(i%5)*24;var p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);var uv=p-terrain.transform.position;var data=terrain.terrainData;
                    var normal=data.GetInterpolatedNormal(Mathf.Clamp01(uv.x/data.size.x),Mathf.Clamp01(uv.z/data.size.z));p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name="Owned textured basalt rim outcrop "+i;go.transform.SetParent(root);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,normal,.65f))*Quaternion.Euler(0,i*137.5f,0)*source.transform.rotation);
                    var renderer=go.GetComponentInChildren<MeshRenderer>();float size=20+(i%4)*6;go.transform.localScale*=size/Mathf.Max(renderer.bounds.size.x,renderer.bounds.size.z);var b=renderer.bounds;go.transform.position=p-new Vector3(b.center.x,b.min.y+b.size.y*.22f,b.center.z);
                    var materials=renderer.sharedMaterials;for(int k=0;k<materials.Length;k++)materials[k]=material;renderer.sharedMaterials=materials;
                }
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("LAVA_OWNED_BASALT_RIM_REPLACES_OBSOLETE_PALE_CLUSTERS");
        }
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(LavaAuthoring.Scene);var m=Object.FindAnyObjectByType<LavaMatch>();
            for(int i=0;i<2;i++)
            {
                var floor=m.platforms[i==0?11:13];if(floor.transform.Find("Finite narrow bridge extension")!=null)continue;float side=i==0?1:-1;
                var bridge=new GameObject("Finite narrow bridge extension");bridge.transform.SetParent(floor.transform,false);bridge.transform.localPosition=new Vector3(side*24,-1,0);
                var box=bridge.AddComponent<BoxCollider>();box.size=new Vector3(4,2,12);box.sharedMaterial=floor.surface.sharedMaterial;
                string path="Assets/TruckFloorIsLava/Materials/Warning-linked basalt bridge "+i+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null)
                {
                    var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var unit=cube.GetComponent<MeshFilter>().sharedMesh;
                    mesh=new Mesh{name="Original single-renderer slab and finite narrow bridge "+i};mesh.CombineMeshes(new[]{new CombineInstance{mesh=floor.plate.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.identity},new CombineInstance{mesh=unit,transform=Matrix4x4.TRS(bridge.transform.localPosition,Quaternion.identity,box.size)}},true,true);
                    Object.DestroyImmediate(cube);AssetDatabase.CreateAsset(mesh,path);
                }
                floor.plate.GetComponent<MeshFilter>().sharedMesh=mesh;EditorUtility.SetDirty(floor);
            }
            var terrain=Object.FindAnyObjectByType<Terrain>();var steel=SportsEventAuthoring.Mat(LavaAuthoring.Folder,"Volcanic venue suspension cables",new Color(.13f,.1f,.08f),.45f);
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if(renderer.name!="Original volcanic event title sign"||renderer.transform.Find("Crater-rim banner suspension")!=null)continue;
                renderer.sharedMaterial.SetFloat("_Cull",0);EditorUtility.SetDirty(renderer.sharedMaterial);
                var root=new GameObject("Crater-rim banner suspension");root.transform.SetParent(renderer.transform,false);
                for(int end=-1;end<=1;end+=2)
                {
                    var anchor=new Vector3(Mathf.Sign(renderer.transform.position.x)*310,0,160+end*35);anchor.y=terrain.SampleHeight(anchor)+terrain.transform.position.y+2;
                    var cable=new GameObject("Visible banner-to-crater tension cable").AddComponent<LineRenderer>();cable.transform.SetParent(root.transform,false);cable.useWorldSpace=true;cable.positionCount=2;cable.SetPosition(0,renderer.transform.TransformPoint(new Vector3(end*.48f,.45f,0)));cable.SetPosition(1,anchor);cable.widthMultiplier=.35f;cable.sharedMaterial=steel;
                }
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("LAVA_FINITE_BRIDGES_AND_SUPPORTED_VENUE_SIGNS_SAVED");
        }
    }
}
