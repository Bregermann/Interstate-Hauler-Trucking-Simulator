using UnityEditor;
namespace LWS.TruckPedestrianPlowing.Editor {
 public static class PlowPresentationFixtures {
  [MenuItem("Truck Sports/Pedestrian Plowing/Validate Overview Safe Framing")]public static void Run(){
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save scene edits before validating Plowing presentation.");
var oldSetup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckPedestrianPlowing.Editor.PlowAuthoring.Scene);
var match=UnityEngine.Object.FindAnyObjectByType<LWS.TruckPedestrianPlowing.PlowMatch>();
var root=new UnityEngine.GameObject("Temporary overview geometry fixture");root.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
int checks=0;
try{
 var camera=root.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.orthographic=true;
 var fit=root.AddComponent<LWS.TruckSports.SportsCameraFit>();fit.cameraTarget=camera;fit.arenaFit=true;fit.topInset=.12f;fit.bottomInset=.2f;
 var contain=typeof(LWS.TruckPedestrianPlowing.PlowViews).GetMethod("ContainGround",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 foreach(float aspect in new[]{1280f/720,1920f/1080,1280f/800,1024f/768,1920f/800})for(int scenario=0;scenario<2;scenario++)for(int count=1;count<=4;count++){
  var positions=new UnityEngine.Vector3[count];UnityEngine.Bounds bounds=default;
  for(int i=0;i<count;i++){positions[i]=scenario==0?match.truckStarts[i].position:new UnityEngine.Vector3((i%2==0?-1:1)*66,.65f,(i/2==0?-1:1)*66);var part=new UnityEngine.Bounds(positions[i],UnityEngine.Vector3.one*36);if(i==0)bounds=part;else bounds.Encapsulate(part);}
  bounds.size=new UnityEngine.Vector3(UnityEngine.Mathf.Min(144,bounds.size.x),12,UnityEngine.Mathf.Min(144,bounds.size.z));
  var focus=bounds.center;focus.y=0;camera.aspect=aspect;camera.transform.SetPositionAndRotation(focus+new UnityEngine.Vector3(0,118,-66),UnityEngine.Quaternion.Euler(61,0,0));fit.arenaBounds=bounds;fit.Apply();contain.Invoke(match.views,new object[]{camera});
  foreach(var position in positions){var point=camera.WorldToViewportPoint(position+UnityEngine.Vector3.up);if(point.z<=0||point.x<=0||point.x>=1||point.y<=.2f||point.y>=.88f)throw new System.Exception("Unsafe overview aspect="+aspect+" scenario="+scenario+" count="+count+" projection="+point);checks++;}
 }
}finally{UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(oldSetup);}
UnityEngine.Debug.Log("PLOW_OVERVIEW_SAFE_FIT_PASS checks="+checks);

  }
 }
}
