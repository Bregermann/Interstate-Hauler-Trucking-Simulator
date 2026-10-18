using System.Collections.Generic;using UnityEngine;
namespace LWS.TruckTerrierRacing {
// Scene-owned lava contact and pooled feedback. Finish authority stays in TerrierMatch.
public sealed class TerrierVolcano:MonoBehaviour {
 public TerrierMatch match;public Terrain terrain;public Transform lava;public ParticleSystem[] splashes;public int Impacts{get;private set;}readonly HashSet<Rigidbody> contacted=new HashSet<Rigidbody>();
 public void ResetHeat(){contacted.Clear();Impacts=0;foreach(var effect in splashes)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 public void Contact(Collider shape){var body=shape.attachedRigidbody;if(body==null||contacted.Contains(body))return;var delta=body.worldCenterOfMass-lava.position;if(new Vector2(delta.x,delta.z).sqrMagnitude>340*340)return;var racer=body.GetComponent<TerrierRacer>();if(racer!=null&&racer.Finished)return;contacted.Add(body);Impacts++;var effect=splashes[Impacts%splashes.Length];effect.transform.position=new Vector3(body.position.x,lava.position.y+1,body.position.z);effect.Play();if(racer!=null){match.QueueWreck(racer.slot,Time.fixedTime,"ACTUAL_LAVA_CONTACT");return;}if(match.wrecks!=null)foreach(var piece in match.wrecks.pieces)if(piece.body==body){body.isKinematic=true;body.gameObject.SetActive(false);break;}}
}
}
