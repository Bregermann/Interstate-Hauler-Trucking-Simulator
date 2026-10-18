#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;
using DigitalRuby.WeatherMaker;
using LWS.TruckSports;
namespace LWS.TruckPedestrianPlowing {
 public sealed partial class PlowValidationProbe {
  IEnumerator CostumeCases(){
   m.ProbeHandsOff=true;yield return new WaitForSeconds(4);
   foreach(var t in m.trucks)t.Park(new Vector3(-65,2,65));
   Check(System.Array.TrueForAll(m.Crowd,p=>!p.beacon.gameObject.activeSelf&&!p.frontBadge.gameObject.activeSelf&&!p.backBadge.gameObject.activeSelf),"no persistent world-space target text");
   Check(System.Array.TrueForAll(m.Crowd,p=>p.GetComponent<PlowRoleCostume>()!=null&&p.GetComponent<PlowBonusEvasion>()!=null),"every shared-roster character has role wardrobe and bounded evasion");
   var night=m.GetComponent<PlowNight>();Check(night!=null&&System.Array.TrueForAll(m.cameras,c=>night.weather.vendor.AllowCameras.Contains(c)),"scene-owned Weather Maker night tracks every actual gameplay view");
   var cycle=night.weather.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>();Check(cycle.TimeOfDay==22*3600&&cycle.Speed==0&&cycle.NightSpeed==0,"fixed actual vendor night, not a weather label");
   m.Pause();yield return new WaitForSecondsRealtime(.3f);yield return Capture("polished-pause");m.SetupScreen();yield return new WaitForSecondsRealtime(.3f);yield return Capture("polished-setup");Check(m.StartMatch(),"wardrobe match rematches from setup");m.ProbeHandsOff=true;yield return new WaitForSeconds(4);
   foreach(var t in m.trucks)t.Park(new Vector3(-65,2,65));foreach(var p in m.Crowd)HoldForWardrobe(p);
   m.views.enabled=false;m.menus.hud.SetActive(false);foreach(var cab in m.views.cabRoots)if(cab!=null)cab.gameObject.SetActive(false);
   var moon=System.Array.Find(night.weather.vendor.GetComponentsInChildren<WeatherMakerCelestialObjectScript>(true),c=>!c.IsSun);
   var skyCamera=m.cameras[0];skyCamera.orthographic=false;skyCamera.transform.SetPositionAndRotation(new Vector3(-24,3,-24),Quaternion.LookRotation(-moon.transform.forward));skyCamera.fieldOfView=55;
   log.AppendLine("NIGHT_RENDER sunIntensity="+night.weather.sun.intensity+" moonDirection="+(-moon.transform.forward)+" nightMultiplier="+cycle.NightMultiplier+" skybox="+(RenderSettings.skybox!=null?RenderSettings.skybox.name:"vendor sky pass"));
   Check(RenderSettings.skybox==null,"legacy Unity daylight sky cannot suppress the vendor night sky");
   Check(cycle.NightMultiplier>.8f,"vendor computes actual night illumination");yield return new WaitForSecondsRealtime(.3f);
   var manager=WeatherMakerCommandBufferManagerScript.Instance;
   log.AppendLine("SKY_PATH ignored="+WeatherMakerScript.ShouldIgnoreCamera(night.weather.clouds,skyCamera)+" actions="+manager.GetCameraCommandBufferActions(skyCamera,UnityEngine.Rendering.CameraEvent.AfterImageEffectsOpaque)?.Count+" moonShader="+moon.Renderer.sharedMaterial.shader.name+" supported="+moon.Renderer.sharedMaterial.shader.isSupported+" pipeline="+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name);
   yield return Capture("full-moon-sky");
   for(int role=0;role<3;role++){
    var p=m.Crowd[role];p.ResetTarget(m,p.id,(PlowTargetKind)role,new Vector3(-24+role*2.5f,.05f,-24),Quaternion.identity,p.characterName,p.panicLine);HoldForWardrobe(p);
    var wardrobe=p.GetComponent<PlowRoleCostume>();Check(wardrobe.standard.activeSelf==(role==0)&&wardrobe.bonus.activeSelf==(role==1)&&wardrobe.penalty.activeSelf==(role==2),"exclusive role costume "+role);
    Check(System.Array.TrueForAll(wardrobe.clothing,r=>!r.HasPropertyBlock()),"role fabric retains shared batching without renderer blocks "+role);
    var color=role==1?wardrobe.bonusFabric:role==2?wardrobe.penaltyFabric:wardrobe.standardFabric.Length>0?wardrobe.standardFabric[p.id%wardrobe.standardFabric.Length]:Color.cyan;
    Check(System.Array.TrueForAll(wardrobe.clothing,r=>System.Array.TrueForAll(r.sharedMaterials,material=>!material.HasProperty("_BaseColor")||Vector4.Distance(material.GetColor("_BaseColor"),color)<.001f)),"shared palette preserves exact configured fabric color "+role);
    var camera=m.cameras[0];camera.orthographic=false;camera.transform.SetPositionAndRotation(p.transform.position+new Vector3(0,1.7f,4),Quaternion.LookRotation(new Vector3(0,-.15f,-4)));camera.fieldOfView=45;
    for(int lod=0;lod<2;lod++){p.presentationLOD.ForceLOD(lod);yield return new WaitForSecondsRealtime(.2f);yield return Capture("role-"+role+"-lod-"+lod);}p.presentationLOD.ForceLOD(-1);
   }
   var runner=m.Crowd[1];runner.agent.isStopped=false;var evasion=runner.GetComponent<PlowBonusEvasion>();yield return new WaitForSeconds(11);var before=runner.transform.position;
   Check(evasion.TryEscape(Vector3.left),"bonus ordinary ground dodge begins");float highest=before.y;for(float elapsed=0;elapsed<1;elapsed+=Time.fixedDeltaTime){highest=Mathf.Max(highest,runner.transform.position.y);yield return new WaitForFixedUpdate();}
   Check(highest>before.y+.4f&&!evasion.Active&&runner.agent.updatePosition,"bounded jump rises and returns to ordinary navigation");
   runner.ResetTarget(m,runner.id,PlowTargetKind.BonusRunner,new Vector3(-24,.05f,-24),Quaternion.identity,runner.characterName,runner.panicLine);HoldForWardrobe(runner);
   var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.name="Explicit safe climb fixture";prop.transform.position=new Vector3(-22.4f,.5f,-24);prop.transform.localScale=new Vector3(1.5f,1,1.5f);m.climbSurfaces=new[]{prop.GetComponent<Collider>()};Physics.SyncTransforms();yield return new WaitForSeconds(11);
   Check(evasion.TryEscape(Vector3.left),"bonus prop climb begins through bounded traversal");yield return new WaitForSeconds(.8f);Check(evasion.ClimbCount==1&&runner.transform.position.y>.9f,"runner actually perches on solid prop");var perchCamera=m.cameras[0];perchCamera.transform.SetPositionAndRotation(runner.transform.position+new Vector3(2,2,4),Quaternion.LookRotation(new Vector3(-2,-1,-4)));var screen=perchCamera.WorldToViewportPoint(runner.transform.position+Vector3.up);Check(screen.z>0&&screen.x>.1f&&screen.x<.9f&&screen.y>.1f&&screen.y<.9f,"actual perch evidence camera contains the runner");yield return Capture("bonus-prop-perch");yield return new WaitForSeconds(2);Check(!evasion.Active&&runner.agent.updatePosition&&runner.transform.position.y<.2f,"perch automatically returns to reachable ground");Destroy(prop);
   m.views.enabled=true;m.menus.hud.SetActive(true);m.SetupScreen();Check(m.StartMatch(),"normal crowd and trucks restore after explicit wardrobe fixtures");m.ProbeHandsOff=false;yield return new WaitForSeconds(12);yield return Capture("night-gameplay");
  }
  void HoldForWardrobe(PlowPedestrian p){p.agent.isStopped=true;typeof(PlowPedestrian).GetField("nextThink",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p,Time.time+1000);}
 }
}
#endif
