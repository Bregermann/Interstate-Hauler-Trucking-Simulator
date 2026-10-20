using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckPoker.Editor {
public static class PokerRestaurantSurfaceAuthoring {
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(PokerAuthoring.Scene);Install(Object.FindAnyObjectByType<PokerMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(PokerMatch match){
  var root=match.transform.Find("Modeled midnight casino interior");if(root==null)throw new System.InvalidOperationException("Saved restaurant required");
  foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)){
   string name=renderer.transform.parent!=null?renderer.transform.parent.name:"";bool wall=name.Contains("casino side panel")||name.Contains("casino end panel"),floor=name.Contains("modular casino floor"),roof=name.Contains("coffered ceiling");if(!wall&&!floor&&!roof)continue;
   // Vendor architectural panels have effectively zero thickness. The old
   // three-axis fitting divided by that tiny extent, producing >200,000 scale
   // factors and invalid lighting after Unity's static mesh combination.
   var scale=renderer.transform.localScale;var center=renderer.bounds.center;
   for(int axis=0;axis<3;axis++)if(Mathf.Abs(scale[axis])>1000)scale[axis]=1;
   renderer.transform.localScale=scale;renderer.transform.position+=center-renderer.bounds.center;
   GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&~StaticEditorFlags.BatchingStatic);
   var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++){
    if(materials[i]==null)continue;string kind=wall?"Warm restaurant wall":floor?"Quiet restaurant tile":"Quiet restaurant ceiling";string path="Assets/TruckPoker/Materials/"+kind+" "+i+".mat";
    var owned=AssetDatabase.LoadAssetAtPath<Material>(path);if(owned==null){owned=new Material(materials[i]);AssetDatabase.CreateAsset(owned,path);}
    // Same-camera A/B confirmed that high-frequency industrial normal detail
    // caused the broad-surface noise. Retain authored albedo/grout and avoid
    // altering the vendor materials or smaller furniture/character surfaces.
    owned.SetTexture("_BumpMap",null);owned.DisableKeyword("_NORMALMAP");owned.SetFloat("_BumpScale",0);owned.SetFloat("_Smoothness",.2f);
    if(wall){owned.SetColor("_BaseColor",new Color(.67f,.56f,.43f));owned.SetFloat("_Metallic",0);owned.DisableKeyword("_METALLICSPECGLOSSMAP");}
    // The vendor wall modules are also viewed from inside the restaurant.
    // Keep their back faces visible to cab cameras without duplicating meshes.
    if(wall||roof)owned.SetFloat("_Cull",0);
    EditorUtility.SetDirty(owned);materials[i]=owned;
   }renderer.sharedMaterials=materials;
  }
  RenderSettings.ambientSkyColor=new Color(.24f,.18f,.25f);RenderSettings.ambientEquatorColor=new Color(.18f,.13f,.18f);RenderSettings.ambientGroundColor=new Color(.085f,.055f,.075f);
  var volume=match.GetComponentInChildren<Volume>();if(volume!=null&&volume.sharedProfile!=null&&volume.sharedProfile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(-.1f);EditorUtility.SetDirty(color);EditorUtility.SetDirty(volume.sharedProfile);}
  EditorUtility.SetDirty(match);
 }
}
}
