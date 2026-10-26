using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;

namespace LWS.TruckSports
{
    // One presentation coordinator; the sport remains the sole finish/score/camera authority.
    [DefaultExecutionOrder(720)] public sealed class SportsRaceFinishPresentation:MonoBehaviour
    {
        public SportsRaceFinishSource source;public RaceFinishJugPresentation jugs;
        public ParticleSystem[] fireworks,confetti;public Light[] victoryLights;public AudioSource crowd;public AudioClip cheer;
        readonly List<SportsRaceFinishReceipt> rows=new List<SportsRaceFinishReceipt>(12);
        readonly HashSet<string> seen=new HashSet<string>(),ceremonies=new HashSet<string>();
        readonly HashSet<ParticleSystem> pausedParticles=new HashSet<ParticleSystem>();
        readonly StringBuilder names=new StringBuilder();string matchId,scope;float clock,firstObserved=-1;Sequence lighting;bool paused;
        public int VictoryBursts{get;private set;}public int GagRequests{get;private set;}public string LastWinnerIds{get;private set;}public string LastWinnerName{get;private set;}public float LastWinnerSeconds{get;private set;}
        public void Present(float delta)
        {
            if(source==null||!source.Ready){if(matchId!=null)ResetPresentation();return;}
            if(matchId!=source.session.MatchId){ResetPresentation();matchId=source.session.MatchId;}
            bool pause=source.session.Paused;SetPaused(pause);jugs.Tick(delta,pause);if(pause)return;clock+=Mathf.Clamp(delta,0,.1f);
            for(int gate=0;gate<source.gates.Length;gate++){source.GatePose(gate,out var point,out var rotation,out _);source.gates[gate].root.SetPositionAndRotation(point,rotation);source.gates[gate].root.gameObject.SetActive(!(source.session is LWS.TruckFrogger.FrogMatch)||gate==source.GateIndex);}
            if(scope!=source.Scope){scope=source.Scope;firstObserved=-1;source.ProjectChecks();}
            source.Read(rows);if(rows.Count==0)return;
            if(firstObserved<0)firstObserved=clock;
            // Wait across the adjacent native fixed-step boundary before presenting
            // shared first places. Never crown an array/callback-order winner.
            if(clock-firstObserved<.06f||!source.FinishBatchSettled(rows))return;
            string ceremony=matchId+"|"+scope;
            if(!ceremonies.Contains(ceremony))
            {
                int winners=0;foreach(var row in rows)if(row.winner)winners++;
                if(winners==0)return;ceremonies.Add(ceremony);Celebrate(rows,winners);
            }
            foreach(var row in rows)
            {
                if(!seen.Add(row.Key))continue;
                if(!row.winner&&row.gag){jugs.Enqueue(row);GagRequests++;}
            }
        }
        void LateUpdate(){Present(Time.unscaledDeltaTime);}
        void Celebrate(List<SportsRaceFinishReceipt> finishes,int winners)
        {
            names.Clear();var ids=new StringBuilder();SportsRaceFinishReceipt first=default;
            foreach(var row in finishes)if(row.winner){if(names.Length>0){names.Append(" + ");ids.Append(',');}names.Append(row.name);ids.Append(row.id);first=row;}
            LastWinnerIds=ids.ToString();LastWinnerName=names.ToString();LastWinnerSeconds=first.seconds;VictoryBursts++;
            bool sequential=source.session is LWS.TruckFrogger.FrogMatch;int gate=Mathf.Clamp(sequential?source.GateIndex:first.gate,0,source.gates.Length-1);source.GatePose(gate,out var point,out var facing,out var width);
            var board=source.gates[gate].caption;board.text=(winners>1?"SHARED FIRST":sequential?"CROSSING CHAMPION":"RACE WINNER")+"\n"+LastWinnerName+" · "+(sequential?"RECORDED CLEAR ":"")+first.seconds.ToString("F3")+"s";board.color=first.color;
            for(int i=0;i<fireworks.Length;i++){var effect=fireworks[i];effect.transform.position=point+facing*new Vector3((i%2==0?-1:1)*Mathf.Min(width*.4f,22),14+i*8,6);effect.transform.rotation=Quaternion.identity;foreach(var child in effect.GetComponentsInChildren<ParticleSystem>(true)){var main=child.main;main.startColor=i%2==0?first.color:Color.white;}effect.Clear(true);effect.Play(true);}
            for(int i=0;i<confetti.Length;i++){var effect=confetti[i];effect.transform.position=point+facing*new Vector3((i%2==0?-1:1)*Mathf.Min(width*.35f,16),4,4);effect.Clear(true);effect.Emit(160);}
            lighting?.Kill();lighting=DOTween.Sequence().SetUpdate(true);
            for(int i=0;i<victoryLights.Length;i++){var light=victoryLights[i];light.transform.position=point+facing*new Vector3(i%2==0?-10:10,10,3);light.color=first.color;light.intensity=0;lighting.Join(light.DOIntensity(7,.16f).SetLoops(8,LoopType.Yoyo));}
            if(crowd!=null&&cheer!=null){crowd.transform.position=point;crowd.clip=cheer;crowd.volume=source.session.settings.sfx*.55f;crowd.Play();}
        }
        void SetPaused(bool value)
        {
            if(paused==value)return;paused=value;if(lighting!=null){if(value)lighting.Pause();else lighting.Play();}
            if(value){foreach(var root in fireworks)if(root!=null)foreach(var effect in root.GetComponentsInChildren<ParticleSystem>(true))if(effect.isPlaying){pausedParticles.Add(effect);effect.Pause(false);}foreach(var root in confetti)if(root!=null)foreach(var effect in root.GetComponentsInChildren<ParticleSystem>(true))if(effect.isPlaying){pausedParticles.Add(effect);effect.Pause(false);}}
            else{foreach(var effect in pausedParticles)if(effect!=null)effect.Play(false);pausedParticles.Clear();}
            if(crowd!=null){if(value)crowd.Pause();else crowd.UnPause();}
        }
        public void ResetPresentation()
        {
            lighting?.Kill();lighting=null;if(fireworks!=null)foreach(var effect in fireworks)if(effect!=null)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(confetti!=null)foreach(var effect in confetti)if(effect!=null)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(victoryLights!=null)foreach(var light in victoryLights)if(light!=null)light.intensity=0;if(crowd!=null)crowd.Stop();jugs?.Clear();
            seen.Clear();ceremonies.Clear();pausedParticles.Clear();rows.Clear();matchId=scope=null;firstObserved=-1;clock=0;VictoryBursts=GagRequests=0;LastWinnerIds=LastWinnerName=null;LastWinnerSeconds=0;paused=false;if(source?.gates!=null)foreach(var gate in source.gates)if(gate.caption!=null)gate.caption.text="";
        }
        void OnDisable(){ResetPresentation();}
    }
}
