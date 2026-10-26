using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckCornhole
{
 // Mode-local pairs only. NWH approach forces and native board contacts remain
 // authoritative; eligibility never moves a body or resolves an overlap itself.
 public sealed class CornholeLandingCollision
 {
  sealed class Entry{public CornholeTruck truck;public readonly List<Collider> shapes=new List<Collider>(8);}
  readonly struct Key:System.IEquatable<Key>
  {
   public readonly Collider a,b;public Key(Collider left,Collider right){a=left;b=right;}
   public bool Equals(Key other)=>a==other.a&&b==other.b;public override bool Equals(object other)=>other is Key key&&Equals(key);public override int GetHashCode()=>a.GetInstanceID()*397^b.GetInstanceID();
  }
  readonly List<Entry> entries=new List<Entry>(16);readonly Dictionary<Key,bool> original=new Dictionary<Key,bool>();readonly HashSet<int> enabledPairs=new HashSet<int>();
  public int PendingOverlaps{get;private set;}
  public int ActivePairs=>enabledPairs.Count;
  public CornholeLandingCollision(CornholeMatch match){foreach(var course in match.courses)foreach(var truck in course.trucks)entries.Add(new Entry{truck=truck});}
  static bool Active(Collider shape)=>shape!=null&&shape.enabled&&!shape.isTrigger&&shape.gameObject.activeInHierarchy;
  public void Tick()
  {
   PendingOverlaps=0;
   foreach(var entry in entries)if(entry.truck.gameObject.activeInHierarchy){entry.truck.GetComponentsInChildren(true,entry.shapes);entry.truck.ObserveLandingCollisionRegion();}
   for(int i=0;i<entries.Count;i++)
   {
    var left=entries[i];if(!left.truck.gameObject.activeInHierarchy||!left.truck.body.detectCollisions){for(int j=i+1;j<entries.Count;j++)enabledPairs.Remove(i*entries.Count+j);continue;}
    for(int j=i+1;j<entries.Count;j++)
    {
     var right=entries[j];if(!right.truck.gameObject.activeInHierarchy||!right.truck.body.detectCollisions){enabledPairs.Remove(i*entries.Count+j);continue;}
     int pair=i*entries.Count+j;bool eligible=left.truck.LandingCollisionReady&&right.truck.LandingCollisionReady,overlap=false;
     if(!eligible)enabledPairs.Remove(pair);
     if(eligible&&!enabledPairs.Contains(pair))
     {
      foreach(var a in left.shapes)if(Active(a))foreach(var b in right.shapes)if(Active(b)&&a.bounds.Intersects(b.bounds)&&Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out float depth)&&depth>0)overlap=true;
      if(!overlap)enabledPairs.Add(pair);
     }
     if(overlap)PendingOverlaps++;
     foreach(var a in left.shapes)if(Active(a))foreach(var b in right.shapes)if(Active(b))
     {
      var key=new Key(a,b);if(!original.TryGetValue(key,out bool baseline)){baseline=Physics.GetIgnoreCollision(a,b);original.Add(key,baseline);}
      bool ignore=baseline||!eligible||overlap;if(Physics.GetIgnoreCollision(a,b)!=ignore)Physics.IgnoreCollision(a,b,ignore);
     }
    }
   }
  }
  // Retain each original pair baseline across pooled collider deactivation; a
  // rematch must never mistake this policy's previous ignore for vendor state.
  public void Clear(){foreach(var pair in original)if(Active(pair.Key.a)&&Active(pair.Key.b))Physics.IgnoreCollision(pair.Key.a,pair.Key.b,pair.Value);enabledPairs.Clear();PendingOverlaps=0;}
 }
}
