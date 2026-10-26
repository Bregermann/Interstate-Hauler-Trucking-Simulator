#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LWS.TruckSports
{
    // Explicit development acceptance only. Original match owners still drive,
    // record, score, advance attempts and determine the final winner.
    public static class SportsNightReplayAcceptance
    {
        public static bool Enabled => Array.IndexOf(Environment.GetCommandLineArgs(), "-tsNightReplayLights") >= 0;
        public static void Configure(SportsChaosWeather weather, Action<bool,string> check)
        {
            if (!Enabled) return;
            SportsTimeOfDay.Select(SportsTimeChoice.Night);
            var clear = weather.presets.entries.Single(entry => entry.displayName == "Clear");
            weather.presets.Select(clear.id);
            check(weather.presets.Selection == clear.id, "explicit night replay fixture uses the installed Clear preset and isolated saved Night preference");
        }
        public static IEnumerator Inspect(SportsEventSession match, SportsEventTruck physical, Transform recorded,
            Func<string> scores, Action<bool,string> check, string folder, bool capture)
        {
            float scale = Time.timeScale; Time.timeScale = 0;
            int receipts = match.ScoreJournal.Count; string originalScores = scores();
            bool kinematic = physical.body.isKinematic, collisions = physical.body.detectCollisions;
            try
            {
                yield return new WaitForSecondsRealtime(.3f);
                var binding = match.GetComponent<SportsNightHeadlights>();
                var beams = binding.beams.Where(beam => beam.target == physical.transform).ToArray();
                check(binding.time.Applied == SportsTimeChoice.Night && beams.Length > 0 && recorded.gameObject.activeInHierarchy,
                    "actual original replay has saved Night selection and visible recorded vehicle");
                check(beams.All(beam => beam.light.enabled && beam.replaySource is ISportsRecordedVehicleView view &&
                    view.TryGetRecordedVehicle(physical.transform, out var source, out var target) && target == recorded &&
                    (source == physical.transform || source.IsChildOf(physical.transform)) &&
                    Vector3.Distance(beam.light.transform.position, recorded.TransformPoint(source.InverseTransformPoint(physical.transform.TransformPoint(beam.position)))) < .1f),
                    "existing authored beam follows the actual recorded truck rather than the hidden live body");
                check(recorded.GetComponentsInChildren<Light>(true).Length == 0,
                    "actual playback hierarchy contains no duplicate lights or added recording components");
                if (capture)
                {
                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsSwingVenueLights") >= 0)
                    {
                        var lamps = binding.time.nightLights;
                        var fixtures = binding.time.transform.Find("Authored Swing night venue lamps");
                        check(fixtures != null && lamps.Length == 6 && lamps.All(lamp => lamp != null && lamp.enabled && lamp.intensity > 0 && lamp.shadows == LightShadows.None && lamp.transform.IsChildOf(fixtures)),
                            "saved Swing venue uses six active clock-owned non-shadow fixtures during the original Night replay");
                        check(fixtures != null && fixtures.GetComponentsInChildren<Collider>(true).Length == 0 && fixtures.GetComponentsInChildren<Rigidbody>(true).Length == 0 && fixtures.GetComponentsInChildren<Joint>(true).Length == 0,
                            "purchased venue fixtures introduce no physics into the original pendulum or landing area");
                    }
                    foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) })
                    {
                        yield return SportsWindowCapture.Save(size.x,size.y,Path.Combine(folder,"night-replay-"+size.y+".png"));
                        check(Screen.width == size.x && Screen.height == size.y, "actual night replay backbuffer size " + size);
                    }
                }
                check(match.ScoreJournal.Count == receipts && scores() == originalScores && physical.body.isKinematic == kinematic && physical.body.detectCollisions == collisions,
                    "night replay presentation preserves original points, receipts and physical collision authority");
            }
            finally { Time.timeScale = scale; }
        }
    }
}
#endif
