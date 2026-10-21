using CompassNavigatorPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckSports
{
    // CNP owns projection, edge placement, overlap avoidance and POI lifetime.
    // Each shared or local shot sees only its explicitly assigned legitimate cues.
    [DefaultExecutionOrder(950)]
    public sealed class SportsBroadcastCompass:CompassPro
    {
        public CompassProPOI[] permittedTargets;
        public float iconPixels=28,labelFontSize=16,labelWidth=150;
        bool Permitted(CompassProPOI poi)
        {if(permittedTargets==null)return false;foreach(var target in permittedTargets)if(target==poi)return true;return false;}
        protected override void ComputePOIViewportPos(CompassProPOI poi)
        {base.ComputePOIViewportPos(poi);poi.GetState(compassGroup).viewportPos=cameraMain.WorldToViewportPoint(poi.positionWS);}
        protected override void UpdateIndicators()
        {
            // This is this compass instance's public vendor collection, not the global
            // POI registry. Other participants' GPS indicators remain untouched.
            for(int i=pois.Count-1;i>=0;i--)if(pois[i]==null||!Permitted(pois[i]))pois.RemoveAt(i);
            base.UpdateIndicators();
        }
        protected override GameObject CreateIndicator(CompassProPOI poi)
        {
            var widget=base.CreateIndicator(poi);var elements=widget.GetComponentInChildren<GizmoElements>();
            if(elements!=null)
            {
                elements.iconImage.rectTransform.sizeDelta=Vector2.one*iconPixels;elements.iconImage.preserveAspect=true;
                elements.titleText.fontSize=labelFontSize;elements.titleText.outlineWidth=.25f;elements.titleText.outlineColor=Color.black;
                elements.titleText.rectTransform.sizeDelta=new Vector2(labelWidth,28);elements.titleText.enableAutoSizing=true;elements.titleText.fontSizeMin=12;elements.titleText.fontSizeMax=labelFontSize;
                elements.distanceText.gameObject.SetActive(false);
            }
            foreach(var graphic in widget.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            return widget;
        }
    }
}
