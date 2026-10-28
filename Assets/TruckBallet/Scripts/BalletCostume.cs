using UnityEngine;
namespace LWS.TruckBallet
{
    // Render-only costume follows the one original truck assembly authority.
    // Its local ruffle sway never moves wheels, cabs, colliders or rigidbodies.
    public sealed class BalletCostume:MonoBehaviour
    {
        public BalletTruckRig rig;
        public Transform ruffles;
        public Renderer[] tintable;
        public MeshFilter cloth;
        public float stageFloor;
        Quaternion rest;
        float phase;
        bool performing;
        Mesh original,owned;
        Vector3[] restVertices,vertices;
        float nextNormals;
        void Awake(){rest=ruffles.localRotation;original=cloth.sharedMesh;restVertices=original.vertices;vertices=new Vector3[restVertices.Length];owned=Instantiate(original);owned.name=original.name+" · owned runtime hem only";owned.MarkDynamic();cloth.sharedMesh=owned;}
        public void Begin(){performing=true;phase=0;var block=new MaterialPropertyBlock();var color=Color.Lerp(rig.truck.session.setup.players[rig.slot].color,Color.white,.3f);block.SetColor("_BaseColor",color);block.SetColor("_Color",color);foreach(var renderer in tintable)renderer.SetPropertyBlock(block);}
        void LateUpdate()
        {
            if(!performing||Time.deltaTime<=0)return;phase+=Time.deltaTime;ruffles.localRotation=rest*Quaternion.Euler(Mathf.Sin(phase*2.3f)*1.5f,Mathf.Sin(phase*1.2f)*2+rig.Achieved.turnRate*.025f,Mathf.Cos(phase*2.1f)*1.5f);
            // A bounded render-only hem compression, not a cloth simulator or
            // collision/actor correction. Source mesh, UVs and rig stay intact.
            for(int i=0;i<vertices.Length;i++){var point=cloth.transform.TransformPoint(restVertices[i]);point.y=Mathf.Max(point.y,stageFloor+.035f);vertices[i]=cloth.transform.InverseTransformPoint(point);}owned.vertices=vertices;owned.RecalculateBounds();if(Time.time>=nextNormals){owned.RecalculateNormals();nextNormals=Time.time+.1f;}
        }
        public void Stop(){performing=false;ruffles.localRotation=rest;if(owned!=null){owned.vertices=restVertices;owned.normals=original.normals;owned.RecalculateBounds();}}
        void OnDestroy(){if(cloth!=null&&cloth.sharedMesh==owned)cloth.sharedMesh=original;if(owned!=null)Destroy(owned);}
    }
}
