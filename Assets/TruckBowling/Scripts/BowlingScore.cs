using System;
using System.Collections.Generic;
using System.Text;
namespace LWS.TruckBowling {
// One score authority. RemainingPins is the legal rack for the NEXT delivery.
public sealed class BowlingScore {
 readonly List<int>[] frames=new List<int>[10];readonly List<int> rolls=new List<int>(21);
 public int Frame{get;private set;}public int Ball{get;private set;}public int RemainingPins{get;private set;}=10;
 public bool Finished{get;private set;}public bool FreshRack{get;private set;}=true;public int Deliveries=>rolls.Count;
 public BowlingScore(){for(int i=0;i<10;i++)frames[i]=new List<int>(3);}
 public IReadOnlyList<int> FrameRolls(int frame)=>frames[frame];
 public void Record(int pins){if(Finished)throw new InvalidOperationException("Bowling game is complete");if(pins<0||pins>RemainingPins)throw new ArgumentOutOfRangeException(nameof(pins));frames[Frame].Add(pins);rolls.Add(pins);FreshRack=false;
  if(Frame<9){if(Ball==0&&pins<10){Ball=1;RemainingPins-=pins;return;}Frame++;Ball=0;RemainingPins=10;FreshRack=true;return;}
  var tenth=frames[9];if(Ball==0){Ball=1;RemainingPins=pins==10?10:10-pins;FreshRack=pins==10;return;}
  if(Ball==1){if(tenth[0]<10&&tenth[0]+pins<10){Finished=true;return;}Ball=2;FreshRack=tenth[0]<10||pins==10;RemainingPins=FreshRack?10:10-pins;return;}Finished=true;
 }
 public int? FrameValue(int frame){var f=frames[frame];if(f.Count==0)return null;int offset=0;for(int i=0;i<frame;i++)offset+=frames[i].Count;
  if(frame==9){bool bonus=f[0]==10||(f.Count>=2&&f[0]+f[1]==10);if(f.Count<(bonus?3:2))return null;int value=0;foreach(int p in f)value+=p;return value;}
  if(f[0]==10){if(rolls.Count<offset+3)return null;return 10+rolls[offset+1]+rolls[offset+2];}if(f.Count<2)return null;if(f[0]+f[1]==10){if(rolls.Count<offset+3)return null;return 10+rolls[offset+2];}return f[0]+f[1];
 }
 public int? Cumulative(int frame){int sum=0;for(int i=0;i<=frame;i++){var value=FrameValue(i);if(!value.HasValue)return null;sum+=value.Value;}return sum;}
 public int ResolvedTotal{get{int total=0;for(int i=0;i<10;i++){var value=FrameValue(i);if(!value.HasValue)break;total+=value.Value;}return total;}}
 public int FinalTotal=>Finished?ResolvedTotal:throw new InvalidOperationException("Bonuses are pending");
 public string Symbols(int frame){var f=frames[frame];var b=new StringBuilder();for(int i=0;i<f.Count;i++){if(i>0)b.Append(' ');int p=f[i];bool spare=i==1&&f[0]<10&&f[0]+p==10||frame==9&&i==2&&f[0]==10&&f[1]<10&&f[1]+p==10;b.Append(spare?"/":p==10?"X":p==0?"−":p.ToString());}return b.Length==0?"·":b.ToString();}
 public string Card(){var b=new StringBuilder();for(int i=0;i<10;i++){if(i>0)b.Append("   |   ");b.Append(i+1).Append(": ").Append(Symbols(i)).Append('\n').Append(Cumulative(i)?.ToString()??"…");}return b.ToString();}
}
}