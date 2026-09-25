using System.Collections;
using LWS.TruckTaxi;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public sealed class TruckTaxiPixelCrushersBarkAdapter : MonoBehaviour, ITruckTaxiBarkOutput, IBarkUI
{
    private AudioSource source;
    private AudioClip nextClip;
    private string speakerName;
    private float remaining;
    private bool paused;
    private Coroutine routine;
    public bool isPlaying => remaining > 0;
    public bool IsPlaying => isPlaying;
    public bool AudioPlaying => source!=null && source.isPlaying;
    public string Subtitle { get; private set; } = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register() => TruckTaxiBarkBridge.Create=go=>
    {
        var adapter=go.GetComponent<TruckTaxiPixelCrushersBarkAdapter>();
        return adapter!=null ? adapter : go.AddComponent<TruckTaxiPixelCrushersBarkAdapter>();
    };
    private void Awake()
    {
        if(!DialogueManager.hasInstance)
        {
            var manager=new GameObject("Truck Taxi Dialogue System"); manager.SetActive(false);
            manager.transform.SetParent(transform,false);
            var controller=manager.AddComponent<DialogueSystemController>(); controller.dontDestroyOnLoad=false;
            manager.SetActive(true);
        }
        source=gameObject.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=0; source.volume=.9f;
    }
    public void Play(string speaker,string subtitle,AudioClip clip)
    {
        Stop(); speakerName=speaker; nextClip=clip;
        var info=new PixelCrushers.DialogueSystem.CharacterInfo(0,speaker,transform,CharacterType.NPC,(Sprite)null);
        var line=new Subtitle(info,null,FormattedText.Parse(subtitle),"","",null);
        routine=StartCoroutine(BarkController.Bark(line,true));
    }
    public void Bark(Subtitle subtitle)
    {
        Subtitle=speakerName+": "+subtitle.formattedText.text;
        remaining=nextClip!=null ? nextClip.length+.3f : Mathf.Clamp(subtitle.formattedText.text.Length/14f,3,12);
        source.clip=nextClip; if(nextClip!=null) source.Play();
    }
    public void Hide() { remaining=0; Subtitle=""; if(source!=null) source.Stop(); }
    public void Stop() { Hide(); if(routine!=null) { StopCoroutine(routine); routine=null; } }
    public void SetPaused(bool value) { if(paused==value) return; paused=value; if(source==null) return; if(paused) source.Pause(); else source.UnPause(); }
    private void Update() { if(!paused) remaining=Mathf.Max(0,remaining-Time.unscaledDeltaTime); if(!isPlaying) Subtitle=""; }
    private void OnDisable() => Stop();
}
