using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LWS.TruckDragonEggDelivery;
using LWS.TruckDragonEscape;
using LWS.TruckDragonRiders;
using LWS.TruckHoardHeist;
using LWS.TruckSports;

namespace LWS.TruckDragonKingdom
{
    // Read-only opt-in for these four modes. The existing Heat/DOTween board
    // consumes inherited identity/readiness; rules remain the sole award owner.
    public sealed class DragonFinalResultsSource : SportsFinishResultsSource
    {
        public override bool TryCapture(out SportsResultsSnapshot snapshot)
        {
            snapshot=null;
            if(session==null||!session.Finished||session.setup==null||session.setup.players==null||session.setup.count<1||session.setup.count>4||session.setup.players.Length<session.setup.count)return false;
            switch(session)
            {
                case DragonEggDeliveryMatch egg:return CaptureEgg(egg,out snapshot);
                case HoardHeistMatch heist:return CaptureHeist(heist,out snapshot);
                case DragonEscapeMatch escape:return CaptureEscape(escape,out snapshot);
                case DragonRidersMatch riders:return CaptureRiders(riders,out snapshot);
                default:return false;
            }
        }
        static bool CaptureEgg(DragonEggDeliveryMatch match,out SportsResultsSnapshot snapshot)
        {
            snapshot=null;var rules=match.Rules;int count=match.setup.count;if(rules==null||rules.Count!=count)return false;
            var scores=rules.Cargo.Select(cargo=>cargo.Score).ToArray();var winners=rules.Winners();var rows=new List<SportsResultRow>(count);int delivered=0;
            for(int slot=0;slot<count;slot++)
            {
                var cargo=rules.Cargo[slot];bool safe=cargo.Stage==DragonEggStage.Delivered;if(safe)delivered++;
                // Only delivered cargo receives a placing. A lost/unfinished
                // load retains its actual zero score and explicit DNF state.
                int place=safe?SportsResultPlacings.FromFinalTotals(slot,count,scores):0;
                string detail=safe?(cargo.DeliveredBaby?"BABY SAFE":"EGG SAFE"):cargo.Stage==DragonEggStage.Lost?"DNF · CARGO LOST":"DNF · NOT DELIVERED";
                detail+=" · "+Number(cargo.Condition*100,"0")+"% CONDITION · "+cargo.RoughImpacts+" ROUGH IMPACT"+(cargo.RoughImpacts==1?"":"S");
                if(safe)detail+=" · "+Number(cargo.DeliverySeconds,"0.0")+" SEC";
                rows.Add(new SportsResultRow(match.setup.players[slot],slot,place,Array.IndexOf(winners,slot)>=0,cargo.Score,"PTS",detail,format:"0"));
            }
            string heading=winners.Length>1?"SHARED BROOD HONORS":winners.Length==1?"BROOD CHAMPION":delivered==0?"NO SAFE DELIVERIES":"BROOD DELIVERY RESULTS";
            snapshot=new SportsResultsSnapshot(heading,delivered+" / "+count+" SAFE · ACTUAL DELIVERY POINTS · UNDELIVERED CARGO EARNS NOTHING",rows);return true;
        }
        static bool CaptureHeist(HoardHeistMatch match,out SportsResultsSnapshot snapshot)
        {
            snapshot=null;var rules=match.Rules;int count=match.setup.count;if(rules==null||rules.Participants!=count)return false;
            int best=rules.Scores.Max(),leaders=rules.Scores.Count(score=>score==best);var rows=new List<SportsResultRow>(count);
            for(int slot=0;slot<count;slot++)rows.Add(new SportsResultRow(match.setup.players[slot],slot,
                SportsResultPlacings.FromFinalTotals(slot,count,rules.Scores),rules.Scores[slot]==best,rules.Scores[slot],"VALUE","PHYSICALLY RECEIVED TREASURE · UNBANKED CARGO DID NOT SCORE",format:"0"));
            // StopGame releases racks and clears their owner assignments. Read
            // the exact global inventory disposition, never invent a per-crew
            // unbanked value from the now-cleared ownership or a results string.
            int unbanked=rules.States.Length-rules.DeliveredCount;
            snapshot=new SportsResultsSnapshot(leaders>1?"SHARED TOP HOARD VALUE":"HOARD HEIST WINNER",
                rules.DeliveredCount+" ORIGINAL TREASURES RECEIVED · "+unbanked+" UNBANKED · FINAL RECEIVED VALUE ONLY",rows);return true;
        }
        static bool CaptureEscape(DragonEscapeMatch match,out SportsResultsSnapshot snapshot)
        {
            snapshot=null;int count=match.setup.count;if(match.ExtractionTimes.Length<count||match.Placements.Length<count)return false;
            var rows=new List<SportsResultRow>(count);
            for(int slot=0;slot<count;slot++)
            {
                bool extracted=match.ExtractionTimes[slot]>=0;int place=extracted?match.Placements[slot]:0;
                // ExtractionTimes already includes the owner's recovery penalty.
                // Preserve same-step placements even when adjusted times differ.
                rows.Add(new SportsResultRow(match.setup.players[slot],slot,place,!match.Cooperative&&extracted&&place==1,
                    extracted?(double?)match.ExtractionTimes[slot]:null,"SEC",extracted?"PHYSICALLY EXTRACTED · RECOVERY PENALTIES INCLUDED":"DNF · NO PHYSICAL EXTRACTION","DNF","0.0"));
            }
            bool convoyRescued=match.ActualExtractions==count;
            string heading=match.Cooperative?(convoyRescued?"CONVOY RESCUED":"CONVOY RESCUE FAILED"):match.ActualExtractions>0?"EXTRACTION RESULTS":"NO CREW EXTRACTED";
            // Cooperative success belongs to the whole convoy. Giving every
            // crew a fabricated first place to highlight it would change rules.
            snapshot=new SportsResultsSnapshot(heading,match.ActualExtractions+" / "+count+" EXTRACTED · "+(match.Cooperative?"COOPERATIVE TEAM OUTCOME · ALL CREWS REQUIRED":"AUTHORITATIVE EXTRACTION PLACEMENTS · ADJUSTED SECONDS"),rows);return true;
        }
        static bool CaptureRiders(DragonRidersMatch match,out SportsResultsSnapshot snapshot)
        {
            snapshot=null;var rules=match.rules;int count=match.setup.count;if(rules==null||rules.Count!=count)return false;
            var rows=new List<SportsResultRow>(count);int finishedCount=0,winners=0;
            for(int slot=0;slot<count;slot++)
            {
                bool finished=rules.Finished(slot);int place=match.ResultPlace(slot);bool winner=finished&&place==1;if(finished)finishedCount++;if(winner)winners++;
                string detail=rules.Passed[slot]+" / "+(rules.GateCount*rules.Laps)+" GATES · +"+Number(rules.Penalty[slot],"0.##")+" PENALTY SEC";
                if(!finished)detail+=" · "+(rules.Retired[slot]?"RETIRED":"COURSE INCOMPLETE");
                // FinishTime is already the genuine finish plus its penalty.
                // Progress placings can still exist with no finish; none wins.
                rows.Add(new SportsResultRow(match.setup.players[slot],slot,place,winner,finished?(double?)rules.FinishTime[slot]:null,
                    "SEC",detail,rules.Retired[slot]?"RETIRED":"DNF","0.00"));
            }
            string heading=finishedCount==0?"NO CREW COMPLETED THE PHYSICAL COURSE":winners>1?"SHARED SKYFIRE VICTORY":"SKYFIRE GRAND PRIX RESULTS";
            snapshot=new SportsResultsSnapshot(heading,finishedCount+" / "+count+" FINISHED · ORDERED GATES · PENALTIES ALREADY INCLUDED IN FINISH TIME",rows);return true;
        }
        static string Number(double value,string format)=>value.ToString(format,CultureInfo.InvariantCulture);
    }
}
