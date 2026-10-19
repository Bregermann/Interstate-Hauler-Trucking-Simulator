using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace LWS.TruckCheeseRolling.Editor {
// One owned TMP material; the existing position/distance tracking remains untouched.
public static class CheeseIndicatorAuthoring {
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(CheeseAuthoring.Scene);Install(Object.FindAnyObjectByType<CheeseMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(CheeseMatch match){const string path="Assets/TruckCheeseRolling/Materials/Outlined cheese locator.mat";var source=match.menus.indicators[0].font.material;var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(source);material.name="Outlined cheese locator";material.SetFloat("_OutlineWidth",.28f);material.SetColor("_OutlineColor",new Color(.01f,.025f,.04f,1));material.EnableKeyword("OUTLINE_ON");material.SetColor("_UnderlayColor",new Color(0,0,0,.9f));material.SetFloat("_UnderlayOffsetX",.8f);material.SetFloat("_UnderlayOffsetY",-.8f);material.SetFloat("_UnderlayDilate",.15f);material.SetFloat("_UnderlaySoftness",.25f);material.EnableKeyword("UNDERLAY_ON");AssetDatabase.CreateAsset(material,path);}foreach(var label in match.menus.indicators){label.fontSharedMaterial=material;label.extraPadding=true;label.UpdateMeshPadding();EditorUtility.SetDirty(label);}}
}
}
