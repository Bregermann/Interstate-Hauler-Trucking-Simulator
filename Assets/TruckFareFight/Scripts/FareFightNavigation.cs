using UnityEngine;
using CompassNavigatorPro;
namespace LWS.TruckFareFight {
 public sealed class FareFightNavigation:MonoBehaviour {
  public FareFightMatch match;public CompassPro[] gps;
  [HideInInspector]public int playtestRepairVersion;
  public CompassProPOI[] objectiveMarkers;
  readonly int[] routeVersions={-1,-1,-1,-1};readonly int[] owners={-1,-1,-1,-1};
  public void ResetMatch(){for(int i=0;i<4;i++){owners[i]=routeVersions[i]=-1;gps[i].ClearRoute();if(objectiveMarkers!=null&&i<objectiveMarkers.Length)objectiveMarkers[i].gameObject.SetActive(false);}}
  public void Tick(float dt){for(int v=0;v<4;v++){bool visible=v<match.drivingViews.rally.Humans&&!match.drivingViews.NeedsArena;gps[v].gameObject.SetActive(visible);if(objectiveMarkers!=null&&v<objectiveMarkers.Length){var marker=objectiveMarkers[v];marker.gameObject.SetActive(visible);if(visible){int owner=match.drivingViews.rally.owners[v];marker.transform.position=match.Goal(owner);marker.title=match.rules.Carrier==owner?"DELIVER":match.rules.Carrier>=0?"STEAL":"PICK UP";marker.tintColor=match.rules.Carrier==owner?new Color(1,.75f,.1f):match.rules.Carrier>=0?new Color(1,.25f,.1f):new Color(.1f,.75f,1);}}if(!visible)continue;int slot=match.drivingViews.rally.owners[v];gps[v].follow=match.trucks[slot].transform;gps[v].cameraMain=match.cameras[v];gps[v].viewportRect=match.cameras[v].rect;var route=match.routes[slot];if(owners[v]!=slot||routeVersions[v]!=route.Searches){owners[v]=slot;routeVersions[v]=route.Searches;if(route.points.Count>1)gps[v].SetRoute(route.points);else gps[v].ClearRoute();}}}
  void OnDisable(){if(objectiveMarkers!=null)foreach(var marker in objectiveMarkers)if(marker!=null)marker.gameObject.SetActive(false);}
 }
}
