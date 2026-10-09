using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiCareerPanelTests
    {
        [Test] public void RatingUsesLifetimeTotalsAndHandlesNoRides()
        {
            var state=new TruckTaxiPersistentState();
            StringAssert.Contains("Not yet rated",TruckTaxiCareerPanel.BuildCategoryRows("RATING",state,null)[0]);
            state.statistics.completedRides=2; state.statistics.totalStarsEarned=8;
            state.career.threeStars=1; state.career.fiveStars=1;
            var rows=TruckTaxiCareerPanel.BuildCategoryRows("RATING",state,null);
            StringAssert.Contains((4d).ToString("0.00"),rows[0]);
            CollectionAssert.Contains(rows,"3-star: 1"); CollectionAssert.Contains(rows,"5-star: 1");
        }

        [Test] public void RideAndGoalTotalsComeFromPersistedRecordsWithoutMutation()
        {
            var state=new TruckTaxiPersistentState(); state.passengerSelection.offerIndex=9;
            state.passengerSelection.passengers.Add(new TruckTaxiPassengerExposure { timesAccepted=3 });
            state.passengerSelection.passengers.Add(new TruckTaxiPassengerExposure { timesAccepted=4 });
            state.career.goals.Add(new TruckTaxiCareerGoalStatistics { id="goal.a",offered=5,attempted=4,completed=3,failed=1 });
            state.career.goals.Add(new TruckTaxiCareerGoalStatistics { id="goal.b",offered=2,attempted=1,completed=1 });
            string before=JsonUtility.ToJson(state);
            var rides=TruckTaxiCareerPanel.BuildCategoryRows("RIDES",state,null);
            CollectionAssert.Contains(rides,"Offered: 9"); CollectionAssert.Contains(rides,"Accepted: 7");
            var goals=TruckTaxiCareerPanel.BuildCategoryRows("GOALS",state,null);
            CollectionAssert.Contains(goals,"Offered: 7"); CollectionAssert.Contains(goals,"Attempted: 5");
            CollectionAssert.Contains(goals,"Completed: 4"); CollectionAssert.Contains(goals,"Failed: 1");
            Assert.AreEqual(before,JsonUtility.ToJson(state));
        }

        [Test] public void LastSessionNeverSubstitutesLifetimeTotals()
        {
            var state=new TruckTaxiPersistentState(); state.statistics.completedRides=90;
            CollectionAssert.Contains(TruckTaxiCareerPanel.BuildCategoryRows("LAST SESSION",state,null),"No recorded last session.");
            state.lastSession=new TruckTaxiLastSessionStatistics { rides=2,totalStars=7 };
            CollectionAssert.Contains(TruckTaxiCareerPanel.BuildCategoryRows("LAST SESSION",state,null),"Rides completed: 2");
        }

        [Test] public void OwnershipWithoutCatalogKeepsIdsAndEquippedMarkers()
        {
            var state=new TruckTaxiPersistentState { ownedProfile=new TruckTaxiCareerProfileData { equippedTruckId="truck.saved" } };
            var truck=new TruckTaxiOwnedTruck { truckId="truck.saved" }; state.ownedProfile.trucks.Add(truck);
            truck.ownedUpgradeIds.Add("upgrade.saved"); truck.upgrades.Add(new TruckTaxiCareerSelection { itemId="upgrade.saved",slotId="engine" });
            state.ownedProfile.ownedHeadwearIds.Add("hat.saved");
            CollectionAssert.Contains(TruckTaxiCareerPanel.BuildCategoryRows("TRUCK",state,null),"truck.saved [EQUIPPED]");
            CollectionAssert.Contains(TruckTaxiCareerPanel.BuildCategoryRows("UPGRADES",state,null),"upgrade.saved [EQUIPPED]");
            CollectionAssert.Contains(TruckTaxiCareerPanel.BuildCategoryRows("COSMETICS",state,null),"Headwear: hat.saved [OWNED]");
        }

        [Test] public void PagesAreBoundedAndEveryRowRemainsReachable()
        {
            var rows=new List<string>(); for(int i=0;i<19;i++) rows.Add("Row "+i);
            var combined=new List<string>();
            for(int page=0;page<4;page++)
            {
                var slice=TruckTaxiCareerPanel.PageRows(rows,page);
                Assert.LessOrEqual(slice.Count,TruckTaxiCareerPanel.RowsPerPage); combined.AddRange(slice);
            }
            CollectionAssert.AreEqual(rows,combined);
            Assert.AreEqual("Row 0",TruckTaxiCareerPanel.PageRows(rows,-1)[0]);
            Assert.AreEqual("Row 18",TruckTaxiCareerPanel.PageRows(rows,int.MaxValue)[0]);
            Assert.IsEmpty(TruckTaxiCareerPanel.PageRows(new List<string>(),0));
        }

        [Test] public void LongMultilineIdsCannotCreateAdditionalDisplayRows()
        {
            var state=new TruckTaxiPersistentState(); state.career.events.Add(new TruckTaxiCareerEventCount { id=new string('x',100)+"\nextra",count=3 });
            var rows=TruckTaxiCareerPanel.BuildCategoryRows("EVENTS",state,null);
            string entry=rows[rows.Count-1]; Assert.Less(entry.Length,60);
            StringAssert.DoesNotContain("\n",entry); StringAssert.EndsWith(": 3",entry);
        }

        [Test] public void FinancesCanPresentCombinedSpendingBeyondLongRange()
        {
            var state=new TruckTaxiPersistentState { shiftEarningsCents=long.MaxValue,purchaseChargesCents=long.MaxValue,serviceChargesCents=10 };
            var rows=TruckTaxiCareerPanel.BuildCategoryRows("FINANCES",state,null);
            CollectionAssert.Contains(rows,"Debt: "+TruckTaxiHud.Money(10));
            CollectionAssert.Contains(rows,"Lifetime spending: $"+(((decimal)long.MaxValue+10)/100m).ToString("0.00"));
        }
    }
}
