using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckMusicalParking
{
    // World presentation only. ParkingBay retains ownership/open/capture authority.
    public sealed class ParkingBayAppearance:MonoBehaviour
    {
        public ParkingMatch match;public ParkingBay bay;public int number=1;
        public RectTransform plate;public TMP_Text numberText,stateText,ownerText;
        public Image stripe,portrait;public GameObject closedMark,openMark;
        public Color available=new Color(.3f,.95f,.58f),closed=new Color(.98f,.25f,.13f);
        int previous=int.MinValue,previousNumber;string previousName;Color previousColor;
        void OnEnable(){previous=int.MinValue;if(plate!=null)plate.gameObject.SetActive(true);}
        void LateUpdate()
        {
            if(plate==null||bay==null||match==null)return;
            plate.position=bay.transform.TransformPoint(new Vector3(0,.085f,-bay.size.y*.5f-.7f));
            plate.rotation=Quaternion.Euler(90,0,0);
            int state=bay.Owner>=0?bay.Owner+2:bay.Open?1:0;
            string owner=bay.Owner>=0?match.setup.players[bay.Owner].name:"";
            Color color=bay.Owner>=0?match.setup.players[bay.Owner].color:bay.Open?available:closed;
            if(state==previous&&number==previousNumber&&owner==previousName&&color==previousColor)return;
            bool animate=previous!=int.MinValue;previous=state;previousNumber=number;previousName=owner;previousColor=color;
            numberText.text=number.ToString("00");stripe.color=stateText.color=color;
            stateText.text=bay.Owner>=0?"SECURED":bay.Open?"OPEN":"CLOSED";
            ownerText.text=bay.Owner>=0?owner:bay.Open?"PARK INSIDE":"WAIT FOR HORN";
            closedMark.SetActive(bay.Owner<0&&!bay.Open);openMark.SetActive(bay.Owner<0&&bay.Open);portrait.gameObject.SetActive(bay.Owner>=0);portrait.color=Color.Lerp(color,Color.white,.25f);
            if(animate){stateText.transform.DOKill();stateText.transform.localScale=Vector3.one;stateText.transform.DOPunchScale(Vector3.one*.06f,.22f,2,.4f).SetUpdate(true).SetLink(plate.gameObject);}
        }
        void OnDisable(){if(stateText!=null){stateText.transform.DOKill();stateText.transform.localScale=Vector3.one;}if(plate!=null)plate.gameObject.SetActive(false);}
    }
}
