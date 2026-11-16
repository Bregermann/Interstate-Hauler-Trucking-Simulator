using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
namespace LWS.TruckSports {
public sealed class SportsCatalogue:MonoBehaviour {
 public GameObject[] cards;public TMP_Text indicator;public GameObject previous,next;public int pageSize=8;int page;
 public enum Category { All, Racing, BallAndTable, AimAndLaunch, PartyAndCollision, Dragon }
 Category category;
 Category[] families;
 Vector2[] cells;
 Vector3[] scales;
 ButtonManager filter;
 SportsFrontEnd front;
 public Category CurrentCategory=>category;
 public GameObject CategoryControl=>filter!=null?filter.gameObject:null;
 public int FilteredCount { get { EnsureLayout();return cards==null?0:Enumerable.Range(0,cards.Length).Count(Matches); } }
 public int Pages=>Mathf.Max(1,Mathf.CeilToInt(FilteredCount/(float)Mathf.Max(1,pageSize)));
 static readonly HashSet<string> racing=new HashSet<string>{"cheese","terrier","mattress","wife","redlight","frogger","avalanche","ninjawarrior","bog","skytow"};
 static readonly HashSet<string> ball=new HashSet<string>{"billiards","practice","poker","beerpong","hockey","airhockey","keepaway","keepawayfp","buzkashi","kickball","fieldgoal","volleyball","dodgeball","wallball","tetherball"};
 static readonly HashSet<string> aiming=new HashSet<string>{"dock","cornhole","bowling","curling","targetlaunch","skee","minefield","swing","bungee","basejump","trailerplug","fishing"};
 static string Label(Category value)=>value==Category.All?"ALL SPORTS":value==Category.Racing?"RACING":value==Category.BallAndTable?"BALL & TABLE":value==Category.AimAndLaunch?"AIM & LAUNCH":value==Category.Dragon?"DRAGON SPORTS":"PARTY & COLLISION";
 void EnsureLayout(){
  if(cards==null||families!=null&&families.Length==cards.Length)return;
  pageSize=Mathf.Max(1,pageSize);cells=new Vector2[pageSize];scales=new Vector3[cards.Length];families=new Category[cards.Length];
  for(int i=0;i<cards.Length;i++){
   var card=cards[i];if(card==null)continue;scales[i]=card.transform.localScale;if(i<cells.Length)cells[i]=card.GetComponent<RectTransform>().anchoredPosition;
   var action=card.GetComponentsInChildren<SportsMenuAction>(true).FirstOrDefault();string key=action!=null?action.action:"";
   families[i]=SportsDragonGames.TryFindKey(key,out _)?Category.Dragon:racing.Contains(key)?Category.Racing:ball.Contains(key)?Category.BallAndTable:aiming.Contains(key)?Category.AimAndLaunch:Category.PartyAndCollision;
  }
 }
 bool Matches(int index)=>cards[index]!=null&&(category==Category.All||families[index]==category);
 IEnumerator Start(){
  // Wait for the saved menu's normal action wiring before cloning its Heat control.
  yield return null;EnsureLayout();front=GetComponentInParent<SportsFrontEnd>(true);
  var available=Available();var saved=(Category)SportsPrefs.GetInt("Sports.CatalogueCategory",0);category=available.Contains(saved)?saved:Category.All;
  if(front!=null&&next!=null&&next.GetComponent<ButtonManager>()!=null){
   var control=Instantiate(next,next.transform.parent);control.name="Sport category filter";var marker=control.GetComponent<SportsMenuAction>();if(marker!=null)Destroy(marker);
   var rect=control.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(420,-337);rect.sizeDelta=new Vector2(500,48);rect.localScale=Vector3.one;
   filter=control.GetComponent<ButtonManager>();filter.onClick=new UnityEngine.Events.UnityEvent();filter.onClick.AddListener(()=>front.Action("cataloguefilter"));
   foreach(var text in control.GetComponentsInChildren<TMP_Text>(true)){text.enableAutoSizing=true;text.fontSize=24;text.fontSizeMax=24;text.fontSizeMin=20;}
   control.SetActive(true);
  }
  Refresh();
  if(front!=null&&front.Page==1){var selected=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;front.Focus();if(selected!=null&&selected.activeInHierarchy&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(selected);}
 }
 List<Category> Available(){EnsureLayout();return System.Enum.GetValues(typeof(Category)).Cast<Category>().Where(c=>c==Category.All||families!=null&&families.Contains(c)).ToList();}
 public void CycleCategory(){var available=Available();category=available[(available.IndexOf(category)+1)%available.Count];page=0;SportsPrefs.SetInt("Sports.CatalogueCategory",(int)category);SportsPrefs.Save();Refresh();}
 public void FocusFilter(){if(filter!=null&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(filter.gameObject);}
 public void Step(int direction){page=Mathf.Clamp(page+direction,0,Pages-1);Refresh();}
 void OnEnable(){Refresh();}
 void OnDestroy(){DOTween.Kill(this);}
 void Refresh(){
  if(cards==null)return;EnsureLayout();page=Mathf.Clamp(page,0,Pages-1);DOTween.Kill(this);int rank=0;
  for(int i=0;i<cards.Length;i++){
   var card=cards[i];if(card==null)continue;bool match=Matches(i);bool visible=match&&rank/pageSize==page;
   if(match){card.GetComponent<RectTransform>().anchoredPosition=cells[rank%pageSize];rank++;}
   card.transform.localScale=scales[i];card.SetActive(visible);
   if(visible&&card.activeInHierarchy){card.transform.localScale=scales[i]*.97f;card.transform.DOScale(scales[i],.16f).SetEase(Ease.OutQuad).SetUpdate(true).SetId(this).SetLink(card);}
  }
  if(indicator!=null)indicator.text="SPORTS  "+(page+1)+" / "+Pages;
  if(previous!=null)previous.SetActive(page>0);if(next!=null)next.SetActive(page<Pages-1);
  if(filter!=null){filter.buttonText="CATEGORY  ·  "+Label(category);filter.UpdateUI();}
 }
}
}
