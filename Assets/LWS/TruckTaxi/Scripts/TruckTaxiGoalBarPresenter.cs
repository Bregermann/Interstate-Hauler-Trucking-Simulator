using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // HUD-only projection. Request instances and ride history remain owned by TruckTaxiSession.
    public sealed class TruckTaxiGoalBarPresenter : MonoBehaviour
    {
        private const float RowHeight=58;
        private const float RowSpacing=7;
        private const float SuccessHold=1.2f;
        private const float SuccessFade=.7f;
        private const float SoundSpacing=.35f;
        private readonly List<Row> rows=new List<Row>();
        private TruckTaxiSession session;
        private RectTransform container;
        private TMP_FontAsset font;
        private Action<AudioClip> playSound;
        private AudioClip successClip;
        private string displayedRideId;
        private int pendingSuccessSounds;
        private float nextSoundAt;

        private sealed class Row
        {
            public TaxiRequestProgress Goal;
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Fill;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Value;
            public float ResolvedAt=-1;
            public float Y;
        }

        public void Initialize(TruckTaxiSession value,RectTransform parent,TMP_FontAsset textFont,
            AudioClip positiveClip,Action<AudioClip> play)
        {
            if(session!=null) Unsubscribe();
            foreach(var row in rows) if(row.Root!=null) Destroy(row.Root.gameObject);
            rows.Clear();
            session=value; font=textFont; successClip=positiveClip; playSound=play;
            if(parent!=null)
            {
                parent.gameObject.AddComponent<RectMask2D>();
                parent.gameObject.AddComponent<Image>().color=Color.clear;
                var content=new GameObject("Scrollable goals",typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(parent,false); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1);
                content.sizeDelta=new Vector2(0,RowHeight);
                var scroll=parent.gameObject.AddComponent<ScrollRect>(); scroll.viewport=parent; scroll.content=content; scroll.horizontal=false; scroll.scrollSensitivity=35;
                container=content;
            }
            displayedRideId=session?.CurrentRideId;
            pendingSuccessSounds=0; nextSoundAt=0;
            if(session==null || container==null) return;
            session.RequestCreated+=OnCreated;
            session.RequestResolved+=OnResolved;
            foreach(var request in session.Requests) OnCreated(request);
        }

        private void OnCreated(TaxiRequestProgress goal)
        {
            if(goal==null || container==null) return;
            foreach(var row in rows) if(row.Goal==goal) return;
            var root=new GameObject("Goal bar",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();
            root.SetParent(container,false);
            root.anchorMin=root.anchorMax=new Vector2(0,1);
            root.pivot=new Vector2(0,1);
            root.sizeDelta=new Vector2(0,RowHeight);
            root.anchorMax=new Vector2(1,1);
            root.offsetMin=new Vector2(0,-RowHeight);
            root.offsetMax=Vector2.zero;
            var track=MakeImage(root,"Track",new Color(.08f,.12f,.16f,.92f));
            Stretch(track.rectTransform,0,4,30,4);
            var fill=MakeImage(track.rectTransform,"Progress",new Color(.1f,.8f,.67f,1));
            fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal;
            Stretch(fill.rectTransform,0,0,0,0);
            var label=MakeText(root,"Goal",18,TextAlignmentOptions.Left);
            Stretch(label.rectTransform,8,0,4,30);
            label.rectTransform.anchorMax=new Vector2(.57f,1);
            var value=MakeText(root,"Progress",16,TextAlignmentOptions.Right);
            Stretch(value.rectTransform,8,8,4,30);
            value.rectTransform.anchorMin=new Vector2(.58f,0);
            var rowView=new Row { Goal=goal,Root=root,Group=root.GetComponent<CanvasGroup>(),
                Fill=fill,Label=label,Value=value,Y=rows.Count*(RowHeight+RowSpacing),
                ResolvedAt=goal.State==TaxiRequestState.Active ? -1 : Time.unscaledTime };
            rows.Add(rowView);
            Refresh(rowView);
        }

        private void OnResolved(TaxiRequestProgress goal)
        {
            foreach(var row in rows)
            {
                if(row.Goal!=goal) continue;
                row.ResolvedAt=Time.unscaledTime;
                if(goal.State==TaxiRequestState.Succeeded) pendingSuccessSounds++;
                Refresh(row);
                break;
            }
        }

        private void Update()
        {
            if(session==null) return;
            if(session.CurrentRideId!=null && displayedRideId!=session.CurrentRideId)
            {
                foreach(var row in rows) if(row.Root!=null) Destroy(row.Root.gameObject);
                rows.Clear(); pendingSuccessSounds=0;
                displayedRideId=session.CurrentRideId;
                foreach(var request in session.Requests) OnCreated(request);
            }
            if(pendingSuccessSounds>0 && Time.unscaledTime>=nextSoundAt)
            {
                if(successClip!=null) playSound?.Invoke(successClip);
                pendingSuccessSounds--;
                nextSoundAt=Time.unscaledTime+SoundSpacing;
            }
            int visibleIndex=0;
            foreach(var row in rows)
            {
                Refresh(row);
                float age=row.ResolvedAt<0 ? 0 : Time.unscaledTime-row.ResolvedAt;
                bool hide=row.Goal.State==TaxiRequestState.Succeeded && age>=SuccessHold+SuccessFade;
                if(hide) { row.Root.gameObject.SetActive(false); continue; }
                row.Root.gameObject.SetActive(true);
                row.Group.alpha=row.Goal.State==TaxiRequestState.Succeeded && age>SuccessHold
                    ? 1-Mathf.Clamp01((age-SuccessHold)/SuccessFade) : 1;
                float targetY=visibleIndex++*(RowHeight+RowSpacing);
                row.Y=Mathf.Lerp(row.Y,targetY,1-Mathf.Exp(-12*Time.unscaledDeltaTime));
                row.Root.anchoredPosition=new Vector2(0,-row.Y);
            }
            if(container!=null) container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(RowHeight,visibleIndex*(RowHeight+RowSpacing)));
        }

        private static void Refresh(Row row)
        {
            var goal=row.Goal;
            row.Fill.rectTransform.anchorMax=new Vector2(goal.NormalizedProgress,1);
            row.Fill.color=goal.State==TaxiRequestState.Failed ? new Color(.9f,.22f,.2f) :
                goal.State==TaxiRequestState.Succeeded ? new Color(.18f,.95f,.58f) : new Color(.1f,.8f,.67f);
            row.Label.text=goal.Description.ToUpperInvariant();
            row.Value.text=goal.State==TaxiRequestState.Succeeded ? "SUCCESS" :
                goal.State==TaxiRequestState.Failed ? goal.FailureReason : goal.ProgressText;
        }

        private Image MakeImage(Transform parent,string name,Color color)
        {
            var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent,false); image.color=color; image.raycastTarget=false;
            return image;
        }

        private TextMeshProUGUI MakeText(Transform parent,string name,int size,TextAlignmentOptions alignment)
        {
            var value=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            value.transform.SetParent(parent,false); value.font=font; value.fontSize=size;
            value.alignment=alignment; value.color=Color.white; value.raycastTarget=false;
            value.enableAutoSizing=true; value.fontSizeMin=12; value.fontSizeMax=size;
            return value;
        }

        private static void Stretch(RectTransform rect,float left,float right,float top,float bottom)
        {
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
            rect.offsetMin=new Vector2(left,bottom); rect.offsetMax=new Vector2(-right,-top);
        }

        private void Unsubscribe()
        {
            session.RequestCreated-=OnCreated;
            session.RequestResolved-=OnResolved;
        }
        private void OnDestroy() { if(session!=null) Unsubscribe(); }
    }
}
