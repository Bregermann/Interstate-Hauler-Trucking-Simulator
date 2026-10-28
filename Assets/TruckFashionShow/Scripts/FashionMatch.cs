using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckDanceOff;
namespace LWS.TruckFashionShow
{
    public enum FashionPhase { Hub, Lineup, Entrance, Performance, Judging, Exit, Complete }
    public sealed partial class FashionMatch:SportsEventSession
    {
        public SportsEventTruck[] trucks;public FashionStrut[] walkers;public SportsEventCamera broadcast;public FashionPeople people;public Transform entrance,finalMark;
        public FashionGalleryPresentation gallery;
        public AudioSource music;public float hubSeconds=60;public FashionCompetition Competition {get;private set;}public FashionPhase Phase {get;private set;}public FashionTheme Theme=>(FashionTheme)option;
        public int ActiveOwner {get;private set;}public float Clock {get;private set;}public float PerformanceTime {get;private set;}public DanceRules Timing {get;private set;}
        public int NaturalRunwayEntries {get;private set;}public int CompletedWalks {get;private set;}public readonly bool[] OutfitReady=new bool[4];
        public string[] FrozenOutfitIds {get;private set;}=new string[4];
        readonly DanceInputRouter[] routers=new DanceInputRouter[4];readonly List<Hit> hits=new List<Hit>();readonly bool[] aiEquipped=new bool[4];readonly float[] aiShopAge=new float[4];
        struct Hit {public double time;public int mask;public bool down;public Hit(double t,int m,bool d){time=t;mask=m;down=d;}}
        DanceNote[] chart;FashionItem[] preparedOutfit;System.Random aiRandom;float phaseAge,resumeAge,lastReaction=-10;int held,exitLeg;double judged;int cameraShot=-1;bool configured;
        public override string Title=>"TRUCK FASHION SHOW";public override int MinimumPlayers=>4;public override int MaximumPlayers=>4;public override int OptionCount=>4;public override int WeatherCount=>1;
        public override string OptionLabel=>new[]{"ROYALTY","FANCY EVENING","SPOOKY","WILDLY RIDICULOUS"}[option];
        public override string Help=>"TOTALLY RIDICULOUS RUNWAY\nDrive a deliberately cute, controlled walking truck through the indoor fashion hub. Movement steers, sways and bounces the original complete tractor assembly; this choreography is local to Fashion. Visit the marked hat, face, wheel or mirror/accessory shop, or the dressing room, and press your assigned ACTION. The existing FREE customizer provides real asset previews, camera rotation, equip, SAVE and CANCEL over YOUR stable named profile. No currency, purchases or unlocks. Unsaved previews never replace another participant's outfit. Press ACTION near the runway entrance or your READY button when prepared. After sixty live seconds the show begins with the actual saved outfits.\nFour contestants include one to four assigned humans and AI. Each walks on individually while others watch. Steer to the entrance, then LEFT / DOWN / UP / RIGHT prompts use the existing Dance-Off lane controls: assigned movement keys, whole-pad D-pad/face buttons, or the assigned shared stick. Inputs are judged once, with forgiving Great120ms / Good280ms windows; wrong presses reduce timing score. Successful prompts cause real strut/pose/spin/showcase choreography. Keep walking toward the final mark; the controlled performance pace advances only during your runway turn. There are twelve prompts and an actual final-mark completion award.\nJudges score actual composed-item theme tags, coordinated gold/chrome/cherry finishes, accessory variety, actual QTE accuracy, successful poses and real runway completion. Scores show each component; no random judging. The four available themes match real current catalog content. AI shops/equips real assets and uses difficulty-timed input through the same Dance rules. Pause/reconnect gate held inputs without granting notes; rematch starts a fresh show while saved outfits persist. The later cross-sport economy is deliberately deferred.";
        public override bool BroadcastFramingApplicable=>false;public override bool CompactBroadcastStatus=>true;public override int CompactScoreColumns=>4;
        public override string Header=>OptionLabel+" | "+(Phase==FashionPhase.Hub?Mathf.CeilToInt(Mathf.Max(0,hubSeconds-Clock))+"s TO RUNWAY":"CONTESTANT "+(ActiveOwner+1)+" / 4");
        public override string Status=>!configured?"FREE STORES / OWNED COSMETICS":Phase==FashionPhase.Hub?"DRESS / SAVE / READY | "+OutfitReady.Count(v=>v)+" OF 4 READY":Phase==FashionPhase.Performance?Prompt:Phase==FashionPhase.Judging?GradeText(ActiveOwner):Phase.ToString().ToUpperInvariant();
        public override string Standings=>Competition==null?"":string.Join("\n",Enumerable.Range(0,4).Select(i=>setup.players[i].name+" | "+Competition.Scores[i]));
        public override string Results=>Competition==null?"":"RUNWAY CHAMPION | "+string.Join(" / ",Competition.Leaders().Select(i=>setup.players[i].name))+"\n"+Standings;
        public override bool TryReadScoreCard(int card,out string value,out string detail){if(card<0||card>=4){value=detail=null;return false;}value=Competition==null?"0":Competition.Scores[card].ToString();detail=Phase==FashionPhase.Hub?(OutfitReady[card]?"OUTFIT READY":"FREE SHOPS"):ActiveOwner==card?Phase.ToString().ToUpperInvariant():card<ActiveOwner?"JUDGED":"WATCHING";return true;}
        public override string ControlCaption(int slot)=>Phase==FashionPhase.Performance?setup.players[slot].source==BowlingSource.WholeController?"D-PAD / WEST SOUTH NORTH EAST":"LEFT / DOWN / UP / RIGHT":Controls(slot);
        public string Prompt
        {
            get{if(Timing==null)return "GET READY";int i=Enumerable.Range(0,Timing.Count).Where(Timing.Pending).DefaultIfEmpty(-1).First();if(i<0)return "FINAL POSE / WALK TO THE MARK";var n=Timing.Note(i);return new[]{"STRIKE A POSE","RUNWAY STRUT","SPIN","ACCESSORY SHOWCASE","FINAL POSE"}[i%5]+" | "+Lane(n.mask)+" | "+Math.Max(0,n.time-PerformanceTime).ToString("0.0")+"s";}
        }
        static string Lane(int mask)=>string.Join(" + ",Enumerable.Range(0,4).Where(i=>(mask&(1<<i))!=0).Select(i=>new[]{"LEFT","DOWN","UP","RIGHT"}[i]));
        string GradeText(int owner){if(Competition==null||owner>=Competition.Completed)return "JUDGES PREPARING";var g=Competition.Grades[owner];return "THEME "+g.theme+" | COORD "+g.coordination+" | CREATIVE "+g.creativity+" | TIMING "+g.qte+" | POSES "+g.poses+" | FINISH "+g.completion;}
        public Vector3 Queue(int owner)=>new Vector3(-24+owner*16,.7f,-39);
        protected override void BeginGame()
        {
            if(trucks.Length!=4||walkers.Length!=4||catalog==null||!catalog.Validate(out string error)||trucks.Any(t=>t.appearanceHost==null||!t.appearanceHost.Applied))throw new InvalidOperationException("Actual four full rigs, existing appearance hosts and real catalog required.");
            Competition=new FashionCompetition(4);if(people!=null)people.ResetPresentation();Phase=FashionPhase.Hub;ActiveOwner=0;Clock=PerformanceTime=phaseAge=0;NaturalRunwayEntries=CompletedWalks=SavedOutfits=0;Timing=null;configured=true;resumeAge=0;lastReaction=-10;cameraShot=-1;if(music!=null){music.volume=settings.music;music.Play();}preparedOutfit=null;hits.Clear();held=0;
            Array.Clear(OutfitReady,0,4);Array.Clear(aiEquipped,0,4);Array.Clear(aiShopAge,0,4);Array.Clear(FrozenOutfitIds,0,4);
            for(int i=0;i<4;i++)walkers[i].Begin(Queue(i),Quaternion.identity);CreateRouters();
        }
        public bool ReadyOutfit(int owner)
        {
            if(!Live||Phase!=FashionPhase.Hub||WardrobeOpen||owner<0||owner>=4)return false;OutfitReady[owner]=true;return true;
        }
        protected override void Simulate(float dt)
        {
            if(!configured||WardrobeOpen)return;if(resumeAge>0){resumeAge=Mathf.Max(0,resumeAge-dt);return;}Clock+=dt;phaseAge+=dt;
            if(Phase==FashionPhase.Hub)
            {
                for(int i=0;i<4;i++)
                {
                    if(setup.players[i].source==BowlingSource.AI)
                    {
                        if(OutfitReady[i]){walkers[i].Idle(Clock);continue;}
                        Vector3 destination=shops[i].position;destination.y=.7f;
                        if(walkers[i].WalkTo(destination,dt,Clock)){aiShopAge[i]+=dt;if(!aiEquipped[i])aiEquipped[i]=EquipAi(i);if(aiEquipped[i]&&aiShopAge[i]>1)ReadyOutfit(i);}
                    }
                    else{walkers[i].Drive(commands[i],dt,Clock);if(actions[i]){if(NearShop(i)){OpenDressing(i);return;}if(Vector3.ProjectOnPlane(walkers[i].Position-entrance.position,Vector3.up).magnitude<8)ReadyOutfit(i);}}
                }
                if(OutfitReady.All(v=>v)||Clock>=hubSeconds){for(int i=0;i<4;i++){OutfitReady[i]=true;FrozenOutfitIds[i]=string.Join(",",ActualOutfit(i).Select(item=>item.id));}Change(FashionPhase.Lineup);}
                return;
            }
            if(Phase==FashionPhase.Lineup)
            {
                bool ready=true;for(int i=0;i<4;i++)ready&=walkers[i].WalkTo(Queue(i),dt,Clock);if(ready)Change(FashionPhase.Entrance);return;
            }
            for(int i=0;i<4;i++)if(i!=ActiveOwner)walkers[i].Idle(Clock);
            if(Phase==FashionPhase.Entrance)
            {
                bool arrived;
                if(setup.players[ActiveOwner].source==BowlingSource.AI)arrived=walkers[ActiveOwner].WalkTo(entrance.position,dt,Clock);
                else{walkers[ActiveOwner].Drive(commands[ActiveOwner],dt,Clock);arrived=Vector3.ProjectOnPlane(walkers[ActiveOwner].Position-entrance.position,Vector3.up).magnitude<1.5f;}
                if(arrived){NaturalRunwayEntries++;BeginPerformance();}return;
            }
            if(Phase==FashionPhase.Performance)
            {
                PerformanceTime+=dt;JudgeInput(PerformanceTime);walkers[ActiveOwner].WalkTo(finalMark.position,dt*.7f,Clock);
                if(PerformanceTime>chart[chart.Length-1].time+.7)
                {
                    bool final=Vector3.ProjectOnPlane(walkers[ActiveOwner].Position-finalMark.position,Vector3.up).magnitude<1;
                    if(!final&&PerformanceTime<35)return;
                    if(final)CompletedWalks++;int successful=Timing.Count-Timing.Miss;
                    var grade=FashionRules.Judge(Theme,preparedOutfit,successful,Timing.Count,Timing.Score,chart.Sum(n=>1000+(int)Math.Round(n.duration*100)),final);
                    if(!Competition.Lock(ActiveOwner,grade))throw new InvalidOperationException("One actual runway turn may be judged only once.");
                    ConfirmScore("fashion.judge."+ActiveOwner,ActiveOwner,grade.Total,grade.Total,"ACTUAL OUTFIT / TIMED RUNWAY",SportsScoreTier.Major,"PTS",roundEnded:true);if(people!=null)people.React(grade.qte>=400,Clock);DisposeRouters();Change(FashionPhase.Judging);
                }
                return;
            }
            if(Phase==FashionPhase.Judging){walkers[ActiveOwner].Face(Quaternion.identity,dt,Clock);if(phaseAge>4)Change(FashionPhase.Exit);return;}
            if(Phase==FashionPhase.Exit)
            {
                float bayZ=32-ActiveOwner*16;Vector3 exit=exitLeg==0?new Vector3(20,.7f,finalMark.position.z):exitLeg==1?new Vector3(20,.7f,bayZ):new Vector3(40,.7f,bayZ);
                if(!walkers[ActiveOwner].WalkTo(exit,dt,Clock))return;if(exitLeg++<2)return;
                if(ActiveOwner==3){Change(FashionPhase.Complete);EndMatch();return;}ActiveOwner++;Change(FashionPhase.Entrance);CreateRouters();
            }
        }
        void Change(FashionPhase phase){Phase=phase;phaseAge=0;exitLeg=0;cameraShot=-1;Gate();foreach(var r in routers)if(r!=null)r.Gate();}
        void BeginPerformance()
        {
            preparedOutfit=ActualOutfit(ActiveOwner);PerformanceTime=0;held=0;judged=0;hits.Clear();chart=Enumerable.Range(0,12).Select(i=>new DanceNote(2+i*1.45,1<<(i%4),i%5==1?.35:0)).ToArray();Timing=new DanceRules(chart,.12,.28);
            Timing.Judged+=(note,grade)=>{lastReaction=Clock;walkers[ActiveOwner].Strike(note,grade!=DanceGrade.Miss,Clock);if(people!=null)people.React(grade!=DanceGrade.Miss,Clock);};
            if(setup.players[ActiveOwner].source==BowlingSource.AI)
            {
                aiRandom=new System.Random(15150+option*101+ActiveOwner*7919+(int)setup.players[ActiveOwner].difficulty*53);int difficulty=(int)setup.players[ActiveOwner].difficulty;double miss=difficulty==0?.22:difficulty==1?.1:.025,spread=difficulty==0?.25:difficulty==1?.16:.07;
                foreach(var note in chart){if(aiRandom.NextDouble()<miss)continue;double at=note.time+(aiRandom.NextDouble()*2-1)*spread;hits.Add(new Hit(at,note.mask,true));hits.Add(new Hit(Math.Max(at+.12,note.time+note.duration+.03),note.mask,false));}hits.Sort((a,b)=>a.time.CompareTo(b.time));
            }
            if(people!=null)people.React(preparedOutfit.Any(item=>(item.tags&FashionRules.ThemeTag(Theme))!=0),Clock);Change(FashionPhase.Performance);
        }
        void Receive(int owner,double at,int bits,bool down)
        {
            if(!Live||Phase!=FashionPhase.Performance||owner!=ActiveOwner||WardrobeOpen)return;double time=Math.Max(judged,PerformanceTime+at-Time.realtimeSinceStartupAsDouble);hits.Add(new Hit(time,bits,down));hits.Sort((a,b)=>a.time.CompareTo(b.time));
        }
        void JudgeInput(double now)
        {
            while(hits.Count>0&&hits[0].time<=now){var h=hits[0];hits.RemoveAt(0);double at=Math.Max(judged,h.time);int presses=h.down?h.mask&~held:0;held=h.down?held|h.mask:held&~h.mask;Timing.Step(at,presses,held);judged=at;}if(now>=judged){Timing.Step(now,0,held);judged=now;}
        }
        void CreateRouters(bool preserveEvents=false){DisposeRouters(!preserveEvents);if(!configured)return;for(int i=0;i<4;i++)if(setup.players[i].source!=BowlingSource.AI&&Ready(i)){int owner=i;routers[i]=new DanceInputRouter(setup.players[i],(at,bits,down)=>Receive(owner,at,bits,down));}}
        void DisposeRouters(bool clearEvents=true){for(int i=0;i<4;i++){if(routers[i]!=null)routers[i].Dispose();routers[i]=null;}if(clearEvents){held=0;hits.Clear();}}
        protected override void SetPaused(bool paused){if(paused){if(music!=null)music.Pause();foreach(var r in routers)if(r!=null)r.Gate();}else{if(music!=null)music.UnPause();CreateRouters(true);if(setup.players[ActiveOwner].source!=BowlingSource.AI){held=0;hits.Clear();}resumeAge=1;Gate();}}
        protected override void Present(float dt)
        {
            if(!configured)return;foreach(var r in routers)if(r!=null)r.Tick(Time.unscaledDeltaTime);if(!Live||WardrobeOpen)return;
            int shot=Phase==FashionPhase.Hub||Phase==FashionPhase.Lineup?0:Phase==FashionPhase.Judging?5:Phase==FashionPhase.Performance?1+(Mathf.FloorToInt(PerformanceTime/5)%3):1;
            if(Phase==FashionPhase.Performance&&Timing!=null&&Timing.Resolved>0&&Timing.Resolved%4==0&&Clock-lastReaction<.55f&&chart.Where(n=>n.time>PerformanceTime).All(n=>n.time-PerformanceTime>.8))shot=4;
            if(Phase==FashionPhase.Judging&&phaseAge>1.3f)shot=1;
            Vector3 p=walkers[ActiveOwner].Position+Vector3.up*2.4f,target=p,eye=p+new Vector3(0,8,16);
            if(shot==0){target=new Vector3(0,0,-7);eye=new Vector3(0,65,-73);}else if(shot==2)eye=p+new Vector3(13,5,1);else if(shot==3){target=p+Vector3.up*1.3f;eye=p+new Vector3(6,4,9);}else if(shot==4){target=new Vector3(-18,2,25);eye=new Vector3(-16,7,40);}else if(shot==5){target=new Vector3(17,3,35);eye=new Vector3(6,8,28);}
            float lens=shot==0?60:48;Vector3 offset=eye-target;
            if(shot==0&&gallery!=null&&gallery.TryHubShot(out var hubFocus,out var hubOffset,out var hubLens)){target=hubFocus;offset=hubOffset;lens=hubLens;}
            broadcast.Pose(target,offset,lens,shot!=cameraShot);cameraShot=shot;
        }
        public override void ExtraAction(string key)
        {
            if(key.StartsWith("fashion.ready.")&&int.TryParse(key.Substring(14),out int owner)&&owner>=0&&owner<4&&setup.players[owner].source!=BowlingSource.AI)ReadyOutfit(owner);
            else if(key.StartsWith("fashion.dress.")&&int.TryParse(key.Substring(14),out owner))OpenDressing(owner);
        }
        protected override void StopGame(){configured=false;if(music!=null)music.Stop();DisposeRouters();ReleaseWardrobe();if(walkers!=null)foreach(var w in walkers)if(w!=null)w.Stop();}
        protected override void StopSounds(){if(trucks!=null)foreach(var t in trucks)if(t!=null)t.StopSound();}
        protected override void OnDestroy(){StopGame();base.OnDestroy();}
    }
}
