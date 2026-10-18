using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LWS.TruckBowling {
public enum BowlingMode {Simultaneous,HotSeat}
public enum BowlingSource {AI,Keyboard,WholeController,LeftStick,RightStick}
public enum BowlingDifficulty {Easy,Normal,Hard}
[Serializable]public sealed class BowlingPlayer : LWS.TruckSports.ISportsParticipantSlot {
 public string participantId;
 string LWS.TruckSports.ISportsParticipantSlot.ParticipantId {get=>participantId;set=>participantId=value;}
 string LWS.TruckSports.ISportsParticipantSlot.DisplayName {get=>name;set=>name=value;}
 Color LWS.TruckSports.ISportsParticipantSlot.ParticipantColor {get=>color;set=>color=value;}
 bool LWS.TruckSports.ISportsParticipantSlot.IsBot=>source==BowlingSource.AI;

 public string name="Player";public Color color=Color.cyan;public BowlingSource source;public BowlingDifficulty difficulty=BowlingDifficulty.Normal;public Key forward,left,back,right;
 [NonSerialized]public Gamepad pad;[NonSerialized]public bool released;[NonSerialized]public BowlingScore score;
 public bool Controller=>source>=BowlingSource.WholeController;
}
[Serializable]public sealed class BowlingSetup {public BowlingMode mode;public int count=2;public bool sharedHotSeat=true,reducedMotion;public BowlingPlayer[] players=Defaults();
 public static BowlingPlayer[] Defaults(){Key[,] keys={{Key.W,Key.A,Key.S,Key.D},{Key.UpArrow,Key.LeftArrow,Key.DownArrow,Key.RightArrow},{Key.T,Key.F,Key.G,Key.H},{Key.I,Key.J,Key.K,Key.L},{Key.Z,Key.X,Key.C,Key.V},{Key.Numpad8,Key.Numpad4,Key.Numpad5,Key.Numpad6},{Key.Digit8,Key.Digit4,Key.Digit5,Key.Digit6},{Key.Home,Key.Delete,Key.End,Key.PageDown}};Color[] colors={new Color(.1f,.8f,1),new Color(1,.6f,.15f),new Color(.4f,1,.4f),new Color(1,.35f,.65f),new Color(.65f,.45f,1),new Color(1,.9f,.25f),new Color(.2f,1,.85f),new Color(.95f,.65f,.4f)};var result=new BowlingPlayer[8];for(int i=0;i<8;i++)result[i]=new BowlingPlayer{name="Player "+(i+1),color=colors[i],source=i==0?BowlingSource.Keyboard:BowlingSource.AI,forward=keys[i,0],left=keys[i,1],back=keys[i,2],right=keys[i,3]};return result;}
 public bool Validate(out string reason){for(int i=0;i<count;i++){var a=players[i];if(a.source==BowlingSource.Keyboard){Key[] keys={a.forward,a.left,a.back,a.right};for(int k=0;k<4;k++){if(keys[k]==Key.None||keys[k]==Key.Escape){reason="Escape is reserved; gameplay keys must be assigned.";return false;}for(int l=0;l<k;l++)if(keys[l]==keys[k]){reason="Duplicate key within slot "+(i+1);return false;}if(mode==BowlingMode.Simultaneous)for(int j=0;j<i;j++){var p=players[j];if(p.source==BowlingSource.Keyboard&&(keys[k]==p.forward||keys[k]==p.left||keys[k]==p.back||keys[k]==p.right)){reason="Keyboard clusters overlap: slots "+(j+1)+" / "+(i+1);return false;}}}}
  if(a.Controller&&a.pad!=null&&mode==BowlingMode.Simultaneous)for(int j=0;j<i;j++){var p=players[j];if(p.Controller&&p.pad==a.pad&&(p.source==a.source||p.source==BowlingSource.WholeController||a.source==BowlingSource.WholeController)){reason="Controller source already owned by slot "+(j+1);return false;}}}reason="";return true;}
}
}