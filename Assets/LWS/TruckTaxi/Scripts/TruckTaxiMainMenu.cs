using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // A transient handoff only. The driving sandbox does not create or load career saves.
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
        public const string FreePlaySceneName = "TruckTaxi_DemoCity";
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

        private void Start()
        {
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
            input=new TruckTaxiUIInput(EventSystem.current);
            input.Submit.performed+=Submit;
            input.Cancel.performed+=Cancel;
            Show(main);
        }

        private void BuildPages()
        {
            view.Text(viewRoot,"Truck Taxi title",new Vector2(.04f,.88f),new Vector2(.44f,.98f),54).text="TRUCK TAXI";
            main=Page("Main menu");
            Button(main,"NEW GAME",.76f,()=>Show(modes));
            Button(main,"LOAD GAME",.62f,()=>Show(load));
            Button(main,"OPTIONS",.48f,()=>Show(options));
            Button(main,"STATS",.34f,OpenStats);
            Button(main,"QUIT GAME",.20f,Quit);

            modes=Page("Mode select");
            Label(modes,"NEW GAME");
            Disabled(modes,"STORY MODE - COMING SOON",.70f);
            Disabled(modes,"ARCADE MODE - COMING SOON",.55f);
            Button(modes,"FREE PLAY",.40f,FreePlay);
            Button(modes,"BACK",.12f,()=>Show(main));

            load=Page("Load game");
            Label(load,"LOAD GAME");
            view.Text(load,"Save status",new Vector2(.08f,.42f),new Vector2(.92f,.67f),28).text="NO SAVE AVAILABLE";
            Button(load,"BACK",.12f,()=>Show(main));

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
        private void Button(RectTransform panel,string label,float y,System.Action action) =>
            view.Button(panel,label,new Vector2(.08f,y),new Vector2(.92f,y+.105f),action);
        private void Disabled(RectTransform panel,string label,float y)
        {
            var button=view.Button(panel,label,new Vector2(.08f,y),new Vector2(.92f,y+.105f),()=>{});
            foreach(var selectable in button.GetComponents<Selectable>()) selectable.interactable=false;
            foreach(var component in button.GetComponents<Component>())
                if(component.GetType().FullName=="Michsky.UI.Heat.ButtonManager")
                {
                    var type=component.GetType();
                    type.GetField("isInteractable").SetValue(component,false);
                    type.GetMethod("UpdateUI").Invoke(component,null);
                }
        }
        private void Show(RectTransform next)
        {
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
        private static void FreePlay()
        {
            if(!Application.CanStreamedLevelBeLoaded(FreePlaySceneName))
            {
                Debug.LogError("TruckTaxi_DemoCity is missing from enabled Build Settings.");
                return;
            }
            SceneManager.LoadScene(FreePlaySceneName);
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
