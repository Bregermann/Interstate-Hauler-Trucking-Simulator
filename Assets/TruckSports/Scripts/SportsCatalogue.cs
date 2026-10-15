using UnityEngine;using TMPro;
namespace LWS.TruckSports {
public sealed class SportsCatalogue:MonoBehaviour {
 public GameObject[] cards;public TMP_Text indicator;public GameObject previous,next;public int pageSize=8;int page;
 public int Pages=>cards==null?1:Mathf.Max(1,Mathf.CeilToInt(cards.Length/(float)pageSize));
 public void Step(int direction){page=Mathf.Clamp(page+direction,0,Pages-1);Refresh();}
 void OnEnable(){Refresh();}void Refresh(){if(cards==null)return;page=Mathf.Clamp(page,0,Pages-1);for(int i=0;i<cards.Length;i++)cards[i].SetActive(i/pageSize==page);if(indicator!=null)indicator.text="SPORTS  "+(page+1)+" / "+Pages;if(previous!=null)previous.SetActive(page>0);if(next!=null)next.SetActive(page<Pages-1);}
}
}
