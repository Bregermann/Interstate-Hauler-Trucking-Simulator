using System;
using UnityEngine;
using UnityEditor;
using LWS.TruckSports.Editor;
namespace LWS.TruckSkyTow.Editor
{
    public static class SkyTowReviewAuthoring
    {
        public static void Install(SkyTowMatch match)
        {
            var review = match.GetComponent<SkyTowReview>();
            if (review == null)
            {
                review = match.gameObject.AddComponent<SkyTowReview>(); review.match = match;
                var sources = new Transform[8];
                for (int i = 0; i < 4; i++) { sources[i * 2] = match.racers[i].transform; sources[i * 2 + 1] = match.racers[i].boat.transform; }
                review.motion = SportsRallyAuthoring.Replay(match, sources); review.motion.capacity = 2700;
                review.motion.interval = .05f; review.motion.maximumReplayAge = 40;
                foreach (var ghost in review.motion.ghosts)
                    if (ghost.GetComponentsInChildren<Rigidbody>(true).Length != 0 || ghost.GetComponentsInChildren<Collider>(true).Length != 0)
                        throw new InvalidOperationException("Sky Tow replay ghosts must be render-only.");
                review.cables = new LineRenderer[4];
                for (int i = 0; i < 4; i++)
                {
                    var line = new GameObject("Recorded tow cable " + i).AddComponent<LineRenderer>();
                    line.positionCount = 2; line.useWorldSpace = true; line.widthMultiplier = .07f;
                    line.sharedMaterial = match.racers[i].cable.sharedMaterial; line.enabled = false; review.cables[i] = line;
                }
            }
            match.review = review;
            if (match.menus.pages[6].transform.Find("WATCH FLIGHT") == null)
                SportsUIAuthoring.Button(match.menus.pages[6].transform, "WATCH FLIGHT", "towreview", new Vector2(0, -240), new Vector2(440, 58));
            EditorUtility.SetDirty(match); EditorUtility.SetDirty(review);
        }
    }
}
