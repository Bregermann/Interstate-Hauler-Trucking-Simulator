using System;
using System.Linq;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckGuessThat
{
    public sealed class GuessMatch : SportsEventSession
    {
        public GuessIdentity[] identities=Array.Empty<GuessIdentity>();public GuessTile[] tiles=Array.Empty<GuessTile>();
        public SportsEventCamera boardCamera;public Transform[] boardFocus=Array.Empty<Transform>();
        public TMP_Text questionLabel,answerLabel,privateLabel;public GameObject questionPanel,privacyCurtain;
        public GuessRules Rules { get; private set; }
        public int SelectedQuestion { get; private set; }
        public int SelectedIdentity { get; private set; }
        public bool Guessing { get; private set; }
        public bool Handoff { get; private set; }
        public string LastAnswer { get; private set; }="";
        float timer,navDelay;int lastOwner=-1;
        public override string Title=>"GUESS THAT TRUCK!";
        public override string Help=>"Each rival has one secret truck from the same 24 real catalog identities. On your turn ask a truthful yes/no body, hat, face, mirror or wheel question; incompatible tiles fold down. Your board tracks the next player's secret. First correct identification wins. An incorrect guess removes that identity and ends your turn. LEFT/RIGHT chooses a question or truck, UP toggles ASK/GUESS, ACTION confirms. DOWN briefly reveals only your own secret behind the privacy curtain. Release all controls at handoff, then the incoming owner confirms before their board opens. AI chooses balanced questions from public candidates and received answers only. Secrets never appear in broadcast HUD or standings. One human can play against AI; up to four independent local owners take turns.";
        public override int MinimumPlayers=>2;public override int MaximumPlayers=>4;public override int OptionCount=>1;public override int WeatherCount=>1;
        public override string OptionLabel=>"24 DISTINCT TRUCKS";
        public override bool BroadcastStandingsVisible=>false;
        public override bool BroadcastFramingApplicable=>false;public override bool CompactBroadcastStatus=>true;
        public override string Header=>Rules==null?Title:Rules.Winner>=0?setup.players[Rules.Winner].name+" SOLVED IT":setup.players[Rules.Turn].name+" → "+setup.players[Rules.Target].name;
        public override string Status=>Handoff?"PRIVATE HANDOFF · INCOMING OWNER CONFIRM":LastAnswer;
        public override string Standings=>Rules==null?"":string.Join("\n",Enumerable.Range(0,setup.count).Select(i=>setup.players[i].name+" · "+Rules.PublicCandidates(i).Length+" CANDIDATES"));
        public override string Results=>Rules==null?"":Rules.Winner<0?Standings:setup.players[Rules.Winner].name+" IDENTIFIED THE SECRET TRUCK\n"+Rules.Turns+" truthful turns";
        public override bool TryReadScoreCard(int card,out string score,out string detail){if(card<0||card>=setup.count){score=detail=null;return false;}score=Rules==null?"24":Rules.PublicCandidates(card).Length.ToString();detail="CANDIDATES";return card>=0&&card<setup.count;}
        protected override void BeginGame(){Rules=new GuessRules(identities,setup.count,MatchId.GetHashCode());LastAnswer="";SelectedIdentity=SelectedQuestion=0;Guessing=false;timer=navDelay=0;lastOwner=-1;foreach(var tile in tiles)tile.SetFolded(false,true);EnterTurn();}
        void EnterTurn(){timer=0;Guessing=Rules.PublicCandidates(Rules.Turn).Length==1;SelectedIdentity=Rules.PublicCandidates(Rules.Turn)[0];SelectedQuestion=0;AdvanceQuestion(1);Handoff=setup.players[Rules.Turn].source!=BowlingSource.AI;ConsumeActions();UpdateBoards();}
        public bool Ask(int question){if(!Live||Handoff||Rules==null)return false;if(!Rules.Ask(Rules.Turn,question,out var answer))return false;LastAnswer=(answer.yes?"YES":"NO")+" · "+Readable(Rules.Questions[question])+" · "+answer.remaining+" REMAIN";timer=0;UpdateBoards();feedback.Play(2,0,true);return true;}
        public bool Identify(int identity){if(!Live||Handoff||Rules==null||!Rules.Guess(Rules.Turn,identity,out bool correct))return false;LastAnswer=correct?"CORRECT · "+identities[identity].name:"NOT "+identities[identity].name;timer=0;UpdateBoards();if(correct){ConfirmScore("guess.solved",Rules.Turn,1,1,"CORRECT IDENTIFICATION",SportsScoreTier.MatchChampion,matchEnded:true);feedback.Play(4,0,true);}return true;}
        public void ConfirmHandoff(){if(!Live||!Handoff)return;Handoff=false;ConsumeActions();timer=0;}
        public void Navigate(int direction){if(Handoff||Rules.AwaitingNext)return;if(Guessing){for(int n=0;n<24;n++){SelectedIdentity=(SelectedIdentity+direction+24)%24;if(Rules.Candidate(Rules.Turn,SelectedIdentity))break;}}else AdvanceQuestion(direction);}
        void AdvanceQuestion(int direction){for(int n=0;n<Rules.Questions.Length;n++){SelectedQuestion=(SelectedQuestion+direction+Rules.Questions.Length)%Rules.Questions.Length;if(Rules.CanAsk(Rules.Turn,SelectedQuestion))break;}}
        public override void ExtraAction(string key){if(!Live||Rules==null||setup.players[Rules.Turn].source==BowlingSource.AI)return;switch(key){case "guess.previous":Navigate(-1);break;case "guess.next":Navigate(1);break;case "guess.mode":if(!Handoff&&!Rules.AwaitingNext)Guessing=!Guessing;break;case "guess.confirm":if(Handoff)ConfirmHandoff();else if(Guessing)Identify(SelectedIdentity);else Ask(SelectedQuestion);break;}}
        protected override void Simulate(float dt){timer+=dt;navDelay-=dt;int seat=Rules.Turn;if(Rules.Winner>=0){if(timer>2.5f)EndMatch();return;}if(Rules.AwaitingNext){if(timer>2.8f){Rules.Next();EnterTurn();}return;}if(setup.players[seat].source==BowlingSource.AI){if(timer<1.3f)return;int[] choices=Rules.PublicCandidates(seat);int question=GuessDeduction.BestQuestion(identities,Rules.Questions,choices,q=>Rules.CanAsk(seat,q));if(choices.Length==1||question<0)Identify(choices[0]);else Ask(question);return;}if(Handoff){if(actions[seat])ConfirmHandoff();return;}var drive=commands[seat];if(navDelay<=0){if(Mathf.Abs(drive.x)>.5f){Navigate(drive.x>0?1:-1);navDelay=.22f;}else if(drive.y>.5f){Guessing=!Guessing;navDelay=.35f;}}if(actions[seat]){if(Guessing)Identify(SelectedIdentity);else Ask(SelectedQuestion);}}
        void UpdateBoards(){foreach(var tile in tiles)tile.SetFolded(tile.owner>=setup.count||!Rules.Candidate(tile.owner,tile.identity));}
        public static string Readable(GuessQuestion question)=>question.label.Replace("semi.","").Replace("truck.","").Replace("novelty.","").Replace("hat.","").Replace("eyes.","").Replace("mouth.","").Replace("mirrors.","").Replace("wheels.","").Replace('.', ' ').ToUpperInvariant();
        protected override void Present(float dt){if(Rules==null)return;int owner=Rules.Turn;bool secret=Live&&!Handoff&&!Rules.AwaitingNext&&setup.players[owner].source!=BowlingSource.AI&&commands[owner].y<-.5f;
            if(privacyCurtain!=null)privacyCurtain.SetActive(Handoff||secret);if(questionPanel!=null)questionPanel.SetActive(Running&&!Finished&&!Paused&&!Handoff&&!secret);
            if(privateLabel!=null){privateLabel.gameObject.SetActive(secret||Handoff);privateLabel.text=secret?"YOUR SECRET · "+identities[Rules.OwnerSecret(owner)].name:"PASS TO "+setup.players[owner].name+"\nRELEASE CONTROLS, THEN CONFIRM";}
            if(questionLabel!=null)questionLabel.text=Guessing?"IDENTIFY · "+identities[SelectedIdentity].name:"ASK · "+Readable(Rules.Questions[SelectedQuestion]);if(answerLabel!=null)answerLabel.text=LastAnswer;
            for(int s=0;s<boardFocus.Length;s++)if(boardFocus[s]!=null)boardFocus[s].gameObject.SetActive(s==owner&&!Handoff&&!secret);
            if(boardCamera!=null&&owner<boardFocus.Length){boardCamera.Pose(boardFocus[owner].position+Vector3.up*8,new Vector3(0,6,-31),48,lastOwner!=owner);lastOwner=owner;}}
        protected override void SetPaused(bool paused){foreach(var tile in tiles)tile.Pause(paused);}
        protected override void StopGame(){foreach(var tile in tiles)tile.SetFolded(false,true);Handoff=false;if(privateLabel!=null)privateLabel.gameObject.SetActive(false);if(privacyCurtain!=null)privacyCurtain.SetActive(false);}
    }
}
