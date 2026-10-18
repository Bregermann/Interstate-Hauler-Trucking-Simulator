using UnityEngine;
using UnityEngine.UI;
namespace LWS.TruckSports {
// Code-native broadcast silhouette; keeps existing Heat buttons and their Image references intact.
[DisallowMultipleComponent]public sealed class SportsBroadcastPlate:BaseMeshEffect {
 public float cut=12;public bool accent;
 public override void ModifyMesh(VertexHelper mesh){
  if(!IsActive()||mesh.currentVertCount==0)return;var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,0);Color color=vertex.color;
  var r=((RectTransform)transform).rect;float radius=Mathf.Clamp(cut,0,Mathf.Min(r.width,r.height)*.45f);
  mesh.Clear();mesh.AddVert(r.center,color,Vector2.zero);
  const int steps=6;int count=4*(steps+1);
  for(int corner=0;corner<4;corner++){
   var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
   for(int step=0;step<=steps;step++){float angle=(corner*90+step*90f/steps)*Mathf.Deg2Rad;var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;mesh.AddVert(p,color,Vector2.zero);}
  }
  for(int i=0;i<count;i++)mesh.AddTriangle(0,i+1,(i+1)%count+1);
  if(accent){int at=mesh.currentVertCount;Color orange=new Color(1,.39f,.08f,color.a);mesh.AddVert(new Vector2(r.xMin+radius,r.yMin),orange,Vector2.zero);mesh.AddVert(new Vector2(r.xMax-radius,r.yMin),orange,Vector2.zero);mesh.AddVert(new Vector2(r.xMax-radius,r.yMin+3),orange,Vector2.zero);mesh.AddVert(new Vector2(r.xMin+radius,r.yMin+3),orange,Vector2.zero);mesh.AddTriangle(at,at+1,at+2);mesh.AddTriangle(at,at+2,at+3);}
 }
}
}
