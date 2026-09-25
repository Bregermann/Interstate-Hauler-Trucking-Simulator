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
        private RectTransform[] pins;
        private readonly Rect[] labelRects=new Rect[3];
        private TruckTaxiRideOffer offer;
        private readonly Vector3[] points=new Vector3[3];
        public void Initialize(TruckTaxiHud hud,TruckTaxiGPSAdapter adapter)
        {
            gps=adapter;
            image=gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget=false;
            var path=TruckTaxiHud.Rect(transform,"Offer road paths",Vector2.zero,Vector2.one);
            routes=path.gameObject.AddComponent<TruckTaxiOfferRouteGraphic>(); routes.raycastTarget=false;
            markers=new RectTransform[3];
            pins=new RectTransform[3];
            string[] labels={"YOU","A  PICKUP","B  DESTINATION"};
            Color[] colors={new Color(.1f,.8f,.96f),new Color(1,.82f,.18f),new Color(.95f,.42f,.75f)};
            for(int i=0;i<3;i++)
            {
                pins[i]=hud.Panel(transform,labels[i]+" point",Vector2.zero,Vector2.zero);
                pins[i].sizeDelta=new Vector2(10,10);
                pins[i].localRotation=Quaternion.Euler(0,0,45);
                pins[i].GetComponent<UnityEngine.UI.Image>().color=colors[i];
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
                pins[i].anchorMin=pins[i].anchorMax=new Vector2(position.x,position.y);
                pins[i].anchoredPosition=Vector2.zero;
                var center=Vector2.Scale(new Vector2(position.x,position.y),size)+Vector2.up*(i==0 ? -28 : 28);
                labelRects[i]=PlaceLabel(center,markers[i].sizeDelta,size,labelRects,i);
                markers[i].anchorMin=markers[i].anchorMax=new Vector2(labelRects[i].center.x/Mathf.Max(1,size.x),labelRects[i].center.y/Mathf.Max(1,size.y));
                markers[i].anchoredPosition=Vector2.zero;
            }
        }
        public static Rect PlaceLabel(Vector2 center,Vector2 labelSize,Vector2 mapSize,Rect[] occupied,int count)
        {
            // Three labels need at most three separated vertical lanes. Pins stay
            // at the exact coordinates even when nearby labels move apart.
            Rect candidate=default;
            for(int attempt=0;attempt<9;attempt++)
            {
                float offset=attempt==0 ? 0 : ((attempt+1)/2)*(labelSize.y+6)*(attempt%2==1 ? 1 : -1);
                candidate=new Rect(Mathf.Clamp(center.x-labelSize.x*.5f,0,Mathf.Max(0,mapSize.x-labelSize.x)),
                    Mathf.Clamp(center.y+offset-labelSize.y*.5f,0,Mathf.Max(0,mapSize.y-labelSize.y)),labelSize.x,labelSize.y);
                bool overlaps=false;
                for(int j=0;j<count;j++)
                {
                    var padded=new Rect(occupied[j].x-3,occupied[j].y-3,occupied[j].width+6,occupied[j].height+6);
                    if(candidate.Overlaps(padded)) { overlaps=true; break; }
                }
                if(!overlaps) return candidate;
            }
            return candidate;
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
