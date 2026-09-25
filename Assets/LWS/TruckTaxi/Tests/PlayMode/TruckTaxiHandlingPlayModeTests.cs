using System.Collections;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiHandlingPlayModeTests
    {
        private Keyboard keyboard;
        private InputSettings original;
        private GameObject ground,stop;
        private float originalCaptureDelta;
        [SetUp] public void Setup() { originalCaptureDelta=Time.captureDeltaTime; Time.captureDeltaTime=1f/60f; }
        [TearDown] public void Cleanup()
        {
            if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
            if(original!=null) { var temporary=InputSystem.settings; InputSystem.settings=original; Object.Destroy(temporary); }
            if(ground!=null) Object.Destroy(ground); if(stop!=null) Object.Destroy(stop);
            Time.timeScale=1;
            Time.captureDeltaTime=originalCaptureDelta;
        }
        [UnityTest, Timeout(600000)]
        public IEnumerator LowSpeedUTurnUsesNwhAndOverrideRestores()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+45;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            var host=TruckTaxiBootstrap.Instance; Assert.IsTrue(host.Ready);
            var handling=host.Handling; var vehicle=host.Player.GetComponent<VehicleController>();
            Assert.IsTrue(handling.Applied); Assert.AreEqual(65,vehicle.steering.maximumSteerAngle);
            handling.Restore(); float interstate=vehicle.steering.maximumSteerAngle;
            Assert.Less(interstate,65); handling.Apply();
            Assert.That(vehicle.steering.speedSensitiveSteeringCurve.Evaluate(70/111.8468f)*65,Is.EqualTo(8).Within(.1));
            ground=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name="Temporary handling test ground"; ground.transform.position=new Vector3(1000,-.5f,1000); ground.transform.localScale=new Vector3(150,1,150);
            stop=new GameObject("Temporary handling start"); stop.transform.position=new Vector3(1000,.15f,1000);
            var location=stop.AddComponent<TruckTaxiRideLocation>();
            host.StartShift(); host.TeleportNear(location);
            original=InputSystem.settings; InputSystem.settings=Object.Instantiate(original);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>("Taxi steering test keyboard");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E)); yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return new WaitForSeconds(2);
            var body=host.Player.GetComponent<Rigidbody>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            float simulationDeadline=Time.time+12;
            deadline=Time.realtimeSinceStartup+120;
            while(body.linearVelocity.magnitude<2.5f && Time.time<simulationDeadline && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.Greater(body.linearVelocity.magnitude,2,"NWH failed to accelerate through normal input.");
            Vector3 initialForward=host.Player.transform.forward;
            var bounds=new Bounds(body.position,Vector3.zero);
            float previousYaw=body.rotation.eulerAngles.y, turn=0, minimumUp=1, peakSpeed=0;
            simulationDeadline=Time.time+25;
            deadline=Time.realtimeSinceStartup+240;
            while(turn<175 && Time.time<simulationDeadline && Time.realtimeSinceStartup<deadline)
            {
                // Coasting alone retains engine torque and overshoots the low-speed fixture.
                // Use the existing simultaneous W/S service-brake command, never edit velocity.
                float speed=body.linearVelocity.magnitude;
                InputSystem.QueueStateEvent(keyboard,speed<3.0f ? new KeyboardState(Key.W,Key.D) :
                    speed>3.5f ? new KeyboardState(Key.W,Key.S,Key.D) : new KeyboardState(Key.D));
                yield return null;
                float yaw=body.rotation.eulerAngles.y;
                turn+=Mathf.Abs(Mathf.DeltaAngle(previousYaw,yaw)); previousYaw=yaw;
                bounds.Encapsulate(body.position); peakSpeed=Mathf.Max(peakSpeed,body.linearVelocity.magnitude*2.236936f);
                minimumUp=Mathf.Min(minimumUp,Vector3.Dot(host.Player.transform.up,Vector3.up));
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            float width=bounds.size.x+2.2f;
            Debug.Log($"TAXI U-TURN: {turn:0} degrees; swept tractor width ~{width:0.00}m; peak {peakSpeed:0.0} MPH; minimum upright dot {minimumUp:0.00}; original lock {interstate:0}; taxi lock {vehicle.steering.maximumSteerAngle:0}.");
            Assert.Greater(turn,174); Assert.Less(Vector3.Dot(initialForward,host.Player.transform.forward),-.8f);
            Assert.Less(peakSpeed,9f,"Test driver failed to hold the controlled low-speed band.");
            Assert.Less(width,11,"Low-speed U-turn wider than the target corridor.");
            Assert.Greater(minimumUp,.7f,"Taxi steering caused excessive roll.");
            handling.Restore(); Assert.AreEqual(interstate,vehicle.steering.maximumSteerAngle);
        }
    }
}
