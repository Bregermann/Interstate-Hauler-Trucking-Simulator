using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Last-session presentation only; persisted Vibin discovery comes from the scoped facade.
    public struct TruckTaxiMainMenuStats
    {
        public int rides, pedestriansHit, trafficCollisions, passengersEjected;
        public int jugEvents, bottlesThrown, specialEvents, objectivesCompleted;
        public long earningsCents;
        public float averageStars, chaos;
    }

    public sealed class TruckTaxiMainMenu : MonoBehaviour
    {
        public const string SceneName = "TruckTaxi_MainMenu";
        public const string VibinSceneName = "TruckTaxi_DemoCity";
        // Kept for existing editor tooling that identifies the authored city scene.
        public const string FreePlaySceneName = VibinSceneName;
        public static TruckTaxiMainMenuStats? LastSessionStats { get; private set; }
        public static void PublishSessionStats(TruckTaxiMainMenuStats stats) => LastSessionStats=stats;
        public static void ClearSessionStats() => LastSessionStats=null;

        [SerializeField] private RectTransform viewRoot;
        [SerializeField] private TruckTaxiHud view;
        [SerializeField] private TruckTaxiAudioConfiguration audioConfiguration;
        private TruckTaxiAudioController audio;
        private TruckTaxiAudioSettingsPanel audioPanel;
        private TruckTaxiGPSSettingsPanel gpsPanel;
        private TruckTaxiUIInput input;
        private RectTransform main, modes, options, load, stats;
        private TextMeshProUGUI statsText;
        private RectTransform page;
        private LwsSaveService saves;
        private IReadOnlyList<LwsSaveProfileMetadata> profiles = Array.Empty<LwsSaveProfileMetadata>();
        private readonly GameObject[] slotButtons = new GameObject[4];
        private readonly LwsManualSaveSlotMetadata[] slots = new LwsManualSaveSlotMetadata[4];
        private GameObject newButton, continueButton, loadButton, vibinButton, previousProfile, nextProfile;
        private TextMeshProUGUI saveStatus, loadStatus, profileText;
        private int profileIndex;
        private bool discoveryReady, enteringGame;
        public bool HasVibinSave { get; private set; }
        public string SaveStatus { get; private set; } = "VIBIN' SAVES UNAVAILABLE";

        private void Start()
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            foreach(var developmentHud in FindObjectsByType<LWS.InterstateHauler.LwsDevelopmentUiRoot>(FindObjectsSortMode.None))
                developmentHud.gameObject.SetActive(false);
            if(viewRoot==null || view==null || EventSystem.current==null)
            {
                Debug.LogError("Truck Taxi menu requires its generated canvas, HUD factory and EventSystem.",this);
                return;
            }
            view.InitializeViewRoot(viewRoot);
            audio=gameObject.AddComponent<TruckTaxiAudioController>();
            audio.Initialize(audioConfiguration);
            audioPanel=gameObject.AddComponent<TruckTaxiAudioSettingsPanel>();
            audioPanel.InitializeView(audio,view);
            gpsPanel=gameObject.AddComponent<TruckTaxiGPSSettingsPanel>();
            gpsPanel.InitializeView(view);
            BuildPages();
            InitializeSaves();
            input=new TruckTaxiUIInput(EventSystem.current);
            input.Submit.performed+=Submit;
            input.Cancel.performed+=Cancel;
            Show(main);
        }

        private void BuildPages()
        {
            view.Text(viewRoot,"Truck Taxi title",new Vector2(.04f,.88f),new Vector2(.44f,.98f),54).text="TRUCK TAXI";
            main=Page("Main menu");
            newButton=Button(main,"NEW GAME",.78f,()=>Show(modes));
            continueButton=Button(main,"CONTINUE VIBIN'",.65f,ContinueVibin);
            loadButton=Button(main,"LOAD GAME",.52f,OpenLoad);
            Button(main,"OPTIONS",.39f,()=>Show(options));
            Button(main,"STATS",.26f,OpenStats);
            Button(main,"QUIT GAME",.13f,Quit);
            saveStatus=view.Text(main,"Vibin save status",new Vector2(.08f,.01f),new Vector2(.92f,.10f),20);

            modes=Page("Mode select");
            Label(modes,"NEW GAME");
            Disabled(modes,"STORY MODE - COMING SOON",.70f);
            Disabled(modes,"ARCADE MODE - COMING SOON",.55f);
            vibinButton=Button(modes,"VIBIN'",.40f,NewVibin);
            Button(modes,"BACK",.12f,()=>Show(main));

            load=Page("Load game");
            Label(load,"VIBIN' SAVES");
            previousProfile=view.Button(load,"<",new Vector2(.08f,.70f),new Vector2(.20f,.80f),()=>ChangeProfile(-1));
            nextProfile=view.Button(load,">",new Vector2(.80f,.70f),new Vector2(.92f,.80f),()=>ChangeProfile(1));
            profileText=view.Text(load,"Vibin profile",new Vector2(.22f,.70f),new Vector2(.78f,.80f),23);
            profileText.richText=false; profileText.enableAutoSizing=true;
            profileText.fontSizeMin=14; profileText.fontSizeMax=23;
            profileText.overflowMode=TextOverflowModes.Ellipsis;
            for(int i=0;i<slotButtons.Length;i++)
            {
                int index=i;
                slotButtons[i]=Button(load,i==3 ? "AUTOSAVE" : "SLOT "+(i+1),.57f-i*.13f,()=>LoadSlot(index));
            }
            loadStatus=view.Text(load,"Load status",new Vector2(.08f,.145f),new Vector2(.92f,.175f),16);
            loadStatus.enableAutoSizing=true; loadStatus.fontSizeMin=12; loadStatus.fontSizeMax=16;
            loadStatus.overflowMode=TextOverflowModes.Ellipsis;
            Button(load,"BACK",.035f,()=>Show(main));

            options=Page("Options");
            Label(options,"OPTIONS");
            Button(options,"AUDIO",.66f,()=> { audioPanel.Open(); FocusCurrent(); });
            Button(options,"GPS / DISPLAY",.51f,()=> { gpsPanel.Open(); FocusCurrent(); });
            Button(options,"BACK",.12f,()=>Show(main));

            stats=Page("Stats");
            Label(stats,"STATS");
            statsText=view.Text(stats,"Session stats",new Vector2(.08f,.24f),new Vector2(.92f,.80f),23);
            Button(stats,"BACK",.12f,()=>Show(main));
        }

        private RectTransform Page(string name)
        {
            var panel=view.Panel(viewRoot,name,new Vector2(.035f,.13f),new Vector2(.38f,.86f));
            panel.gameObject.SetActive(false);
            return panel;
        }
        private void Label(RectTransform panel,string label) =>
            view.Text(panel,label+" heading",new Vector2(.08f,.82f),new Vector2(.92f,.96f),34).text=label;
        private GameObject Button(RectTransform panel,string label,float y,System.Action action) =>
            view.Button(panel,label,new Vector2(.08f,y),new Vector2(.92f,y+.105f),action);
        private void Disabled(RectTransform panel,string label,float y)
        {
            var button=view.Button(panel,label,new Vector2(.08f,y),new Vector2(.92f,y+.105f),()=>{});
            SetButton(button,label,false);
        }
        private static void SetButton(GameObject button,string label,bool enabled)
        {
            if(button==null) return;
            foreach(var component in button.GetComponents<Component>())
                if(component.GetType().FullName=="Michsky.UI.Heat.ButtonManager")
                {
                    var type=component.GetType();
                    type.GetMethod("Interactable")?.Invoke(component,new object[]{enabled});
                    type.GetMethod("SetText")?.Invoke(component,new object[]{label});
                }
            foreach(var selectable in button.GetComponentsInChildren<Selectable>(true)) selectable.interactable=enabled;
            foreach(var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            { text.text=label; text.enableAutoSizing=true; text.fontSizeMin=18; text.fontSizeMax=26; }
        }
        private void Show(RectTransform next)
        {
            if(next==main) RefreshSaveMetadata();
            if(page!=null) page.gameObject.SetActive(false);
            page=next;
            page.gameObject.SetActive(true);
            FocusCurrent();
        }
        private void OpenStats()
        {
            var s=LastSessionStats.GetValueOrDefault();
            statsText.text=(LastSessionStats.HasValue ? "LAST SESSION\n" : "NO SESSION\n")+
                "Rides  "+s.rides+"\nEarnings  "+TruckTaxiHud.Money(s.earningsCents)+
                "\nAverage stars  "+s.averageStars.ToString("0.0")+
                "\nChaos  "+s.chaos.ToString("0")+
                "\nPedestrians hit  "+s.pedestriansHit+
                "\nTraffic collisions  "+s.trafficCollisions+
                "\nPassengers ejected  "+s.passengersEjected+
                "\nJug events  "+s.jugEvents+
                "\nBottles thrown  "+s.bottlesThrown+
                "\nSpecial events  "+s.specialEvents+
                "\nObjectives completed  "+s.objectivesCompleted;
            Show(stats);
        }
        private void FocusCurrent()
        {
            if(input==null) return;
            input.Focus(audioPanel!=null && audioPanel.IsOpen ? audioPanel.FocusRoot :
                gpsPanel!=null && gpsPanel.IsOpen ? gpsPanel.FocusRoot : page);
        }
        private void Update() => FocusCurrent();
        private void Submit(InputAction.CallbackContext _) => input?.SubmitSelected();
        private void Cancel(InputAction.CallbackContext _)
        {
            if(audioPanel!=null && audioPanel.IsOpen) audioPanel.Close();
            else if(gpsPanel!=null && gpsPanel.IsOpen) gpsPanel.Close();
            else if(page!=main) Show(main);
            FocusCurrent();
        }
        private void InitializeSaves()
        {
            var application=LwsApplicationBootstrap.Instance;
            if(application==null)
            {
                // The generated menu has no application bootstrap. Configure the existing
                // bootstrap while inactive, before Awake can create career-scoped services.
                var owner=new GameObject("Vibin menu LWS services"); owner.SetActive(false);
                application=owner.AddComponent<LwsApplicationBootstrap>();
                JsonUtility.FromJsonOverwrite("{\"drivingSandbox\":true,\"dontDestroyOnLoad\":false,\"initializeOnAwake\":true}",application);
                owner.SetActive(true);
            }
            var result=TruckTaxiVibinPersistence.GetOrRegisterSaveService(application.Registry,out saves);
            discoveryReady=result.Succeeded && saves?.Adapter?.ReadyForRuntimeSaves==true;
            RefreshSaveMetadata();
        }
        public void RefreshSaveMetadata()
        {
            HasVibinSave=discoveryReady && TruckTaxiVibinPersistence.FindLatest(saves)!=null;
            profiles=discoveryReady ? saves.Profiles : Array.Empty<LwsSaveProfileMetadata>();
            SaveStatus=!discoveryReady ? "VIBIN' SAVES UNAVAILABLE" : HasVibinSave ? "VIBIN' SAVE AVAILABLE" : TruckTaxiVibinPersistence.NoSaveMessage;
            if(saveStatus!=null) saveStatus.text=SaveStatus;
            SetButton(newButton,"NEW GAME",discoveryReady && !enteringGame);
            SetButton(vibinButton,"VIBIN'",discoveryReady && !enteringGame);
            SetButton(continueButton,"CONTINUE VIBIN'",HasVibinSave && !enteringGame);
            SetButton(loadButton,"LOAD GAME",HasVibinSave && !enteringGame);
            RefreshLoadSlots();
        }
        private void OpenLoad()
        {
            RefreshSaveMetadata();
            if(!HasVibinSave) return;
            var latest=TruckTaxiVibinPersistence.FindLatest(saves);
            for(int i=0;i<profiles.Count;i++) if(profiles[i].stableProfileId==latest?.stableProfileId) profileIndex=i;
            RefreshLoadSlots(); Show(load);
        }
        private void ChangeProfile(int direction)
        {
            if(profiles.Count==0) return;
            profileIndex=(profileIndex+direction+profiles.Count)%profiles.Count;
            RefreshLoadSlots(); FocusCurrent();
        }
        private void RefreshLoadSlots()
        {
            if(profileText==null) return;
            profileIndex=Mathf.Clamp(profileIndex,0,Mathf.Max(0,profiles.Count-1));
            var profile=profiles.Count>0 ? profiles[profileIndex] : null;
            profileText.text=profile==null ? "VIBIN'" : profile.DisplayNameOrFallback+"  "+(profileIndex+1)+"/"+profiles.Count;
            SetButton(previousProfile,"<",profiles.Count>1 && !enteringGame);
            SetButton(nextProfile,">",profiles.Count>1 && !enteringGame);
            bool any=false;
            for(int i=0;i<slots.Length;i++)
            {
                slots[i]=profile==null ? null : i==3 ? saves.GetAutosaveSlotMetadata(profile.stableProfileId) : saves.GetSlotMetadata(profile.stableProfileId,i+1);
                bool exists=SlotExists(slots[i]); any|=exists;
                SetButton(slotButtons[i],FormatSlotLabel(slots[i],i==3 ? "AUTOSAVE" : "SLOT "+(i+1),exists),exists && !enteringGame);
            }
            loadStatus.text=!discoveryReady ? "VIBIN' SAVES UNAVAILABLE" : any ? "" : TruckTaxiVibinPersistence.NoSaveMessage;
        }
        public static string FormatSlotLabel(LwsManualSaveSlotMetadata slot,string label,bool exists)
        {
            if(!exists || slot==null) return label+" - EMPTY";
            string timestamp=slot.savedUtcTicks>0 && slot.savedUtcTicks<=DateTime.MaxValue.Ticks
                ? new DateTime(slot.savedUtcTicks,DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "SAVED";
            return label+"\n"+timestamp;
        }
        private bool SlotExists(LwsManualSaveSlotMetadata slot) => discoveryReady && slot!=null &&
            TruckTaxiVibinPersistence.ExistsInVendorStore(saves,slot);
        private void NewVibin()
        {
            if(!discoveryReady || enteringGame) return;
            TruckTaxiVibinPersistence.ClearContinueRequest();
            EnterVibin();
        }
        private void ContinueVibin()
        {
            RefreshSaveMetadata();
            if(!HasVibinSave || enteringGame) return;
            StartSaved(TruckTaxiVibinPersistence.FindLatest(saves));
        }
        private void LoadSlot(int index)
        {
            if(index<0 || index>=slots.Length || enteringGame) return;
            StartSaved(slots[index]);
        }
        private void StartSaved(LwsManualSaveSlotMetadata slot)
        {
            if(!SlotExists(slot)) { RefreshSaveMetadata(); return; }
            if(TruckTaxiVibinPersistence.SetContinueRequest(slot.stableProfileId,slot.slotNumber,slot.slotType==LwsSaveSlotType.Autosave)) EnterVibin();
        }
        private void EnterVibin()
        {
            if(!Application.CanStreamedLevelBeLoaded(VibinSceneName))
            { ReportEntryFailure(); return; }
            enteringGame=true;
            try { SceneManager.LoadScene(VibinSceneName); }
            catch { enteringGame=false; ReportEntryFailure(); }
        }
        private void ReportEntryFailure()
        {
            TruckTaxiVibinPersistence.ClearContinueRequest();
            SaveStatus="VIBIN' SCENE UNAVAILABLE";
            saveStatus.text=SaveStatus; loadStatus.text=SaveStatus;
        }
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy()
        {
            if(input==null) return;
            input.Submit.performed-=Submit;
            input.Cancel.performed-=Cancel;
            input.Dispose();
        }
    }
}
