using UnityEngine;
namespace LWS.TruckTerrierRacing {public sealed class TerrierLavaSurface:MonoBehaviour {public TerrierVolcano volcano;void OnTriggerEnter(Collider shape){volcano.Contact(shape);}}}
