using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckSports
{
    // Render-only, match-end replay over the existing shared pose recorder.
    // Separate body/debris masks preserve actual destruction without resimulation.
    public sealed class SportsRecordedReview : MonoBehaviour
    {
        public SportsEventSession match;
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public Transform[] extras = System.Array.Empty<Transform>();
        public int focusSlot;
        public string watchAction = "watchrecording";
        public SportsMotionReplay record;
        public bool Playing { get; private set; }
        Renderer[][] liveRenderers;
        bool[][] hiddenBefore;
        int focus;

        public void ResetRecord()
        {
            End(false);
            if (record != null) record.ResetRecord();
        }
        void Ensure()
        {
            if (record != null) return;
            record = gameObject.AddComponent<SportsMotionReplay>();
            record.capacity = 360; record.interval = .08f; record.rolling = true; record.maximumReplayAge = 25;
            int count = trucks.Length * 2 + extras.Length;
            if (count > 31) throw new System.InvalidOperationException("Recorded sport visibility mask supports at most31source hierarchies.");
            record.sources = new Transform[count]; record.ghosts = new Transform[count];
            liveRenderers = new Renderer[count][]; hiddenBefore = new bool[count][];
            for (int i = 0; i < trucks.Length; i++)
            {
                record.sources[i] = trucks[i].transform;
                record.sources[trucks.Length + i] = fragments[i].pieces[0].body.transform.parent;
                liveRenderers[i] = record.sources[i].GetComponentsInChildren<Renderer>(true);
                hiddenBefore[i] = new bool[liveRenderers[i].Length];
                liveRenderers[trucks.Length + i] = record.sources[trucks.Length + i].GetComponentsInChildren<Renderer>(true);
                hiddenBefore[trucks.Length + i] = new bool[liveRenderers[trucks.Length + i].Length];
            }
            for (int i = 0; i < extras.Length; i++)
            {
                record.sources[trucks.Length * 2 + i] = extras[i];
                liveRenderers[trucks.Length * 2 + i] = extras[i].GetComponentsInChildren<Renderer>(true);
                hiddenBefore[trucks.Length * 2 + i] = new bool[liveRenderers[trucks.Length * 2 + i].Length];
            }
            for (int i = 0; i < count; i++)
            {
                var ghost = SportsReplayVisuals.Copy(record.sources[i]);
                ghost.name = "Recorded sport " + (i < trucks.Length ? "tractor " : "actual debris ") + i;
                record.ghosts[i] = ghost.transform; ghost.SetActive(false);
            }
            record.ResetRecord();
        }
        public void Capture(float time)
        {
            if (time < 3 || Playing) return;
            Ensure(); int mask = 0;
            for (int i = 0; i < match.setup.count; i++) mask |= 1 << (fragments[i].Bursting ? trucks.Length + i : i);
            for (int i = 0; i < extras.Length; i++) if (extras[i].gameObject.activeInHierarchy) mask |= 1 << (trucks.Length * 2 + i);
            record.Record(time, mask);
        }
        public void Begin()
        {
            if (Playing || !match.Finished || record == null || record.Duration < 4) return;
            focus = Mathf.Clamp(focusSlot, 0, match.setup.count - 1);
            foreach (var camera in drivingViews.rally.views) camera.view.enabled = false;
            for (int i = 0; i < liveRenderers.Length; i++) for (int j = 0; j < liveRenderers[i].Length; j++)
            { hiddenBefore[i][j] = liveRenderers[i][j].forceRenderingOff; liveRenderers[i][j].forceRenderingOff = true; }
            broadcast.view.enabled = true; broadcast.view.orthographic = false; broadcast.view.rect = new Rect(0, 0, 1, 1);
            // A review entered from cab/split view renders through broadcast.
            // Weather Maker must follow that actual camera as well.
            atmosphere.vendor.AllowCameras.Clear(); atmosphere.vendor.AllowCameras.Add(broadcast.view);
            framing.enabled = false;
            match.cameras = new[] { broadcast.view }; match.menus.Show(SportsEventScreen.Play);
            record.Begin(Mathf.Max(3, record.Duration - 18)); Playing = true; Time.timeScale = 0; atmosphere.Pause(true);
        }
        public void Tick()
        {
            if (!Playing) return;
            if (record.Step(Time.unscaledDeltaTime)) { End(); return; }
            var position = record.Position(focus);
            var offset = record.Angle == 0 ? new Vector3(17, 12, -24) : record.Angle == 1 ? new Vector3(-18, 8, 14) : new Vector3(0, 48, -8);
            broadcast.Pose(position + Vector3.up * 2, offset, 60, true);
        }
        public void Action(string key)
        {
            if (key == watchAction) { Begin(); return; }
            if (!Playing) return;
            if (key == "replayskip") End();
            else if (key == "replayangle") record.Angle = (record.Angle + 1) % 3;
            else if (key == "replayspeed") record.Speed = record.Speed == 1 ? .35f : record.Speed < 1 ? 2 : 1;
            else if (key == "replayrewind") record.Seek(Mathf.Max(3, record.Duration - 18));
        }
        public void End(bool results = true)
        {
            if (!Playing) return;
            record.End();
            for (int i = 0; i < liveRenderers.Length; i++) for (int j = 0; j < liveRenderers[i].Length; j++)
                if (liveRenderers[i][j] != null) liveRenderers[i][j].forceRenderingOff = hiddenBefore[i][j];
            Playing = false; Time.timeScale = 1; atmosphere.Pause(false); drivingViews.Layout();
            framing.enabled = true;
            if (results && match.Finished) match.menus.Show(SportsEventScreen.Result);
        }
        void OnDisable() { End(false); }
        void OnDestroy() { if (record != null && record.ghosts != null) foreach (var ghost in record.ghosts) if (ghost != null) Destroy(ghost.gameObject); }
    }
}
