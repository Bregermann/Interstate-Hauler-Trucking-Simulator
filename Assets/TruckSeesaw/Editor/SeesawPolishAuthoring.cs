using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports.Editor;
namespace LWS.TruckSeesaw.Editor
{
    public static class SeesawPolishAuthoring
    {
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(SeesawAuthoring.Scene);var m=Object.FindAnyObjectByType<SeesawMatch>();
            if(m.platform.transform.Find("Original combined deck plate seams and rivets")!=null)return;
            var ground=GameObject.Find("Existing modeled deep quarry impact basin");if(ground.GetComponent<LWS.TruckTaxi.TruckTaxiSurface>()==null)ground.AddComponent<LWS.TruckTaxi.TruckTaxiSurface>().isRoad=false;
            var dark=SportsEventAuthoring.Mat(SeesawAuthoring.Folder,"Recessed plate seams",new Color(.06f,.07f,.075f),.15f);
            var rivet=SportsEventAuthoring.Mat(SeesawAuthoring.Folder,"Worn steel rivet heads",new Color(.68f,.7f,.72f),.65f);
            var vertices=new List<Vector3>();var seams=new List<int>();var bolts=new List<int>();
            foreach(float p in new[]{-30f,-10f,10f,30f})
            {
                Quad(vertices,seams,new Vector3(p-.07f,.925f,-39),new Vector3(p+.07f,.925f,-39),new Vector3(p+.07f,.925f,39),new Vector3(p-.07f,.925f,39));
                Quad(vertices,seams,new Vector3(-39,.925f,p-.07f),new Vector3(39,.925f,p-.07f),new Vector3(39,.925f,p+.07f),new Vector3(-39,.925f,p+.07f));
            }
            for(int x=0;x<11;x++)for(int z=0;z<11;z++)
            {
                var center=new Vector3(-38+x*7.6f,.96f,-38+z*7.6f);int start=vertices.Count;vertices.Add(center);
                for(int k=0;k<6;k++){float a=k*Mathf.PI/3;vertices.Add(center+new Vector3(Mathf.Sin(a)*.25f,0,Mathf.Cos(a)*.25f));}
                for(int k=0;k<6;k++)bolts.AddRange(new[]{start,start+1+k,start+1+(k+1)%6});
            }
            var mesh=new Mesh{name="Original combined quarry deck seam and hex-rivet artwork"};mesh.SetVertices(vertices);mesh.subMeshCount=2;mesh.SetTriangles(seams,0);mesh.SetTriangles(bolts,1);mesh.RecalculateNormals();mesh.RecalculateBounds();Directory.CreateDirectory("Assets/TruckSeesaw/Models");AssetDatabase.Refresh();AssetDatabase.CreateAsset(mesh,"Assets/TruckSeesaw/Models/Original riveted deck detailing.asset");
            var detail=new GameObject("Original combined deck plate seams and rivets");detail.transform.SetParent(m.platform.transform,false);detail.AddComponent<MeshFilter>().sharedMesh=mesh;detail.AddComponent<MeshRenderer>().sharedMaterials=new[]{dark,rivet};
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void OpenGantry()
        {
            var scene=EditorSceneManager.OpenScene(SeesawAuthoring.Scene);var beam=GameObject.Find("Suspension gantry crossbeam");
            var match=Object.FindAnyObjectByType<SeesawMatch>();GameObject.Find("Visible suspended universal pivot hub").transform.position=match.platformOrigin-Vector3.up*4;match.menus.pages[4].transform.Find("Rules and controls").GetComponent<TMPro.TMP_Text>().text=match.Help;
            string path="Assets/TruckSeesaw/Models/Original open suspension truss.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
                var pieces=new List<CombineInstance>();
                foreach(int side in new[]{-1,1})
                {
                    foreach(int level in new[]{-1,1})pieces.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(new Vector3(0,level*1.4f,side*1.8f),Quaternion.identity,new Vector3(115,.18f,.18f))});
                    for(int i=0;i<14;i++)
                    {
                        float a=-57.5f+i*115/14;var from=new Vector3(a,i%2==0?-1.4f:1.4f,side*1.8f);var to=new Vector3(a+115/14,i%2==0?1.4f:-1.4f,side*1.8f);var delta=to-from;
                        pieces.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS((from+to)*.5f,Quaternion.LookRotation(delta),new Vector3(.14f,.14f,delta.magnitude))});
                    }
                }
                mesh=new Mesh{name="Original open load-bearing quarry suspension truss"};mesh.CombineMeshes(pieces.ToArray(),true,true);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            beam.transform.localScale=Vector3.one;beam.GetComponent<MeshFilter>().sharedMesh=mesh;
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void SupportBalconies()
        {
            var scene=EditorSceneManager.OpenScene(SeesawAuthoring.Scene);
            if(GameObject.Find("Spectator balcony structural bracing")!=null)return;
            var root=new GameObject("Spectator balcony structural bracing").transform;
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckSeesaw/Models/Original open suspension truss.asset");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSeesaw/Materials/Chipped construction yellow.mat");
            foreach(int side in new[]{-1,1})
            {
                foreach(int end in new[]{-1,1})
                {
                    var column=new GameObject("Visible quarry-floor-to-balcony lattice column");column.transform.SetParent(root);column.transform.SetPositionAndRotation(new Vector3(side*79,43,end*30),Quaternion.Euler(0,0,90));column.transform.localScale=new Vector3(86f/115,.5f,.5f);column.AddComponent<MeshFilter>().sharedMesh=mesh;column.AddComponent<MeshRenderer>().sharedMaterial=material;
                }
                var header=new GameObject("Continuous lattice support beneath spectator terraces");header.transform.SetParent(root);header.transform.SetPositionAndRotation(new Vector3(side*79,84.5f,0),Quaternion.Euler(0,90,0));header.transform.localScale=new Vector3(64f/115,.5f,.5f);header.AddComponent<MeshFilter>().sharedMesh=mesh;header.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void Quad(List<Vector3> vertices,List<int> faces,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {int s=vertices.Count;vertices.AddRange(new[]{a,b,c,d});faces.AddRange(new[]{s,s+2,s+1,s,s+3,s+2});}
    }
}
