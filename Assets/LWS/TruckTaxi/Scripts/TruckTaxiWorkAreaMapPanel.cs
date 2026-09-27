using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // A selection overlay on Compass's existing map, never another map renderer.
    public sealed class TruckTaxiWorkAreaMapPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel,boundaryClip;
        private TextMeshProUGUI summary;
        private Slider radius;
        private bool picking;
        private TruckTaxiWorkAreaGraphic graphic;
        private readonly TruckTaxiWorkAreaGraphic[] zones=new TruckTaxiWorkAreaGraphic[8];
        private readonly TruckTaxiRideWorkArea zoneArea=new TruckTaxiRideWorkArea {mode=TruckTaxiWorkAreaMode.Custom};
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;

        public void Initialize(TruckTaxiBootstrap value,TruckTaxiHud view,RectTransform mapRoot)
        {
            host=value; hud=view;
            panel=hud.Panel(mapRoot,"Ride Work Area",new Vector2(.33f,.44f),new Vector2(.65f,.89f));
            hud.Text(panel,"Heading",new Vector2(.05f,.87f),new Vector2(.95f,.98f),28).text="RIDE WORK AREA";
            summary=hud.Text(panel,"Selection",new Vector2(.05f,.70f),new Vector2(.95f,.86f),21);
            hud.Button(panel,"ANYWHERE NEAR ME",new Vector2(.05f,.58f),new Vector2(.95f,.68f),()=>Preset(TruckTaxiWorkAreaMode.AnywhereNearMe));
            hud.Button(panel,"CURRENT DISTRICT",new Vector2(.05f,.46f),new Vector2(.48f,.56f),()=>Preset(TruckTaxiWorkAreaMode.CurrentDistrict));
            hud.Button(panel,"CURRENT TOWN",new Vector2(.52f,.46f),new Vector2(.95f,.56f),()=>Preset(TruckTaxiWorkAreaMode.CurrentTown));
            hud.Button(panel,"PICK CENTER",new Vector2(.05f,.34f),new Vector2(.48f,.44f),()=>{picking=true; Close();});
            hud.Button(panel,"USE MAP CENTER",new Vector2(.52f,.34f),new Vector2(.95f,.44f),SetMapCenter);
            hud.Text(panel,"Radius",new Vector2(.05f,.20f),new Vector2(.37f,.32f),22).text="RADIUS";
            var go=Instantiate(hud.heatSliderPrefab,panel,false); go.name="Work Area radius";
            var rt=(RectTransform)go.transform; rt.anchorMin=new Vector2(.4f,.22f); rt.anchorMax=new Vector2(.95f,.30f); rt.offsetMin=rt.offsetMax=Vector2.zero;
            foreach(var c in go.GetComponents<Component>()) if(c.GetType().FullName=="Michsky.UI.Heat.SliderManager")
            { c.GetType().GetField("saveValue").SetValue(c,false); c.GetType().GetField("invokeOnAwake").SetValue(c,false); c.GetType().GetField("useSounds").SetValue(c,false); }
            foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)) text.enabled=false;
            foreach(var field in go.GetComponentsInChildren<TMP_InputField>(true)) field.gameObject.SetActive(false);
            radius=go.GetComponent<Slider>(); radius.minValue=100; radius.maxValue=4000; radius.wholeNumbers=true;
            radius.SetValueWithoutNotify(host.Configuration.defaultWorkAreaRadius);
            radius.onValueChanged.AddListener(v=> {var area=host.Session.WorkArea; if(area.Restricted) {area.radius=v; host.Session.SetWorkArea(area);} Refresh();});
            hud.Button(panel,"CLEAR",new Vector2(.05f,.045f),new Vector2(.48f,.16f),()=>Preset(TruckTaxiWorkAreaMode.AnywhereNearMe));
            hud.Button(panel,"CLOSE",new Vector2(.52f,.045f),new Vector2(.95f,.16f),Close);
            panel.gameObject.SetActive(false);
        }
        public void Toggle() { if(IsOpen) Close(); else {picking=false; panel.gameObject.SetActive(true); panel.SetAsLastSibling(); Refresh(); hud.UIInput.Focus(panel);} }
        public void Close() { panel.gameObject.SetActive(false); hud.UIInput.Focus(null); }
        public void CancelSelection() { picking=false; Close(); }
        private void Preset(TruckTaxiWorkAreaMode mode) { host.Session.SetWorkAreaPreset(mode); Refresh(); }
        private void Refresh()
        {
            var area=host.Session.WorkArea;
            summary.text=area.Restricted ? $"{area.label}\n{area.radius:0} m radius" : "ANYWHERE NEAR ME";
            if(area.Restricted) radius.SetValueWithoutNotify(area.radius);
        }
        private void SetMapCenter()
        {
            var camera=host.GPS.PreviewCamera;
            if(camera!=null && ProjectGround(camera,new Vector2(.5f,.5f),out var point)) SetCenter(point);
        }
        private void SetCenter(Vector3 position)
        {
            host.Session.SetWorkArea(new TruckTaxiRideWorkArea {mode=TruckTaxiWorkAreaMode.Custom,center=position,radius=radius.value,label="CUSTOM AREA"});
            picking=false; Refresh();
        }
        internal static bool ProjectGround(Camera camera,Vector2 viewport,out Vector3 point)
        {
            point=default; var ray=camera.ViewportPointToRay(viewport);
            if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance)) return false;
            point=ray.GetPoint(distance); return true;
        }
        private void Update()
        {
            if(boundaryClip!=null) boundaryClip.gameObject.SetActive(host?.GPS?.FullMapOpen==true);
            if(host?.GPS?.FullMapOpen!=true) return;
            var map=host.GPS.MapScreenRect;
            if(graphic==null && map!=null)
            {
                var clip=TruckTaxiHud.Rect(map,"Work Area boundary clip",Vector2.zero,Vector2.one);
                boundaryClip=clip;
                clip.gameObject.AddComponent<RectMask2D>();
                graphic=TruckTaxiHud.Rect(clip,"Work Area boundary",Vector2.zero,Vector2.one).gameObject.AddComponent<TruckTaxiWorkAreaGraphic>();
                graphic.raycastTarget=false;
                for(int i=0;i<zones.Length;i++)
                {
                    zones[i]=TruckTaxiHud.Rect(clip,"Regional area "+i,Vector2.zero,Vector2.one).gameObject.AddComponent<TruckTaxiWorkAreaGraphic>();
                    zones[i].raycastTarget=false;
                }
                graphic.transform.SetAsLastSibling();
            }
            if(graphic!=null) graphic.SetArea(host.Session.WorkArea,host.GPS.PreviewCamera);
            RefreshZones();
            if(!picking || IsOpen || Mouse.current==null || !Mouse.current.leftButton.wasPressedThisFrame || map==null) return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(map,Mouse.current.position.ReadValue(),null,out var local) && map.rect.Contains(local))
            {
                var uv=new Vector2((local.x-map.rect.xMin)/map.rect.width,(local.y-map.rect.yMin)/map.rect.height);
                if(ProjectGround(host.GPS.PreviewCamera,uv,out var point)) SetCenter(point);
            }
        }
        private void RefreshZones()
        {
            if(graphic==null) return;
            int index=0;
            if(host.Extremes!=null)
                foreach(var hazard in host.Extremes.ActiveSnapshots)
                {
                    if(index>=zones.Length) break;
                    DrawZone(index++,hazard.Position,hazard.RadiusMeters,new Color(.96f,.24f,.19f));
                }
            if(host.Venues!=null)
                foreach(var venue in host.Venues.Venues)
                {
                    if(index>=zones.Length) break;
                    var demand=host.Venues.GetDemand(venue.worldPosition);
                    if(demand.RideFrequencyMultiplier<=1.15f || demand.PickupWeight<=1 && demand.DestinationWeight<=1) continue;
                    DrawZone(index++,venue.worldPosition,venue.influenceRadiusMeters,new Color(1f,.65f,.12f));
                }
            for(;index<zones.Length;index++) zones[index].SetArea(null,null);
        }
        private void DrawZone(int index,Vector3 center,float radius,Color tint)
        {
            zoneArea.center=center; zoneArea.radius=radius;
            zones[index].color=tint;
            zones[index].SetArea(zoneArea,host.GPS.PreviewCamera);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TruckTaxiWorkAreaGraphic : MaskableGraphic
    {
        private Vector2 center,radii;
        private bool show;
        public void SetArea(TruckTaxiRideWorkArea area,Camera camera)
        {
            bool visible=area!=null && area.Restricted && camera!=null;
            Vector2 c=default,r=default;
            if(visible)
            {
                var v=camera.WorldToViewportPoint(area.center);
                var edge=camera.WorldToViewportPoint(area.center+camera.transform.right*area.radius);
                var up=camera.WorldToViewportPoint(area.center+camera.transform.up*area.radius);
                c=new Vector2(v.x,v.y); r=new Vector2(Mathf.Abs(edge.x-v.x),Mathf.Abs(up.y-v.y));
            }
            if(show==visible && center==c && radii==r) return;
            show=visible; center=c; radii=r; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if(!show) return;
            var rect=rectTransform.rect;
            Vector2 c=rect.min+Vector2.Scale(center,rect.size),r=Vector2.Scale(radii,rect.size);
            const int segments=72;
            var tint=color==Color.white ? new Color(.25f,.83f,.65f) : color;
            var fill=(Color32)new Color(tint.r,tint.g,tint.b,.14f); var border=(Color32)new Color(tint.r,tint.g,tint.b,.86f);
            vh.AddVert(c,fill,Vector2.zero);
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                vh.AddVert(c+Vector2.Scale(d,r),fill,Vector2.zero);
                if(i>0) vh.AddTriangle(0,i,i+1);
            }
            int start=vh.currentVertCount;
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments; Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                vh.AddVert(c+Vector2.Scale(d,r),border,Vector2.zero);
                vh.AddVert(c+Vector2.Scale(d,new Vector2(Mathf.Max(0,r.x-2),Mathf.Max(0,r.y-2))),border,Vector2.zero);
                if(i>0) { int n=start+i*2; vh.AddTriangle(n-2,n,n-1); vh.AddTriangle(n-1,n,n+1); }
            }
        }
    }
}
