using UnityEngine;
namespace LWS.TruckNinjaWarrior
{
 // Checkpoint recovery follows meaningful body movement, not instantaneous
 // suspension velocity. Small PhysX contact jitter must not postpone it forever.
 public sealed class NinjaProgressWatch
 {
  Vector3 anchor;bool initialized;
  public float Seconds{get;private set;}
  public void Reset(Vector3 position){anchor=position;Seconds=0;initialized=true;}
  public void Step(Vector3 position,float dt){if(!float.IsFinite(dt)||dt<=0)return;if(!initialized||(position-anchor).sqrMagnitude>=4){Reset(position);return;}Seconds+=dt;}
 }
}
