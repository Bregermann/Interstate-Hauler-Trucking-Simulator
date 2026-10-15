using UnityEngine;
namespace LWS.TruckSports {public sealed class SportsPerCameraBillboard:MonoBehaviour {public int excludedLayer=-1;void OnWillRenderObject(){var camera=Camera.current;if(camera!=null)transform.rotation=camera.transform.rotation;}}}
