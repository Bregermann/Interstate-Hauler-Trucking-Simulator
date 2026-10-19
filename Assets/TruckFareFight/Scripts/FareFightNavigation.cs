using UnityEngine;
using CompassNavigatorPro;
namespace LWS.TruckFareFight {
 public sealed class FareFightNavigation:MonoBehaviour {
  public FareFightMatch match;public CompassPro[] gps;
  readonly int[] routeVersions={-1,-1,-1,-1};readonly int[] owners={-1,-1,-1,-1};
  public void ResetMatch(){for(int i=0;i<4;i++){owners[i]=routeVersions[i]=-1;gps[i].ClearRoute();}}
  public void Tick(float dt){for(int v=0;v<4;v++){bool visible=v<match.drivingViews.rally.Humans&&!match.drivingViews.NeedsArena;gps[v].gameObject.SetActive(visible);if(!visible)continue;int slot=match.drivingViews.rally.owners[v];gps[v].follow=match.trucks[slot].transform;gps[v].cameraMain=match.cameras[v];var route=match.routes[slot];if(owners[v]!=slot||routeVersions[v]!=route.Searches){owners[v]=slot;routeVersions[v]=route.Searches;if(route.points.Count>1)gps[v].SetRoute(route.points);else gps[v].ClearRoute();}}}
 }
}
