using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NWH.VehiclePhysics2.Damage;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiStatusBars : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel, effectPanel;
        private DamageHandler damage;
        private float nextRefresh;
        private readonly Image[] fills = new Image[3];
        private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[3];
        private Image[] effectFills;
        private TextMeshProUGUI[] effectLabels;
        private GameObject[] effectRows;
        private readonly RawImage[] cravings = new RawImage[2];
        private AudioSource cravingAudio;
        private float nextCravingSound;
        private bool lastSoundWasHunger;
        private static readonly string[] Names = { "FUEL", "DAMAGE", "PATIENCE" };
        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud hud)
        {
            host = owner; this.hud = hud; damage = host.Player.GetComponent<DamageHandler>();
            var settings = owner.Environment?.settings;
            Texture[] images = {
                settings?.hungerCravingImage != null ? settings.hungerCravingImage : Resources.Load<Texture2D>("TruckTaxi/Needs/Hunger"),
                settings?.thirstCravingImage != null ? settings.thirstCravingImage : Resources.Load<Texture2D>("TruckTaxi/Needs/Thirst") };
            for (int i = 0; i < cravings.Length; i++)
            {
                var rect = TruckTaxiHud.Rect(hud.Root, i == 0 ? "Hunger craving" : "Thirst craving",
                    new Vector2(i == 0 ? .25f : .64f, .63f), new Vector2(i == 0 ? .25f : .64f, .63f));
                rect.sizeDelta = new Vector2(images[i] != null ? 260f * images[i].width / Mathf.Max(1, images[i].height) : 260, 260);
                cravings[i] = rect.gameObject.AddComponent<RawImage>();
                cravings[i].texture = images[i]; cravings[i].raycastTarget = false; cravings[i].color = Color.clear;
                rect.gameObject.SetActive(false);
            }
            cravingAudio = gameObject.AddComponent<AudioSource>();
            cravingAudio.playOnAwake = false; cravingAudio.spatialBlend = 0;
            TruckTaxiAudioController.Instance?.Route(cravingAudio, TruckTaxiAudioCategory.UI);
            panel = TruckTaxiHud.Rect(hud.Root, "Vehicle status bars", new Vector2(.37f,.19f), new Vector2(.71f,.27f));
            for(int i=0;i<fills.Length;i++)
            {
                var row=TruckTaxiHud.Rect(panel,Names[i],new Vector2(i/3f,0),new Vector2((i+1)/3f-.015f,1));
                CreateRow(hud,row,Names[i],out fills[i],out labels[i]);
                if(i==1)
                {
                    var button=row.gameObject.AddComponent<Button>();
                    button.onClick.AddListener(()=>hud.EnvironmentNeeds?.OpenRepair());
                }
            }
            effectPanel=TruckTaxiHud.Rect(hud.Root,"Active item effects",new Vector2(.38f,.28f),new Vector2(.7f,.42f));
            int count=TruckTaxiTemporaryEffects.ProfileCount;
            effectFills=new Image[count]; effectLabels=new TextMeshProUGUI[count]; effectRows=new GameObject[count];
            for(int i=0;i<count;i++)
            {
                var row=TruckTaxiHud.Rect(effectPanel,"Effect "+i,new Vector2(0,1-(i+1f)/count),new Vector2(1,1-i/(float)count));
                effectRows[i]=row.gameObject; CreateRow(hud,row,"",out effectFills[i],out effectLabels[i]);
            }
        }
        private static void CreateRow(TruckTaxiHud hud, RectTransform row, string name, out Image fill, out TextMeshProUGUI label)
        {
            var track=TruckTaxiHud.Rect(row,"Track",new Vector2(0,.12f),new Vector2(1,.36f));
            track.gameObject.AddComponent<Image>().color=new Color(.06f,.08f,.1f,.85f);
            fill=TruckTaxiHud.Rect(track,"Fill",Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
            fill.color=new Color(.22f,.8f,.68f); fill.raycastTarget=false;
            label=hud.Text(row,name,new Vector2(0,.38f),Vector2.one,18); label.text=name;
        }
        private void Update()
        {
            if(host==null || Time.unscaledTime<nextRefresh) return;
            nextRefresh=Time.unscaledTime+.15f;
            bool visible=RefreshVisibility();
            if(!visible) return;
            var state=host.DriverNeeds?.State;
            RefreshCravings(state);
            Set(0,host.Fuel?.Fraction ?? 1); Set(1,damage!=null ? damage.Damage : 0);
            bool riding=host.Session.HasPassenger;
            labels[2].transform.parent.gameObject.SetActive(riding || host.Session.State==TruckTaxiState.DrivingToPickup);
            float patience=riding ? host.Session.OnboardPatience :
                host.Session.State==TruckTaxiState.DrivingToPickup ? host.Session.PickupRemaining/Mathf.Max(1,host.Session.PickupDuration) : 1;
            Set(2,patience);
            labels[2].text=(riding ? "PASSENGER" : "PICKUP")+$"\nPATIENCE {patience*100:0}%";
            for(int i=0;i<effectRows.Length;i++)
            {
                var snapshot=state?.Effects.GetSnapshot((TruckTaxiTemporaryEffectKind)i) ?? default;
                effectRows[i].SetActive(snapshot.RemainingSeconds>0);
                effectFills[i].rectTransform.anchorMax=new Vector2(snapshot.Progress01,1);
                effectLabels[i].text=snapshot.Profile!=null ? snapshot.Profile.Label.ToUpperInvariant()+$"  {snapshot.RemainingSeconds:0}s" : "";
            }
        }
        public bool RefreshVisibility()
        {
            bool visible=host!=null && !host.Paused && !hud.NormalHudSuppressed && host.Ready &&
                host.Session!=null && host.Session.State!=TruckTaxiState.Inactive;
            if(panel!=null) panel.gameObject.SetActive(visible);
            if(effectPanel!=null) effectPanel.gameObject.SetActive(visible);
            if(!visible) foreach(var image in cravings) if(image!=null)
            { image.color=Color.clear; image.gameObject.SetActive(false); }
            if(!visible && cravingAudio!=null && cravingAudio.isPlaying) cravingAudio.Stop();
            return visible;
        }
        public static float CravingOpacity(float need, float threshold, float maximum)
        {
            if (!float.IsFinite(need) || !float.IsFinite(threshold) || !float.IsFinite(maximum)) return 0;
            return Mathf.InverseLerp(Mathf.Clamp(threshold, .8f, .99f), 1, need) * Mathf.Clamp(maximum, 0, .5f);
        }
        private void RefreshCravings(TruckTaxiDriverNeedsState state)
        {
            var settings = host.Environment?.settings;
            float hunger = state?.Hunger ?? 0, thirst = state?.Thirst ?? 0;
            float threshold = settings != null ? settings.cravingThreshold : .9f;
            float maximum = settings != null ? settings.cravingMaximumOpacity : .5f;
            for(int i=0;i<cravings.Length;i++)
            {
                var image=cravings[i];
                float alpha=CravingOpacity(i==0 ? hunger : thirst,threshold,maximum);
                image.color=new Color(1,1,1,Mathf.MoveTowards(image.color.a,alpha,.22f));
                image.gameObject.SetActive(image.texture!=null && image.color.a>.001f);
                float phase=Time.unscaledTime*.4f+i*2;
                image.rectTransform.anchoredPosition=new Vector2(Mathf.Sin(phase)*35,Mathf.Cos(phase*.7f)*28);
            }
            if(hunger<threshold && thirst<threshold)
            { if(cravingAudio.isPlaying) cravingAudio.Stop(); return; }
            if(settings==null || Time.unscaledTime<nextCravingSound || cravingAudio.isPlaying) return;
            bool playHunger=hunger>=threshold && (thirst<threshold || !lastSoundWasHunger);
            var clip=playHunger ? settings.hungerCravingSound : settings.thirstCravingSound;
            nextCravingSound=Time.unscaledTime+Mathf.Clamp(settings.cravingSoundInterval,10,120);
            lastSoundWasHunger=playHunger;
            if(clip!=null) cravingAudio.PlayOneShot(clip,.4f);
        }
        private void Set(int index,float value)
        {
            value=Mathf.Clamp01(value); fills[index].rectTransform.anchorMax=new Vector2(value,1);
            bool danger=index==0 || index==2 ? value<.2f : value>.8f;
            fills[index].color=danger ? new Color(.95f,.3f,.2f) : new Color(.22f,.8f,.68f);
            labels[index].text=Names[index]+$"  {value*100:0}%";
        }
    }
}
