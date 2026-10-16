using UnityEngine;using UnityEngine.UI;using TMPro;
namespace LWS.TruckPoker {
// Textureless suit art avoids a second cached card-face image. Clear removes data AND rendered geometry.
[RequireComponent(typeof(CanvasRenderer))]public sealed class PokerSuitGraphic:MaskableGraphic {
 [System.NonSerialized]public int Card=-1;
 public void SetCard(int value){if(Card==value)return;Card=value;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();if(Card<0)return;var rect=GetPixelAdjustedRect();Vector2 center=rect.center;float scale=Mathf.Min(rect.width,rect.height)*.45f;int suit=PokerHand.Suit(Card);var tint=suit==1||suit==2?new Color(.8f,.05f,.12f):new Color(.025f,.04f,.08f);Color32 c=tint;
 void Poly(Vector2[] points){int first=vh.currentVertCount;vh.AddVert(center,c,Vector2.zero);foreach(var p in points)vh.AddVert(center+p*scale,c,Vector2.zero);for(int i=0;i<points.Length;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%points.Length);}
 void Circle(Vector2 at,float radius){var points=new Vector2[20];for(int i=0;i<20;i++){float a=i*Mathf.PI*.1f;points[i]=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}Poly(points);}
 if(suit==1)Poly(new[]{Vector2.up,Vector2.right,Vector2.down,Vector2.left});else if(suit==0){Circle(new Vector2(0,.45f),.45f);Circle(new Vector2(-.43f,-.05f),.45f);Circle(new Vector2(.43f,-.05f),.45f);Poly(new[]{new Vector2(-.25f,-.9f),new Vector2(.25f,-.9f),new Vector2(0,.2f)});}else{var points=new Vector2[48];for(int i=0;i<48;i++){float t=i*Mathf.PI*2/48;float x=16*Mathf.Pow(Mathf.Sin(t),3)/18,y=(13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t))/18;points[i]=new Vector2(x,suit==3?-y:y);}Poly(points);if(suit==3)Poly(new[]{new Vector2(-.25f,-.95f),new Vector2(.25f,-.95f),new Vector2(0,-.2f)});}}
 public void Clear(){Card=-1;canvasRenderer.Clear();SetVerticesDirty();}
}
}
