using UnityEngine;
using LWS.TruckSports;
using LWS.TruckTaxi;
namespace LWS.TruckFareFight {
 public sealed class FareFightDriver:MonoBehaviour {
  public SportsEventTruck truck;public TruckTaxiDriverPresentation presentation;
  // Taxi reserves29for its hidden seated avatar; Sports reserves26-29for
  // four cockpit interiors. Keep the avatar hidden without masking cab4.
  public const int HiddenCabActorLayer=25;
  void Start(){presentation.Initialize(truck.transform);if(presentation.Occupant!=null)foreach(var part in presentation.Occupant.GetComponentsInChildren<Transform>(true))part.gameObject.layer=HiddenCabActorLayer;if(truck.session is FareFightMatch match&&match.drivingViews!=null){foreach(var view in match.drivingViews.rally.views)view.view.cullingMask&=~(1<<HiddenCabActorLayer);match.drivingViews.rally.broadcast.view.cullingMask&=~(1<<HiddenCabActorLayer);}}
 }
}
