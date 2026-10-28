#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
public sealed class WildernessKeyboardFeatheringTests
{
    [Test]public void RealKeyDutyCycleRetainsRequestedAnalogSteeringAndPedalMeansInsteadOfSaturatingEverySmallRequest()
    {
        foreach(var requested in new[]{new Vector2(.21f,.18f),new Vector2(-.53f,-.12f),new Vector2(1,.7f),new Vector2(-1,-.6f),Vector2.zero})
        {
            var mean=Vector2.zero;const int samples=10000;
            for(int i=0;i<samples;i++)
            {
                var pulse=WildernessDriveObservation.KeyboardPulse(requested,(i+.5f)/samples);mean+=pulse/samples;
                Assert.That(pulse.x==0||Mathf.Abs(pulse.x)==1,Is.True);Assert.That(pulse.y==0||pulse.y==.7f||pulse.y==-.65f,Is.True,"Only actual digital keys, never direct analog command injection.");
            }
            Assert.That(mean.x,Is.EqualTo(requested.x).Within(.0002f));Assert.That(mean.y,Is.EqualTo(requested.y).Within(.0002f));
        }
    }
}
#endif
