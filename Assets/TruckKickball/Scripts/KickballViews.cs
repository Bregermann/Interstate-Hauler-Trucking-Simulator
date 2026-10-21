using UnityEngine;
using UnityEngine.Rendering;
using LWS.TruckSports;

namespace LWS.TruckKickball
{
    [DefaultExecutionOrder(300)]
    public sealed class KickballViews : MonoBehaviour
    {
        public KickballMatch match;
        public SportsRallyViews rally;
        public Transform[] cabs;
        public SportsCabLook[] looks;
        readonly Quaternion[] headings = new Quaternion[4];
        Renderer[][] exteriors;
        Renderer[] hidden;
        bool[] prior;
        Camera hiddenFor;
        int lastMode = -1;

        void Awake()
        {
            exteriors = new Renderer[match.agents.Length][];
            int maximum = 0;
            for (int i = 0; i < exteriors.Length; i++)
            {
                exteriors[i] = match.agents[i].truck.visuals.GetComponentsInChildren<Renderer>(true);
                maximum = Mathf.Max(maximum, exteriors[i].Length);
            }
            prior = new bool[maximum];
        }
        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            Restore();
        }
        void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (match.ViewMode != 2 || match.ReplayControls) return;
            for (int v = 0; v < rally.Humans; v++)
            {
                if (rally.views[v].view != camera) continue;
                Restore(); hidden = exteriors[rally.owners[v]]; hiddenFor = camera;
                for (int i = 0; i < hidden.Length; i++)
                { prior[i] = hidden[i].forceRenderingOff; hidden[i].forceRenderingOff = true; }
                break;
            }
        }
        void EndCamera(ScriptableRenderContext context, Camera camera) { if (camera == hiddenFor) Restore(); }
        public void Restore()
        {
            if (hidden != null) for (int i = 0; i < hidden.Length; i++) if (hidden[i] != null) hidden[i].forceRenderingOff = prior[i];
            hidden = null; hiddenFor = null;
        }
        public void Layout()
        {
            Restore(); lastMode = match.ViewMode; rally.Restore();
            if (lastMode == 0)
            {
                foreach (var v in rally.views) v.view.enabled = false;
                rally.broadcast.view.enabled = true; rally.broadcast.view.rect = new Rect(0, 0, 1, 1);
                match.cameras = new[] { rally.broadcast.view };
            }
            for (int v = 0; v < cabs.Length; v++)
            {
                cabs[v].gameObject.SetActive(lastMode == 2 && rally.views[v].view.enabled);
                headings[v] = Quaternion.identity; looks[v].ResetLook();
            }
            if (match.atmosphere != null)
            {
                match.atmosphere.vendor.AllowCameras.Clear();
                foreach (var camera in match.cameras) match.atmosphere.vendor.AllowCameras.Add(camera);
            }
        }
        public void Tick(float dt)
        {
            if (match.ReplayControls)
            {
                foreach (var cab in cabs) cab.gameObject.SetActive(false);
                Vector3 focus = match.replay.Position(12);
                if (focus.y < -3) focus = match.replay.Position(match.rules.Batter);
                rally.ReplayView(focus, match.replay.Angle); return;
            }
            if (lastMode != match.ViewMode || rally.Replay) Layout();
            if (lastMode == 0 || rally.Humans == 0)
            {
                // Whole legal diamond/fence remains visible for spectators; no
                // empty screen in an all-AI match when AI viewports are disabled.
                var camera = rally.broadcast.view;
                // Weather Maker's volumetric sky supports perspective cameras.
                // Switching this already-rendered camera to orthographic leaves
                // its perspective command buffer covering the stadium. Keep the
                // same projection through setup, broadcast, replay and rematches.
                if(rally.broadcast.broadcastFraming!=null&&rally.broadcast.broadcastFraming.enabled)rally.broadcast.Pose(match.ball.body.position,new Vector3(32,36,-38),58);else FrameStadium(camera);
                return;
            }
            foreach (var v in rally.views) v.view.orthographic = false;
            rally.Follow(dt);
            if (lastMode != 2) return;
            for (int v = 0; v < rally.Humans; v++)
            {
                int slot = rally.owners[v]; var truck = match.agents[slot].truck;
                var forward = Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up);
                if (forward.sqrMagnitude > .01f && Vector3.Dot(truck.transform.up, Vector3.up) > .2f)
                    headings[v] = Quaternion.Slerp(headings[v], Quaternion.LookRotation(forward), 1 - Mathf.Exp(-dt * 9));
                var camera = rally.views[v].view;
                camera.transform.position = truck.body.position + headings[v] * new Vector3(-.17f, 1.3f, .65f);
                camera.transform.rotation = looks[v].Apply(headings[v], match.setup.players[slot], slot, v == 0, match.Live, SportsLookOptions.Sensitivity, SportsLookOptions.InvertY);
                camera.fieldOfView = 78; camera.nearClipPlane = .025f;
                rally.views[v].cabSteering.Bind(truck);
            }
        }

        public static void FrameStadium(Camera camera)
        {
            camera.orthographic = false;
            camera.fieldOfView = 58;
            var rotation = Quaternion.Euler(70, 0, 0);
            var center = new Vector3(0, 3, 27);
            var inverse = Quaternion.Inverse(rotation);
            float vertical = Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
            float horizontal = vertical * Mathf.Max(.2f, camera.aspect);
            float distance = 0;
            // The real fence is radius 78m around (0,0,27). Include its height,
            // both dugouts and a margin below the compact top/bottom HUD rails.
            for (int i = 0; i < 112; i++)
            {
                float angle = (i / 2) * Mathf.PI * 2 / 56;
                var corner = new Vector3(Mathf.Sin(angle) * 82, (i & 1) == 0 ? -3 : 7,
                    Mathf.Cos(angle) * 82);
                var local = inverse * corner;
                distance = Mathf.Max(distance, Mathf.Max(Mathf.Abs(local.x) / (horizontal * .94f) - local.z,
                    Mathf.Abs(local.y) / (vertical * .86f) - local.z));
            }
            camera.transform.SetPositionAndRotation(center - rotation * Vector3.forward * distance, rotation);
        }
    }
}
