using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckPoker.Editor {
public static class PokerTabletopDealerAuthoring {
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(PokerAuthoring.Scene);Install(Object.FindAnyObjectByType<PokerMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(PokerMatch match){
  var dealer=match.dealerDrive;if(dealer==null)throw new System.InvalidOperationException("Existing mobile dealer required");
  dealer.tabletopRadius=new Vector2(4.9f,3.1f);dealer.driveSpeed=9;
  dealer.vehicle.localScale=Vector3.one*.62f;
  // Fit the actual bottom of the authored tyre meshes to the existing felt.
  var renderers=dealer.vehicle.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
  dealer.vehicle.position=new Vector3(0,dealer.vehicle.position.y+1.57f-bounds.min.y,3.1f);dealer.vehicle.rotation=Quaternion.Euler(0,180,0);
  dealer.giggles=Enumerable.Range(1,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckPoker/Audio/Dealer giggle "+i+".wav")).Where(c=>c!=null).ToArray();
  if(dealer.giggles.Length==0)Debug.LogWarning("Dealer tabletop choreography installed; original giggle assets still pending import.");
  EditorUtility.SetDirty(dealer);EditorUtility.SetDirty(match);
 }
}
}
