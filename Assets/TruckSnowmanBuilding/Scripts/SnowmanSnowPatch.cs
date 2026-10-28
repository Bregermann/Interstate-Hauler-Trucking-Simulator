using UnityEngine;
namespace LWS.TruckSnowmanBuilding
{
    // Finite snow-gathering semantics only. Weatherade owns surface coverage and tracks.
    public sealed class SnowmanSnowPatch:MonoBehaviour
    {
        public BoxCollider ground;public float depth=.25f,cellSize=3;public float[] Remaining {get;private set;}
        int nx,nz;
        public void ResetSnow(){nx=Mathf.CeilToInt(ground.bounds.size.x/cellSize);nz=Mathf.CeilToInt(ground.bounds.size.z/cellSize);Remaining=new float[nx*nz];for(int i=0;i<Remaining.Length;i++)Remaining[i]=cellSize*cellSize*depth;}
        int Cell(Vector3 world){var p=world-ground.bounds.min;int x=Mathf.FloorToInt(p.x/cellSize),z=Mathf.FloorToInt(p.z/cellSize);return x<0||z<0||x>=nx||z>=nz?-1:z*nx+x;}
        public float Available(Vector3 point,float width){if(Remaining==null)ResetSnow();float total=0;int[] visited={-1,-1,-1};for(int i=-1;i<=1;i++){int index=Cell(point+Vector3.right*(i*width*.45f));if(index>=0&&System.Array.IndexOf(visited,index)<0){visited[i+1]=index;total+=Remaining[index];}}return total;}
        public float Consume(Vector3 point,float width,float amount){if(Remaining==null)ResetSnow();float left=amount;int[] visited={-1,-1,-1};for(int n=0;n<3;n++){int index=Cell(point+Vector3.right*((n-1)*width*.45f));if(index<0||System.Array.IndexOf(visited,index)>=0)continue;visited[n]=index;float used=Mathf.Min(left,Remaining[index]);Remaining[index]-=used;left-=used;}return amount-left;}
        public bool Valid(float[] values)=>values!=null&&values.Length==Mathf.CeilToInt(ground.bounds.size.x/cellSize)*Mathf.CeilToInt(ground.bounds.size.z/cellSize)&&System.Array.TrueForAll(values,x=>float.IsFinite(x)&&x>=0&&x<=cellSize*cellSize*depth+.001f);
        public void Restore(float[] values){nx=Mathf.CeilToInt(ground.bounds.size.x/cellSize);nz=Mathf.CeilToInt(ground.bounds.size.z/cellSize);Remaining=(float[])values.Clone();}
    }
}
