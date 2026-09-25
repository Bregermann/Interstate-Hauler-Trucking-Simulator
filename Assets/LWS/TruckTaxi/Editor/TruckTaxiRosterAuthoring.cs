using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiRosterAuthoring
    {
        [Serializable] private sealed class Roster { public Entry[] passengers; }
        [Serializable] private sealed class Entry
        {
            public string id,name,reference,species,race,sex,age,build,hair,features,clothing,personality,mechanic,seat,pair,rarity,greeting,arrival;
            public float height=1.75f,chaos;
        }
        [MenuItem("Truck Taxi/Passenger Factory Tools/Ensure Initial Roster")]
        public static void EnsureRoster()
        {
            var roster=JsonUtility.FromJson<Roster>(File.ReadAllText("Tools/TruckTaxiPassengerFactory/roster.json"));
            string folder=TruckTaxiPassengerFactoryBuilder.Root+"/Profiles"; TruckTaxiPassengerDialogueAuthoring.EnsureFolder(folder);
            foreach(var entry in roster.passengers)
            {
                if(!TruckTaxiChatterboxQueue.IsSafeId(entry.id)) throw new InvalidOperationException("Invalid roster ID");
                if(TruckTaxiPassengerDialogueAuthoring.AllPassengers().Any(p=>p.passengerId==entry.id)) continue;
                var p=ScriptableObject.CreateInstance<PassengerProfile>(); p.passengerId=entry.id; p.passengerName=entry.name;
                p.developmentReference=entry.reference; p.archetype=entry.reference.Split(';')[0]; p.description=p.personality=entry.personality;
                p.chaosAffinity=entry.chaos; p.smoothAffinity=entry.chaos<0 ? 1.5f : .4f;
                p.speedPreference=p.collisionPreference=p.shortcutPreference=p.offroadPreference=entry.chaos;
                p.basePatience=entry.mechanic=="TimeObsessed" ? 150 : 360;
                p.dialogueSet=new[]{entry.greeting,entry.personality}; p.ejectionReaction="This was not the exit I had in mind!";
                if(Enum.TryParse(entry.mechanic,out TruckTaxiMechanic mechanic)) p.uniqueMechanics=new[]{mechanic};
                p.specialTraits=p.uniqueMechanics.Select(x=>x.ToString()).ToArray();
                if(mechanic==TruckTaxiMechanic.NeverTips) { p.tipMultiplier=0; p.baseTipChance=0; }
                if(Enum.TryParse(entry.rarity,out TruckTaxiRarity rarity)) { p.rarity=rarity; p.spawnWeight=.08f; }
                AssetDatabase.CreateAsset(p,folder+"/"+entry.id+".asset"); TruckTaxiPassengerFactoryBuilder.BuildMissing(p);
                p.casting.species=entry.species; p.casting.human=entry.species=="Human"; p.casting.raceEthnicity=entry.race;
                p.casting.sexPresentation=entry.sex; p.casting.approximateAge=entry.age; p.casting.build=entry.build;
                p.casting.hair=entry.hair; p.casting.distinctiveFeatures=entry.features; p.casting.clothingStyle=entry.clothing;
                float height=entry.height>.1f ? entry.height : 1.75f;
                p.casting.heightMeters=p.appearance.heightMeters=height; p.appearance.raceEthnicity=entry.race; p.appearance.bodyDescription=entry.build;
                p.appearance.distinctiveFeatures=entry.features;
                p.appearance.characterType=p.casting.human ? TruckTaxiCharacterType.ModularHuman : entry.species.IndexOf("robot",StringComparison.OrdinalIgnoreCase)>=0 ? TruckTaxiCharacterType.Robot : TruckTaxiCharacterType.GeneratedCreature;
                p.appearance.rigType=p.casting.human ? TruckTaxiRigType.Humanoid : TruckTaxiRigType.Generic;
                p.appearance.generationPrompt=$"Original stylized comedy passenger {entry.name}. Species: {entry.species}. Explicit casting: {entry.race} {entry.sex}, {entry.age}. {entry.build}. {entry.hair}. {entry.clothing}. {entry.features}. Neutral A-pose, full body, unobstructed silhouette. No logos. Same outfit in front, side, rear and three-quarter views.";
                p.appearance.avoidPrompt="No copied costume, no source logos, no racial caricature, no extra limbs, no floating feet, no scenery.";
                p.appearance.generationStatus="Awaiting final model; playable fallback";
                if(Enum.TryParse(entry.seat,out TruckTaxiSeatType seat)) p.seatProfile.seatType=seat;
                if(p.seatProfile.seatType==TruckTaxiSeatType.OversizedPassenger)
                {
                    p.appearance.characterType=TruckTaxiCharacterType.OversizedPassenger;
                    p.appearance.bodyScale=new Vector3(1.25f,1,1.15f); p.seatProfile.additionalMassKg=350; p.seatProfile.boardingImpulse=700;
                    p.seatProfile.localScale=Vector3.one*.7f; p.seatProfile.localPosition=new Vector3(.5f,-.2f,-.12f);
                }
                if(p.seatProfile.seatType==TruckTaxiSeatType.DashboardPassenger || p.seatProfile.seatType==TruckTaxiSeatType.CompanionPassenger)
                { p.seatProfile.localPosition=new Vector3(.16f,.65f,.4f); p.seatProfile.localScale=Vector3.one*.45f; }
                p.appearance.fallbackColor=entry.species.IndexOf("green",StringComparison.OrdinalIgnoreCase)>=0 ? new Color(.2f,.65f,.25f) :
                    entry.species.IndexOf("purple",StringComparison.OrdinalIgnoreCase)>=0 ? new Color(.55f,.2f,.8f) : Color.HSVToRGB((Array.IndexOf(roster.passengers,entry)*.137f)%1,.55f,.8f);
                p.voiceProfile.description="UNCAST: "+entry.personality; p.voiceProfile.voicePresentation=entry.sex; p.voiceProfile.approximateAge=entry.age;
                p.voiceProfile.deliveryStyle=entry.personality;
                var arrival=p.authoredDialogue.lines.First(l=>l.lineId=="arrival"); arrival.text=arrival.subtitle=entry.arrival;
                foreach(var asset in new UnityEngine.Object[]{p,p.casting,p.appearance,p.seatProfile,p.voiceProfile,p.authoredDialogue}) EditorUtility.SetDirty(asset);
            }
            var all=TruckTaxiPassengerDialogueAuthoring.AllPassengers();
            foreach(var entry in roster.passengers.Where(e=>!string.IsNullOrEmpty(e.pair)))
            {
                var p=all.First(x=>x.passengerId==entry.id);
                if(p.pairPassenger==null) { p.pairPassenger=all.First(x=>x.passengerId==entry.pair); EditorUtility.SetDirty(p); }
            }
            AssetDatabase.SaveAssets(); TruckTaxiPassengerFactoryBuilder.BuildAllBatch();
            Debug.Log("TRUCK TAXI ROSTER: "+TruckTaxiPassengerDialogueAuthoring.AllPassengers().Length+" profiles, originals preserved.");
        }
    }
}
