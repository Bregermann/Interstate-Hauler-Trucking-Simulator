using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckSports {
// Party identities are preferences, independent of a career save, team, device or seat.
public interface ISportsParticipantSlot {
 string ParticipantId {get;set;}
 string DisplayName {get;set;}
 Color ParticipantColor {get;set;}
 bool IsBot {get;}
}
[Serializable] public sealed class SportsParticipant {
 public string id,displayName,preferredTruck="",origin="";
 public Color color=Color.cyan;
 public bool preferredAI;
}
[Serializable] public sealed class SportsLegacySetup {
 public string key,json;
}
[Serializable] public sealed class SportsMatchReceipt {
 public string matchId,mode,completedUtc,summary;
 public string[] participantIds,names;
}
[Serializable] public sealed class SportsParticipantData {
 public int version=1;
 public List<SportsParticipant> participants=new List<SportsParticipant>();
 public List<string> roster=new List<string>();
 public List<SportsLegacySetup> legacy=new List<SportsLegacySetup>();
 public List<SportsMatchReceipt> results=new List<SportsMatchReceipt>();
}
public static class SportsParticipantRegistry {
 public const string Key="TruckSports.Participants.v1";
 const string BackupKey=Key+".Backup";
 static SportsParticipantData data;
 static string scope;
 static bool readOnly;
 public static string Notice {get;private set;}="";
 public static event Action Changed;
 static string Scope=>SportsPrefs.StorageScope;
 public static SportsParticipantData Current {get{Load();return data;}}
 public static void ForgetCache(){data=null;scope=null;Notice="";readOnly=false;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetRuntime()=>ForgetCache();
 static bool Decode(string json,out SportsParticipantData value){
  value=null;if(string.IsNullOrWhiteSpace(json))return false;
  try{value=JsonUtility.FromJson<SportsParticipantData>(json);var candidate=value;return candidate!=null&&candidate.version==1&&candidate.participants!=null&&candidate.roster!=null&&candidate.legacy!=null&&candidate.results!=null&&candidate.participants.All(p=>p!=null&&!string.IsNullOrWhiteSpace(p.id))&&candidate.participants.Select(p=>p.id).Distinct().Count()==candidate.participants.Count&&candidate.roster.All(id=>candidate.participants.Any(p=>p.id==id))&&candidate.roster.Distinct().Count()==candidate.roster.Count;}catch(Exception){return false;}
 }
 static void Load(){
  if(data!=null&&scope==Scope)return;scope=Scope;readOnly=false;Notice="";
  string json=SportsPrefs.GetString(Key);
  if(Decode(json,out data))return;
  // Never overwrite a newer schema with an older game executable.
  if(!string.IsNullOrEmpty(json)){try{var header=JsonUtility.FromJson<SportsParticipantData>(json);if(header!=null&&header.version>1){data=new SportsParticipantData();readOnly=true;Notice="Participant data is from a newer game version. Changes are temporary.";Debug.LogWarning(Notice);return;}}catch(Exception){} }
  if(Decode(SportsPrefs.GetString(BackupKey),out data)){
   PreserveUnreadable(json);Notice="Participant preferences recovered from their previous saved copy.";Debug.LogWarning(Notice);Persist();return;
  }
  PreserveUnreadable(json);data=Migrate(ReadLegacy());Persist();
 }
 static void PreserveUnreadable(string json){if(!string.IsNullOrEmpty(json)){SportsPrefs.SetString(Key+".Recovered."+DateTime.UtcNow.Ticks,json);Notice="Unreadable participant preferences were preserved; legacy settings remain available.";Debug.LogWarning(Notice);}}
 static IEnumerable<SportsLegacySetup> ReadLegacy(){
  var keys=new SortedSet<string>(StringComparer.Ordinal){"TruckBilliards.Setup.v1","TruckBilliards.Setup.v2","TruckHockey.Setup.v1","TruckHockey.Setup.v2","TruckAirHockey.Setup.v1","TruckWallball.Setup.v1","TruckBowling.Setup.v1","TruckCornhole.Setup.v1","TruckCheese.Setup.v1","TruckCheeseRolling.Setup.v1"};
  foreach(var type in typeof(SportsEventSession).Assembly.GetTypes())if(!type.IsAbstract&&typeof(SportsEventSession).IsAssignableFrom(type))keys.Add(type.Name+".Setup.v1");
  foreach(string key in keys)if(SportsPrefs.HasKey(key))yield return new SportsLegacySetup{key=key,json=SportsPrefs.GetString(key)};
 }
 [Serializable] sealed class LegacySlot {public string name,participantId;public Color color;public int source,control;}
 [Serializable] sealed class LegacyRoster {public LegacySlot[] players;}
 [Serializable] sealed class LegacyEnvelope {public LegacySlot[] players,slots;public LegacyRoster setup;}
 static bool CustomName(string name,int slot){if(string.IsNullOrWhiteSpace(name))return false;return name!="Player"&&name!="Player "+(slot+1)&&name!="Racer "+(slot+1)&&name!="Blue"&&name!="Orange"&&name!="You"&&name!="AI";}
 static Color ImportedColor(Color value,Color fallback)=>value.a>0&&!float.IsNaN(value.r)&&!float.IsNaN(value.g)&&!float.IsNaN(value.b)?value:fallback;
 // Sorted source-key precedence, first custom name per seat wins. Every conflicting
 // name becomes a selectable profile; original JSON is retained without rewriting.
 public static SportsParticipantData Migrate(IEnumerable<SportsLegacySetup> sources){
  var result=new SportsParticipantData();var chosen=new HashSet<int>();var initialized=new HashSet<int>();
  foreach(var source in sources.OrderBy(s=>LegacyPriority(s.key)).ThenBy(s=>s.key,StringComparer.Ordinal)){
   result.legacy.Add(new SportsLegacySetup{key=source.key,json=source.json});
   LegacySlot[] slots=null;try{var e=JsonUtility.FromJson<LegacyEnvelope>(source.json);slots=e?.setup?.players?.Length>0?e.setup.players:e?.slots?.Length>0?e.slots:e?.players;}catch(Exception){}
   if(slots==null)continue;
   for(int i=0;i<slots.Length;i++){
    Ensure(result,i+1);var s=slots[i];if(s==null)continue;
    var existing=result.participants.Find(p=>p.id==result.roster[i]);
    Color color=ImportedColor(s.color,existing.color);
    if(initialized.Add(i)){existing.color=color;existing.preferredAI=source.key.StartsWith("TruckBilliards",StringComparison.Ordinal)?s.control==3:s.source==0;}
    if(!CustomName(s.name,i))continue;
    if(!chosen.Contains(i)){existing.displayName=s.name;existing.color=color;existing.preferredAI=source.key.StartsWith("TruckBilliards",StringComparison.Ordinal)?s.control==3:s.source==0;existing.origin=source.key+" / seat "+(i+1);chosen.Add(i);}
    else if(existing.displayName!=s.name)result.participants.Add(new SportsParticipant{id=Guid.NewGuid().ToString("N"),displayName=s.name,color=color,preferredAI=source.key.StartsWith("TruckBilliards",StringComparison.Ordinal)?s.control==3:s.source==0,origin=source.key+" / seat "+(i+1)});
   }
  }
  Ensure(result,12);return result;
 }
 static int LegacyPriority(string key)=>key.EndsWith(".v2",StringComparison.Ordinal)?0:key=="TruckBilliards.Setup.v1"||key=="TruckHockey.Setup.v1"||key=="TruckCheese.Setup.v1"?2:1;
 static void Ensure(SportsParticipantData target,int count){
  while(target.roster.Count<count){int i=target.roster.Count;var p=new SportsParticipant{id=Guid.NewGuid().ToString("N"),displayName="Player "+(i+1),color=Color.HSVToRGB(Mathf.Repeat(i*.618034f,1),.7f,1),preferredAI=i!=0};target.participants.Add(p);target.roster.Add(p.id);}
 }
 public static SportsParticipant Default(int seat){Load();if(seat<0)throw new ArgumentOutOfRangeException(nameof(seat));if(data.roster.Count<=seat){Ensure(data,seat+1);Persist();}return Find(data.roster[seat]);}
 public static SportsParticipant Find(string id){Load();return data.participants.Find(p=>p.id==id);}
 public static bool Rename(string id,string name,Color color,bool ai){var p=Find(id);if(p==null)return false;name=string.IsNullOrWhiteSpace(name)?p.displayName:name.Trim();if(p.displayName==name&&p.color==color&&p.preferredAI==ai)return false;p.displayName=name;p.color=color;p.preferredAI=ai;Persist();Changed?.Invoke();return true;}
 public static bool UseInSeat(int seat,string id){Load();if(Find(id)==null||seat<0)return false;Ensure(data,seat+1);int previous=data.roster.IndexOf(id);string old=data.roster[seat];if(previous>=0)data.roster[previous]=old;data.roster[seat]=id;Persist();Changed?.Invoke();return true;}
 public static SportsParticipant Create(string name){Load();var p=new SportsParticipant{id=Guid.NewGuid().ToString("N"),displayName=string.IsNullOrWhiteSpace(name)?"New driver":name.Trim(),color=Color.cyan};data.participants.Add(p);Persist();Changed?.Invoke();return p;}
 public static string Totals(ISportsParticipantSlot[] slots,int count,Func<int,int> score){
  if(slots==null||score==null||count<0||count>slots.Length)throw new ArgumentException("Result participants must match the active roster.");
  var lines=new string[count];for(int i=0;i<count;i++)lines[i]=slots[i].DisplayName+"="+score(i);return string.Join("; ",lines);
 }
 public static bool Record(string matchId,string mode,string summary,ISportsParticipantSlot[] slots,int count){
  Load();if(string.IsNullOrWhiteSpace(matchId)||data.results.Any(r=>r.matchId==matchId))return false;
  count=Math.Min(count,slots.Length);var ids=new string[count];var names=new string[count];for(int i=0;i<count;i++){ids[i]=slots[i].ParticipantId;names[i]=slots[i].DisplayName;}
  data.results.Add(new SportsMatchReceipt{matchId=matchId,mode=mode,completedUtc=DateTime.UtcNow.ToString("O"),summary=summary??"",participantIds=ids,names=names});
  if(data.results.Count>128)data.results.RemoveAt(0);Persist();return true;
 }
 static void Persist(){
  if(readOnly)return;string previous=SportsPrefs.GetString(Key);if(Decode(previous,out _))SportsPrefs.SetString(BackupKey,previous);SportsPrefs.SetString(Key,JsonUtility.ToJson(data));SportsPrefs.Save();
 }
}
// Explicit load/save boundary. It never reads or writes controller assignments.
public sealed class SportsParticipantBinding {
 readonly ISportsParticipantSlot[] slots;readonly string[] names;readonly Color[] colors;readonly bool[] bots;
 public SportsParticipantBinding(ISportsParticipantSlot[] slots){this.slots=slots;names=new string[slots.Length];colors=new Color[slots.Length];bots=new bool[slots.Length];Refresh(false);}
 public bool Matches(ISportsParticipantSlot[] value)=>ReferenceEquals(slots,value);
 public void Refresh(bool saveEdits){
  if(saveEdits)for(int i=0;i<slots.Length;i++){var s=slots[i];if(s==null)continue;if(names[i]!=s.DisplayName||colors[i]!=s.ParticipantColor||bots[i]!=s.IsBot)SportsParticipantRegistry.Rename(s.ParticipantId,s.DisplayName,s.ParticipantColor,s.IsBot);}
  for(int i=0;i<slots.Length;i++){var s=slots[i];if(s==null)continue;var p=SportsParticipantRegistry.Default(i);s.ParticipantId=p.id;s.DisplayName=p.displayName;s.ParticipantColor=p.color;names[i]=s.DisplayName;colors[i]=s.ParticipantColor;bots[i]=s.IsBot;}
 }
}
}
