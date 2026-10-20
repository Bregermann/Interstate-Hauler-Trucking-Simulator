using UnityEngine;
using TMPro;
using Michsky.UI.Heat;
namespace LWS.TruckSports {
// Four large cards per page; the full shared roster and migrated alternatives remain accessible.
public sealed class SportsParticipantsMenu:MonoBehaviour {
 public SportsFrontEnd front;public int page;public TMP_InputField[] names;
 public TMP_Text[] details;public ButtonManager[] profiles,colors,kinds;
 public TMP_Text heading,notice,choiceHeading;public int choicePage;public ButtonManager[] choices;int bank,choiceBank,choiceSeat,returnPage;
 int Seat(int row)=>bank*4+row;
 void Start(){for(int i=0;i<names.Length;i++){int row=i;names[i].onEndEdit.AddListener(value=>{var p=SportsParticipantRegistry.Default(Seat(row));SportsParticipantRegistry.Rename(p.id,value,p.color,p.preferredAI);Refresh();});}}
 public void Open(){returnPage=front.Page;bank=0;Refresh();front.Show(page);}
 public void Action(string action){
  if(action=="peoplechooseback"){front.Show(page);Refresh();return;}
  if(action=="peoplechooseprev"||action=="peoplechoosenext"){int banks=Mathf.CeilToInt(SportsParticipantRegistry.Current.participants.Count/(float)choices.Length);choiceBank=(choiceBank+(action=="peoplechooseprev"?banks-1:1))%banks;RefreshChoices();return;}
  if(action.StartsWith("peoplepick")){int index=choiceBank*choices.Length+int.Parse(action.Substring(10));var all=SportsParticipantRegistry.Current.participants;if(index<all.Count)SportsParticipantRegistry.UseInSeat(choiceSeat,all[index].id);front.Show(page);Refresh();return;}
  if(action=="peopleback"){front.Show(returnPage);return;}
  if(action=="peopleprev"||action=="peoplenext"){int banks=Mathf.CeilToInt(Mathf.Max(12,SportsParticipantRegistry.Current.roster.Count)/4f);bank=(bank+(action=="peopleprev"?banks-1:1))%banks;Refresh();return;}
  int row=action[action.Length-1]-'0';if(row<0||row>=names.Length)return;int seat=Seat(row);var p=SportsParticipantRegistry.Default(seat);
  if(action.StartsWith("peopleprofile")){
   choiceSeat=seat;choiceBank=0;RefreshChoices();front.Show(choicePage);return;
  }else if(action.StartsWith("peoplecolor")){Color.RGBToHSV(p.color,out float h,out _,out _);SportsParticipantRegistry.Rename(p.id,p.displayName,Color.HSVToRGB(Mathf.Repeat(h+.137f,1),.7f,1),p.preferredAI);
  }else if(action.StartsWith("peoplekind"))SportsParticipantRegistry.Rename(p.id,p.displayName,p.color,!p.preferredAI);
  else if(action.StartsWith("peoplenew")){var added=SportsParticipantRegistry.Create("New driver");SportsParticipantRegistry.UseInSeat(seat,added.id);}
  Refresh();
 }
 static void Caption(ButtonManager button,string value){button.buttonText=value;button.UpdateUI();}
 void RefreshChoices(){var all=SportsParticipantRegistry.Current.participants;int banks=Mathf.CeilToInt(all.Count/(float)choices.Length);choiceHeading.text="CHOOSE DRIVER  /  SEAT "+(choiceSeat+1)+(banks>1?"  /  "+(choiceBank+1)+" OF "+banks:"");for(int i=0;i<choices.Length;i++){int at=choiceBank*choices.Length+i;choices[i].gameObject.SetActive(at<all.Count);if(at>=all.Count)continue;var p=all[at];Caption(choices[i],p.displayName+"\nID "+p.id.Substring(0,Mathf.Min(8,p.id.Length)));choices[i].normalTextObj.color=p.color;}foreach(var action in front.pages[choicePage].GetComponentsInChildren<SportsMenuAction>(true))if(action.action=="peoplechooseprev"||action.action=="peoplechoosenext")action.gameObject.SetActive(banks>1);}
 public void Refresh(){
  heading.text="YOUR CREW  /  "+(bank*4+1)+"–"+(bank*4+4);
  for(int i=0;i<names.Length;i++){var p=SportsParticipantRegistry.Default(Seat(i));names[i].SetTextWithoutNotify(p.displayName);names[i].textComponent.color=p.color;Caption(profiles[i],"CHOOSE DRIVER");Caption(colors[i],"COLOR");colors[i].normalTextObj.color=p.color;Caption(kinds[i],p.preferredAI?"AI PREFERENCE":"HUMAN PREFERENCE");details[i].text="SEAT "+(Seat(i)+1)+"  ·  ID "+p.id.Substring(0,Mathf.Min(8,p.id.Length))+(string.IsNullOrEmpty(p.origin)?"":"\nImported: "+p.origin);}
  notice.text=string.IsNullOrEmpty(SportsParticipantRegistry.Notice)?"Names follow your crew across every sport. Duplicate names are OK.\nChoose any saved or imported identity; choosing an occupied driver swaps seats.\nController assignments and human/AI participation stay in each sport’s setup.":SportsParticipantRegistry.Notice;
 }
}
}
