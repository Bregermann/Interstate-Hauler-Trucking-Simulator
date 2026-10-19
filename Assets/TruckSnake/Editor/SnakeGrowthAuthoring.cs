using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.InterstateHauler;
namespace LWS.TruckSnake.Editor {
 // Targeted upgrade: keep the saved world, truck references and existing gate art.
 public static class SnakeGrowthAuthoring {
  public const int PickupCount=64,StandardCount=56;
  [MenuItem("Truck Sports/Truck Snake/Upgrade starter trailers and frequent pickups")]
  public static void Upgrade(){EditorSceneManager.OpenScene(SnakeAuthoring.Scene);var m=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();Install(m);EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);Directory.CreateDirectory("Logs/TruckSports/EssentialRelease");File.WriteAllText("Logs/TruckSports/EssentialRelease/snake-growth-job.txt","PASS saved "+DateTime.UtcNow.ToString("o")+" · "+m.pickups.Length+" gates; "+m.world.pickupPoints.Length+" road candidates; one actual starter trailer per participant.");}
  public static void Install(SnakeMatch m){
   if(m==null||m.pickups==null||m.pickups.Length==0||m.world.graph==null)throw new InvalidOperationException("Truck Snake needs its existing saved world and gate art before the growth upgrade.");
   var gates=new List<SnakePickup>(m.pickups);var green=gates.Find(p=>!p.Golden);var gold=gates.Find(p=>p.Golden);if(green==null||gold==null)throw new InvalidOperationException("Preserve both existing green and gold pickup models.");
   int greens=gates.FindAll(p=>!p.Golden).Count;
   while(gates.Count<PickupCount){bool standard=greens<StandardCount;var gate=UnityEngine.Object.Instantiate(standard?green:gold);gate.name=standard?"Frequent green one-trailer route gate":"Golden three-trailer route gate";gate.match=m;gate.gameObject.SetActive(false);gates.Add(gate);if(standard)greens++;}
   m.pickups=gates.ToArray();m.pickupRespawnInterval=.5f;m.jumpVelocity=Mathf.Max(m.jumpVelocity,13.5f);
   var points=new List<SnakeWorld.RoadPoint>();
   foreach(var edge in m.world.graph.edges){
    // Stops are curb access stubs, and the stunt gap is air, not a road surface.
    if(edge.fromNodeId.StartsWith("taxi.stop")||edge.toNodeId.StartsWith("taxi.stop")||edge.edgeId=="snake.stunt.jump"||edge.samples.Count<2)continue;
    float next=11;
    for(int n=1;n<edge.samples.Count;n++){var a=edge.samples[n-1];var b=edge.samples[n];float length=b.distanceFromStartMeters-a.distanceFromStartMeters;if(length<.01f)continue;
     while(next<=b.distanceFromStartMeters){if(next>=a.distanceFromStartMeters){var position=Vector3.Lerp(a.position,b.position,(next-a.distanceFromStartMeters)/length);if(m.world.Contains(position))points.Add(new SnakeWorld.RoadPoint{position=position,hard=edge.roadClass==LwsRoadClass.Ramp});}next+=22;}
    }
   }
   if(points.Count<100)throw new InvalidOperationException("Truck Snake road sampling did not produce enough usable pickup candidates.");m.world.pickupPoints=points.ToArray();EditorUtility.SetDirty(m.world);EditorUtility.SetDirty(m);
  }
 }
}
