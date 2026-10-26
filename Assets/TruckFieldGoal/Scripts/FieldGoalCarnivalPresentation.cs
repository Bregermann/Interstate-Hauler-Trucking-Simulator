using LWS.TruckSports;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckFieldGoal
{
    // The original SportsMotionReplay alone samples/plays real poses. This
    // camera renders those existing ghosts; it never steps or reruns physics.
    [DefaultExecutionOrder(450)]
    public sealed class FieldGoalCarnivalPresentation : MonoBehaviour
    {
        public FieldGoalMatch match;
        public SportsEventCamera feedCamera;
        public Canvas videoCanvas;
        public RawImage video;
        public TMP_Text caption;
        public Transform bowl;
        public MeshRenderer[] replacedStraightStands;
        public MeshFilter[] curvedDecks;
        public Transform[] remappedCrowd;
        public Transform supersededAudience;
        [HideInInspector] public string retirementPhysicsBefore, retirementPhysicsAfter;
        public int authoredVersion = 1;
        public float KickTime { get; private set; } = -1;
        public bool FeedVisible => texture != null && feedCamera != null && feedCamera.view.enabled;
        public RenderTexture FeedTexture => texture;
        public int FeedFrames { get; private set; }
        public int RunupFrames { get; private set; }
        public int FlightFrames { get; private set; }
        public float FirstRecordedCursor { get; private set; } = -1;
        public bool BoardShotFresh { get; private set; }
        public Vector3 TruckReplayCenterLocal { get; private set; }
        public Vector3 RecordedTruckCenter { get; private set; }
        public Vector3 LastFeedFocus { get; private set; }
        RenderTexture texture;
        bool wasReplay, boardShot, truckCenterReady;
        readonly Vector3[] boardCorners = new Vector3[4];
        public void RecordKick(float time) { if (float.IsFinite(time) && time >= 0) KickTime = time; }
        public void CaptureReplayTruckAnchor()
        {
            truckCenterReady = false;
            if (match == null || match.truck == null || match.truck.body == null || match.replay == null || match.replay.sources == null || match.replay.sources.Length == 0 || match.replay.sources[0] == null) return;
            // Imported NWH geometry has an offset pivot. The replay records that
            // visual root, so transfer the actual physical centre through its
            // local space instead of aiming at the imported origin.
            TruckReplayCenterLocal = match.replay.sources[0].InverseTransformPoint(match.truck.body.worldCenterOfMass);
            truckCenterReady = float.IsFinite(TruckReplayCenterLocal.x) && float.IsFinite(TruckReplayCenterLocal.y) && float.IsFinite(TruckReplayCenterLocal.z);
        }
        public void ResetAttemptPresentation()
        {
            Close(); truckCenterReady = false; KickTime = -1; FirstRecordedCursor = -1; FeedFrames = RunupFrames = FlightFrames = 0;
        }
        bool Ready => truckCenterReady && match != null && match.stadiumBoard != null && match.stadiumBoard.screen != null && match.replay != null && match.replay.ghosts != null && match.replay.ghosts.Length == 3 && feedCamera != null && feedCamera.view != null && videoCanvas != null && video != null;
        void LateUpdate()
        {
            bool replay = Ready && match.Running && match.ReplayControls && match.replay.Playing;
            if (!replay) { if (wasReplay || texture != null) Close(); return; }
            if (match.Paused && wasReplay) return;
            if (texture == null)
            {
                texture = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32) { name = "Field Goal actual recorded scoreboard feed" }; texture.Create();
                video.texture = texture; videoCanvas.gameObject.SetActive(true); feedCamera.view.targetTexture = texture;
                feedCamera.view.aspect = 16f / 9; feedCamera.view.rect = new Rect(0, 0, 1, 1); feedCamera.view.enabled = true;
                if (match.atmosphere != null && match.atmosphere.vendor != null && !match.atmosphere.vendor.AllowCameras.Contains(feedCamera.view)) match.atmosphere.vendor.AllowCameras.Add(feedCamera.view);
                FirstRecordedCursor = match.replay.Cursor;
            }
            bool runup = KickTime < 0 || match.replay.Cursor < KickTime;
            var subject = match.replay.ghosts[runup ? 0 : 1];
            RecordedTruckCenter = match.replay.ghosts[0].TransformPoint(TruckReplayCenterLocal);
            Vector3 focus = runup ? RecordedTruckCenter + Vector3.up * .9f + Vector3.forward * 3 : subject.position;
            LastFeedFocus = focus;
            feedCamera.Pose(focus, runup ? new Vector3(9, 6, -15) : new Vector3(8, 5, -11), 56, !wasReplay);
            if (!match.Paused) { FeedFrames++; if (runup) RunupFrames++; else FlightFrames++; }
            if (caption != null) caption.text = "INSTANT REPLAY  /  " + match.setup.players[match.rules.Current].name + "  /  " + match.Outcome;
            wasReplay = true;
        }
        public bool TryDescribeBoardShot(out Vector3 focus, out Vector3 offset, out float fov)
        {
            focus = offset = Vector3.zero; fov = 42;
            if (!Ready || !match.ReplayControls || !match.replay.Playing || match.replay.Angle != 0) { boardShot = BoardShotFresh = false; return false; }
            BoardShotFresh = !boardShot; boardShot = true;
            videoCanvas.GetComponent<RectTransform>().GetWorldCorners(boardCorners);
            var bounds = new Bounds(boardCorners[0], Vector3.zero); foreach (var point in boardCorners) bounds.Encapsulate(point);
            match.stadiumBoard.screen.GetComponent<RectTransform>().GetWorldCorners(boardCorners); foreach (var point in boardCorners) bounds.Encapsulate(point);
            focus = bounds.center; offset = new Vector3(0, 3, -Mathf.Max(90, bounds.size.y * 1.9f)); return true;
        }
        public void Close()
        {
            if (videoCanvas != null) videoCanvas.gameObject.SetActive(false);
            if (feedCamera != null && feedCamera.view != null)
            {
                feedCamera.view.enabled = false; feedCamera.view.targetTexture = null;
                if (match != null && match.atmosphere != null && match.atmosphere.vendor != null) match.atmosphere.vendor.AllowCameras.Remove(feedCamera.view);
            }
            if (video != null) video.texture = null;
            if (texture != null) { texture.Release(); Destroy(texture); texture = null; }
            wasReplay = boardShot = BoardShotFresh = false;
        }
        void OnDisable() => Close();
        void OnDestroy() => Close();
    }
}
