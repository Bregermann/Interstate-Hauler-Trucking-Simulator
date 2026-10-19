using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckFieldGoal.Editor {
// Non-colliding painted presentation; all coordinates are relative to the back end line.
public static class FieldGoalFieldArtAuthoring {
 public const string LogoPath="Assets/TruckFieldGoal/Art/Truck Field Goal end-zone wordmark.png";
 public static void Install(FieldGoalMatch match){
  var assembly=match.stadiumBoard.fieldAssembly;
  var party=assembly.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name=="Original graphical Party end-zone wordmark"||r.name=="Original Party logo · fifty-yard midfield");
  if(party==null)throw new System.InvalidOperationException("The existing Party wordmark is required.");
  party.name="Original Party logo · fifty-yard midfield";party.transform.SetParent(assembly,false);party.transform.localPosition=new Vector3(0,.056f,-54.864f);party.transform.localScale=new Vector3(16,10.667f,1);party.transform.localRotation=Quaternion.Euler(90,0,90);
  var importer=(TextureImporter)AssetImporter.GetAtPath(LogoPath);if(importer==null)throw new System.InvalidOperationException("Original Field Goal end-zone artwork is missing.");
  if(!importer.alphaIsTransparency||!importer.mipmapEnabled||importer.maxTextureSize<4096){importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;importer.SaveAndReimport();}
  string path="Assets/TruckFieldGoal/Materials/Original Field Goal end-zone turf ink.mat";
  var ink=AssetDatabase.LoadAssetAtPath<Material>(path);if(ink==null){ink=new Material(party.sharedMaterial);ink.name="Original Field Goal end-zone turf ink";AssetDatabase.CreateAsset(ink,path);}
  ink.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath));EditorUtility.SetDirty(ink);
  // Keep the image's native aspect. UV window removes transparent top/bottom
  // padding without stretching or rewriting the generated PNG.
  ink.SetTextureScale("_BaseMap",new Vector2(1,.6f));ink.SetTextureOffset("_BaseMap",new Vector2(0,.2f));
  var mark=match.endZone.Find("Original Field Goal full-width end-zone artwork");if(mark==null){var g=GameObject.CreatePrimitive(PrimitiveType.Quad);Object.DestroyImmediate(g.GetComponent<Collider>());g.name="Original Field Goal full-width end-zone artwork";mark=g.transform;mark.SetParent(match.endZone,false);}
  mark.localPosition=new Vector3(0,.035f,0);mark.localRotation=Quaternion.Euler(90,0,0);mark.localScale=new Vector3(50,10,1);mark.GetComponent<MeshRenderer>().sharedMaterial=ink;
  // The 10m quad's transparent margins extend beyond the 9.144m paint, while
  // all visible ink remains inside it. Visible logo is approximately 50 x 7m.
  var labels=assembly.Find("Traditional sideline yard numbers");if(labels==null){labels=new GameObject("Traditional sideline yard numbers").transform;labels.SetParent(assembly,false);
   var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
   for(int yard=10;yard<=90;yard+=10)foreach(int side in new[]{-1,1}){
    int number=Mathf.Min(yard,100-yard);var g=new GameObject("Sideline "+side+" · "+number+" yards",typeof(TextMeshPro));g.transform.SetParent(labels,false);g.transform.localPosition=new Vector3(side*22,.045f,-9.144f-yard*.9144f);g.transform.localRotation=Quaternion.Euler(90,0,side<0?90:-90);
    var t=g.GetComponent<TextMeshPro>();t.text=number.ToString();if(theme!=null)t.font=theme.headingFont;t.fontSize=27;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.94f,.93f,.83f);t.textWrappingMode=TextWrappingModes.NoWrap;t.rectTransform.sizeDelta=new Vector2(5,4);t.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   }
  }
  EditorUtility.SetDirty(party);EditorUtility.SetDirty(match.stadiumBoard);
 }
}
}
