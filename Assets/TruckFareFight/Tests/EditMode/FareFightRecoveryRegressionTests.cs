using System;
using NUnit.Framework;
using UnityEngine;

public sealed class FareFightRecoveryRegressionTests
{
    [Test]public void MovingCarrierDoesNotReverseTheSteeringOfAnInFlightBackingManeuver()
    {
        var type=Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp",true);var route=Activator.CreateInstance(type);var root=new GameObject("Moving goal recovery fixture");var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.transform.position=new Vector3(0,1.5f,4);wall.transform.localScale=new Vector3(20,4,1);Physics.SyncTransforms();
            var body=root.AddComponent<Rigidbody>();body.useGravity=false;
            type.GetField("useGoalProgressRecovery").SetValue(route,true);type.GetField("stallSeconds").SetValue(route,.5f);
            type.GetMethod("DirectGoal").Invoke(route,new object[]{Vector3.zero,new Vector3(10,0,50)});
            Vector2 initial=default;for(int step=0;step<60;step++)initial=(Vector2)type.GetMethod("Drive").Invoke(route,new object[]{body,root.transform,1,.02f,1f,null,null});
            Assert.That(initial.y,Is.LessThan(0));Assert.That(Mathf.Abs(initial.x),Is.GreaterThan(.5f));
            type.GetMethod("DirectGoal").Invoke(route,new object[]{Vector3.zero,new Vector3(-10,0,50)});
            var changed=(Vector2)type.GetMethod("Drive").Invoke(route,new object[]{body,root.transform,1,.02f,1f,null,null});
            Assert.That(changed.x,Is.EqualTo(initial.x),"A moving target must not flip the recovery steering while the same reverse maneuver is still active.");
        }
        finally{UnityEngine.Object.DestroyImmediate(wall);UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test]public void ActualForwardProgressDoesNotTriggerRecoveryAndResetClearsIt()
    {
        var type=Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp",true);var route=Activator.CreateInstance(type);var root=new GameObject("Forward route progress fixture");
        try
        {
            var body=root.AddComponent<Rigidbody>();body.useGravity=false;body.linearVelocity=Vector3.forward;
            type.GetField("useGoalProgressRecovery").SetValue(route,true);type.GetField("stallSeconds").SetValue(route,1.5f);type.GetMethod("DirectGoal").Invoke(route,new object[]{Vector3.zero,Vector3.forward*50});
            Vector2 command=default;for(int step=0;step<100;step++){body.position=Vector3.forward*(step*.02f);command=(Vector2)type.GetMethod("Drive").Invoke(route,new object[]{body,root.transform,1,.02f,1f,null,null});}
            Assert.That(command.y,Is.GreaterThanOrEqualTo(0));Assert.That((int)type.GetProperty("RecoveryStarts").GetValue(route),Is.Zero);
            type.GetMethod("Reset").Invoke(route,null);Assert.That((bool)type.GetProperty("Reversing").GetValue(route),Is.False);Assert.That((float)type.GetProperty("SecondsWithoutProgress").GetValue(route),Is.Zero);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test]public void SidewaysWallMotionDoesNotMasqueradeAsObjectiveProgress()
    {
        var type=Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp",true);
        var route=Activator.CreateInstance(type);var root=new GameObject("Isolated route recovery fixture");
        try
        {
            var body=root.AddComponent<Rigidbody>();body.useGravity=false;body.linearVelocity=Vector3.right;
            // Opt into this sport's recovery policy when available. The original
            // driver fails the behavior assertion even though total speed is1m/s.
            type.GetField("useGoalProgressRecovery")?.SetValue(route,true);
            type.GetField("stallSeconds")?.SetValue(route,1.5f);
            type.GetMethod("DirectGoal").Invoke(route,new object[]{Vector3.zero,Vector3.forward*50});
            Vector2 command=default;
            for(int step=0;step<100;step++)command=(Vector2)type.GetMethod("Drive").Invoke(route,new object[]{body,root.transform,1,.02f,1f,null,null});
            Assert.That(command.y,Is.LessThan(-.1f),"Motion along a wall without progress toward the route must trigger a bounded reverse recovery.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
