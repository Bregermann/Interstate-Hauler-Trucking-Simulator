using System;
using System.Linq;
namespace LWS.TruckArchaeology
{
    public enum ArchaeologyTool { Scanner, Excavator, Drill, Blower, Brush, Lift }
    public enum ArtifactStage { Buried, Scanned, Exposed, Lifting, Cargo, Delivered }
    public sealed class ArchaeologyRules
    {
        public readonly ArtifactStage[] Stages = new ArtifactStage[12];
        public readonly double[] Preservation = Enumerable.Repeat(1d,12).ToArray();
        public readonly int[] Owners = Enumerable.Repeat(-1,12).ToArray(), Scores = new int[4];
        public readonly int Players; public readonly bool Cooperative;
        public ArchaeologyRules(int players,bool cooperative){if(players<1||players>4)throw new ArgumentOutOfRangeException();Players=players;Cooperative=cooperative;}
        public bool CanWork(int artifact,int owner)=>artifact>=0&&artifact<Players*3&&owner>=0&&owner<Players&&(Cooperative||artifact/3==owner);
        public int Cargo(int owner){for(int i=0;i<Players*3;i++)if(Owners[i]==owner&&Stages[i]==ArtifactStage.Cargo)return i;return -1;}
        public bool Scan(int artifact,int owner,bool realScannerRange){if(!CanWork(artifact,owner)||!realScannerRange||Stages[artifact]!=ArtifactStage.Buried)return false;Stages[artifact]=ArtifactStage.Scanned;return true;}
        public bool Expose(int artifact,bool actualTerrainClear){if(artifact<0||artifact>=Players*3||Stages[artifact]!=ArtifactStage.Scanned||!actualTerrainClear)return false;Stages[artifact]=ArtifactStage.Exposed;return true;}
        public bool Lift(int artifact,int owner,bool actualClearance)
        {
            if(!CanWork(artifact,owner)||!actualClearance||Stages[artifact]!=ArtifactStage.Exposed||Enumerable.Range(0,Players*3).Any(i=>Owners[i]==owner&&(Stages[i]==ArtifactStage.Lifting||Stages[i]==ArtifactStage.Cargo)))return false;
            Owners[artifact]=owner;Stages[artifact]=ArtifactStage.Lifting;return true;
        }
        public bool Load(int artifact,int owner,bool actualRackRest){if(!CanWork(artifact,owner)||Owners[artifact]!=owner||Stages[artifact]!=ArtifactStage.Lifting||!actualRackRest)return false;Stages[artifact]=ArtifactStage.Cargo;return true;}
        public bool DropCargo(int artifact,int owner,bool actualJointLost)
        {
            if(!CanWork(artifact,owner)||Owners[artifact]!=owner||Stages[artifact]!=ArtifactStage.Cargo||!actualJointLost)return false;
            Owners[artifact]=-1;Stages[artifact]=ArtifactStage.Exposed;return true;
        }
        public int Deliver(int artifact,int owner,bool actualResearchReception,int value)
        {
            if(!CanWork(artifact,owner)||Owners[artifact]!=owner||Stages[artifact]!=ArtifactStage.Cargo||!actualResearchReception||value<1)return -1;
            Stages[artifact]=ArtifactStage.Delivered;int points=(int)Math.Round(value*Preservation[artifact],MidpointRounding.AwayFromZero);Scores[owner]+=points;return points;
        }
        public void Damage(int artifact,double loss)
        {
            if(artifact<0||artifact>=Players*3||Stages[artifact]==ArtifactStage.Delivered||!double.IsFinite(loss)||loss<=0)return;Preservation[artifact]=Math.Max(0,Preservation[artifact]-loss);
        }
        public bool Complete=>Enumerable.Range(0,Players*3).All(i=>Stages[i]==ArtifactStage.Delivered);
        public int Delivered=>Enumerable.Range(0,Players*3).Count(i=>Stages[i]==ArtifactStage.Delivered);
        public static double ToolLoss(ArchaeologyTool tool)=>tool==ArchaeologyTool.Excavator?.09:tool==ArchaeologyTool.Drill?.15:tool==ArchaeologyTool.Blower?.002:tool==ArchaeologyTool.Brush?.0002:0;
    }
}
