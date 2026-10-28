using System;
using System.Linq;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckOrchestra.Editor
{
 // Only the existing world enamel label moves. Existing font/depth shader,
 // material, billboard, song/status HUD, camera and musical scene stay intact.
 public static class OrchestraVenueTitleAuthoring
 {
  static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Orchestra venue title: "+reason);}
  static bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.0001f;
  static void ValidateKnownPose(TextMesh title)
  {
   Require(title!=null&&title.name=="Concert venue original enamel title"&&title.text=="THE SIX-TON SYMPHONY","exact original enamel content/identity required");
   Require(Near(title.transform.position,new Vector3(0,10.5f,25))&&Near(title.transform.localScale,Vector3.one)&&Quaternion.Angle(title.transform.rotation,Quaternion.identity)<.001f&&title.transform.parent==null,"custom/already-moved original title pose refused");
   Require(Mathf.Abs(title.characterSize-.26f)<.0001f&&title.fontSize==64&&title.anchor==TextAnchor.MiddleCenter&&title.alignment==TextAlignment.Center&&title.fontStyle==FontStyle.Normal&&title.richText&&Mathf.Abs(title.lineSpacing-1)<.0001f&&((Color32)title.color).Equals((Color32)new Color(1,.84f,.45f)),"original owned title typography/color required");
   Require(title.transform.childCount==0&&title.GetComponents<Collider>().Length==0&&title.GetComponent<Rigidbody>()==null,"original render-only label required");
  }
  // Used after the full original new-presentation preflight as well.
  internal static void ApplyNewTitlePosition(TextMesh title){title.transform.position=new Vector3(0,5.4f,25);}
  [MenuItem("Truck Sports/Impossible Games/Repair OPEN Orchestra/Existing venue title curtain clearance")]
  public static void InstallOnOpenOriginalScene()
  {
   Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped compiled Editor required");
   var matches=UnityEngine.Object.FindObjectsByType<OrchestraMatch>(FindObjectsInactive.Include,FindObjectsSortMode.None);Require(matches.Length==1&&matches[0].gameObject.scene.path==OrchestraAuthoring.ScenePath,"exact original saved Orchestra scene required");
   var scene=matches[0].gameObject.scene;var title=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)).Single(t=>t.name=="Concert venue original enamel title");ValidateKnownPose(title);
   var renderer=title.GetComponent<MeshRenderer>();var billboard=title.GetComponent<SportsPerCameraBillboard>();var depth=title.GetComponent<SportsDepthTestedWorldText>();
   Require(renderer!=null&&renderer.sharedMaterials.Length==1&&renderer.sharedMaterial!=null&&title.font!=null,"existing real font/material required");
   Require(title.font==Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),"exact original built-in font from SportsEventAuthoring.Label required");
   Require(renderer.sharedMaterial==title.font.material&&renderer.sharedMaterial.shader!=null&&renderer.sharedMaterial.shader.name=="GUI/Text Shader","actual saved original font material required; existing depth adapter changes it only at runtime");
   Require(billboard!=null&&billboard.enabled&&!billboard.distanceSizing&&billboard.maximumPixelWidth==600&&billboard.maximumPixelHeight==36&&billboard.cameraFacingOffset==0,"known original bounded billboard required");
   Require(depth!=null&&depth.enabled&&depth.worldTextShader!=null,"existing lead-owned actual depth-tested font binding required; never replace it");
   Require(title.GetComponents<MonoBehaviour>().All(c=>c==billboard||c==depth),"custom label behavior refused");
   // All checks precede the only write; no component/material/font/rotation change.
   ApplyNewTitlePosition(title);EditorUtility.SetDirty(title.transform);EditorSceneManager.MarkSceneDirty(scene);
   Debug.Log("ORCHESTRA_VENUE_TITLE_V5 only existing world label y10.5->5.4 in blank curtain; actual font/depth shader/material/content/rotation and all audio/gameplay/camera/HUD preserved. Caller owns save; actual720/1080 no-overlap/GPU tests required.");
  }
 }
}
