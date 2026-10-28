#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.AI;
using static WildernessNativeScope;
// Test-owned real-keyboard feathering plus read-only physical telemetry.
// Converts requested analog steering/pedals to actual key pulses; never writes
// input commands directly to the truck, or changes poses, routes or outcomes.
public static class WildernessDriveObservation
{
    [Serializable]sealed class Contact{public string name,path;public float distance,mass;public Vector3 point;}
    [Serializable]sealed class Wheel{public string name,hit;public bool grounded;public float load,spring;}
    [Serializable]sealed class Row{public string status="OBSERVED_ORIGINAL_KEYBOARD_DRIVE_NOT_COMPLETION",stage,screen,pathStatus;public float clock,real,speed,remaining;public bool live,ready,paused;public Vector3 position,euler,velocity,destination;public Vector2 command,requestedAnalog,keyPulse;public float pwmPeriod=.2f;public Vector3[] corners;public Contact[] frontSweep;public Wheel[] wheels;}
    [Serializable]sealed class PathReceipt{public string version,output,parent,assembly,moduleId,assemblySha256,pdbSha256;public int characters;public bool parentExists;}
    static string Digest(string path){if(!File.Exists(path))return "MISSING";using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static string Name(Transform t){string result=t.name;while(t.parent!=null){t=t.parent;result=t.name+"/"+result;}return result;}
    public static Vector2 KeyboardPulse(Vector2 requested,float normalizedPhase)
    {
        float phase=Mathf.Repeat(normalizedPhase,1);
        float steering=phase<Mathf.Clamp01(Mathf.Abs(requested.x))?Mathf.Sign(requested.x):0;
        float maximum=requested.y>=0?.7f:.65f;
        float pedal=phase<Mathf.Clamp01(Mathf.Abs(requested.y)/maximum)?Mathf.Sign(requested.y)*maximum:0;
        return new Vector2(steering,pedal);
    }
    static IEnumerator DriveFeatheredKeyboard(WildernessNativeScope scope,Component explorer,Vector3 point,float realLimit,Action<Vector2,Vector2> observed)
    {
        var body=(Rigidbody)Get(Get(explorer,"truck"),"body");var player=((Array)Get(Get(scope.match,"setup"),"players")).GetValue(0);float end=Time.realtimeSinceStartup+realLimit;
        while(Time.realtimeSinceStartup<end)
        {
            if(Vector3.ProjectOnPlane(body.position-point,Vector3.up).magnitude<7.5f&&body.linearVelocity.magnitude<1.2f)break;
            var requested=(Vector2)Call(explorer,"Navigate",point);var pulse=KeyboardPulse(requested,Time.time/.2f);observed(requested,pulse);
            var keys=new System.Collections.Generic.List<Key>();if(pulse.x>0)keys.Add((Key)Get(player,"right"));else if(pulse.x<0)keys.Add((Key)Get(player,"left"));if(pulse.y>0)keys.Add((Key)Get(player,"forward"));else if(pulse.y<0)keys.Add((Key)Get(player,"back"));scope.Keys(keys.ToArray());yield return null;
        }
        scope.Keys();scope.Trace("machine-keyboard-feathered-drive-to");Assert.That(Vector3.ProjectOnPlane(body.position-point,Vector3.up).magnitude,Is.LessThan(9),"Actual NWH keyboard driving must reach the real activity within its original deadline; no pose/route fallback.");yield return scope.Advance(.8f);
    }
    public static IEnumerator Run(WildernessNativeScope scope,Component explorer,Vector3 destination,float realLimit,string label)
    {
        string id=Guid.NewGuid().ToString("N"),output=Path.Combine(Path.GetDirectoryName(scope.progress),"drive-"+id+"-"+label+".jsonl");var truck=(Component)Get(explorer,"truck");var body=(Rigidbody)Get(truck,"body");float next=0;Vector2 requestedAnalog=Vector2.zero,keyPulse=Vector2.zero;
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var assembly=typeof(WildernessDriveObservation).Assembly;var dll=assembly.Location;var pdb=Path.ChangeExtension(dll,".pdb");
        var receipt=new PathReceipt{version="V11c_SHORT_ABSOLUTE_PATH",output=output,parent=Path.GetDirectoryName(output),characters=output.Length,parentExists=Directory.Exists(Path.GetDirectoryName(output)),assembly=dll,moduleId=assembly.ManifestModule.ModuleVersionId.ToString(),assemblySha256=Digest(dll),pdbSha256=Digest(pdb)};
        Debug.Log("WILDERNESS_DRIVE_PATH "+JsonUtility.ToJson(receipt));
        if(output.Length>240)throw new IOException("Owned drive diagnostic path exceeds its explicit240character budget: "+output.Length);
        File.WriteAllText(Path.ChangeExtension(output,".path.json"),JsonUtility.ToJson(receipt,true));
        using(var geometry=label.IndexOf("hammock",StringComparison.OrdinalIgnoreCase)>=0?new WildernessHammockGeometryObservation.CaptureScope(scope,explorer,destination,Path.Combine(Path.GetDirectoryName(output),"hg-"+id+".jsonl")):null)
        {
        Action<string> record=stage=>
        {
            if(geometry!=null)geometry.Capture(stage);
            var path=(NavMeshPath)Get(explorer,"path");var delta=Vector3.ProjectOnPlane(destination-body.position,Vector3.up);var hits=delta.sqrMagnitude>.001f?body.SweepTestAll(delta.normalized,8,QueryTriggerInteraction.Ignore):Array.Empty<RaycastHit>();
            var row=new Row{requestedAnalog=requestedAnalog,keyPulse=keyPulse,stage=stage,clock=(float)Get(scope.match,"WildernessSeconds"),real=Time.realtimeSinceStartup,screen=Get(Get(scope.match,"menus"),"Screen").ToString(),live=(bool)Get(scope.match,"Live"),paused=(bool)Get(scope.match,"Paused"),ready=(bool)Get(scope.match,"DevicesReady"),position=body.position,euler=body.rotation.eulerAngles,velocity=body.linearVelocity,speed=body.linearVelocity.magnitude,destination=destination,remaining=delta.magnitude,command=(Vector2)Get(truck,"command"),pathStatus=path==null?"NULL":path.status.ToString(),corners=path==null?Array.Empty<Vector3>():path.corners,frontSweep=hits.Where(h=>h.collider!=null&&!h.collider.transform.IsChildOf(truck.transform)).OrderBy(h=>h.distance).Take(8).Select(h=>new Contact{name=h.collider.name,path=Name(h.collider.transform),distance=h.distance,point=h.point,mass=h.rigidbody==null?0:h.rigidbody.mass}).ToArray(),wheels=Components(Get(truck,"wheels")).Select(w=>new Wheel{name=w.name,hit=((Collider)Get(w,"HitCollider"))==null?"NONE":Name(((Collider)Get(w,"HitCollider")).transform),grounded=(bool)Get(w,"IsGrounded"),load=(float)Get(w,"Load"),spring=(float)Get(w,"SpringForce")}).ToArray()};File.AppendAllText(output,JsonUtility.ToJson(row)+"\n");
        };
        record("begin");var original=DriveFeatheredKeyboard(scope,explorer,destination,realLimit,(requested,pulse)=>{requestedAnalog=requested;keyPulse=pulse;});
        while(true)
        {
            if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+.5f;record("ordinary-input-frame");}
            bool more;
            try{more=original.MoveNext();}
            catch
            {
                record("original-drive-assertion-failed");try{scope.Capture(label+"-actual-drive-failure-"+id);}catch(Exception error){Debug.Log("Read-only capture diagnostic unavailable: "+error.Message);}throw;
            }
            if(!more)break;yield return original.Current;
        }
        record("original-drive-complete");
        }
    }
}
#endif
