using NUnit.Framework;
using System;
using System.IO;
namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCareer")]
    public sealed class TruckTaxiRosterArgumentTests
    {
        [TestCase("0",true)][TestCase("5",true)][TestCase("6",false)][TestCase("-1",false)][TestCase("../save",false)]
        public void RosterIndicesRemainExplicitAndSaveRootIsIsolated(string value,bool accepted)
        {
            string run=Guid.NewGuid().ToString("N");
            var args=new[]{TruckTaxiRestartTestArguments.ModeFlag,"seed",TruckTaxiRestartTestArguments.RunFlag,run,TruckTaxiRestartTestArguments.RosterFlag,value};
            Assert.AreEqual(accepted,TruckTaxiRestartTestArguments.TryParse(args,Path.GetTempPath(),out var request,out _));
            if(accepted){Assert.AreEqual(int.Parse(value),request.RosterIndex);Assert.AreEqual(run,new DirectoryInfo(request.Root).Name);}
        }
        [Test] public void DuplicateOrConflictingRosterFlagsAreRejected()
        {
            foreach(string flag in new[]{TruckTaxiRestartTestArguments.DriveFlag,TruckTaxiRestartTestArguments.DualWorldFlag,TruckTaxiRestartTestArguments.RosterFlag})
            {
                var args=new[]{TruckTaxiRestartTestArguments.ModeFlag,"seed",TruckTaxiRestartTestArguments.RunFlag,Guid.NewGuid().ToString("N"),TruckTaxiRestartTestArguments.RosterFlag,"1",flag,"2"};
                Assert.IsFalse(TruckTaxiRestartTestArguments.TryParse(args,Path.GetTempPath(),out _,out _));
            }
        }
    }
}
