using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.AI.Navigation;
namespace LWS.TruckPedestrianPlowing.Editor {
 [CustomEditor(typeof(PlowSettings))]public sealed class PlowSettingsEditor:UnityEditor.Editor {
  public override void OnInspectorGUI(){EditorGUILayout.HelpBox("PARADE CROWD\nCounts and role fractions apply on the next match. Stop and re-enter Play Mode after changing character prefabs or weights. Character identity never determines scoring. Edit spawn rectangles in the scene using their green gizmos. See Documentation/TruckSports/PedestrianPlowingCustomization.md.",MessageType.Info);DrawDefaultInspector();if(GUILayout.Button("Open Arena Scene"))EditorSceneManager.OpenScene(PlowAuthoring.Scene);if(GUILayout.Button("Open Cab Chaos Scene"))EditorSceneManager.OpenScene(PlowCabAuthoring.Scene);if(GUILayout.Button("Select Spawn Zones in Current Plowing Scene")){if(!Current())return;Selection.objects=Object.FindObjectsByType<PlowSpawnZone>();}if(GUILayout.Button("Rebake Current Plowing Navigation (preserves scene layout)"))Bake();}
  static bool Current(){var path=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;if(path==PlowAuthoring.Scene||path==PlowCabAuthoring.Scene)return true;Debug.LogError("Open one of the two owned Plowing scenes first.");return false;}
  [MenuItem("Truck Sports/Pedestrian Plowing/Rebake Current Scene Navigation")]
  public static void Bake(){if(EditorApplication.isPlaying||!Current())return;var surface=Object.FindAnyObjectByType<NavMeshSurface>();if(surface==null){Debug.LogError("Plowing navigation surface is missing.");return;}PlowAuthoring.BakeNavigation(surface);var m=Object.FindAnyObjectByType<PlowMatch>();m.spawnZones=Object.FindObjectsByType<PlowSpawnZone>();EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("PLOW_CUSTOM_LAYOUT_NAVIGATION_SAVED");}
 }
}
