using NWH.Common.Cameras;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DefaultExecutionOrder(1000),DisallowMultipleComponent]
    public sealed class TruckTaxiCabLook : MonoBehaviour
    {
        public const float MinYaw=-120f,MaxYaw=120f,MinPitch=-45f,MaxPitch=60f;
        public TruckTaxiCabLookSettings Settings { get; private set; }
        public float Yaw => yaw;
        public float Pitch => pitch;
        private TruckTaxiBootstrap host;
        private TruckTaxiUIInput input;
        private CameraChanger changer;
        private LwsWheelInputSource wheel;
        private CameraMouseDrag vendorCamera;
        private bool previousAllowRotation;
        private float yaw,pitch,shownYaw,shownPitch,yawVelocity,pitchVelocity;

        public void Initialize(TruckTaxiBootstrap owner,TruckTaxiUIInput actions)
        {
            host=owner; input=actions;
            Settings=TruckTaxiCabLookSettings.Load();
            changer=owner?.Player?.GetComponentInChildren<CameraChanger>(true);
            wheel=owner?.Player?.GetComponentInChildren<LwsWheelInputSource>(true);
        }
        public void CenterView() { yaw=pitch=shownYaw=shownPitch=yawVelocity=pitchVelocity=0f; }
        public static Vector2 ClampAngles(Vector2 angles) => new Vector2(Mathf.Clamp(angles.x,MinYaw,MaxYaw),Mathf.Clamp(angles.y,MinPitch,MaxPitch));
        private void LateUpdate()
        {
            if(changer==null || input==null || host==null) return;
            GameObject current=changer.cameras!=null && changer.currentCameraIndex>=0 && changer.currentCameraIndex<changer.cameras.Count
                ? changer.cameras[changer.currentCameraIndex] : null;
            var cab=current!=null && current.activeInHierarchy && current.GetComponent<CameraInsideVehicle>()?.isInsideVehicle==true
                ? current.GetComponent<CameraMouseDrag>() : null;
            if(cab!=vendorCamera) { RestoreVendor(); vendorCamera=cab; if(cab!=null) { previousAllowRotation=cab.allowRotation; cab.allowRotation=false; CenterView(); } }
            if(vendorCamera==null) return;
            if(input.CenterView.WasPressedThisFrame() || wheel!=null && wheel.LastFrame.commands.lookReset==LwsMomentaryIntent.Pressed) CenterView();
            bool canLook=!host.Paused && host.Ready && input.FocusRoot==null;
            if(canLook)
            {
                Vector2 mouse=input.CabMouseLookHold.IsPressed() ? input.CabMouseLook.ReadValue<Vector2>()*Settings.MouseSensitivity : Vector2.zero;
                Vector2 stick=input.CabGamepadLook.ReadValue<Vector2>()*Settings.GamepadSensitivity*Time.unscaledDeltaTime;
                Vector2 delta=mouse+stick;
                yaw=Mathf.Clamp(yaw+delta.x,MinYaw,MaxYaw);
                pitch=Mathf.Clamp(pitch+(Settings.InvertY?delta.y:-delta.y),MinPitch,MaxPitch);
            }
            float smooth=Settings.Smoothing;
            shownYaw=Mathf.SmoothDampAngle(shownYaw,yaw,ref yawVelocity,smooth);
            shownPitch=Mathf.SmoothDampAngle(shownPitch,pitch,ref pitchVelocity,smooth);
            vendorCamera.transform.rotation*=Quaternion.Euler(shownPitch,shownYaw,0f);
        }
        private void RestoreVendor()
        {
            if(vendorCamera!=null) vendorCamera.allowRotation=previousAllowRotation;
            vendorCamera=null;
        }
        private void OnDisable() => RestoreVendor();
        private void OnDestroy() => RestoreVendor();
    }
}
