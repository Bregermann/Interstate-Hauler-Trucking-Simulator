using System;
using UnityEngine;
namespace LWS.TruckHideAndSeek
{
 // Only actual observations enter the seeker's knowledge. A sound provides a
 // quantized bearing, never a hidden truck's live transform or identity.
 [Serializable]public sealed class HideKnowledge
 {
  public readonly Vector3[] lastSeen=new Vector3[8];
  public readonly float[] seenAt=new float[8];
  public int SoundSector{get;private set;}=-1;public string SoundBand{get;private set;}="";public float SoundAt{get;private set;}=-1000;
  public void Reset(){Array.Fill(seenAt,-1000);Array.Clear(lastSeen,0,lastSeen.Length);SoundSector=-1;SoundBand="";SoundAt=-1000;}
  public void See(int target,Vector3 position,float now){if(target<0||target>=8)return;lastSeen[target]=position;seenAt[target]=now;}
  public bool Recent(int target,float now,float lifetime=5)=>target>=0&&target<8&&now>=seenAt[target]&&now-seenAt[target]<=lifetime;
  public void Hear(Vector3 listener,Vector3 source,float now){var delta=source-listener;SoundSector=HideSignalRules.Sector(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg);SoundBand=HideSignalRules.Band(delta.magnitude);SoundAt=now;}
  public Vector3 SectorGoal(Vector3 origin)=>origin+Quaternion.Euler(0,SoundSector*45,0)*Vector3.forward*(SoundBand=="NEAR"?45:SoundBand=="MID-RANGE"?100:180);
 }
}
