using UnityEngine;

namespace LWS.TruckTaxi
{
    // Displays the existing Compass camera texture; does not calculate routes or own navigation.
    public sealed class TruckTaxiOfferMap : MonoBehaviour
    {
        private TruckTaxiGPSAdapter gps;
        private UnityEngine.UI.RawImage image;
        private TruckTaxiOfferRouteGraphic routes;
        private RectTransform[] markers;
        private TruckTaxiRideOffer offer;
        private readonly Vector3[] points=new Vector3[3];
        public void Initialize(TruckTaxiHud hud,TruckTaxiGPSAdapter adapter)
        {
            gps=adapter;
            image=gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget=false;
            var path=TruckTaxiHud.Rect(transform,"Offer road paths",Vector2.zero,Vector2.one);
            routes=path.gameObject.AddComponent<TruckTaxiOfferRouteGraphic>(); routes.raycastTarget=false;
            markers=new RectTransform[3];
            string[] labels={"YOU","A  PICKUP","B  DESTINATION"};
            Color[] colors={new Color(.1f,.8f,.96f),new Color(1,.82f,.18f),new Color(.95f,.42f,.75f)};
            for(int i=0;i<3;i++)
            {
                var badge=hud.Panel(transform,labels[i],Vector2.zero,Vector2.zero);
                badge.sizeDelta=new Vector2(i==2 ? 225 : 150,38);
                var label=hud.Text(badge,labels[i],new Vector2(.06f,.05f),new Vector2(.94f,.95f),22);
                label.text=labels[i]; label.color=colors[i]; label.alignment=TMPro.TextAlignmentOptions.Center;
                markers[i]=badge;
            }
        }
        public void Show(TruckTaxiRideOffer value)
        {
            if(offer==value) return;
            offer=value; gps.FrameOffer(value);
        }
        private void LateUpdate()
        {
            var camera=gps?.PreviewCamera;
            if(camera==null || offer==null) return;
            image.texture=camera.targetTexture;
            routes.Bind(camera,offer);
            points[0]=offer.PlayerPosition; points[1]=offer.Pickup.StopPosition; points[2]=offer.Destination.StopPosition;
            var size=((RectTransform)transform).rect.size;
            for(int i=0;i<3;i++)
            {
                Vector3 position=camera.WorldToViewportPoint(points[i]);
                float margin=markers[i].sizeDelta.x*.5f/Mathf.Max(1,size.x);
                float y=position.y+(i==0 ? -24 : 24)/Mathf.Max(1,size.y);
                markers[i].anchorMin=markers[i].anchorMax=new Vector2(Mathf.Clamp(position.x,margin,1-margin),Mathf.Clamp(y,.05f,.95f));
                markers[i].anchoredPosition=Vector2.zero;
            }
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TruckTaxiOfferRouteGraphic : UnityEngine.UI.MaskableGraphic
    {
        private Camera mapCamera;
        private TruckTaxiRideOffer offer;
        private Matrix4x4 projection;
        public void Bind(Camera camera,TruckTaxiRideOffer value)
        {
            var matrix=camera.projectionMatrix*camera.worldToCameraMatrix;
            if(offer==value && matrix==projection) return;
            mapCamera=camera; offer=value; projection=matrix; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear(); if(offer==null || mapCamera==null) return;
            Draw(helper,offer.ToPickup,new Color(.1f,.8f,.96f));
            Draw(helper,offer.Trip,new Color(1,.82f,.18f));
        }
        private void Draw(UnityEngine.UI.VertexHelper helper,TruckTaxiRouteLeg route,Color color)
        {
            var rect=rectTransform.rect;
            for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=mapCamera.WorldToViewportPoint(route.Points[i-1]),b=mapCamera.WorldToViewportPoint(route.Points[i]);
                Vector2 start=rect.min+Vector2.Scale(new Vector2(a.x,a.y),rect.size);
                Vector2 end=rect.min+Vector2.Scale(new Vector2(b.x,b.y),rect.size);
                Vector2 delta=end-start; if(delta.sqrMagnitude<.01f) continue;
                Vector2 side=new Vector2(-delta.y,delta.x).normalized*2.5f;
                int index=helper.currentVertCount;
                helper.AddVert(start-side,color,Vector2.zero); helper.AddVert(start+side,color,Vector2.zero);
                helper.AddVert(end+side,color,Vector2.zero); helper.AddVert(end-side,color,Vector2.zero);
                helper.AddTriangle(index,index+1,index+2); helper.AddTriangle(index,index+2,index+3);
            }
        }
    }
}
