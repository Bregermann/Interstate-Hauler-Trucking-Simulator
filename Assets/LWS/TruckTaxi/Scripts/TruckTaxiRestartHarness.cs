using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckTaxi
{
    // No production backend. This only configures the existing vendor and drives real menu callbacks.
    public sealed class TruckTaxiRestartHarness : MonoBehaviour
    {
        private static bool argumentsRead, requested, configured;
        private static string failure;
        private static TruckTaxiRestartTestArguments request;
        public static bool Requested { get { ReadArguments(); return requested; } }
        public static bool Configured => configured && string.IsNullOrEmpty(failure);
        public static string Failure { get { ReadArguments(); return failure; } }

        private static void ReadArguments()
        {
            if (argumentsRead) return;
            argumentsRead = true;
            var args = Environment.GetCommandLineArgs();
            requested = TruckTaxiRestartTestArguments.IsRequested(args);
            if (requested) TruckTaxiRestartTestArguments.TryParse(args, Application.temporaryCachePath, out request, out failure);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        { argumentsRead = requested = configured = false; failure = null; request = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureBeforeDiscovery()
        {
            if (!Requested) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isEditor) { failure = "Restart harness requires a standalone development player."; Debug.LogError(failure); return; }
            if (failure != null) { Debug.LogError("TRUCK TAXI RESTART FAIL: " + failure); Application.Quit(2); return; }
            var root = new GameObject("Opt-in Truck Taxi restart harness");
            DontDestroyOnLoad(root);
            var harness = root.AddComponent<TruckTaxiRestartHarness>();
            try { harness.ConfigureVendor(); configured = true; }
            catch (Exception ex) { harness.Fail(ex); }
#else
            failure = "Restart tests are disabled in non-development players.";
            Debug.LogError(failure); Application.Quit(2);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private Component disk;
        private PropertyInfo instanceProperty, storerProperty;
        private FieldInfo basePathField, customPathField;
        private object customEnum;
        private MethodInfo newGame, continueGame, openLoad, loadSlot;
        private FieldInfo menuReady, menuSaves;
        private string loggedError;
        private bool finished;
        private readonly Stack<IEnumerator> activeIterators = new Stack<IEnumerator>();
        private float deadline;
        private const string Prefix = "TRUCK TAXI RESTART ";
        private const int EvidenceVersion = 2;

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            { var type = assembly.GetType(fullName, false); if (type != null) return type; }
            throw new InvalidOperationException("Required installed type missing: " + fullName);
        }

        private void ConfigureVendor()
        {
            request.VerifyFilesystemBoundary();
            if (request.Mode == "seed" && Directory.Exists(request.Root))
                throw new InvalidOperationException("Seed requires a fresh GUID directory; no overwrite of an existing test root.");
            if (request.Mode != "seed" && (!Directory.Exists(request.Root) || !File.Exists(Path.Combine(request.Root, "saveinfo.dat"))))
                throw new InvalidOperationException("Existing isolated vendor root/saveinfo.dat is required; no fallback root.");
            var saveType = FindType("PixelCrushers.SaveSystem");
            var diskType = FindType("PixelCrushers.DiskSavedGameDataStorer");
            var storerType = FindType("PixelCrushers.SavedGameDataStorer");
            instanceProperty = saveType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            storerProperty = saveType.GetProperty("storer", BindingFlags.Public | BindingFlags.Static);
            basePathField = diskType.GetField("storeSaveFilesIn", BindingFlags.Public | BindingFlags.Instance);
            customPathField = diskType.GetField("customPath", BindingFlags.Public | BindingFlags.Instance);
            if (instanceProperty == null || storerProperty == null || basePathField == null || customPathField == null)
                throw new MissingMemberException("Installed Pixel Crushers public storer configuration is unavailable.");
            customEnum = Enum.Parse(basePathField.FieldType, "Custom");
            var owner = instanceProperty.GetValue(null) as Component;
            if (owner == null) throw new InvalidOperationException("SaveSystem.instance is missing.");
            var existing = owner.GetComponents(storerType);
            if (existing.Length > 1 || existing.Length == 1 && !diskType.IsInstanceOfType(existing[0]))
                throw new InvalidOperationException("Unexpected existing vendor storer; refusing to replace it.");
            // No storer getter is accessed until the disk component has its safe public configuration.
            disk = existing.Length == 1 ? existing[0] : owner.gameObject.AddComponent(diskType);
            if (disk is Behaviour pendingStorer) pendingStorer.enabled = false;
            basePathField.SetValue(disk, customEnum);
            customPathField.SetValue(disk, request.Root);
            request.VerifyFilesystemBoundary();
            if (request.Mode == "seed") Directory.CreateDirectory(request.Root);
            request.VerifyFilesystemBoundary();
            VerifyVendor();
            if (disk is Behaviour readyStorer) readyStorer.enabled = true;
            var menuType = typeof(TruckTaxiMainMenu);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            newGame = menuType.GetMethod("NewVibin", flags);
            continueGame = menuType.GetMethod("ContinueVibin", flags);
            openLoad = menuType.GetMethod("OpenLoad", flags);
            loadSlot = menuType.GetMethod("LoadSlot", flags);
            menuReady = menuType.GetField("discoveryReady", flags);
            menuSaves = menuType.GetField("saves", flags);
            if (newGame == null || continueGame == null || openLoad == null || loadSlot == null || menuReady == null || menuSaves == null)
                throw new MissingMemberException("Actual Truck Taxi menu callbacks changed; refusing alternate load path.");
            Application.runInBackground = true;
            Debug.Log(Prefix + "CONFIGURED mode=" + request.Mode + " run=" + request.RunId + " vendorRoot=" + request.Root + " evidenceVersion=" + EvidenceVersion);
        }

        private void VerifyVendor()
        {
            request.VerifyFilesystemBoundary();
            if (disk == null || !Equals(basePathField.GetValue(disk), customEnum) ||
                !string.Equals(customPathField.GetValue(disk) as string, request.Root, StringComparison.Ordinal) ||
                !ReferenceEquals(storerProperty.GetValue(null), disk) ||
                !(instanceProperty.GetValue(null) is Component owner) || disk.gameObject != owner.gameObject)
                throw new InvalidOperationException("Vendor isolation changed; refusing save, load, or deletion.");
        }

        private void OnEnable() => Application.logMessageReceived += ObserveError;
        private void OnDisable()
        {
            Application.logMessageReceived -= ObserveError;
            if (activeIterators.Count > 0 && !finished)
                Fail(new OperationCanceledException("Restart harness disabled during validation."));
        }
        private void ObserveError(string message, string stack, LogType type)
        {
            if (!finished && loggedError == null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                loggedError = message;
        }
        private void Update()
        {
            if (finished || !Configured) return;
            if (loggedError != null) Fail(new InvalidOperationException("Player logged an error: " + loggedError));
            else if (deadline > 0 && Time.realtimeSinceStartup > deadline) Fail(new TimeoutException("Harness exceeded its bounded validation deadline."));
        }
        private void Fail(Exception ex)
        {
            if (finished) return;
            finished = true; configured = false;
            if (disk is Behaviour storer) storer.enabled = false;
            failure = (ex is TargetInvocationException invocation ? invocation.InnerException ?? ex : ex).ToString();
            try { GetComponent<TruckTaxiDrivingPerformanceProbe>()?.RecordSoakFailure(failure); }
            catch (Exception reportError) { failure += "\nFailure report: " + reportError; }
            var cleanupError = DisposeIterators(activeIterators);
            if (cleanupError != null) failure += "\nIterator cleanup: " + cleanupError;
            Debug.LogError(Prefix + "FAIL: " + failure);
            StopAllCoroutines(); Application.Quit(2);
        }
        private void Check(bool condition, string assertion)
        {
            if (!condition) throw new InvalidOperationException(assertion);
            Debug.Log(Prefix + "ASSERT PASS: " + assertion);
        }
        private void Result(LwsSaveOperationResult result, string assertion) => Check(result.Succeeded, assertion + " | " + result.Message);

        // Pop before disposing so an outer finally cannot dispose the same iterator twice.
        public static Exception DisposeIterators(Stack<IEnumerator> stack)
        {
            var errors = new List<Exception>();
            while (stack.Count > 0)
            {
                try { (stack.Pop() as IDisposable)?.Dispose(); }
                catch (Exception error) { errors.Add(error); }
            }
            return errors.Count == 0 ? null : new AggregateException(errors);
        }

        // Flatten our iterators so exceptions in nested waits also result in a nonzero process exit.
        private IEnumerator Start()
        {
            if (!Configured || finished) yield break;
            deadline = Time.realtimeSinceStartup + (request.Mode=="soak" ? 4200 : 600);
            float nextIsolationCheck=0;
            var stack = activeIterators; stack.Push(Run());
            while (stack.Count > 0 && !finished)
            {
                object next = null; Exception error = null;
                try
                {
                    if (loggedError != null) throw new InvalidOperationException(loggedError);
                    if(request.Mode!="soak" || Time.realtimeSinceStartup>=nextIsolationCheck)
                    { VerifyVendor(); nextIsolationCheck=Time.realtimeSinceStartup+1; }
                    var current = stack.Peek();
                    if (!current.MoveNext()) { stack.Pop(); (current as IDisposable)?.Dispose(); }
                    else if (current.Current is IEnumerator nested) stack.Push(nested);
                    else next = current.Current;
                }
                catch (Exception ex) { error = ex; }
                if (error != null) { Fail(error); yield break; }
                if (next != null || stack.Count > 0) yield return next;
            }
            if (finished) yield break;
            finished = true;
            Debug.Log(Prefix + "PASS mode=" + request.Mode + " run=" + request.RunId + " evidenceVersion=" + EvidenceVersion + " (this process only)");
            Application.Quit(0);
        }

        private TruckTaxiMainMenu menu;
        private TruckTaxiBootstrap host;
        private LwsSaveService saves;
        private IEnumerator WaitForMenu()
        {
            float until = Time.realtimeSinceStartup + 90;
            while (Time.realtimeSinceStartup < until)
            {
                menu = FindFirstObjectByType<TruckTaxiMainMenu>();
                if (menu != null && menuReady.GetValue(menu) is bool ready && ready)
                {
                    saves = menuSaves.GetValue(menu) as LwsSaveService;
                    Check(TruckTaxiVibinPersistence.IsVibinScope(saves) && saves.Adapter.ReadyForRuntimeSaves, "actual menu discovered scoped vendor facade");
                    yield break;
                }
                yield return null;
            }
            throw new TimeoutException("Main menu discovery did not become ready.");
        }
        private IEnumerator WaitForRun(bool pauseForSignature = true)
        {
            float until = Time.realtimeSinceStartup + 180;
            while (Time.realtimeSinceStartup < until)
            {
                host = TruckTaxiBootstrap.Instance;
                if (!pauseForSignature && driveSourceBeforeLoad == null && host?.Player != null &&
                    LwsApplicationBootstrap.Instance?.Registry != null &&
                    LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsVehicleInputService observedInput) &&
                    observedInput.ActiveSourceId == "lws.input.keyboard-gamepad.truck")
                    CaptureDriveInput(observedInput);
                if (host != null && host.CanResume && host.Persistence?.HasActiveRun == true && !host.Persistence.IsBusy && !host.Persistence.ResumeBlocked)
                {
                    if (pauseForSignature) { host.SetPaused(true); host.Environment.Clock.SetPaused(true); }
                    saves = host.Persistence.SaveService;
                    Check(FindObjectsByType<TruckTaxiBootstrap>(FindObjectsSortMode.None).Length == 1, "one active Truck Taxi host");
                    yield break;
                }
                yield return null;
            }
            throw new TimeoutException("Vibin load/start timed out: " + host?.Persistence?.Status);
        }

        private LwsSaveProfileMetadata FindTestProfile(bool required)
        {
            VerifyVendor();
            LwsSaveProfileMetadata found = null;
            foreach (var profile in saves.Profiles)
            {
                if (profile.displayName == TruckTaxiRestartTestArguments.ProfileName && profile.profileScopeId == TruckTaxiVibinPersistence.ScopeId)
                { Check(found == null, "test profile name is unique"); found = profile; continue; }
                // The facade creates this empty scoped placeholder independently of New Game.
                Check(profile.stableProfileId == "profile.truck-taxi" && profile.profileScopeId == TruckTaxiVibinPersistence.ScopeId,
                    "no unexpected profile in isolated directory: " + profile.stableProfileId);
                foreach (var slot in saves.GetManualSlots(profile.stableProfileId))
                    Check(!TruckTaxiVibinPersistence.ExistsInVendorStore(saves, slot), "placeholder has no manual save");
                Check(!TruckTaxiVibinPersistence.ExistsInVendorStore(saves, saves.GetAutosaveSlotMetadata(profile.stableProfileId)), "placeholder has no autosave");
            }
            if (required) Check(found != null, "ASTRA_SANDBOX_TEST discovered through vendor directory");
            return found;
        }

        private void AssertSlots(string profileId)
        {
            foreach (int number in new[] { 1, 2 })
                Check(TruckTaxiVibinPersistence.ExistsInVendorStore(saves, saves.GetSlotMetadata(profileId, number)), "vendor manual slot " + number + " exists");
            Check(TruckTaxiVibinPersistence.ExistsInVendorStore(saves, saves.GetAutosaveSlotMetadata(profileId)), "vendor autosave exists");
        }

        private IEnumerator Run()
        {
            Check(SceneManager.GetActiveScene().name == TruckTaxiMainMenu.SceneName, "player boots into the actual main menu");
            yield return WaitForMenu();
            if (request.Drive)
            {
                Check(Application.platform == RuntimePlatform.WindowsPlayer, "drive regression requires an actual Windows player");
                yield return RunDriveRegression();
                yield break;
            }
            if (request.Mode == "seed")
            {
                Check(FindTestProfile(false) == null && !menu.HasVibinSave, "fresh isolated store has no playable save");
                newGame.Invoke(menu, null);
                yield return WaitForRun();
                yield return AcknowledgeFirstDay();
                Result(saves.RenameProfile(host.Persistence.RunProfileId, TruckTaxiRestartTestArguments.ProfileName), "rename through facade");
                string id = FindTestProfile(true).stableProfileId;
                Check(host.Persistence.RunProfileId == id, "run owns isolated profile " + id);
                Seed(0); Result(host.Persistence.Save(1), "manual slot 1 initial write");
                Seed(1); Result(host.Persistence.Save(1, true), "manual slot 1 overwrite");
                Seed(2); Result(host.Persistence.Save(2), "manual slot 2 write");
                Seed(3); Result(host.Persistence.SaveBeforeLeaving(), "immediate vendor autosave");
                AssertSlots(id);
                host.ReturnToMainMenu(); yield return WaitForMenu();
                FindTestProfile(true); openLoad.Invoke(menu, null); loadSlot.Invoke(menu, new object[] { 0 });
                yield return WaitForRun(); Validate(1);
                host.ReturnToMainMenu(); yield return WaitForMenu();
                continueGame.Invoke(menu, null);
                yield return WaitForRun(); Validate(1);
                Seed(3); Result(host.Persistence.SaveBeforeLeaving(), "final restart autosave signature 3");
                AssertSlots(id);
                yield break;
            }
            var test = FindTestProfile(true);
            if (request.Mode == "cleanup")
            {
                var slots = new List<int>();
                foreach (int slot in LwsSaveSchema.EnumerateReservedVendorSlots(test.profileIndex)) slots.Add(slot);
                VerifyVendor();
                Result(saves.DeleteProfile(test.stableProfileId), "delete only isolated ASTRA_SANDBOX_TEST via facade");
                Check(FindTestProfile(false) == null, "test profile removed");
                foreach (int slot in slots) Check(!saves.Adapter.HasSaveInSlot(slot), "deleted profile vendor slot absent: " + slot);
                yield break;
            }
            AssertSlots(test.stableProfileId);
            if (request.Mode == "continue" || request.Mode=="soak") continueGame.Invoke(menu, null);
            else { openLoad.Invoke(menu, null); loadSlot.Invoke(menu, new object[] { 1 }); }
            yield return WaitForRun();
            Check(host.Persistence.RunProfileId == test.stableProfileId, "fresh process restored same stable profile");
            Validate(request.Mode == "continue" || request.Mode=="soak" ? 3 : 2);
            if(request.Mode=="soak")
            {
                var probe=gameObject.AddComponent<TruckTaxiDrivingPerformanceProbe>();
                yield return probe.RunIsolatedSoak(host);
                VerifyVendor();
            }
        }

        private ILwsWorldOriginService driveOrigin;
        private Keyboard driveKeyboard;
        private ILwsVehicleInputSource driveSourceBeforeLoad;
        private ILwsVehicleInputService driveInputService;
        private LwsVehicleInputOwner driveInputOwner;

        private static ILwsVehicleInputSource RegisteredDriveSource(ILwsVehicleInputService input)
        {
            // Read-only harness inspection: the public service exposes an ID, not source identity.
            var field = typeof(LwsVehicleInputService).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(input is LwsVehicleInputService) || field == null)
                throw new InvalidOperationException("Registered input identity inspection contract changed.");
            return field.GetValue(input) as ILwsVehicleInputSource;
        }

        private void CaptureDriveInput(ILwsVehicleInputService input)
        {
            driveSourceBeforeLoad = RegisteredDriveSource(input);
            driveInputService = input;
            driveInputOwner = input.ActiveOwner;
            Debug.Log(Prefix + "DRIVE SOURCE OBSERVED before completion: " + input.ActiveSourceId + " owner=" + driveInputOwner);
        }

        private IEnumerator RunDriveRegression()
        {
            if (request.Mode == "seed")
            {
                Check(FindTestProfile(false) == null && !menu.HasVibinSave, "fresh drive-only vendor fixture");
                newGame.Invoke(menu, null);
                yield return WaitForRun();
                yield return AcknowledgeFirstDay();
                Result(saves.RenameProfile(host.Persistence.RunProfileId, TruckTaxiRestartTestArguments.ProfileName), "name isolated drive fixture");
                Check(LwsApplicationBootstrap.Instance.Registry.TryGet(out driveOrigin), "drive seed origin authority");
                Check(TruckTaxiFlagshipRoute.TryCreate(host.roadGraph?.Graph, host.traffic?.cityLanes, out var road, out var reason),
                    "drive seed uses validated authored lane: " + reason);
                Check(TrySeedDriveRoad(road, null, out string firstRegion), "loaded safe straight road exists for slot 1");
                yield return PrepareDriveSeedEngine();
                Result(host.Persistence.Save(1), "drive road slot 1");
                Result(host.Persistence.SaveBeforeLeaving(), "drive road restart autosave");
                yield return SeedSecondDriveRegion(firstRegion);
                yield return PrepareDriveSeedEngine();
                Result(host.Persistence.Save(2), "drive road slot 2");
                AssertSlots(host.Persistence.RunProfileId);
                Debug.Log(Prefix + "DRIVE SEED ONLY: restart separate continue/manual processes with the same GUID and drive flag; no driving proof yet.");
                yield break;
            }

            var profile = FindTestProfile(true); AssertSlots(profile.stableProfileId);
            if (request.Mode == "continue") continueGame.Invoke(menu, null);
            else { openLoad.Invoke(menu, null); loadSlot.Invoke(menu, new object[] { 0 }); }
            yield return WaitForRun(false);
            yield return AssertPostLoadDrive("fresh-process " + request.Mode, profile.stableProfileId);
            yield return WaitForDriveSaveSafety();
            // Keep regional seed slots 1/2 intact. Slot 3 proves saving an actually driven pose.
            const int drivenSlot = 3;
            string initialRegion = host.GetComponent<TruckTaxiRegionalWorld>().ResolveRegion(host.Player.transform.position);
            RequireDriveReady();
            Check(host.Player.GetComponent<Rigidbody>().linearVelocity.magnitude <= .2f &&
                host.Player.TruckControlController.CurrentState.parkingBrakeOn && host.Player.CaptureState().engineRunning &&
                DriveRoadClear(host.Player.GetComponent<Rigidbody>(), host.Player.transform.position, host.Player.transform.rotation, out _, 25),
                "actual driven pose remains stopped, parked, engine-running on validated road before save | " + DriveDiagnostics() +
                " speed=" + host.Player.GetComponent<Rigidbody>().linearVelocity.magnitude + " road=" +
                DriveRoadClear(host.Player.GetComponent<Rigidbody>(), host.Player.transform.position, host.Player.transform.rotation, out _, 25));
            var drivenPosition = driveOrigin.LocalToGlobal(host.Player.transform.position);
            Result(host.Persistence.Save(drivenSlot, true), "save actual post-drive state, not the original seed pose");
            for (int pass = 0; pass < 2; pass++)
            {
                int slot = pass == 0 ? drivenSlot : request.Mode == "continue" ? 1 : 2;
                yield return WaitForDriveSaveSafety();
                RequireDriveReady(); // Source object/owner retained from the previous successful drive, before BeginLoad.
                yield return LoadDriveSlotThroughPanel(profile.stableProfileId, slot);
                if (pass == 0) Check(driveOrigin.LocalToGlobal(host.Player.transform.position).DistanceTo(drivenPosition) <= 1,
                    "post-drive save restored measured global position before driving again");
                else Check(host.GetComponent<TruckTaxiRegionalWorld>().ResolveRegion(host.Player.transform.position) != initialRegion,
                    "second load really changes region; regional seed slots were not overwritten");
                yield return AssertPostLoadDrive("in-session slot " + slot, profile.stableProfileId);
            }

            yield return WaitForDriveSaveSafety();
            var retiredSource = driveSourceBeforeLoad;
            var persistentInput = driveInputService;
            host.ReturnToMainMenu();
            yield return WaitForMenu();
            Check(!persistentInput.HasActiveSource, "destroyed truck released its input registration on menu return");
            // Reset observations only. The replacement truck must register itself through normal spawning.
            driveSourceBeforeLoad = null; driveInputService = null; driveInputOwner = LwsVehicleInputOwner.None;
            continueGame.Invoke(menu, null);
            yield return WaitForRun(false);
            Check(!ReferenceEquals(retiredSource, driveSourceBeforeLoad), "newly spawned truck owns its own live input source");
            yield return AssertPostLoadDrive("same-process menu-return Continue", profile.stableProfileId);
        }

        private IEnumerator LoadDriveSlotThroughPanel(string profileId, int slot)
        {
            var panel = host.hud?.SaveLoad;
            Check(panel != null && !panel.IsOpen, "actual save/load panel exists and starts closed");
            var requestLoad = typeof(TruckTaxiVibinSavePanel).GetMethod("RequestLoad", BindingFlags.Instance | BindingFlags.NonPublic);
            var confirmLoad = typeof(TruckTaxiVibinSavePanel).GetMethod("Confirm", BindingFlags.Instance | BindingFlags.NonPublic);
            var pending = typeof(TruckTaxiVibinSavePanel).GetField("pending", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(requestLoad != null && confirmLoad != null && pending != null, "actual panel callback contract available");
            bool completed = false; LwsSaveOperationResult outcome = default;
            Action<LwsSaveOperationResult> loaded = result => { completed = true; outcome = result; };
            host.Persistence.LoadCompleted += loaded;
            try
            {
                panel.Open();
                Check(panel.IsOpen && !panel.IsBusy, "actual in-session load panel opened");
                requestLoad.Invoke(panel, new object[] { saves.GetSlotMetadata(profileId, slot) });
                Check(pending.GetValue(panel) != null, "actual slot confirmation opened");
                confirmLoad.Invoke(panel, null);
                float until = Time.realtimeSinceStartup + 180;
                while (!completed && Time.realtimeSinceStartup < until) yield return null;
                Check(completed, "panel load completed within deadline");
                Result(outcome, "actual panel load succeeded");
                Check(!panel.IsOpen && !panel.IsBusy && panel.FocusRoot == null, "successful load closed panel and cleared its focus without harness dismissal");
                yield return WaitForRun(false);
            }
            finally { if (host?.Persistence != null) host.Persistence.LoadCompleted -= loaded; }
        }

        private IEnumerator WaitForDriveSaveSafety()
        {
            // A natural hazard may legitimately prohibit saving. Wait while parked; never clear
            // live gameplay or bypass the save guard after a load to manufacture a passing result.
            if (host.Extremes?.LivePhysicsActorCount <= 0) yield break;
            Debug.Log(Prefix + "DRIVE waiting parked for active world hazard to finish naturally before save/load.");
            float deadline = Time.realtimeSinceStartup + 180;
            while (host.Extremes?.LivePhysicsActorCount > 0)
            {
                RequireDriveReady();
                if (Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException("World hazard still prevents ordinary save/load: " + DriveDiagnostics());
                yield return null;
            }
            Debug.Log(Prefix + "DRIVE natural hazard finished; normal save/load safety retained.");
        }

        private IEnumerator PrepareDriveSeedEngine()
        {
            // Seed only. Let native physics/starter run on the grounded road, held by the ordinary parking command.
            var controls = host.Player.TruckControlController;
            host.SetPaused(false); host.Environment.Clock.SetPaused(false);
            // NWH calculates the starter's idle angular velocity in its first physics integration.
            // Seed creation began paused; do not start the engine before that integration has occurred.
            yield return new WaitForFixedUpdate();
            controls.ApplyCommandFrame(new LwsVehicleCommandFrame { engineStart = LwsMomentaryIntent.Pressed,
                parkingBrakeToggle = controls.CurrentState.parkingBrakeOn ? LwsMomentaryIntent.None : LwsMomentaryIntent.Pressed }, default);
            float until = Time.realtimeSinceStartup + 15;
            do { yield return null; }
            while ((!host.Player.CaptureState().engineRunning || host.Player.GetComponent<Rigidbody>().linearVelocity.magnitude > .1f) &&
                Time.realtimeSinceStartup < until);
            Check(host.Player.CaptureState().engineRunning, "seed captures actual running NWH engine | " + DriveDiagnostics() +
                " telemetry=" + JsonUtility.ToJson(host.Player.NwhAdapter.ReadTelemetry()) +
                " starter=" + host.Player.NwhAdapter.VehicleController.powertrain.engine.starterActive +
                " ignition=" + host.Player.NwhAdapter.VehicleController.powertrain.engine.ignition +
                " fixedTime=" + Time.fixedTime);
            Check(host.Player.GetComponent<Rigidbody>().linearVelocity.magnitude <= .1f, "seed settled stationary under native parking brake");
            // The isolated seed must be an ordinary idle drive, not the initial day's random hazard.
            // Never clear hazards or any input/physics hold after loading to make drive checks pass.
            if (host.Extremes?.LivePhysicsActorCount > 0)
            {
                Debug.Log(Prefix + "DRIVE SEED clearing initial ambient hazard through director before saving.");
                host.Extremes.Clear();
            }
            host.SetPaused(true); host.Environment.Clock.SetPaused(true);
            if (controls.CurrentState.parkingBrakeOn)
                controls.ApplyCommandFrame(new LwsVehicleCommandFrame { parkingBrakeToggle = LwsMomentaryIntent.Pressed }, default);
            Check(!controls.CurrentState.parkingBrakeOn, "seed parking brake off (not a persisted payload field)");
        }

        // Only the seed branch can relocate or configure controls. Loaded runs never use this setup.
        private bool TrySeedDriveRoad(TruckTaxiFlagshipRoute road, string excludedRegion, out string selectedRegion)
        {
            selectedRegion = null;
            var body = host.Player.NwhAdapter.VehicleController.vehicleRigidbody;
            var world = host.GetComponent<TruckTaxiRegionalWorld>();
            Check(!host.Player.CouplingAdapter.IsTrailerAttached && !body.isKinematic && !driveOrigin.HasQueuedShift &&
                !driveOrigin.ShiftInProgress, "seed has canonical dynamic uncoupled truck and stable origin");
            for (int i = 0; i < road.Points.Length; i += 8)
            {
                Vector3 local = driveOrigin.GlobalToLocal(LwsWorldPositionD.FromVector3(road.Points[i]));
                string region = world.ResolveRegion(local);
                if (string.IsNullOrEmpty(region) || region == excludedRegion || !world.IsPositionAvailable(local)) continue;
                Quaternion rotation = Quaternion.LookRotation(road.Facing(i), Vector3.up);
                if (!TryPlaceDriveSeed(local, rotation, region)) continue;
                selectedRegion = region;
                return true;
            }
            return false;
        }

        public static bool TryDriveEdgeSeed(LwsRoadEdge edge, out Vector3 global, out Vector3 forward)
        {
            global = forward = Vector3.zero;
            if (edge == null || !edge.oneWay || edge.roadClass != LwsRoadClass.LocalRoad ||
                edge.surfaceType != LwsRoadSurfaceType.AsphaltInterstate || edge.laneCount != 1 || edge.laneWidthMeters < 3 ||
                edge.laneCenterOffsetsMeters?.Count != 1 || edge.samples == null || edge.samples.Count > 8192 ||
                !float.IsFinite(edge.distanceMeters) || edge.distanceMeters < 130 ||
                !float.IsFinite(edge.laneCenterOffsetsMeters[0])) return false;
            if (edge.restrictions != null) foreach (var restriction in edge.restrictions)
                if (restriction != null && (!restriction.truckAllowed || restriction.maxVehicleHeightMeters > 0 || restriction.maxVehicleWeightKg > 0)) return false;
            foreach (var sample in edge.samples)
            {
                if (sample == null || !float.IsFinite(sample.distanceFromStartMeters) || sample.distanceFromStartMeters < 30 || edge.distanceMeters - sample.distanceFromStartMeters < 100 ||
                    !float.IsFinite(sample.position.sqrMagnitude) || !float.IsFinite(sample.forward.sqrMagnitude) || sample.forward.sqrMagnitude < .5f) continue;
                forward = Vector3.ProjectOnPlane(sample.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .9f) continue;
                global = sample.position + Vector3.Cross(Vector3.up, forward) * edge.laneCenterOffsetsMeters[0];
                return true;
            }
            return false;
        }

        private IEnumerator SeedSecondDriveRegion(string firstRegion)
        {
            var world = host.GetComponent<TruckTaxiRegionalWorld>();
            var edges = new List<LwsRoadEdge>(host.roadGraph.Graph.edges);
            edges.Sort((a, b) => string.CompareOrdinal(a?.edgeId, b?.edgeId));
            var prepared = new HashSet<string>(StringComparer.Ordinal);
            int attempts = 0;
            foreach (var edge in edges)
            {
                if (!TryDriveEdgeSeed(edge, out Vector3 global, out Vector3 forward)) continue;
                Vector3 local = driveOrigin.GlobalToLocal(LwsWorldPositionD.FromVector3(global));
                string region = world.ResolveRegion(local);
                if (string.IsNullOrEmpty(region) || region == firstRegion || !prepared.Contains(region) && prepared.Count >= 2) continue;
                if (++attempts > 32) break;
                if (prepared.Add(region))
                {
                    yield return world.PrepareInitialWorld(local);
                    Check(world.InitialWorldReady && world.IsPositionAvailable(local), "actual second-region scene prepared: " + region);
                }
                local = driveOrigin.GlobalToLocal(LwsWorldPositionD.FromVector3(global));
                Physics.SyncTransforms();
                if (!TryPlaceDriveSeed(local, Quaternion.LookRotation(forward, Vector3.up), region)) continue;
                Debug.Log(Prefix + "DRIVE SECOND REGION SEEDED first=" + firstRegion + " second=" + region + " edge=" + edge.edgeId);
                yield break;
            }
            throw new InvalidOperationException("No safe authored second-region road within 32 candidates/two preparations. No same-region substitute or second-region proof.");
        }

        private bool TryPlaceDriveSeed(Vector3 local, Quaternion rotation, string region)
        {
                var body = host.Player.NwhAdapter.VehicleController.vehicleRigidbody;
                var world = host.GetComponent<TruckTaxiRegionalWorld>();
                if (!world.IsPositionAvailable(local) || world.ResolveRegion(local) != region ||
                    !DriveRoadClear(body, local, rotation, out Vector3 grounded)) return false;
                host.SetPaused(true); host.Environment.Clock.SetPaused(true);
                var state = CreateSessionFixture(0, host.Session.CapturePersistentState());
                Check(host.Session.TryRestorePersistentState(state, out var error), "idle drive fixture semantics: " + error);
                var needs = host.DriverNeeds.CapturePersistentState();
                needs.Hunger = needs.Thirst = needs.Fatigue = needs.Pressure = needs.BowelPressure = .2f;
                needs.AmbientChoiceIds = new[] { "astra.restart.drive." + request.RunId, "astra.restart.drive.region." + region };
                Check(host.DriverNeeds.RestorePersistentState(needs, out error), "drive fixture run/region marker: " + error);
                Check(host.Player.TransmissionController.TrySetAutomaticMode(out error), "seed automatic transmission: " + error);
                var controls = host.Player.TruckControlController;
                controls.ApplyCommandFrame(new LwsVehicleCommandFrame { engineStop = LwsMomentaryIntent.Pressed,
                    parkingBrakeToggle = controls.CurrentState.parkingBrakeOn ? LwsMomentaryIntent.None : LwsMomentaryIntent.Pressed }, default);
                Check(host.Fuel?.Fuel != null && host.Fuel.Fuel.capacity > 40, "drive fixture fuel authority");
                host.Fuel.Fuel.amount = 40;
                TruckTaxiRecoverySpace.Move(body, grounded, rotation);
                body.linearVelocity = body.angularVelocity = Vector3.zero;
                Physics.SyncTransforms();
                Debug.Log(Prefix + "DRIVE ROAD SEEDED region=" + region + " global=" + driveOrigin.LocalToGlobal(grounded) +
                    " engine/PB preparation pending before save");
                return true;
        }

        private bool DriveRoadClear(Rigidbody body, Vector3 start, Quaternion rotation, out Vector3 grounded, int metersAhead = 80)
        {
            grounded = start;
            var world = host.GetComponent<TruckTaxiRegionalWorld>();
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up).normalized;
            string startRegion = world.ResolveRegion(start);
            // Validate the whole short test corridor, not just a point near a graph edge.
            for (int meters = 0; meters <= metersAhead; meters += 5)
            {
                Vector3 point = start + forward * meters;
                Vector3 global = driveOrigin.LocalToGlobal(point).ToVector3();
                if (!world.IsPositionAvailable(point) || request.Mode == "seed" && world.ResolveRegion(point) != startRegion ||
                    !LwsRoadGraphQuery.TryFindNearestRoad(host.roadGraph.Graph, global, 9, out var support) ||
                    support.SurfaceType == LwsRoadSurfaceType.Unknown ||
                    Vector3.ProjectOnPlane(global - support.NearestPosition, Vector3.up).magnitude > DriveRoadAllowance(support.EdgeId) ||
                    Mathf.Abs(Vector3.Dot(forward, support.Forward.normalized)) < .98f)
                { if (request.Mode != "seed") Debug.Log(Prefix + "DRIVE ROAD unavailable support/alignment at " + meters + "m"); return false; }
                Vector3 surface = driveOrigin.GlobalToLocal(LwsWorldPositionD.FromVector3(support.NearestPosition));
                surface.x = point.x; surface.z = point.z;
                if (!TruckTaxiRecoverySpace.TryGroundedPose(body, surface, rotation, .3f, body, null, out var pose))
                { if (request.Mode != "seed") Debug.Log(Prefix + "DRIVE ROAD unavailable ground/clearance at " + meters + "m"); return false; }
                if (meters == 0) grounded = pose;
                else if (Mathf.Abs(pose.y - grounded.y) > .03f * meters + .2f) return false;
            }
            return true;
        }

        private float DriveRoadAllowance(string edgeId)
        {
            float allowance = 4;
            foreach (var edge in host.roadGraph.Graph.edges)
                if (edge.edgeId == edgeId && edge.laneCenterOffsetsMeters != null)
                {
                    foreach (float offset in edge.laneCenterOffsetsMeters)
                        if (float.IsFinite(offset)) allowance = Mathf.Max(allowance, Mathf.Abs(offset) + .5f);
                    break;
                }
            return allowance;
        }

        public static void ValidateDriveLifecycle(bool paused, float timeScale, bool clockPaused, bool allowsDriving,
            bool dynamicBody, bool suppressed, int sources, int controllers, int providers)
        {
            if (paused || timeScale != 1 || clockPaused || !allowsDriving || !dynamicBody || suppressed ||
                sources != 1 || controllers != 1 || providers != 1)
                throw new InvalidOperationException("Post-load drive lifecycle invalid: paused=" + paused + " timeScale=" + timeScale +
                    " clockPaused=" + clockPaused + " allowsDriving=" + allowsDriving + " dynamic=" + dynamicBody +
                    " suppressed=" + suppressed + " sources/controllers/providers=" + sources + "/" + controllers + "/" + providers);
        }

        private void RequireDriveReady()
        {
            try { RequireDriveReadyCore(); }
            catch (Exception error) { throw new InvalidOperationException("Post-load drive gate failed: " + DriveDiagnostics(), error); }
        }

        private string DriveDiagnostics()
        {
            var registry = LwsApplicationBootstrap.Instance?.Registry;
            ILwsGameplayStateService gameplay = null; ILwsVehicleInputService input = null;
            if (registry != null) { registry.TryGet(out gameplay); registry.TryGet(out input); }
            var vehicle = host?.Player?.NwhAdapter?.VehicleController;
            var body = vehicle?.vehicleRigidbody;
            string keyboardSources = "";
            foreach (var source in FindObjectsByType<LwsKeyboardGamepadTruckInputSource>(FindObjectsSortMode.None))
                keyboardSources += source.GetInstanceID() + ":enabled=" + source.isActiveAndEnabled + ",suppressed=" + source.DrivingInputSuppressed + ";";
            int nativeProviders = 0;
            foreach (var provider in NWH.Common.Input.InputProvider.Instances)
                if (provider is NWH.VehiclePhysics2.Input.VehicleInputProviderBase && provider.isActiveAndEnabled) nativeProviders++;
            bool sameSource = input is LwsVehicleInputService &&
                ReferenceEquals(typeof(LwsVehicleInputService).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(input), driveSourceBeforeLoad);
            return "focus=" + Application.isFocused + " resume=" + host?.CanResume + " paused=" + host?.Paused +
                " scale=" + Time.timeScale + " clockPaused=" + host?.Environment?.Clock?.CurrentSnapshot.paused +
                " gameplay=" + gameplay?.CurrentState + " allows=" + gameplay?.AllowsDrivingInput +
                " owner=" + input?.ActiveOwner + " source=" + input?.ActiveSourceId + " active=" + input?.HasActiveSource +
                " sameService=" + ReferenceEquals(input, driveInputService) + " sameSource=" + sameSource +
                " sameOwner=" + (input?.ActiveOwner == driveInputOwner) + " initialized=" + vehicle?.IsInitialized +
                " autoInput=" + vehicle?.input?.autoSetInput + " kinematic=" + body?.isKinematic + " constraints=" + body?.constraints +
                " worldLife=" + host?.WorldLife?.Mode + " busy=" + host?.Persistence?.IsBusy + " blocked=" + host?.Persistence?.ResumeBlocked +
                " modal=" + host?.hud?.SaveLoad?.IsOpen + " keyboardSources=" + keyboardSources + " nativeProviders=" + nativeProviders +
                " engine=" + vehicle?.powertrain?.engine?.IsRunning +
                " gear=" + vehicle?.powertrain?.transmission?.Gear + " PB=" + host?.Player?.TruckControlController?.CurrentState.parkingBrakeOn +
                " locks[race/capture/recovery/fuel/travel/sleep]=" + host?.Races?.IsActive + "/" + host?.Kaiju?.IsCapturingPlayer + "/" +
                host?.Roadside?.IsRecovering + "/" + host?.Fuel?.IsRescuing + "/" + host?.FastTravel?.IsTravelling + "/" + host?.DriverNeeds?.IsSleeping +
                " saveEvents[race/custom/companion/private/jug/roadPep/extreme]=" + host?.Session?.RaceEventActive + "/" +
                host?.CustomContent?.IsPlaying + "/" + host?.Companions?.IsBusy + "/" + (host?.Companions?.ActivePrivateStop != null) + "/" +
                host?.DriverNeeds?.JugActive + "/" + (host?.DriverNeeds?.RoadPepChallenge != null) + "/" + host?.Extremes?.LivePhysicsActorCount;
        }

        private void RequireDriveReadyCore()
        {
            var registry = LwsApplicationBootstrap.Instance.Registry;
            var vehicle = host.Player.NwhAdapter.VehicleController;
            var body = vehicle.vehicleRigidbody;
            // Preserve the real focus policy: background keyboard rejection is not a load regression.
            if (!Application.isFocused || !registry.TryGet(out ILwsGameplayStateService gameplay) ||
                !registry.TryGet(out ILwsVehicleInputService input) || !registry.TryGet(out ILwsTruckControlService controls) ||
                !registry.TryGet(out ILwsPlayerVehicleService players) || players.ActiveTruck != host.Player ||
                !registry.TryGet(out driveOrigin) || !vehicle.IsInitialized || !vehicle.isActiveAndEnabled ||
                body != host.Player.GetComponent<Rigidbody>() || !vehicle.input.autoSetInput || !host.CanResume ||
                host.Persistence.ResumeBlocked || host.hud?.SaveLoad?.IsOpen == true || host.WorldLife?.Mode != TruckTaxiPlayerControlMode.Driving ||
                controls.ActiveController != host.Player.TruckControlController ||
                input.ActiveSourceId != "lws.input.keyboard-gamepad.truck" || !input.HasActiveSource ||
                driveSourceBeforeLoad == null || !ReferenceEquals(input, driveInputService) ||
                !ReferenceEquals(RegisteredDriveSource(input), driveSourceBeforeLoad) || input.ActiveOwner != driveInputOwner ||
                host.Player.CouplingAdapter?.IsTrailerAttached == true || host.Races?.IsActive == true ||
                host.Kaiju?.IsCapturingPlayer == true || host.GetComponent<TruckTaxiKaijuPlayerRecovery>()?.Captured == true ||
                host.Roadside?.IsRecovering == true || host.Fuel?.IsRescuing == true || host.FastTravel?.IsTravelling == true ||
                host.DriverNeeds?.IsSleeping == true || driveKeyboard != null && Keyboard.current != driveKeyboard)
                throw new InvalidOperationException("Post-load canonical focused gameplay/input/vehicle authority unavailable; no harness recovery permitted.");
            var sources = FindObjectsByType<LwsKeyboardGamepadTruckInputSource>(FindObjectsSortMode.None);
            int sourceCount = 0, controlCount = 0, providers = 0; bool suppressed = false;
            foreach (var source in sources) if (source.isActiveAndEnabled) { sourceCount++; suppressed |= source.DrivingInputSuppressed; }
            foreach (var control in FindObjectsByType<LwsTruckControlController>(FindObjectsSortMode.None)) if (control.isActiveAndEnabled) controlCount++;
            foreach (var provider in NWH.Common.Input.InputProvider.Instances)
                if (provider is NWH.VehiclePhysics2.Input.VehicleInputProviderBase && provider.isActiveAndEnabled)
                {
                    providers++;
                    if (!(provider is LwsNwhVehicleInputProvider) || provider.gameObject != vehicle.gameObject)
                        throw new InvalidOperationException("Unexpected active vendor vehicle input provider.");
                }
            ValidateDriveLifecycle(host.Paused, Time.timeScale, host.Environment.Clock.CurrentSnapshot.paused, gameplay.AllowsDrivingInput,
                !body.isKinematic && body.detectCollisions && body.constraints == RigidbodyConstraints.None,
                suppressed, sourceCount, controlCount, providers);
        }

        private IEnumerator DriveKeys(int frames, params Key[] keys)
        {
            InputSystem.QueueStateEvent(driveKeyboard, new KeyboardState(keys));
            for (int i = 0; i < frames; i++) { yield return null; RequireDriveReady(); }
        }

        private IEnumerator DrivePulse(string command, params Key[] keys)
        {
            Check(LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsTruckControlService controls), "command evidence service");
            int receipts = 0;
            Action<LwsTruckControlEvent> observe = e => { if (e.controlName == command && e.vehicleId == host.Player.VehicleId) receipts++; };
            controls.ControlEventPublished += observe;
            try
            {
                yield return DriveKeys(4, keys);
                yield return DriveKeys(3);
                Check(receipts == 1, command + " delivered exactly once across held/released frames; actual=" + receipts);
            }
            finally { controls.ControlEventPublished -= observe; }
        }

        private IEnumerator AssertPostLoadDrive(string label, string profileId)
        {
            // First operation after readiness is observational. Do not resume, close panels, start a shift,
            // reset suppression, touch physics, or configure drivetrain to make these assertions pass.
            Debug.Log(Prefix + "DRIVE UNTOUCHED " + label + " " + DriveDiagnostics());
            RequireDriveReady();
            yield return new WaitForFixedUpdate();
            RequireDriveReady();
            VerifyVendor();
            Check(driveSourceBeforeLoad != null, label + " registered input source object and owner unchanged; one canonical native provider");
            Check(host.Persistence.RunProfileId == profileId && saves.ActiveProfileId == profileId, label + " profile ownership");
            bool marker = false;
            var world = host.GetComponent<TruckTaxiRegionalWorld>();
            string region = world.ResolveRegion(host.Player.transform.position);
            foreach (string id in host.DriverNeeds.State.AmbientChoiceIds)
            {
                marker |= id == "astra.restart.drive." + request.RunId;
            }
            // The seeded region label is diagnostic only: subsequent real driving can cross a boundary.
            Check(marker && !string.IsNullOrEmpty(region), label + " loaded isolated drive fixture; live region=" + region);
            var vehicle = host.Player.NwhAdapter.VehicleController;
            var body = vehicle.vehicleRigidbody;
            var controls = host.Player.TruckControlController;
            var transmission = host.Player.TransmissionController;
            Check(transmission.AutomaticModeActive, "saved automatic transmission retained");
            Check(DriveRoadClear(body, body.position, body.rotation, out _, 25), "loaded pose has actual clear 25m test corridor; no relocation");
            Vector3 start = driveOrigin.LocalToGlobal(body.position).ToVector3();
            Vector3 forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, Vector3.up).normalized;
            var previousKeyboard = Keyboard.current;
            try
            {
                driveKeyboard = InputSystem.AddDevice<Keyboard>("Truck Taxi isolated post-load keyboard");
                driveKeyboard.MakeCurrent();
                // Parking is not serialized: an in-session load may retain the prior test's parked state.
                // Only a normal P key may release it. No engine/shift/steering command precedes W motion proof.
                if (controls.CurrentState.parkingBrakeOn) yield return DrivePulse("ParkingBrake", Key.P);
                Check(!controls.CurrentState.parkingBrakeOn, "ordinary parking control released for W; no persistence assumption");
                float until = Time.realtimeSinceStartup + 20;
                bool throttle = false, forwardGear = false;
                Vector3 previous = driveOrigin.LocalToGlobal(body.position).ToVector3();
                float previousTime = Time.realtimeSinceStartup, previousSpeed = body.linearVelocity.magnitude;
                while (Vector3.Dot(driveOrigin.LocalToGlobal(body.position).ToVector3() - start, forward) < 8 || body.linearVelocity.magnitude < 2)
                {
                    if (Time.realtimeSinceStartup > until) throw new TimeoutException("W-first native motion failed without engine-command recovery: " + DriveDiagnostics() +
                        " progress=" + Vector3.Dot(driveOrigin.LocalToGlobal(body.position).ToVector3() - start, forward) +
                        " speed=" + body.linearVelocity.magnitude + " throttle=" + vehicle.input.Throttle + " brakes=" + vehicle.input.Brakes +
                        " rpm=" + host.Player.NwhAdapter.ReadTelemetry().engineRpm + " starter=" + vehicle.powertrain.engine.starterActive);
                    yield return DriveKeys(1, Key.W);
                    throttle |= vehicle.input.Throttle > .5f;
                    forwardGear |= vehicle.powertrain.transmission.Gear > 0;
                    Vector3 current = driveOrigin.LocalToGlobal(body.position).ToVector3();
                    ValidateDriveMotion(previous, current, previousSpeed, body.linearVelocity.magnitude, Time.realtimeSinceStartup - previousTime, start, forward);
                    previous = current; previousTime = Time.realtimeSinceStartup; previousSpeed = body.linearVelocity.magnitude;
                }
                Check(throttle && forwardGear && transmission.AutomaticSelector == LwsAutomaticTransmissionSelector.Drive,
                    "W reaches native throttle, positive NWH gear and actual forward displacement");
                Debug.Log(Prefix + "W-FIRST VERIFIED " + label + "; no engine/shift/steering commands issued before motion");
                // W+S is the existing automatic policy's service brake, including at standstill (no reverse selection).
                yield return DriveKeys(2, Key.W, Key.S);
                Check(vehicle.input.Brakes > .5f && vehicle.input.Throttle < .1f, "W+S reaches native service brake without reverse");
                until = Time.realtimeSinceStartup + 10;
                while (body.linearVelocity.magnitude > .2f)
                {
                    if (Time.realtimeSinceStartup > until) throw new TimeoutException("Native service brake did not stop truck.");
                    yield return DriveKeys(1, Key.W, Key.S);
                    Vector3 current = driveOrigin.LocalToGlobal(body.position).ToVector3();
                    ValidateDriveMotion(previous, current, previousSpeed, body.linearVelocity.magnitude, Time.realtimeSinceStartup - previousTime, start, forward);
                    previous = current; previousTime = Time.realtimeSinceStartup; previousSpeed = body.linearVelocity.magnitude;
                }
                if (!controls.CurrentState.parkingBrakeOn) yield return DrivePulse("ParkingBrake", Key.P);
                Check(controls.CurrentState.parkingBrakeOn, "P engages parking brake after native stop");
                yield return DriveKeys(4, Key.A);
                Check(vehicle.input.Steering < -.5f, "A reaches NWH left steering");
                yield return DriveKeys(4, Key.D);
                Check(vehicle.input.Steering > .5f, "D reaches NWH right steering");
                yield return DriveKeys(4);
                // Automatic mode rejects manual steps; key receipt is not claimed as manual-gearing proof.
                yield return DrivePulse("TransmissionShiftUp", Key.Period);
                yield return DrivePulse("TransmissionShiftDown", Key.Comma);
                Check(transmission.AutomaticModeActive, "manual keys do not silently change automatic mode");
                yield return DrivePulse("EngineStop", Key.LeftShift, Key.E);
                Check(!vehicle.powertrain.engine.IsRunning, "Shift+E stops native engine");
                until = Time.realtimeSinceStartup + 10;
                while (host.Player.NwhAdapter.ReadTelemetry().engineRpm > 50 && Time.realtimeSinceStartup < until)
                    yield return DriveKeys(1);
                Check(host.Player.NwhAdapter.ReadTelemetry().engineRpm <= 50, "native engine finishes spinning down before restart");
                yield return DrivePulse("IgnitionToggle", Key.I);
                yield return DrivePulse("EngineStart", Key.E);
                until = Time.realtimeSinceStartup + 15;
                while ((!vehicle.powertrain.engine.IsRunning || vehicle.powertrain.engine.starterActive) && Time.realtimeSinceStartup < until)
                    yield return DriveKeys(1);
                Check(vehicle.powertrain.engine.IsRunning && !vehicle.powertrain.engine.starterActive,
                    "E restarts native engine and finishes starter after W-first proof");
                Check(controls.CurrentState.parkingBrakeOn, "native truck parked with running engine for subsequent drive-save-load regression");
                Debug.Log(Prefix + "DRIVE VERIFIED " + label + " region=" + region + " meters=" +
                    Vector3.Dot(driveOrigin.LocalToGlobal(body.position).ToVector3() - start, forward) +
                    " normal keyboard only; automatic mode, not manual 18-speed or full sandbox proof");
            }
            finally
            {
                if (driveKeyboard != null) { InputSystem.RemoveDevice(driveKeyboard); driveKeyboard = null; }
                if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            }
        }

        public static void ValidateDriveMotion(Vector3 previous, Vector3 current, float previousSpeed, float speed,
            float seconds, Vector3 start, Vector3 forward)
        {
            Vector3 delta = current - previous;
            float progress = Vector3.Dot(current - start, forward);
            Vector3 lateral = Vector3.ProjectOnPlane(current - start, Vector3.up) - forward * progress;
            // Startup/streaming stalls are not teleportation. Bound displacement by measured elapsed
            // time and native speed; frame-rate acceptance belongs to the performance harness.
            if (!float.IsFinite(delta.sqrMagnitude) || !float.IsFinite(seconds) || seconds <= 0 ||
                !float.IsFinite(speed) || !float.IsFinite(previousSpeed) ||
                delta.magnitude > Mathf.Max(speed, previousSpeed) * seconds + .5f ||
                progress < -1 || progress > 25 || lateral.magnitude > 1.5f || Mathf.Abs(current.y - start.y) > 2)
                throw new InvalidOperationException("Drive evidence left its safe road corridor or contains discontinuous motion: " +
                    "dt=" + seconds + " delta=" + delta + " speed=" + speed + " priorSpeed=" + previousSpeed +
                    " progress=" + progress + " lateral=" + lateral.magnitude + " heightDelta=" + (current.y - start.y));
        }

        private IEnumerator AcknowledgeFirstDay()
        {
            float until = Time.realtimeSinceStartup + 30;
            while (host.hud?.Career?.IsOpen != true && Time.realtimeSinceStartup < until) yield return null;
            Check(host.hud?.Career?.IsOpen == true, "new run opened actual first-day Career panel");
            var identity = host.Session.CaptureCareerProfile();
            Check(identity != null && identity.runProfileId == host.Persistence.RunProfileId &&
                identity.firstGuideEligible && !identity.firstGuideClaimed, "new-run guide belongs to active profile and is unclaimed");
            yield return CaptureCareerResolutions();
            host.hud.Career.Close();
            host.SetPaused(true); host.Environment.Clock.SetPaused(true);
            ValidateCareer();
            // Close may request an autosave; do not mutate the fixture during that operation.
            yield return WaitForRun();
        }

        private IEnumerator CaptureCareerResolutions()
        {
            // Evidence only, under this development player's output; never a gameplay save path.
            string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ValidationScreenshots", request.RunId);
            Directory.CreateDirectory(directory);
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(2560,1440), new Vector2Int(3440,1440), new Vector2Int(3840,2160) })
            {
                Screen.SetResolution(size.x,size.y,false);
                yield return new WaitForSecondsRealtime(.5f);
                Canvas.ForceUpdateCanvases();
                foreach (var graphic in host.hud.Career.FocusRoot.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                    if (graphic.enabled && graphic.color.a>.01f)
                        Check(graphic.rectTransform.rect.width>1 && graphic.rectTransform.rect.height>1,
                            "Career visible graphic has area: " + graphic.name);
                string file=Path.Combine(directory,"Career-"+size.x+"x"+size.y+".png");
                ScreenCapture.CaptureScreenshot(file);
                yield return new WaitForSecondsRealtime(.5f);
                Debug.Log(Prefix+"UI CAPTURE requested="+size+" actual="+Screen.width+"x"+Screen.height+" file="+file);
            }
            Screen.SetResolution(1920,1080,false);
            yield return new WaitForSecondsRealtime(.5f);
        }

        private void ValidateCareer()
        {
            var identity = host.Session.CaptureCareerProfile();
            Check(identity != null && identity.Validate(out _) && identity.runProfileId == host.Persistence.RunProfileId,
                "Career identity retained under same run profile");
            Check(identity.firstGuideEligible && identity.firstGuideClaimed, "first-day guide acknowledgement retained");
            Check(host.hud?.Career != null && !host.hud.Career.IsOpen, "Career guide did not replay on loaded run");
            host.hud.Career.OpenFirstDay();
            Check(!host.hud.Career.IsOpen, "claimed first-day guide cannot reopen through public startup hook");
            Debug.Log(Prefix + "CAREER VERIFIED profile=" + identity.runProfileId + " driver=" + identity.driverName + " guideClaimed=true");
        }

        private void Seed(int signature)
        {
            VerifyVendor(); host.SetPaused(true); host.Environment.Clock.SetPaused(true);
            host.Environment.Clock.SetDateTime(new LwsGameDateTime(2026, 8, 12, 14, signature, 0));
            var session = CreateSessionFixture(signature, host.Session.CapturePersistentState());
            Check(host.Session.TryRestorePersistentState(session, out var sessionError), "seed validated session semantics: " + sessionError);
            Check(host.Session.TrySetDriverName("ASTRA Driver " + signature,out var nameError),"seed driver identity: "+nameError);
            var appearance=host.Session.CaptureDriverAppearance();
            appearance.shirt=new Color(.3f+signature*.1f,.4f,.5f,1);
            appearance.capVisible=signature%2==0;
            Check(host.Session.TrySetDriverAppearance(appearance,out var appearanceError),"seed driver appearance: "+appearanceError);
            SeedDamage(signature);
            RelocateToAuthoredPose(signature);
            var needs = host.DriverNeeds.CapturePersistentState();
            needs.Hunger = .2f + signature * .1f; needs.Thirst = .35f; needs.Fatigue = .25f;
            needs.Pressure = .4f; needs.BowelPressure = .3f;
            needs.AmbientChoiceIds = new[] { "astra.restart." + request.RunId };
            foreach (var item in needs.Inventory) if (item.ItemId == "snack") item.Count = 7 + signature;
            Check(host.DriverNeeds.RestorePersistentState(needs, out var needsError), "seed validated needs semantics: " + needsError);
            Check(host.Fuel?.Fuel != null && host.Fuel.Fuel.capacity > 43, "authored truck fuel capacity supports fixture");
            host.Fuel.Fuel.amount = 40 + signature;
            Validate(signature);
        }

        public static float DamageCondition(int signature)
        {
            if (signature < 0 || signature > 3) throw new ArgumentOutOfRangeException(nameof(signature));
            return .61f - signature * .07f;
        }

        public static string SelectBodyPart(IReadOnlyDictionary<string, TruckTaxiDamagePart> parts)
        {
            if (parts == null) throw new InvalidOperationException("Component damage parts are unavailable.");
            string selected = null;
            foreach (var pair in parts)
                if (pair.Value != null && pair.Value.Category == TruckTaxiDamageCategory.Body &&
                    (selected == null || string.CompareOrdinal(pair.Key, selected) < 0)) selected = pair.Key;
            return selected ?? throw new InvalidOperationException("No existing body damage part is available.");
        }

        private void SeedDamage(int signature)
        {
            Check(host.ComponentDamage != null, "component damage authority exists");
            var damage = host.ComponentDamage;
            string partId = SelectBodyPart(damage.Parts);
            var snapshot = damage.Capture();
            bool found = false;
            foreach (var part in snapshot.Parts)
                if (part.Id == partId) { part.Condition = DamageCondition(signature); found = true; }
            Check(found && TruckTaxiComponentDamage.ValidSnapshot(snapshot, host.Player.VehicleId, damage.Parts),
                "existing body part snapshot is valid: " + partId);
            Check(damage.Restore(snapshot), "body damage applied through public snapshot Restore");
        }

        // Selection depends on authored IDs and the signature, never enumeration order or the saved truck pose.
        public static TruckTaxiRideLocation SelectPoseLocation(IEnumerable<TruckTaxiRideLocation> source,
            Vector3 spawn, Func<Vector3, bool> inSpawnRegion, int signature)
        {
            if (signature < 0 || signature > 3) throw new ArgumentOutOfRangeException(nameof(signature));
            if (source == null || inSpawnRegion == null) throw new ArgumentNullException(nameof(source));
            var candidates = new List<TruckTaxiRideLocation>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var location in source)
            {
                if (location == null || !location.isActiveAndEnabled || location.truckStopPoint == null ||
                    !location.gameObject.scene.isLoaded || !inSpawnRegion(location.StopPosition)) continue;
                if (string.IsNullOrWhiteSpace(location.locationId) || !ids.Add(location.locationId))
                    throw new InvalidOperationException("Missing or duplicate authored ride-location ID in fixture region.");
                if (Vector3.ProjectOnPlane(location.StopPosition - spawn, Vector3.up).sqrMagnitude < 50f * 50f) continue;
                candidates.Add(location);
            }
            candidates.Sort((a, b) => string.CompareOrdinal(a.locationId, b.locationId));
            if (candidates.Count == 0) throw new InvalidOperationException("No authored non-spawn stop in the loaded fixture region.");
            return candidates[signature % candidates.Count];
        }

        private TruckTaxiRideLocation ExpectedPose(int signature, out Rigidbody body, out Vector3 position,
            out Quaternion rotation, out ILwsWorldOriginService origin, out string regionId)
        {
            var services = LwsApplicationBootstrap.Instance?.Registry;
            origin = null;
            Check(services != null && services.TryGet(out origin) && origin != null && !origin.ShiftInProgress && !origin.HasQueuedShift,
                "real world-origin service is ready, with no pending shift");
            var world = host.GetComponent<TruckTaxiRegionalWorld>();
            Check(world != null && world.InitialWorldReady && host.playerSpawn != null, "authored spawn and regional world are ready");
            string spawnRegion = world.ResolveRegion(host.playerSpawn.position);
            Check(!string.IsNullOrWhiteSpace(spawnRegion) && world.IsPositionAvailable(host.playerSpawn.position), "spawn region remains loaded");
            var location = SelectPoseLocation(FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None),
                host.playerSpawn.position, p => world.ResolveRegion(p) == spawnRegion, signature);
            regionId = world.ResolveRegion(location.StopPosition);
            Check(location.gameObject.scene.isLoaded && world.IsPositionAvailable(location.StopPosition) && regionId == spawnRegion,
                "selected authored stop is in the loaded region: " + location.locationId + " / " + regionId);
            body = host.Player.GetComponent<Rigidbody>();
            Check(body != null, "canonical truck rigidbody exists");
            rotation = location.truckStopPoint.rotation;
            Physics.SyncTransforms();
            // Match the existing load coordinator's grounded-pose contract, including full truck footprint.
            Check(TruckTaxiRecoverySpace.TryGroundedPose(body, location.StopPosition, rotation, .3f, body, null, out position),
                "selected stop has safe ground/clearance: " + location.locationId);
            Check(world.IsPositionAvailable(position) && world.ResolveRegion(position) == regionId &&
                TruckTaxiRecoverySpace.HasClearance(body, position, rotation, body, null), "grounded target remains safe and loaded");
            return location;
        }

        private void RelocateToAuthoredPose(int signature)
        {
            var location = ExpectedPose(signature, out var body, out var grounded, out var rotation, out var origin, out var region);
            Vector3 teleportPoint = location.StopPosition + Vector3.up * 1.6f;
            Check(TruckTaxiRecoverySpace.HasClearance(body, teleportPoint, rotation, body, null),
                "TeleportNear intermediate pose has clearance before moving");
            Check(!host.Session.HasPassenger && !host.Session.HasStolenPassenger && host.Companions?.IsBusy != true &&
                host.Races?.IsActive != true && host.CustomContent?.IsPlaying != true && host.Kaiju?.IsCapturingPlayer != true &&
                host.FastTravel?.IsTravelling != true && host.Roadside?.IsRecovering != true && host.Fuel?.IsRescuing != true &&
                host.DriverNeeds.IsSleeping != true && (host.Extremes?.LivePhysicsActorCount ?? 0) == 0 &&
                !host.Player.CaptureState().trailerAttachment.attached && !host.Persistence.IsBusy && host.CanResume,
                "no occupants, events, trailer, or load block relocation");
            host.TeleportNear(location);
            Check(Vector3.Distance(body.position, teleportPoint) < .01f && Quaternion.Angle(body.rotation, rotation) < .1f,
                "approved TeleportNear applied the authored pose");
            // TeleportNear intentionally lifts 1.6m. Use existing validated recovery placement to ground it
            // while paused, not an unbounded physics settle or an arbitrary transform write.
            TruckTaxiRecoverySpace.Move(body, grounded, rotation);
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            Physics.SyncTransforms();
            Check(TruckTaxiRecoverySpace.HasClearance(body, body.position, body.rotation, body, null), "relocated truck remains clear");
            Debug.Log(Prefix + "POSE SEEDED signature=" + signature + " location=" + location.locationId + " region=" + region +
                " local=" + grounded.ToString("F4") + " global=" + origin.LocalToGlobal(grounded) + " origin=" + origin.CurrentOriginOffset);
        }

        private void ValidateDamageAndPose(int signature)
        {
            var damage = host.ComponentDamage;
            Check(damage != null && damage.VehicleId == host.Player.VehicleId, "component damage belongs to the canonical truck");
            string partId = SelectBodyPart(damage.Parts);
            float actualCondition = damage.Parts[partId].Condition;
            Check(Mathf.Abs(actualCondition - DamageCondition(signature)) < .0001f,
                "damage signature=" + signature + " part=" + partId + " expected=" + DamageCondition(signature) + " actual=" + actualCondition);
            var location = ExpectedPose(signature, out var body, out var expected, out var rotation, out var origin, out var region);
            float localError = Vector3.Distance(body.position, expected);
            float headingError = Quaternion.Angle(body.rotation, rotation);
            var actualGlobal = origin.LocalToGlobal(body.position);
            var expectedGlobal = origin.LocalToGlobal(expected);
            double globalError = actualGlobal.DistanceTo(expectedGlobal);
            Check(localError <= .15f && headingError <= 1f && globalError <= .15,
                "authored pose retained signature=" + signature + " location=" + location.locationId +
                " localError=" + localError + " globalError=" + globalError + " headingError=" + headingError);
            Check(Vector3.Distance(origin.GlobalToLocal(actualGlobal), body.position) <= .01f &&
                Vector3.Distance(host.Player.transform.position, body.position) <= .01f &&
                Quaternion.Angle(host.Player.transform.rotation, body.rotation) <= .1f, "real origin roundtrip and truck transform agree");
            Check(Vector3.ProjectOnPlane(body.position - host.playerSpawn.position, Vector3.up).magnitude >= 49.9f,
                "loaded truck is at least 50m from authored default spawn");
            Check(body.linearVelocity.sqrMagnitude <= .0001f && body.angularVelocity.sqrMagnitude <= .0001f, "truck is stationary after placement/load");
            var services = LwsApplicationBootstrap.Instance.Registry;
            Check(services.TryGet(out ILwsPlayerVehicleService vehicles) && vehicles.ActiveTruck == host.Player &&
                FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length == 1, "one live player truck and matching registry owner");
            Check(FindObjectsByType<TruckTaxiBootstrap>(FindObjectsSortMode.None).Length == 1 &&
                FindObjectsByType<TruckTaxiRegionalWorld>(FindObjectsSortMode.None).Length == 1, "one live Taxi host and regional world");
            Check(services.TryGet(out ILwsGameClockService clock) && ReferenceEquals(clock, host.Environment.Clock) &&
                clock.CurrentSnapshot.paused, "canonical registry clock is the paused world clock");
            Debug.Log(Prefix + "POSE VERIFIED evidenceVersion=" + EvidenceVersion + " signature=" + signature +
                " location=" + location.locationId + " region=" + region + " local=" + body.position.ToString("F4") +
                " expectedLocal=" + expected.ToString("F4") + " global=" + actualGlobal + " expectedGlobal=" + expectedGlobal +
                " origin=" + origin.CurrentOriginOffset + " heading=" + body.rotation.eulerAngles.ToString("F3"));
        }

        // Existing semantic snapshot only, never a new on-disk fixture format.
        public static TruckTaxiPersistentState CreateSessionFixture(int signature, TruckTaxiPersistentState session = null)
        {
            if (signature < 0 || signature > 3) throw new ArgumentOutOfRangeException(nameof(signature));
            session = session ?? new TruckTaxiPersistentState();
            session.shiftEarningsCents = 123400 + signature; session.purchaseChargesCents = 1200; session.serviceChargesCents = 300;
            session.rideRequestsEnabled = false; session.gameMinutes = 120;
            session.statistics = new TruckTaxiPersistentStatistics { completedRides = 2, totalStarsEarned = 9,
                shiftScore = 73, totalCollisions = 7, trafficHits = 4, pedestriansHit = 2,
                jugsSucceeded = 3, jugsSpilled = 1, containersThrown = 5, specialStopsCompleted = 2 };
            session.passengerSelection = new TruckTaxiPassengerSelectionState { offerIndex = 2,
                passengers = new List<TruckTaxiPassengerExposure> { new TruckTaxiPassengerExposure {
                    passengerId = "astra.restart.fixture", archetype = "validation", timesOffered = 2, timesAccepted = 2,
                    timesCompleted = 2, lastOfferedRideIndex = 2, lastOfferedGameMinutes = 100 } },
                recentArchetypes = new List<string> { "validation", "validation" } };
            session.intercity = new TruckTaxiIntercityState();
            return session;
        }

        private void Validate(int signature)
        {
            VerifyVendor();
            var profile = FindTestProfile(true);
            Check(host.Persistence.RunProfileId == profile.stableProfileId && saves.ActiveProfileId == profile.stableProfileId, "active profile ownership agrees");
            var session = host.Session.CapturePersistentState(); var stats = session.statistics;
            ValidateCareer();
            Check(host.Session.DriverName=="ASTRA Driver "+signature,"driver name signature retained");
            var appearance=host.Session.CaptureDriverAppearance();
            Check(appearance!=null && Mathf.Abs(appearance.shirt.r-(.3f+signature*.1f))<.0001f &&
                appearance.capVisible==(signature%2==0),"driver appearance signature retained");
            Check(session.shiftEarningsCents == 123400 + signature && session.purchaseChargesCents == 1200 && session.serviceChargesCents == 300,
                "cash signature=" + signature + " actual earnings=" + session.shiftEarningsCents);
            Check(stats.completedRides == 2 && stats.totalStarsEarned == 9 && stats.shiftScore == 73, "rides=2 stars=9 score=73 retained");
            Check(stats.totalCollisions == 7 && stats.trafficHits == 4 && stats.pedestriansHit == 2, "collision/traffic/pedestrian counters retained");
            Check(stats.jugsSucceeded == 3 && stats.jugsSpilled == 1 && stats.containersThrown == 5 && stats.specialStopsCompleted == 2,
                "jug/throw/special counters retained");
            Check(!session.rideRequestsEnabled && !host.Session.HasPassenger && !host.Session.HasStolenPassenger, "safe idle state, dispatch disabled");
            var needs = host.DriverNeeds.State;
            Check(Mathf.Abs(needs.Hunger - (.2f + signature * .1f)) < .001f && Mathf.Abs(needs.Thirst - .35f) < .001f &&
                Mathf.Abs(needs.Fatigue - .25f) < .001f && Mathf.Abs(needs.Pressure - .4f) < .001f && Mathf.Abs(needs.BowelPressure - .3f) < .001f,
                "needs signature=" + signature + " hunger=" + needs.Hunger);
            Check(needs.Count(TruckTaxiNeedsItem.Snack) == 7 + signature && needs.StarterInventoryGranted, "inventory retained without new starter grant");
            bool runMarker = false;
            foreach (string marker in needs.AmbientChoiceIds) if (marker == "astra.restart." + request.RunId) runMarker = true;
            Check(runMarker, "vendor payload belongs to this run GUID");
            Check(host.Fuel?.Fuel != null && Mathf.Abs(host.Fuel.Fuel.amount - (40 + signature)) < .01f,
                "fuel signature=" + signature + " actual liters=" + host.Fuel?.Fuel?.amount);
            var clock = host.Environment.Clock.CurrentSnapshot;
            Check(clock.year == 2026 && clock.month == 8 && clock.day == 12 && clock.hour == 14 && clock.minute == signature,
                "calendar retained: " + clock.DateText + " " + clock.ClockText);
            Check(Mathf.Abs(clock.second) < .05f, "calendar seconds retained: " + clock.second);
            ValidateDamageAndPose(signature);
            Check(host.CanResume && !host.Persistence.ResumeBlocked, "resume controls are not blocked");
            Debug.Log(Prefix + "STATE VERIFIED signature=" + signature + " profile=" + profile.stableProfileId);
        }
#endif
    }
}
