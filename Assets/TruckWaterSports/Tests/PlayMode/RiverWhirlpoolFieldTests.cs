using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckWaterSports.Tests
{
    // Pure field invariants, not a claim of completed gameplay or rendered VFX.
    // Natural Slalom/Survival acceptance must exercise the existing course-force hook.
    public sealed class RiverWhirlpoolFieldTests
    {
        static Type Runtime(string name)=>Type.GetType("LWS.TruckWaterSports."+name+", Assembly-CSharp",true);
        static object Vortex()=>Activator.CreateInstance(Runtime("RiverWhirlpool"));
        static Vector3 Sample(object vortex,Vector3 point)=>(Vector3)Runtime("RiverWhirlpoolCurrentField").GetMethod("Sample").Invoke(null,new[]{vortex,(object)Vector3.zero,point,0f,0f});

        [Test]public void WhirlpoolIsBoundedToAuthoredWaterRadiusAndDepth()
        {
            var vortex=Vortex();
            Assert.That(Sample(vortex,new Vector3(8,0,0)),Is.EqualTo(Vector3.zero));
            Assert.That(Sample(vortex,new Vector3(2,1.1f,0)),Is.EqualTo(Vector3.zero));
            Assert.That(Sample(vortex,new Vector3(2,-4.1f,0)),Is.EqualTo(Vector3.zero));
        }
        [Test]public void CoreDrawsDownWithoutSingularHorizontalAcceleration()
        {
            Vector3 core=Sample(Vortex(),Vector3.zero);
            Assert.That(core.x,Is.Zero);Assert.That(core.z,Is.Zero);
            Assert.That(core.y,Is.LessThan(0).And.GreaterThan(-1));
            Assert.That(float.IsFinite(core.sqrMagnitude),Is.True);
        }
        [Test]public void ClockwiseSwitchReversesSwirlWithoutChangingInwardDraw()
        {
            var vortex=Vortex();Vector3 a=Sample(vortex,Vector3.right*3);
            vortex.GetType().GetField("clockwise").SetValue(vortex,true);Vector3 b=Sample(vortex,Vector3.right*3);
            Assert.That(a.x,Is.LessThan(0));Assert.That(b.x,Is.EqualTo(a.x).Within(.0001f));
            Assert.That(a.z,Is.Not.Zero);Assert.That(b.z,Is.EqualTo(-a.z).Within(.0001f));
        }
        [Test]public void CurrentBlendsOutAtTheRim()
        {
            var vortex=Vortex();
            Assert.That(Sample(vortex,Vector3.right*7.499f).magnitude,Is.LessThan(.001f));
            Assert.That(Sample(vortex,Vector3.right*4).magnitude,Is.GreaterThan(.1f));
        }
    }
}
