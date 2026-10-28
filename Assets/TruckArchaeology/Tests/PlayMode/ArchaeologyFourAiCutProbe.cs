#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
// Test-only exact event observer. Multiple serial cuts in one fixed step are
// no longer attributed from the old single-success round-robin cursor.
[DefaultExecutionOrder(2000)]
public sealed class ArchaeologyFourAiCutProbe:MonoBehaviour
{
 public Component match;public string path;
 public readonly int[,] Cuts=new int[4,6];public readonly double[] RequestedSphereVolume=new double[4],OpacityWeightedSphereVolume=new double[4];
 public int UnattributedEdits{get;private set;}public int ObservedEdits{get;private set;}public int DuplicateOwnersInPass{get;private set;}public int MaximumCutsInPass{get;private set;}public int FullCrewPasses{get;private set;}public int StartingOwnerMask{get;private set;}
 public float MinimumOwnerInterval{get;private set;}=float.PositiveInfinity;
 public double MeasuredCutMilliseconds{get;private set;}public double MaximumSerialPassMilliseconds{get;private set;}
 int baselineEdits,baselineStrokes,passMask,passCuts;float passFixedTime=float.NegativeInfinity;double passMilliseconds;readonly float[] lastClock={-1,-1,-1,-1};bool initialized;EventInfo acceptedEvent;Delegate acceptedHandler;
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
 public void StartObservation()
 {
  baselineEdits=(int)Get(Get(match,"earth"),"ActualEdits");baselineStrokes=(int)Get(match,"ToolStrokes");acceptedEvent=match.GetType().GetEvent("CutAccepted");acceptedHandler=Delegate.CreateDelegate(acceptedEvent.EventHandlerType,this,GetType().GetMethod("Accepted",Any));acceptedEvent.AddEventHandler(match,acceptedHandler);initialized=true;
 }
 void Accepted(int owner,int target,int tool,Vector3 point,float radius,float opacity,float clock,int edit)
 {
  if(owner<0||owner>=4||tool<1||tool>4){UnattributedEdits++;return;}
  if(passFixedTime!=Time.fixedTime){passFixedTime=Time.fixedTime;passMask=passCuts=0;passMilliseconds=0;StartingOwnerMask|=1<<owner;}
  if((passMask&(1<<owner))!=0)DuplicateOwnersInPass++;passMask|=1<<owner;passCuts++;MaximumCutsInPass=Mathf.Max(MaximumCutsInPass,passCuts);if(passMask==15&&passCuts==4)FullCrewPasses++;
  if(lastClock[owner]>=0)MinimumOwnerInterval=Mathf.Min(MinimumOwnerInterval,clock-lastClock[owner]);lastClock[owner]=clock;
  ObservedEdits++;Cuts[owner,tool]++;double volume=4d/3*Math.PI*radius*radius*radius;RequestedSphereVolume[owner]+=volume;OpacityWeightedSphereVolume[owner]+=volume*opacity;double ms=(double)Get(match,"LastCutMilliseconds");MeasuredCutMilliseconds+=ms;passMilliseconds+=ms;MaximumSerialPassMilliseconds=Math.Max(MaximumSerialPassMilliseconds,passMilliseconds);
  File.AppendAllText(path,"ACCEPTED_ACTUAL_DIGGER_CUT clock="+clock.ToString("R")+" fixed="+Time.fixedTime.ToString("R")+" actualEdit="+edit+" owner="+owner+" target="+target+" tool="+((Array)Get(match,"Tools")).GetValue(owner)+" centerAtOriginalCut="+point.ToString("F6")+" radius="+radius.ToString("R")+" opacity="+opacity.ToString("R")+" synchronousVendorCutMilliseconds="+ms.ToString("R")+" requestedSphereVolume="+volume.ToString("R")+" NOTE=exact accepted owner event; requested brush volume, NOT actual net removed soil\n",new UTF8Encoding(false));
 }
 void FixedUpdate()
 {
  if(match==null||!initialized)return;int delta=(int)Get(Get(match,"earth"),"ActualEdits")-baselineEdits,strokes=(int)Get(match,"ToolStrokes")-baselineStrokes;
  UnattributedEdits=Math.Max(UnattributedEdits,Math.Max(Math.Abs(delta-ObservedEdits),Math.Abs(strokes-ObservedEdits)));
 }
 public string Summary()
 {
  var text=new StringBuilder("ACCEPTED_CUT_SUMMARY exactEvents="+ObservedEdits+" unattributed="+UnattributedEdits+" duplicateOwnersInPass="+DuplicateOwnersInPass+" maxCutsPerPass="+MaximumCutsInPass+" fullCrewPasses="+FullCrewPasses+" startingOwnerMask="+StartingOwnerMask+" minOwnerInterval="+MinimumOwnerInterval.ToString("R")+" actualSynchronousVendorMs="+MeasuredCutMilliseconds.ToString("R")+" maxSerialPassVendorMs="+MaximumSerialPassMilliseconds.ToString("R")+"\n");
  var counts=(IReadOnlyList<int>)Get(match,"AcceptedCuts");
  for(int owner=0;owner<4;owner++){text.Append("owner=").Append(owner).Append(" authoritativeAcceptedCuts=").Append(counts[owner]).Append(" cutsByToolScannerExcavatorDrillBlowerBrushLift=");for(int tool=0;tool<6;tool++)text.Append(tool==0?"":",").Append(Cuts[owner,tool]);text.Append(" requestedSphereVolume=").Append(RequestedSphereVolume[owner].ToString("R")).Append(" opacityWeightedRequestedVolume=").Append(OpacityWeightedSphereVolume[owner].ToString("R")).AppendLine(" [overlapping requested brush envelopes; not net removed terrain volume]");text.AppendLine((string)match.GetType().GetMethod("AiWellTelemetry").Invoke(match,new object[]{owner}));}return text.ToString();
 }
 void OnDestroy(){if(match!=null&&acceptedEvent!=null&&acceptedHandler!=null)acceptedEvent.RemoveEventHandler(match,acceptedHandler);initialized=false;}
}
#endif
