using UnityEngine;
using LWS.TruckSports;
using LWS.TruckTaxi;
namespace LWS.TruckFareFight {
 public sealed class FareFightDriver:MonoBehaviour {
  public SportsEventTruck truck;public TruckTaxiDriverPresentation presentation;
  void Start(){presentation.Initialize(truck.transform);}
 }
}
