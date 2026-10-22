using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace LWS.TruckSports.Editor
{
    // Add completed new entries without reauthoring the user's existing cards,
    // title, scene dressing or sport configuration. Explicit regeneration calls
    // the same additive operation after retaining the original catalogue.
    public static class SportsNewEntriesAuthoring
    {
        [MenuItem("Truck Sports/Install completed new sport entries (preserve menu)")]
        public static void Install()
        {
            var scene = EditorSceneManager.OpenScene(SportsBuilder.FrontScene);
            InstallInto(UnityEngine.Object.FindAnyObjectByType<SportsFrontEnd>());
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void InstallInto(SportsFrontEnd front)
        {
            if (front == null || front.catalogue == null) throw new InvalidOperationException("Existing Sports catalogue is required.");
            Add(front, "kickball", "KICKBALL", "TRUCK KICKBALL", "Pitch. Bumper kick. Run the bases.\n3v3–6v6 · four local humans + AI.", "TruckKickball");
            Add(front,"danceoff","DANCE-OFF","TRUCK DANCE-OFF","Seven original genre tracks. Rhythm-powered truck stunts.","TruckDanceOff");
            Add(front,"limbo","LIMBO","TRUCK LIMBO","Real suspension. Physical bars. How low can six tons go?","TruckLimbo");
            Add(front,"skytow","SKY TOW","TRUCK SKY TOW RINGS","A powerboat. A parasail. Six tons of truck. Arctic aerial precision.","TruckSkyTow");
            Add(front,"trailerplug","TRAILER PLUG","TRUCK TRAILER PLUG","Swing the real bungee. Drop the trailer. Seal the quarry sinkhole.","TruckTrailerPlug");
            Add(front,"farefight","FARE FIGHT","TRUCK FARE FIGHT","One passenger. Four taxis. Bump to steal. First to five fares.","TruckFareFight");
            Add(front,"snake","TRUCK SNAKE","TRUCK SNAKE","Keep rolling. Collect trailers. Jump your growing road train.","TruckSnake");
            Add(front,"redlight","RED LIGHT GREEN LIGHT","TRUCK RED LIGHT, GREEN LIGHT","Twelve trucks. One giant game master. Hear the laugh, stop before red.","TruckRedLightGreenLight");
            Add(front,"hotpotato","HOT POTATO","TRUCK HOT POTATO","Bump to pass the ticking bomb. Eight trucks. One survivor.","TruckHotPotato");
            Add(front,"balloon","BALLOON BATTLE","TRUCK BALLOON BATTLE","Three giant balloons. Retractable spikes. Real-contact party combat.","TruckBalloonBattle");
            Add(front,"whackamole","WHACK-A-MOLE","TRUCK WHACK-A-MOLE","Bumper-whack animated moles. Chase golden stars. Dodge explosive decoys.","TruckWhackAMole");
            Add(front,"kingofthehill","KING OF THE HILL","TRUCK KING OF THE HILL","Shove rivals off the crown. Five connected mountain plateaus. Hold60seconds to win.","TruckKingOfTheHill");
            Add(front,"seesaw","SEESAW","TRUCK SEESAW","Four trucks. One physical wobble board. Shift weight above a giant quarry.","TruckSeesaw");
            Add(front,"frogger","FROGGER","TRUCK FROGGER","Traffic, trains, river barges and heavy machinery. Five one-life crossing challenges.","TruckFrogger");
            Add(front,"avalanche","AVALANCHE ESCAPE","TRUCK AVALANCHE ESCAPE","Twelve trucks. An advancing snow wall. Pick your route and escape the mountain.","TruckAvalancheEscape");
            Add(front,"ninjawarrior","NINJA WARRIOR","TRUCK NINJA WARRIOR","Six real game-show obstacles. Squat, jump and bumper-strike the victory bell.","TruckNinjaWarrior");
            Add(front,"hideandseek","HIDE & SEEK","TRUCK HIDE & SEEK","One original seeker. Seven hiding trucks. Stop your engine. Search the industrial night.","TruckHideAndSeek");
            Add(front,"hungryhaulers","HUNGRY HAULERS","HUNGRY HUNGRY HAULERS","Scoop real bouncing balls. Fill your hopper. Bank your cargo. Beware the fuse balls.","TruckHungryHaulers");
            Add(front,"sheepherding","SHEEP HERDING","TRUCK SHEEP HERDING","Gentle driving. Big horns. Guide the animated flock into your color pen.","TruckSheepHerding");
            Add(front,"volleyball","VOLLEYBALL","TRUCK VOLLEYBALL","Real beach-ball rallies. Bumper returns and suspension jumps.\n2v2–4v4 · four local humans + AI.","TruckVolleyball");
            Add(front,"freezetag","FREEZE TAG","TRUCK FREEZE TAG","Four ice cannons. Four rescuers. Break the ice with real impact. Roles switch each round.","TruckFreezeTag");
            Add(front,"magnetmayhem","MAGNET MAYHEM","TRUCK MAGNET MAYHEM","Switch polarity. Haul physical scrap. Settle it in your bay. Eight magnetic trucks.","TruckMagnetMayhem");
            Add(front,"jousting","JOUSTING","TRUCK JOUSTING","Two medieval semi-trucks. Aim the real lance. Strike the shield. Brace for impact.","TruckJousting");
            Add(front,"tetherball","TETHERBALL","TRUCK TETHERBALL","Two trucks. One huge tetherball. Strike, reverse and wind the real cable.","TruckTetherball");
            Add(front,"floorislava","FLOOR IS LAVA","TRUCK FLOOR IS LAVA","Jump. Shove. Escape. Finite collapsing platforms above a giant volcano.","TruckFloorIsLava");
            SportsScrapHeapAuthoring.Apply(front);
            int count = front.catalogue.cards.Length;
            front.pages[0].transform.Find("Collection").GetComponent<TMP_Text>().text = count + " PLAYABLE TRUCK SPORTS. ONE COLLECTION.\nLocal humans, shared controllers and autonomous AI.";
            front.pages[1].transform.Find("Subtitle").GetComponent<TMP_Text>().text = count + " playable entries · Offline local play · Additional games in Scrap Heap";
            front.catalogue.Step(0);
            if(System.IO.Directory.Exists("Assets/TruckSports/Art/Previews"))SportsMenuArtAuthoring.InstallInto(front);
        }
        static void Add(SportsFrontEnd front, string key, string name, string title, string description, string folder)
        {
            string scenePath = "Assets/" + folder + "/Scenes/" + folder + ".unity";
            if (!System.IO.File.Exists(scenePath)) return;
            var parent = front.pages[1].transform;
            var card = parent.Find(name + " Sport Card");
            if (card == null)
            {
                var template = parent.Find("CORNHOLE Sport Card");
                if (template == null) throw new InvalidOperationException("Existing production sport-card template is required.");
                card = UnityEngine.Object.Instantiate(template.gameObject, parent).transform;
                card.name = name + " Sport Card";
                foreach (var action in card.GetComponentsInChildren<SportsMenuAction>(true))
                    action.action = key;
                var cards = front.catalogue.cards.ToList(); cards.Add(card.gameObject); front.catalogue.cards = cards.ToArray();
            }
            int index = Array.IndexOf(front.catalogue.cards, card.gameObject);
            if (index < 0)
            { var cards = front.catalogue.cards.ToList(); cards.Add(card.gameObject); front.catalogue.cards = cards.ToArray(); index = cards.Count - 1; }
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2((index % 4 - 1.5f) * 345, 125 - index % 8 / 4 * 255);
            card.Find("Sport").GetComponent<TMP_Text>().text = title;
            card.Find("Description").GetComponent<TMP_Text>().text = description;
            foreach (var action in card.GetComponentsInChildren<SportsMenuAction>(true))
                if (action.action == key) { action.button.buttonText = "PLAY"; action.button.normalTextObj.text = "PLAY"; }
            var preview = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/" + folder + "/Art/Menu Preview.png");
            if (preview != null)
            {
                var picture = card.Find("Sport preview");
                if (picture == null) picture = SportsUIAuthoring.Rect("Sport preview", card, new Vector2(300, 80), new Vector2(0, 18)).transform;
                var image = picture.GetComponent<UnityEngine.UI.Image>() ?? picture.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.sprite = preview; image.preserveAspect = true; image.raycastTarget = false; picture.gameObject.SetActive(true);
            }
            EditorUtility.SetDirty(front.catalogue); EditorUtility.SetDirty(front);
        }
    }
}
