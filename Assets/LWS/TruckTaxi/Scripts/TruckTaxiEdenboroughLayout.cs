using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public static class TruckTaxiEdenboroughLayout
    {
        public const string SceneName="TruckTaxi_Edenborough";
        public const string WorldId="world.truck-taxi.edenborough";
        public const string ScenePath="Assets/LWS/TruckTaxi/Scenes/TruckTaxi_Edenborough.unity";
        public const float LocalLaneWidth=3.05f, ArterialLaneWidth=3.25f, HighwayLaneWidth=3.45f, ServiceLaneWidth=3.1f;
        public static readonly Bounds WorldBounds=new Bounds(new Vector3(0,180,-1000),new Vector3(12000,600,10000));
        public static readonly Vector3 Spawn=At(-570,-1030)+Vector3.up*.7f;
        public static readonly Vector3 Garage=At(-610,-1100);

        public readonly struct District
        {
            public readonly string Id,Name;
            public readonly Vector3 Center;
            public readonly Vector2 Size;
            public readonly string Dressing;
            public District(string id,string name,float x,float z,Vector2 size,string dressing)
            {Id="taxi.edenborough."+id;Name=name;Center=At(x,z);Size=size;Dressing=dressing;}
        }
        public readonly struct Road
        {
            public readonly string Id,Name,From,To,Kind;
            public readonly Vector3[] Points;
            public readonly int Lanes;
            public readonly float LaneWidth,Shoulder,SpeedMph;
            public float Width=>Lanes*LaneWidth+2*Shoulder;
            public Road(string id,string name,string from,string to,string kind,Vector3[] points)
            {
                Id="taxi.edenborough.road."+id;Name=name;From=from;To=to;Kind=kind;Points=points;
                Lanes=kind=="Highway"?4:2;
                LaneWidth=kind=="Highway"?HighwayLaneWidth:kind=="Avenue"?ArterialLaneWidth:kind=="ServiceRoad"?ServiceLaneWidth:LocalLaneWidth;
                Shoulder=kind=="Highway"?.75f:kind=="Avenue"?.25f:.15f;
                SpeedMph=kind=="Highway"?60:kind=="Avenue"?40:kind=="ServiceRoad"?15:25;
            }
        }
        public static readonly District[] Districts={
            new District("central","Central Edenborough",0,0,new Vector2(1900,1900),"Urban"),
            new District("gogar","Gogar Service District",-4800,-700,new Vector2(1800,1900),"Industrial"),
            new District("zoo","West Gardens / Zoo",-3000,200,new Vector2(1800,2100),"Suburb"),
            new District("leith","Leith Waterfront",1300,2700,new Vector2(2900,2400),"Urban"),
            new District("musselburgh","Musselburgh East",4800,-800,new Vector2(2000,2600),"MotelDistrict"),
            new District("dalkeith","Dalkeith",2700,-3100,new Vector2(2500,2400),"Suburb"),
            new District("bonnyrigg","Bonnyrigg",900,-3850,new Vector2(1600,2100),"Suburb"),
            new District("loanhead","Loanhead / Straiton",-900,-3700,new Vector2(1800,2200),"TruckStop"),
            new District("hillend","Hillend Hills",-3000,-3550,new Vector2(2600,2600),"Forest")
        };
        public static readonly IReadOnlyDictionary<string,Vector3> Nodes=CreateNodes();
        public static readonly Road[] Roads=CreateRoads();
        public static Vector3 At(float x,float z)=>new Vector3(x,Height(x,z),z);
        public static float Height(float x,float z)
        {
            float hill=145*Mathf.Exp(-Mathf.Pow((x+2950)/1900,2)-Mathf.Pow((z+3800)/2000,2));
            float south=42*Mathf.Exp(-Mathf.Pow(x/5200,2)-Mathf.Pow((z+3300)/2400,2));
            float ridge=24*Mathf.Exp(-Mathf.Pow((x+150)/1350,2)-Mathf.Pow((z+100)/1300,2));
            float coast=Mathf.SmoothStep(1,0,Mathf.InverseLerp(1300,3800,z));
            return 6+coast*(23+hill+south+ridge);
        }
        public static string RegionAt(Vector3 p)
        {
            float best=float.PositiveInfinity;string id=Districts[0].Id;
            foreach(var d in Districts)
            {
                float score=Mathf.Pow((p.x-d.Center.x)/d.Size.x,2)+Mathf.Pow((p.z-d.Center.z)/d.Size.y,2);
                if(score<best){best=score;id=d.Id;}
            }
            return id;
        }
        private static IReadOnlyDictionary<string,Vector3> CreateNodes()
        {
            var n=new Dictionary<string,Vector3>(StringComparer.Ordinal){
                ["gogar"]=At(-4800,-700),["zoo"]=At(-3000,200),["west"]=At(-1600,0),
                ["east"]=At(2200,-350),["musselburgh"]=At(4800,-800),["leith"]=At(1300,2700),
                ["port"]=At(2650,2850),["northwest"]=At(-1200,2000),["northeast"]=At(2600,1650),
                ["dalkeith"]=At(2700,-3100),["bonnyrigg"]=At(900,-3850),["loanhead"]=At(-900,-3700),
                ["hillend"]=At(-3000,-3550),["bypass.west"]=At(-4800,-2400),["hermiston"]=At(-3750,-2900),
                ["straiton"]=At(-1000,-2800),["gilmerton"]=At(1150,-2550),["sheriffhall"]=At(3250,-2250),
                ["spawn"]=At(-570,-1030),["garage"]=At(-610,-1100),["venue"]=At(1050,550)};
            for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)n["city."+x+"."+z]=At(x*260,z*260);
            foreach (var district in Districts)
            {
                string key = district.Id.Substring("taxi.edenborough.".Length);
                if (key == "central" || key == "hillend") continue;
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
                    if (x != 0 || z != 0) n[key + ".street." + x + "." + z] = At(district.Center.x + x * 180, district.Center.z + z * 180);
            }
            return n;
        }
        private static Road[] CreateRoads()
        {
            var roads=new List<Road>();
            void Add(string id,string name,string a,string b,string kind="Avenue",params Vector2[] bends)
            {
                var anchors=new List<Vector3>{Nodes[a]};foreach(var p in bends)anchors.Add(At(p.x,p.y));anchors.Add(Nodes[b]);
                var samples=new List<Vector3>();
                for(int i=1;i<anchors.Count;i++)
                {
                    int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(anchors[i-1],anchors[i])/25));
                    for(int k=i==1?0:1;k<=count;k++){var p=Vector3.Lerp(anchors[i-1],anchors[i],k/(float)count);samples.Add(At(p.x,p.z));}
                }
                roads.Add(new Road(id,name,a,b,kind,samples.ToArray()));
            }
            Add("gogar-zoo","Western Gateway","gogar","zoo");Add("zoo-west","Gardens Parkway","zoo","west");
            Add("west-grid","West Arterial","west","city.-2.0");Add("grid-east","East Arterial","city.2.0","east");
            Add("east-musselburgh","Coastal East Road","east","musselburgh");
            Add("center-leith","Leith Boulevard","city.0.2","leith");Add("leith-port","Port Service Route","leith","port","ServiceRoad");
            Add("west-northwest","Northwestern Connector","west","northwest");Add("northwest-leith","Waterfront Approach","northwest","leith");
            Add("east-northeast","Eastern Connector","east","northeast");Add("northeast-leith","Waterfront East","northeast","leith");
            Add("northeast-musselburgh","Eastern Coastal Arc","northeast","musselburgh");
            Add("gogar-bypass","Gogar Bypass Access","gogar","bypass.west");
            Add("bypass-hermiston","Bypass West","bypass.west","hermiston","Highway");
            Add("hermiston-straiton","A720 Equivalent / Hillend Cut","hermiston","straiton","Highway",new Vector2(-2400,-3150));
            Add("straiton-gilmerton","Southern Bypass","straiton","gilmerton","Highway");
            Add("gilmerton-sheriffhall","Bypass East","gilmerton","sheriffhall","Highway");
            Add("sheriffhall-musselburgh","Eastern Bypass Connector","sheriffhall","musselburgh","Highway");
            Add("center-straiton","South City Arterial","city.0.-2","straiton");Add("east-gilmerton","Gilmerton Arterial","east","gilmerton");
            Add("sheriffhall-dalkeith","Dalkeith Approach","sheriffhall","dalkeith");
            Add("dalkeith-bonnyrigg","South Suburban Road","dalkeith","bonnyrigg","CityStreet");
            Add("bonnyrigg-loanhead","Southern Town Connector","bonnyrigg","loanhead","CityStreet");
            Add("loanhead-straiton","Straiton Services Access","loanhead","straiton","ServiceRoad");
            Add("loanhead-hillend","Hillend Scenic Climb","loanhead","hillend","CityStreet");
            Add("hillend-hermiston","Hillside Western Road","hillend","hermiston","CityStreet");
            Add("hillend-zoo","Western Hills Connector","hillend","zoo","CityStreet");
            Add("garage-drive","Garage Entry","spawn","garage","ServiceRoad");
            Add("spawn-west","Spawn Approach","spawn","west","CityStreet");
            Add("spawn-city","Spawn City Connector","spawn","city.-2.-2","CityStreet");
            Add("venue","Venue Access","city.2.2","venue","ServiceRoad");
            for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)
            {
                string a="city."+x+"."+z;
                if(x<2)Add("city.x."+x+"."+z,"Central Street",a,"city."+(x+1)+"."+z,z==0?"Avenue":"CityStreet");
                if(z<2)Add("city.z."+x+"."+z,"Central Avenue",a,"city."+x+"."+(z+1),"CityStreet");
            }
            foreach (var district in Districts)
            {
                string key = district.Id.Substring("taxi.edenborough.".Length);
                if (key == "central" || key == "hillend") continue;
                string Node(int x, int z) => x == 0 && z == 0 ? key : key + ".street." + x + "." + z;
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
                {
                    if (x < 1) Add(key + ".x." + x + "." + z, district.Name + " Local Street", Node(x,z), Node(x+1,z), "CityStreet");
                    if (z < 1) Add(key + ".z." + x + "." + z, district.Name + " Local Street", Node(x,z), Node(x,z+1), "CityStreet");
                }
            }
            return roads.ToArray();
        }
    }
}
