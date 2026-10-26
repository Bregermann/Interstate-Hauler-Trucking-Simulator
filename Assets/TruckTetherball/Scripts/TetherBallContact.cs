using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckTetherball
{
    public sealed class TetherBallContact : MonoBehaviour
    {
        public TetherMatch match;
        Mesh seamMesh;Material originalRubber,rubber,seamRubber;MeshRenderer shell;GameObject seams;
        public void PrepareRubberPresentation()
        {
            if(rubber!=null)return;
            shell=GetComponentInChildren<MeshRenderer>();if(shell==null||shell.sharedMaterial==null)return;
            originalRubber=shell.sharedMaterial;rubber=new Material(originalRubber){name="Tetherball owned pebbled gold rubber"};
            Tint(rubber,new Color(.94f,.76f,.18f));shell.sharedMaterial=rubber;
            seamRubber=new Material(originalRubber){name="Tetherball owned molded rubber seams"};Tint(seamRubber,new Color(.075f,.06f,.025f));
            if(seamRubber.HasProperty("_Smoothness"))seamRubber.SetFloat("_Smoothness",.12f);
            float radius=GetComponent<SphereCollider>().radius;
            seamMesh=PanelSeams(radius);seams=new GameObject("Modeled rubber tetherball panel seams");seams.transform.SetParent(transform,false);
            seams.AddComponent<MeshFilter>().sharedMesh=seamMesh;var renderer=seams.AddComponent<MeshRenderer>();renderer.sharedMaterial=seamRubber;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Tint(Material material,Color color){if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);if(material.HasProperty("_Color"))material.SetColor("_Color",color);}
        static Mesh PanelSeams(float radius)
        {
            const int rings=3,steps=96,sides=8;var vertices=new Vector3[rings*steps*sides];var uv=new Vector2[vertices.Length];var triangles=new int[rings*steps*sides*6];int at=0;
            for(int ring=0;ring<rings;ring++)for(int i=0;i<steps;i++)
            {
                float angle=i*2*Mathf.PI/steps;var radial=ring==0?new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0):ring==1?new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)):new Vector3(0,Mathf.Cos(angle),Mathf.Sin(angle));
                var normal=ring==0?Vector3.forward:ring==1?Vector3.down:Vector3.right;
                for(int side=0;side<sides;side++)
                {
                    float a=side*2*Mathf.PI/sides;int index=(ring*steps+i)*sides+side;
                    vertices[index]=radial*(radius+.008f)+(radial*Mathf.Cos(a)+normal*Mathf.Sin(a))*radius*.012f;uv[index]=new Vector2(i/(float)steps,side/(float)sides);
                    int next=(ring*steps+(i+1)%steps)*sides,here=(ring*steps+i)*sides,j=(side+1)%sides;
                    triangles[at++]=here+side;triangles[at++]=next+side;triangles[at++]=here+j;triangles[at++]=here+j;triangles[at++]=next+side;triangles[at++]=next+j;
                }
            }
            var mesh=new Mesh{name="Original bounded molded rubber tetherball panel geometry"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();mesh.UploadMeshData(true);return mesh;
        }
        void OnCollisionEnter(Collision collision)
        {
            var truck=collision.rigidbody!=null?collision.rigidbody.GetComponentInParent<SportsEventTruck>():null;
            if(truck!=null)match.BallContact(truck,collision.relativeVelocity.magnitude);
        }
        void OnDestroy()
        {
            if(shell!=null&&originalRubber!=null)shell.sharedMaterial=originalRubber;
            if(seams!=null)Destroy(seams);if(rubber!=null)Destroy(rubber);if(seamRubber!=null)Destroy(seamRubber);if(seamMesh!=null)Destroy(seamMesh);
        }
    }
}
