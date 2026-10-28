#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class ArchaeologyExcavationDiagnostics
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
 static object Call(object x,string n,params object[] a)=>x.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length&&m.GetParameters().Select((p,i)=>a[i]==null||(p.ParameterType.IsByRef?p.ParameterType.GetElementType():p.ParameterType).IsInstanceOfType(a[i])).All(v=>v)).Invoke(x,a);
 static string V(Vector3 value)=>value.ToString("F6");
 static string ObjectPath(Transform t)=>t==null?"none":t.parent==null?t.name:ObjectPath(t.parent)+"/"+t.name;
 static bool Surface(object earth,Vector3 point,out RaycastHit hit)
 {
  object[] args={point,default(RaycastHit)};bool found=(bool)earth.GetType().GetMethod("Surface").Invoke(earth,args);hit=(RaycastHit)args[1];return found;
 }
 static string Snapshot(Behaviour match)
 {
  object earth=Get(match,"earth"),rules=Get(match,"Rules");var targets=(int[])Get(match,"Targets");var tools=(Array)Get(match,"Tools");var artifacts=((Array)Get(match,"artifacts")).Cast<Component>().ToArray();var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();var equipment=((Array)Get(match,"equipment")).Cast<Component>().ToArray();var stages=(Array)Get(rules,"Stages");var preservation=(double[])Get(rules,"Preservation");
  var sb=new StringBuilder(18000);sb.Append("ARCHAEOLOGY_EXCAVATION_STATE clock=").Append(Get(match,"Clock")).Append(" real=").Append(Time.realtimeSinceStartup).Append(" frame=").Append(Time.frameCount).Append(" ready=").Append(Get(earth,"Ready")).Append(" quiescent=").Append(Get(earth,"Quiescent")).Append(" edits=").Append(Get(earth,"ActualEdits")).Append(" strokes=").Append(Get(match,"ToolStrokes")).Append(" targets=").Append(string.Join(",",targets)).Append(" stages=").Append(string.Join(",",stages.Cast<object>())).Append(" scores=").Append(string.Join(",",(int[])Get(rules,"Scores"))).Append(" loads=").Append(Get(match,"PhysicalLoads")).Append(" unloads=").Append(Get(match,"PhysicalUnloads")).AppendLine();
  for(int owner=0;owner<4;owner++)
  {
   var truck=(Rigidbody)Get(trucks[owner],"body");var head=(Rigidbody)Get(equipment[owner],"head");object hoist=Get(equipment[owner],"hoist");sb.Append("ARCHAEOLOGY_OWNER owner=").Append(owner).Append(" root=").Append(V(truck.position)).Append(" rootV=").Append(V(truck.linearVelocity)).Append(" forward=").Append(V(truck.transform.forward)).Append(" tool=").Append(tools.GetValue(owner)).Append(" head=").Append(V(head.position)).Append(" aim=").Append(V((Vector3)Get(equipment[owner],"Aim"))).Append(" atWork=").Append(Get(equipment[owner],"AtWork")).Append(" crane=").Append(Get(hoist,"Stage")).Append(" released=").Append(Get(hoist,"Released")).Append(" target=").Append(targets[owner]).AppendLine();
   int id=targets[owner];if(id<0)continue;var artifact=artifacts[id];var shape=(BoxCollider)Get(artifact,"shape");var body=(Rigidbody)Get(artifact,"body");Bounds b=shape.bounds;Vector3 cut=(Vector3)Call(match,"CutPoint",artifact);float gap=cut.y-b.max.y;float radius=tools.GetValue(owner).ToString()=="Excavator"?2.4f:tools.GetValue(owner).ToString()=="Drill"?1.25f:tools.GetValue(owner).ToString()=="Blower"?1.05f:.6f;
   sb.Append("ARCHAEOLOGY_TARGET owner=").Append(owner).Append(" id=").Append(id).Append(" stage=").Append(stages.GetValue(id)).Append(" actualExposure=").Append(Get(artifact,"Exposure")).Append(" clearance=").Append(Get(artifact,"Clearance")).Append(" preservation=").Append(preservation[id].ToString("R")).Append(" body=").Append(V(body.position)).Append(" bodyV=").Append(V(body.linearVelocity)).Append(" kinematic=").Append(body.isKinematic).Append(" boundsMin=").Append(V(b.min)).Append(" boundsMax=").Append(V(b.max)).Append(" cutPoint=").Append(V(cut)).Append(" gap=").Append(gap.ToString("R")).Append(" radius=").Append(radius).Append(" cutReach=").Append(Vector3.Distance(truck.position,cut).ToString("R")).Append(" headCutDistance=").Append(Vector3.Distance(head.position,cut).ToString("R")).Append(" shapeEnabled=").Append(shape.enabled).AppendLine();
   int visible=0,missing=0;
   for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
   {
    Vector3 point=b.center+new Vector3(x*(b.extents.x+.25f),0,z*(b.extents.z+.25f));bool found=Surface(earth,point,out RaycastHit hit);if(!found)missing++;bool clear=found&&hit.point.y<b.min.y+.12f;if(clear)visible++;
    sb.Append("ARCHAEOLOGY_NINE_RAY owner=").Append(owner).Append(" id=").Append(id).Append(" x=").Append(x).Append(" z=").Append(z).Append(" sample=").Append(V(point)).Append(" found=").Append(found).Append(" lowerThanBottom=").Append(clear).Append(" threshold=").Append((b.min.y+.12f).ToString("R"));
    if(found)sb.Append(" hit=").Append(V(hit.point)).Append(" normal=").Append(V(hit.normal)).Append(" collider=").Append(ObjectPath(hit.collider.transform)).Append(" kind=").Append(hit.collider.GetType().Name);
    sb.AppendLine();
   }
   var overlaps=Physics.OverlapBox(b.center,b.extents+Vector3.one*.08f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);int terrainOverlap=0,penetrating=0;float deepest=0;
   foreach(var other in overlaps)if((bool)Call(earth,"IsTerrain",other))
   {
    terrainOverlap++;bool overlap=Physics.ComputePenetration(shape,shape.transform.position,shape.transform.rotation,other,other.transform.position,other.transform.rotation,out Vector3 direction,out float depth);if(overlap&&depth>.015f)penetrating++;deepest=Mathf.Max(deepest,depth);sb.Append("ARCHAEOLOGY_TERRAIN_OVERLAP owner=").Append(owner).Append(" id=").Append(id).Append(" collider=").Append(ObjectPath(other.transform)).Append(" penetrating=").Append(overlap).Append(" depth=").Append(depth.ToString("R")).Append(" normal=").Append(V(direction)).AppendLine();
   }
   sb.Append("ARCHAEOLOGY_CLEARANCE_PREDICATE owner=").Append(owner).Append(" id=").Append(id).Append(" visible=").Append(visible).Append(" missing=").Append(missing).Append(" terrainOverlaps=").Append(terrainOverlap).Append(" penetrating=").Append(penetrating).Append(" deepest=").Append(deepest.ToString("R")).AppendLine();
  }
  return sb.ToString();
 }
 [UnityTest,Timeout(105000)]public IEnumerator OriginalFourAiExcavationReportsRealNineSampleClearanceWithoutOverrides()
 {
  // Reuse exact original prefs, input isolation, real scene load, AllBots and
  // actual StartMatch. No readiness wait, manual cut, pose, tool or state write.
  var type=typeof(ArchaeologyPlayModeTests).GetNestedType("Fixture",BindingFlags.NonPublic);var fixture=(IDisposable)Activator.CreateInstance(type,true);
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ArchaeologyExcavationDiagnosticNative/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"actual-original-ai-clearance.txt");
  using(fixture)
  {
   yield return (IEnumerator)Call(fixture,"Load");var match=(Behaviour)Get(fixture,"match");float end=Time.realtimeSinceStartup+80,next=0;
   while((float)Get(match,"Clock")<65&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<end)
   {
    if((float)Get(match,"Clock")>=next){next=(float)Get(match,"Clock")+2;string state=Snapshot(match);File.AppendAllText(path,state);Debug.Log(state);}yield return null;
   }
   string final=Snapshot(match);File.AppendAllText(path,final);Debug.Log(final);Assert.That((float)Get(match,"Clock"),Is.GreaterThan(20),"Actual original simulation must advance for this bounded measurement.");Assert.That((int)Get(Get(match,"earth"),"ActualEdits"),Is.GreaterThan(12),"Original AI must issue actual Digger edits; this diagnostic cannot substitute for full completion.");TestContext.WriteLine("ARCHAEOLOGY_EXCAVATION_READ_ONLY "+path+"; original600s/full12-delivery acceptance unchanged and remains failed until rerun passes.");
  }
 }
}
#endif
