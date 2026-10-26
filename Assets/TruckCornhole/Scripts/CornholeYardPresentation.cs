using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
namespace LWS.TruckCornhole
{
 // Miniature art convention only: the ORIGINAL 24-unit board represents a
 // 1.2-metre garden board. 20 art units = 1 fictional metre; a proportional
 // 1.8m person is 36 art units tall. Physics, gravity and wind keep their units.
 public sealed class CornholeYardPresentation:MonoBehaviour
 {
  public const float ArtUnitsPerMetre=20,HumanHeightMetres=1.8f;
  [Tooltip("Miniature art convention, not physics scale: 24-unit board = 1.2m; human 1.8m = 36 art units.")]
  public float artUnitsPerMetre=ArtUnitsPerMetre;
  public CornholeMatch match;public Animator[] spectators;public Transform[] yardProps;public Light[] yardLights;
  public int walkingSpectators=8;public int Reactions{get;private set;}
  readonly HashSet<CornholeTruck> holes=new HashSet<CornholeTruck>(),landings=new HashSet<CornholeTruck>();
  Tween[] walks;Vector3[] homes;float reactionUntil;string matchId;int round;bool walkingPaused;
  static readonly int Idle=Animator.StringToHash("Idle"),Walk=Animator.StringToHash("Walk"),Cheer=Animator.StringToHash("Cheer"),React=Animator.StringToHash("React");
  void Start(){Prepare();}
  void Prepare(){if(spectators==null||walks!=null)return;walks=new Tween[Mathf.Min(walkingSpectators,spectators.Length)];homes=new Vector3[spectators.Length];for(int i=0;i<spectators.Length;i++){var actor=spectators[i];homes[i]=actor.transform.position;if(i>=walks.Length){actor.Play(Idle,0,(i*.137f)%1);continue;}var home=homes[i];var forward=Vector3.forward;var right=Vector3.right;var path=new[]{home,home+forward*28,home+forward*28+right*3,home+right*3,home};walks[i]=actor.transform.DOPath(path,34+i*1.7f,DG.Tweening.PathType.Linear).SetEase(Ease.Linear).SetLoops(-1).SetLookAt(.025f,Vector3.forward,Vector3.up).SetLink(gameObject);actor.Play(Walk,0,(i*.193f)%1);}}
  void Update()
  {
   Prepare();if(walks==null||match==null)return;bool paused=match.Paused;
   foreach(var actor in spectators)if(actor!=null)actor.speed=paused?0:1;
   if(matchId!=match.MatchId||round!=match.Round){matchId=match.MatchId;round=match.Round;holes.Clear();landings.Clear();reactionUntil=0;Reactions=0;for(int i=0;i<spectators.Length;i++)spectators[i].CrossFadeInFixedTime(i<walks.Length?Walk:Idle,.2f);}
   if(!paused&&match.InMatch&&!match.Finished)foreach(var course in match.courses)foreach(var truck in course.trucks)
   {
    if(!truck.gameObject.activeInHierarchy)continue;
    if(truck.Holed&&holes.Add(truck))Reaction(true);
    else if((truck.Landed||truck.Excluded||truck.Grounded)&&landings.Add(truck))Reaction(truck.Landed&&!truck.Excluded&&!truck.Grounded);
   }
   bool reacting=Time.time<reactionUntil;
   if(!reacting&&reactionUntil>0){reactionUntil=0;for(int i=0;i<spectators.Length;i++)spectators[i].CrossFadeInFixedTime(i<walks.Length?Walk:Idle,.2f);}
   bool stop=paused||reacting;if(stop!=walkingPaused){walkingPaused=stop;foreach(var tween in walks)if(tween!=null){if(stop)tween.Pause();else tween.Play();}}
  }
  void Reaction(bool positive){Reactions++;reactionUntil=Time.time+2.4f;foreach(var actor in spectators)if(actor!=null)actor.CrossFadeInFixedTime(positive?Cheer:React,.15f);}
  void OnDisable(){if(walks!=null)foreach(var tween in walks)if(tween!=null)tween.Kill();walks=null;homes=null;holes.Clear();landings.Clear();}
 }
}
