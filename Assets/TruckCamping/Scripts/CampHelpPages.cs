using TMPro;
using UnityEngine;
using Michsky.UI.Heat;
namespace LWS.TruckCamping
{
    public sealed class CampHelpPages : MonoBehaviour
    {
        public TMP_Text text,number;
        public ButtonManager previous,next;
        public string[] pages;
        int page;
        void Awake(){previous.onClick.AddListener(Previous);next.onClick.AddListener(Next);Show();}
        public void Previous(){page=(page+pages.Length-1)%pages.Length;Show();}
        public void Next(){page=(page+1)%pages.Length;Show();}
        void Show(){if(pages==null||pages.Length==0)return;text.text=pages[page];number.text=(page+1)+" / "+pages.Length;}
    }
}
