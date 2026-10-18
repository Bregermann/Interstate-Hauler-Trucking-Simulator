using UnityEngine;
namespace LWS.TruckTerrierRacing {
public sealed class TerrierCreamSurface:MonoBehaviour {public TerrierDessert dessert;void OnCollisionEnter(Collision c){var racer=c.rigidbody!=null?c.rigidbody.GetComponent<TerrierRacer>():null;if(racer!=null)dessert.Contact(racer,c);}void OnCollisionStay(Collision c){OnCollisionEnter(c);}}
}