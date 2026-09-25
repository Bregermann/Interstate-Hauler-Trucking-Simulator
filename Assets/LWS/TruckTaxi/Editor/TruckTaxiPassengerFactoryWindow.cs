using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWS.TruckTaxi.Editor
{
    public sealed class TruckTaxiPassengerFactoryWindow : EditorWindow
    {
        private PassengerProfile selected;
        private PassengerProfile[] passengers;
        private ListView passengerList;
        private VisualElement details;
        private Label status;
        private TextField logs;
        private DropdownField selectedLine;
        private string filter="";
        private string typeFilter="All",rarityFilter="All",seatFilter="All";

        [MenuItem("Truck Taxi/Passenger Factory")]
        public static void Open() => GetWindow<TruckTaxiPassengerFactoryWindow>("Passenger Factory");
        public void CreateGUI()
        {
            minSize = new Vector2(820, 620);
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/LWS/TruckTaxi/Editor/TruckTaxiPassengerFactory.uss");
            if (sheet != null) rootVisualElement.styleSheets.Add(sheet);
            var toolbar = new Toolbar(); rootVisualElement.Add(toolbar);
            toolbar.Add(new ToolbarButton(Refresh) { text = "Refresh" });
            toolbar.Add(new ToolbarButton(()=>Run(()=>{ selected=TruckTaxiPassengerFactoryBuilder.CreatePassenger("New Passenger"); Refresh(); })) { text="Create Passenger" });
            toolbar.Add(new ToolbarButton(() => Run(() => { TruckTaxiPassengerDialogueAuthoring.BuildAllMissingDialogueAssets(); Refresh(); })) { text = "Build Missing Dialogue Assets" });
            toolbar.Add(new ToolbarButton(() => Run(() => { EditorUtility.RevealInFinder(TruckTaxiPassengerDialogueAuthoring.WriteMissingVoiceReport()); })) { text = "Missing Voice Report" });
            var settings = new Foldout { text = "Existing Chatterbox Installation", value = true }; rootVisualElement.Add(settings);
            var path = new TextField("WorkHere path") { value = TruckTaxiChatterboxSettings.instance.workHere };
            path.RegisterValueChangedCallback(e => { TruckTaxiChatterboxSettings.instance.workHere = e.newValue; TruckTaxiChatterboxSettings.instance.Persist(); }); settings.Add(path);
            var offline = new Toggle("Offline (cached models only)") { value = TruckTaxiChatterboxSettings.instance.offline };
            offline.RegisterValueChangedCallback(e => { TruckTaxiChatterboxSettings.instance.offline = e.newValue; TruckTaxiChatterboxSettings.instance.Persist(); }); settings.Add(offline);
            var modelSettings=new Foldout { text="Installed Model Tools (Editor Only)",value=false }; rootVisualElement.Add(modelSettings);
            var blender=new TextField("Blender executable") { value=TruckTaxiFactorySettings.instance.blender };
            blender.RegisterValueChangedCallback(e=>{TruckTaxiFactorySettings.instance.blender=e.newValue; TruckTaxiFactorySettings.instance.Persist();}); modelSettings.Add(blender);
            var generator=new TextField("Optional generator executable") { value=TruckTaxiFactorySettings.instance.generatorExecutable };
            generator.RegisterValueChangedCallback(e=>{TruckTaxiFactorySettings.instance.generatorExecutable=e.newValue; TruckTaxiFactorySettings.instance.Persist();}); modelSettings.Add(generator);
            var arguments=new TextField("Generator arguments ({manifest})") { value=TruckTaxiFactorySettings.instance.generatorArguments };
            arguments.RegisterValueChangedCallback(e=>{TruckTaxiFactorySettings.instance.generatorArguments=e.newValue; TruckTaxiFactorySettings.instance.Persist();}); modelSettings.Add(arguments);
            var body = new VisualElement(); body.AddToClassList("factory-body"); rootVisualElement.Add(body);
            var left = new VisualElement(); left.AddToClassList("passenger-list"); body.Add(left);
            var search = new ToolbarSearchField(); left.Add(search);
            var characterTypes=new DropdownField("Type",new[]{"All"}.Concat(Enum.GetNames(typeof(TruckTaxiCharacterType))).ToList(),0);
            characterTypes.RegisterValueChangedCallback(e=>{typeFilter=e.newValue; Filter();}); left.Add(characterTypes);
            var rarities=new DropdownField("Rarity",new[]{"All"}.Concat(Enum.GetNames(typeof(TruckTaxiRarity))).ToList(),0);
            rarities.RegisterValueChangedCallback(e=>{rarityFilter=e.newValue; Filter();}); left.Add(rarities);
            var seats=new DropdownField("Seat",new[]{"All"}.Concat(Enum.GetNames(typeof(TruckTaxiSeatType))).ToList(),0);
            seats.RegisterValueChangedCallback(e=>{seatFilter=e.newValue; Filter();}); left.Add(seats);
            passengerList = new ListView { selectionType = SelectionType.Single, fixedItemHeight = 25 };
            passengerList.AddToClassList("grow"); passengerList.makeItem = () => new Label();
            passengerList.bindItem = (element, index) => ((Label)element).text = ((PassengerProfile)passengerList.itemsSource[index]).passengerName;
            passengerList.selectionChanged += items => Select(items.OfType<PassengerProfile>().FirstOrDefault()); left.Add(passengerList);
            search.RegisterValueChangedCallback(e => {filter=e.newValue; Filter();});
            var scroll = new ScrollView(); scroll.AddToClassList("grow"); body.Add(scroll); details = scroll;
            var actions = new VisualElement(); actions.AddToClassList("actions"); rootVisualElement.Add(actions);
            AddButton(actions, "Generate Selected Passenger", () => Generate(false));
            AddButton(actions, "Generate Missing Passenger Audio", () => Generate(true));
            AddButton(actions, "Generate All Missing Audio", () => TruckTaxiChatterboxQueue.Generate(passengers));
            AddButton(actions, "Generate All Audio (Cache Aware)", () => TruckTaxiChatterboxQueue.Generate(passengers, false));
            AddButton(actions, "Cancel Queue", TruckTaxiChatterboxQueue.Cancel);
            AddButton(actions, "Resume Queue", TruckTaxiChatterboxQueue.Resume);
            AddButton(actions,"Build All Passengers",()=>TruckTaxiPassengerFactoryBatch.Start(passengers));
            AddButton(actions,"Validate All",()=>{ TruckTaxiPassengerFactoryBuilder.WriteReports(); UpdateStatus(); });
            AddButton(actions,"Rebuild Invalid",()=>TruckTaxiPassengerFactoryBatch.Start(passengers.Where(p=>TruckTaxiPassengerFactoryBuilder.Validate(p).Any(i=>i.StartsWith("ERROR"))).ToArray(),true));
            AddButton(actions,"Pause Build",TruckTaxiPassengerFactoryBatch.Pause);
            AddButton(actions,"Resume Build",TruckTaxiPassengerFactoryBatch.Resume);
            AddButton(actions,"Cancel After Operation",TruckTaxiPassengerFactoryBatch.CancelAfterCurrent);
            status = new Label(); rootVisualElement.Add(status);
            logs = new TextField { multiline = true, isReadOnly = true }; logs.AddToClassList("logs"); rootVisualElement.Add(logs);
            Refresh(); UpdateStatus();
        }
        private void OnEnable() => TruckTaxiChatterboxQueue.Changed += UpdateStatus;
        private void OnDisable() => TruckTaxiChatterboxQueue.Changed -= UpdateStatus;
        private void OnInspectorUpdate() => UpdateStatus();
        private void Refresh()
        {
            passengers = TruckTaxiPassengerDialogueAuthoring.AllPassengers();
            if (passengerList == null) return;
            Filter(); Select(selected);
        }
        private void Filter()
        {
            if(passengerList==null || passengers==null) return;
            passengerList.itemsSource=passengers.Where(p=>
                (typeFilter=="All" || p.appearance?.characterType.ToString()==typeFilter) &&
                (rarityFilter=="All" || p.rarity.ToString()==rarityFilter) &&
                (seatFilter=="All" || p.seatProfile?.seatType.ToString()==seatFilter) &&
                string.Join(" ",p.passengerName,p.passengerId,p.casting?.species,p.casting?.raceEthnicity,p.personality,
                    string.Join(" ",p.specialTraits ?? Array.Empty<string>()),string.Join(" ",p.districts ?? Array.Empty<string>()),p.voiceProfile?.voiceProfileId)
                .IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            passengerList.Rebuild();
        }
        private void Select(PassengerProfile passenger)
        {
            selected = passenger; if (details == null) return; details.Clear();
            if (passenger == null) return;
            var header = new Label(passenger.passengerName); header.AddToClassList("section-title"); details.Add(header);
            AddButton(details, "Create Missing Voice / Dialogue Assets", () => { TruckTaxiPassengerDialogueAuthoring.BuildMissingDialogueAssets(passenger); Select(passenger); });
            Section("Passenger Data", passenger, false);
            if(passenger.casting!=null) Section("Casting",passenger.casting,false);
            if(passenger.appearance!=null) Section("Appearance / Model / Rig",passenger.appearance,false);
            if(passenger.animatorProfile!=null) Section("Animation",passenger.animatorProfile,false);
            if(passenger.seatProfile!=null) Section("Seat / Oversized",passenger.seatProfile,false);
            var build=new Foldout { text="Model, Prefab, Validation and Batch Operations",value=true }; details.Add(build);
            var commands=new VisualElement(); commands.AddToClassList("actions"); build.Add(commands);
            AddButton(commands,"Build Selected",()=>TruckTaxiPassengerFactoryBatch.Start(new[]{passenger}));
            AddButton(commands,"Build Missing Components",()=>{TruckTaxiPassengerFactoryBuilder.BuildMissing(passenger); Select(passenger);});
            AddButton(commands,"Rebuild Selected",()=>TruckTaxiPassengerFactoryBatch.Start(new[]{passenger},true));
            AddButton(commands,"Generate Model Manifest",()=>EditorUtility.RevealInFinder(TruckTaxiPassengerFactoryBuilder.WriteManifest(passenger)));
            AddButton(commands,"Generate Model",()=>{TruckTaxiPassengerFactoryBuilder.BuildMissing(passenger); EditorUtility.DisplayDialog("Model",TruckTaxiModelPipeline.Generate(passenger),"OK");});
            AddButton(commands,"Process Model",()=>TruckTaxiModelPipeline.ProcessModel(passenger));
            AddButton(commands,"Setup Humanoid",()=>TruckTaxiModelPipeline.SetupRig(passenger));
            AddButton(commands,"Build Ragdoll",()=>{TruckTaxiPassengerFactoryBuilder.BuildMissing(passenger); passenger.appearance.buildRagdoll=true; EditorUtility.SetDirty(passenger.appearance); TruckTaxiPassengerFactoryBuilder.BuildPrefab(passenger);});
            AddButton(commands,"Build Prefab",()=>TruckTaxiPassengerFactoryBuilder.BuildPrefab(passenger));
            AddButton(commands,"Validate Passenger",()=>EditorUtility.DisplayDialog("Validation",string.Join("\n",TruckTaxiPassengerFactoryBuilder.Validate(passenger)),"OK"));
            AddButton(commands,"Register Passenger",TruckTaxiPassengerFactoryBuilder.RegisterAll);
            AddButton(commands,"Import Dialogue",()=>{ string path=EditorUtility.OpenFilePanel("Import dialogue JSON / CSV / tab-separated text","","json,csv,txt,tsv"); if(!string.IsNullOrEmpty(path)) { TruckTaxiDialogueImport.Import(path); Select(passenger); } });
            AddButton(commands,"Open Output Folder",()=>EditorUtility.RevealInFinder(TruckTaxiPassengerFactoryBuilder.Root));
            AddButton(commands,"Open Reference Audio Folder",()=>EditorUtility.RevealInFinder(TruckTaxiChatterboxSettings.instance.workHere+"/voice_refs"));
            AddButton(commands,"Open Generation Log",()=>EditorUtility.RevealInFinder("Tools/TruckTaxiPassengerFactory/Logs"));
            AddButton(commands,"Regenerate Stale Audio",()=>TruckTaxiChatterboxQueue.Generate(new[]{passenger},true));
            if (passenger.voiceProfile != null)
            {
                Section("Voice", passenger.voiceProfile, true);
                AddButton(details, "Choose Neutral Reference WAV (external)", () =>
                {
                    string path = EditorUtility.OpenFilePanel("Neutral source recording (not copied)", TruckTaxiChatterboxSettings.instance.workHere + "/voice_refs", "wav");
                    if (string.IsNullOrWhiteSpace(path)) return;
                    Undo.RecordObject(passenger.voiceProfile, "Assign source voice reference"); passenger.voiceProfile.neutralReference = path;
                    EditorUtility.SetDirty(passenger.voiceProfile); AssetDatabase.SaveAssets(); Select(passenger);
                });
            }
            if (passenger.authoredDialogue != null)
            {
                Section("Dialogue", passenger.authoredDialogue, true);
                var choices = (passenger.authoredDialogue.lines ?? Array.Empty<TruckTaxiDialogueLine>()).Where(l => l != null).Select(l => l.lineId).ToList();
                selectedLine = new DropdownField("Selected Line", choices, choices.Count > 0 ? 0 : -1); details.Add(selectedLine);
                var row = new VisualElement(); row.AddToClassList("actions"); details.Add(row);
                AddButton(row, "Generate Selected Line", () => GenerateLine(false));
                AddButton(row, "Regenerate Selected Line", () =>
                {
                    if (EditorUtility.DisplayDialog("Regenerate line", "Replace the generated WAV for this exact line/settings? Source recordings are untouched.", "Regenerate", "Cancel")) GenerateLine(true);
                });
                AddButton(row, "Select Generated Clip", () => Selection.activeObject = CurrentLine()?.generatedAudio);
            }
        }
        private void Section(string title, UnityEngine.Object asset, bool expanded)
        {
            var foldout = new Foldout { text = title, value = expanded }; foldout.Add(new InspectorElement(asset)); details.Add(foldout);
        }
        private TruckTaxiDialogueLine CurrentLine() => selected?.authoredDialogue?.lines?.FirstOrDefault(l => l != null && l.lineId == selectedLine?.value);
        private void GenerateLine(bool regenerate)
        {
            var line = CurrentLine(); if (line == null) throw new InvalidOperationException("Select a dialogue line.");
            TruckTaxiChatterboxQueue.Generate(new[] { selected }, false, regenerate, line);
        }
        private void Generate(bool missing) { if (selected != null) TruckTaxiChatterboxQueue.Generate(new[] { selected }, missing); }
        private void AddButton(VisualElement parent, string text, Action action) => parent.Add(new Button(() => Run(action)) { text = text });
        private void Run(Action action) { try { action(); } catch (Exception ex) { Debug.LogException(ex); EditorUtility.DisplayDialog("Passenger Factory", ex.Message, "OK"); } }
        private void UpdateStatus()
        {
            if (status == null || logs == null) return;
            status.text = TruckTaxiPassengerFactoryBatch.Status+"\n"+TruckTaxiChatterboxQueue.Status+" | "+TruckTaxiModelPipeline.Status;
            logs.SetValueWithoutNotify(TruckTaxiPassengerFactoryBatch.Log+TruckTaxiModelPipeline.Log+TruckTaxiChatterboxQueue.Log);
        }
    }
}
