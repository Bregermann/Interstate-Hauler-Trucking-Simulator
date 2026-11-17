using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class HaulingPhysicsTests
    {
        GameObject root;
        static Type Runtime(string name) { foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("LWS.TruckExtremeHauling." + name); if (t != null) return t; } throw new InvalidOperationException(name); }
        static void Set(Component c, string field, object value) => c.GetType().GetField(field).SetValue(c, value);
        static object Call(Component c, string method, params object[] args) => c.GetType().GetMethod(method).Invoke(c, args);
        [TearDown] public void Cleanup() { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        [UnityTest] public IEnumerator OrbitalThrustChangesActualVelocityWithoutGravity()
        {
            root = new GameObject("Orbital physics test"); var body = root.AddComponent<Rigidbody>(); body.useGravity = false; body.mass = 9000;
            var thrusters = root.AddComponent(Runtime("OrbitalFreightThrusters")); Set(thrusters,"body",body);
            Call(thrusters,"Apply",Vector3.forward,Vector3.zero,false,Time.fixedDeltaTime); yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.z, Is.GreaterThan(.01f)); Assert.That(Mathf.Abs(body.linearVelocity.y), Is.LessThan(.01f));
            float fuel = (float)thrusters.GetType().GetField("fuel").GetValue(thrusters); Assert.That(fuel, Is.LessThan(100));
        }
        [UnityTest] public IEnumerator OrbitalBrakingRemovesRealMomentum()
        {
            root = new GameObject("Orbital brake test"); var body = root.AddComponent<Rigidbody>(); body.useGravity = false; body.linearVelocity = Vector3.forward * 10;
            var thrusters = root.AddComponent(Runtime("OrbitalFreightThrusters")); Set(thrusters,"body",body);
            Call(thrusters,"Apply",Vector3.zero,Vector3.zero,true,Time.fixedDeltaTime); yield return new WaitForFixedUpdate(); Assert.That(body.linearVelocity.z, Is.LessThan(10));
        }
        [Test] public void RingRequiresAnActualPlaneCrossingWithinClearance()
        {
            root = new GameObject("Ring test"); var ring = root.AddComponent(Runtime("HaulingTargetRing")); Set(ring,"radius",5f);
            Assert.That((bool)Call(ring,"TryPass",new Vector3(0,0,-2),new Vector3(0,0,2),1f),Is.True);
            Assert.That((bool)Call(ring,"TryPass",new Vector3(6,0,-2),new Vector3(6,0,2),1f),Is.False);
            Assert.That((bool)Call(ring,"TryPass",new Vector3(0,0,-2),new Vector3(0,0,-1),1f),Is.False);
        }
        [UnityTest] public IEnumerator PhysicalLoadBreaksThinIceAndRemovesSupport()
        {
            root = GameObject.CreatePrimitive(PrimitiveType.Cube); root.transform.localScale = new Vector3(10,.4f,10);
            var sheet = root.AddComponent(Runtime("LoadSensitiveIce")); Set(sheet,"bearingMass",100f); Set(sheet,"thickness",.1f); Set(sheet,"damageRate",1f);
            var cargo = new GameObject("Supported actual cargo"); cargo.transform.SetParent(root.transform); cargo.transform.localScale = Vector3.one; cargo.transform.position = Vector3.up;
            var body = cargo.AddComponent<Rigidbody>(); body.mass = 10000; body.useGravity = false;
            Call(sheet,"RegisterSupportedLoad",body,1f); Call(sheet,"Simulate",.1f); yield return null;
            Assert.That((bool)sheet.GetType().GetProperty("Broken").GetValue(sheet),Is.True); Assert.That(root.GetComponent<Collider>().enabled,Is.False);
            Call(sheet,"ResetIce"); Assert.That(root.GetComponent<Collider>().enabled,Is.True);
        }
        [Test] public void StrongIceDoesNotFailUnderNoLoad()
        {
            root = GameObject.CreatePrimitive(PrimitiveType.Cube); var sheet = root.AddComponent(Runtime("LoadSensitiveIce"));
            for(int i=0;i<300;i++) Call(sheet,"Simulate",.02f);
            Assert.That((bool)sheet.GetType().GetProperty("Broken").GetValue(sheet),Is.False);
        }
    }
}
