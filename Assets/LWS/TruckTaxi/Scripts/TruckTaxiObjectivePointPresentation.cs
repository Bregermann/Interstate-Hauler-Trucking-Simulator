using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Runs after Compass positions its clamped point icon. Area overlays never use this component.
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class TruckTaxiObjectivePointPresentation : MonoBehaviour
    {
        private sealed class View
        {
            public Component compass;
            public Camera camera;
            public PropertyInfo group, border, circular, contents, fullScreen;
            public FieldInfo cameraField;
            public Image arrow;
        }
        private readonly View[] views=new View[2];
        private Component poi;
        private MethodInfo getState;
        private FieldInfo scaleField, iconField, rootField;
        private readonly object[] groupArgument=new object[1];
        private Sprite arrowSprite;
        private float baseScale;
        private bool active;

        public void Configure(Component point,Component cab,Component hud,Sprite arrow,float scale,bool isActive)
        {
            if(poi!=point)
            {
                poi=point;
                getState=point.GetType().GetMethod("GetState",new[] { typeof(int) });
                scaleField=point.GetType().GetField("miniMapIconScale");
                iconField=getState?.ReturnType.GetField("miniMapIconImage");
                rootField=getState?.ReturnType.GetField("miniMapIconRT");
            }
            BindView(0,cab); BindView(1,hud);
            arrowSprite=arrow; baseScale=scale; active=isActive;
            if(!active) HideArrows();
        }
        private void BindView(int index,Component compass)
        {
            if(views[index]?.compass==compass) return;
            if(views[index]?.arrow!=null) DestroyOwned(views[index].arrow.gameObject);
            views[index]=null;
            if(compass==null) return;
            var type=compass.GetType();
            views[index]=new View { compass=compass, camera=type.GetField("miniMapCamera")?.GetValue(compass) as Camera,
                group=type.GetProperty("compassGroup"), border=type.GetProperty("miniMapClampBorder"),
                circular=type.GetProperty("miniMapClampBorderCircular"), contents=type.GetProperty("miniMapContents"),
                fullScreen=type.GetProperty("miniMapFullScreenState"), cameraField=type.GetField("miniMapCamera") };
        }
        public static float Pulse(float seconds) => 1.15f+.15f*Mathf.Sin(seconds*Mathf.PI*2);
        public static float DirectionAngle(Vector2 direction) => Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90;
        public static bool AtMapEdge(Vector2 anchor,float border,bool circular) => circular
            ? (anchor-new Vector2(.5f,.5f)).magnitude >= (1-border)*.5f-.002f
            : anchor.x<=border+.002f || anchor.x>=1-border-.002f || anchor.y<=border+.002f || anchor.y>=1-border-.002f;

        private void LateUpdate()
        {
            if(!active || poi==null || !poi.gameObject.activeInHierarchy) { HideArrows(); return; }
            scaleField?.SetValue(poi,baseScale*Pulse(Time.unscaledTime));
            if(getState==null || iconField==null || arrowSprite==null) return;
            foreach(var view in views)
            {
                if(view?.compass==null || view.group==null) continue;
                if(view.camera==null) view.camera=view.cameraField?.GetValue(view.compass) as Camera;
                if(view.fullScreen?.GetValue(view.compass) is bool full && full)
                { if(view.arrow!=null) view.arrow.enabled=false; continue; }
                groupArgument[0]=view.group.GetValue(view.compass);
                var state=getState.Invoke(poi,groupArgument);
                var icon=iconField.GetValue(state) as Image;
                if(icon==null || !icon.isActiveAndEnabled || view.camera==null)
                { if(view.arrow!=null) view.arrow.enabled=false; continue; }
                // The vendor owns the edge position. Add only a direction glyph to its point icon.
                var anchor=icon.GetComponentInParent<RectTransform>();
                var root=rootField?.GetValue(state) as RectTransform;
                float border=view.border?.GetValue(view.compass) is float value ? value : .02f;
                bool circular=view.circular?.GetValue(view.compass) is bool circle && circle ||
                    string.Equals(view.contents?.GetValue(view.compass)?.ToString(),"Radar",StringComparison.Ordinal);
                bool edge=root!=null && AtMapEdge(root.anchorMin,border,circular);
                if(!edge) { if(view.arrow!=null) view.arrow.enabled=false; continue; }
                if(view.arrow==null || view.arrow.transform.parent!=anchor)
                {
                    if(view.arrow!=null) DestroyOwned(view.arrow.gameObject);
                    var go=new GameObject("Objective edge direction",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                    go.transform.SetParent(anchor,false);
                    view.arrow=go.GetComponent<Image>(); view.arrow.raycastTarget=false;
                    view.arrow.rectTransform.sizeDelta=new Vector2(20,20);
                    view.arrow.rectTransform.anchoredPosition=new Vector2(0,20);
                }
                Vector3 direction=poi.transform.position-view.camera.transform.position;
                Vector2 screenDirection=new Vector2(Vector3.Dot(direction,view.camera.transform.right),
                    Vector3.Dot(direction,view.camera.transform.up));
                view.arrow.sprite=arrowSprite; view.arrow.color=Color.white;
                view.arrow.rectTransform.anchoredPosition=-screenDirection.normalized*20;
                view.arrow.rectTransform.localRotation=Quaternion.Euler(0,0,DirectionAngle(screenDirection));
                view.arrow.enabled=true;
            }
        }
        private void HideArrows() { foreach(var view in views) if(view?.arrow!=null) view.arrow.enabled=false; }
        private static void DestroyOwned(GameObject go)
        { if(Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
        private void OnDisable()
        {
            HideArrows();
            if(poi!=null) scaleField?.SetValue(poi,baseScale);
        }
        private void OnDestroy() { foreach(var view in views) if(view?.arrow!=null) DestroyOwned(view.arrow.gameObject); }
    }
}
