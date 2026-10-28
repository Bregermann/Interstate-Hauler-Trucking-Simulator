#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Digger.Modules.Core.Sources;
public sealed class ArchaeologyLongCutDiagnostics
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
 static Vector3 XYZ(object x)=>new Vector3((float)Get(x,"x"),(float)Get(x,"y"),(float)Get(x,"z"));
 static string Samples(Behaviour match)
 {
  var snapshot=typeof(ArchaeologyExcavationDiagnostics).GetMethod("Snapshot",BindingFlags.NonPublic|BindingFlags.Static);var s=new StringBuilder((string)snapshot.Invoke(null,new object[]{match}));object earth=Get(match,"earth");var system=(Component)Get(earth,"system");var chunks=system.GetComponentsInChildren<VoxelChunk>(true);var targets=(int[])Get(match,"Targets");var artifacts=(Array)Get(match,"artifacts");
  for(int owner=0;owner<4;owner++)
  {
   int id=targets[owner];if(id<0)continue;var artifact=artifacts.GetValue(id);Vector3 point=(Vector3)match.GetType().GetMethod("CutPoint",Any).Invoke(match,new object[]{artifact});bool sampled=false;
   foreach(var chunk in chunks)
   {
    if(!chunk.IsLoaded)continue;Vector3 origin=XYZ(Get(chunk,"AbsoluteWorldPosition")),scale=XYZ(Get(chunk,"HeightmapScale"));int size=chunk.SizeVox;Vector3 local=point-origin;int ix=Mathf.RoundToInt(local.x/scale.x),iy=Mathf.RoundToInt(local.y/scale.y),iz=Mathf.RoundToInt(local.z/scale.z);if(ix<1||ix>=size-1||iy<3||iy>=size-2||iz<1||iz>=size-1)continue;
    s.Append("ARCHAEOLOGY_ACTUAL_SDF_BLOCK owner=").Append(owner).Append(" id=").Append(id).Append(" chunk=").Append(chunk.name).Append(" origin=").Append(origin.ToString("F6")).Append(" scale=").Append(scale.ToString("F6")).Append(" size=").Append(size).Append(" cutPoint=").Append(point.ToString("F6")).AppendLine();
    // Same x-major/y-middle/z-minor indexing as unmodified vendor Utils.IndexToXYZ.
    // Read only already-loaded public VoxelArray; never Load/Refresh/edit voxels.
    var voxels=chunk.VoxelArray;for(int dx=-1;dx<=1;dx++)for(int dy=-3;dy<=1;dy++)for(int dz=-1;dz<=1;dz++)
    {
     int x=ix+dx,y=iy+dy,z=iz+dz,index=x*size*size+y*size+z;float value=voxels[index].Value;Vector3 position=origin+Vector3.Scale(new Vector3(x,y,z),scale);
     s.Append("ARCHAEOLOGY_ACTUAL_SDF owner=").Append(owner).Append(" id=").Append(id).Append(" voxel=").Append(x).Append(',').Append(y).Append(',').Append(z).Append(" world=").Append(position.ToString("F6")).Append(" density=").Append(value.ToString("R")).Append(" inside=").Append(value<0).Append(" finite=").Append(float.IsFinite(value)).AppendLine();
    }
    sampled=true;break;
   }
   if(!sampled)s.Append("ARCHAEOLOGY_ACTUAL_SDF_NOT_SAMPLED owner=").Append(owner).Append(" id=").Append(id).Append(" reason=noAlreadyLoadedInteriorChunkForActualCutPoint loadedCandidates=").Append(chunks.Length).AppendLine();
  }
  return s.ToString();
 }
 [UnityTest,Timeout(120000)]public IEnumerator AcceleratedOriginalFixedStepsObserveSixHundredSecondsOfActualAiCuttingAndVendorSdf()
 {
  var type=typeof(ArchaeologyPlayModeTests).GetNestedType("Fixture",BindingFlags.NonPublic);var fixture=(IDisposable)Activator.CreateInstance(type,true);string directory=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ArchaeologyLongCutDiagnosticNative/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(directory);string path=Path.Combine(directory,"original-fixed-steps-accelerated-600s.txt");float fixedStep;
  using(fixture)
  {
   yield return (IEnumerator)type.GetMethod("Load",Any).Invoke(fixture,null);var match=(Behaviour)Get(fixture,"match");fixedStep=Time.fixedDeltaTime;Time.timeScale=8;float end=Time.realtimeSinceStartup+90,next=0;
   try
   {
    while((float)Get(match,"Clock")<600&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<end)
    {
     if((float)Get(match,"Clock")>=next){next=(float)Get(match,"Clock")+20;string state=Samples(match);File.AppendAllText(path,state);Debug.Log(state);}yield return null;
    }
    string final=Samples(match);File.AppendAllText(path,final);Debug.Log(final);Assert.That(Time.fixedDeltaTime,Is.EqualTo(fixedStep),"Original physics step must remain exact; acceleration changes only timeScale");Assert.That((float)Get(match,"Clock"),Is.GreaterThan(65),"Must extend actual original cutting beyond retained65s trace; measured completion is not assumed");
    TestContext.WriteLine("ARCHAEOLOGY_ACCELERATED_MEASUREMENT "+path+" actualClock="+Get(match,"Clock")+"; timeScale8, originalfixedstep="+fixedStep+",90real-second cap; not original unaccelerated full-match, Windows, performance or persistence acceptance.");
   }
   finally{Time.timeScale=1;}
  }
 }
}
#endif
