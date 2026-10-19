using UnityEngine;
using UnityEngine.UI;
namespace LWS.TruckBowling {
 // Original vector pin character; ordinary uGUI geometry, no per-frame textures.
 [RequireComponent(typeof(CanvasRenderer))]public sealed class BowlingPinMascot:MaskableGraphic {
  public int mood;
  protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();Color ink=new Color(.025f,.035f,.055f);Pin(vh,1.12f,ink);Pin(vh,1,Color.white);Quad(vh,new Vector2(-7,62),new Vector2(7,69),new Color(.94f,.12f,.11f));Quad(vh,new Vector2(-7,73),new Vector2(7,78),new Color(.94f,.12f,.11f));
   for(int sign=-1;sign<=1;sign+=2){Ellipse(vh,new Vector2(sign*7,100),new Vector2(5,8),ink);Ellipse(vh,new Vector2(sign*7+(mood==3?sign*1.5f:0),101),new Vector2(2,4),new Color(.08f,.65f,.84f));Ellipse(vh,new Vector2(sign*7,103),new Vector2(.8f,1.4f),Color.white);}
   for(int i=0;i<12;i++){float a=i*Mathf.PI/11,b=(i+1)*Mathf.PI/11;float flip=mood==2?-1:1;Line(vh,new Vector2(Mathf.Cos(a)*8,88-Mathf.Sin(a)*5*flip),new Vector2(Mathf.Cos(b)*8,88-Mathf.Sin(b)*5*flip),1.6f,ink);}
   var v=new UIVertex();for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref v,i);v.position.y-=60;vh.SetUIVertex(v,i);}
  }
  static void Pin(VertexHelper vh,float scale,Color c){float[] y={0,7,20,33,48,61,73,85,96,108,116,120};float[] r={9,14,19,20,16,10,7,8,12,13,8,0};for(int i=0;i<y.Length-1;i++){var a=new Vector2(-r[i]*scale,y[i]);var b=new Vector2(r[i]*scale,y[i]);var d=new Vector2(-r[i+1]*scale,y[i+1]);var e=new Vector2(r[i+1]*scale,y[i+1]);Poly(vh,a,b,d,e,c);}}
  static void Ellipse(VertexHelper vh,Vector2 p,Vector2 r,Color c){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;int n=vh.currentVertCount;vh.AddVert(p,c,Vector2.zero);vh.AddVert(p+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),r),c,Vector2.zero);vh.AddVert(p+Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),r),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}}
  static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Color c)=>Poly(vh,a,new Vector2(b.x,a.y),new Vector2(a.x,b.y),b,c);
  static void Poly(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+1,n+3,n+2);}
  static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c){var d=(b-a).normalized;var side=new Vector2(-d.y,d.x)*width;Poly(vh,a-side,b-side,a+side,b+side,c);}
 }
}
