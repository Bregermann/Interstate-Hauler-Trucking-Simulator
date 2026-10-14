using System;
using System.Collections;
using System.Collections.Generic;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // One transient transaction across scene teardown. No slots, files, serializer, career or weather authority.
    public sealed class TruckTaxiWorldTravel : MonoBehaviour
    {
        private static TruckTaxiWorldTravel instance;
        public static bool IsBusy { get; private set; }
        public static bool HasPendingArrival(string scene) => IsBusy && instance != null && instance.targetScene == scene;
        public static string LastStatus { get; private set; }
        public static int CompletedTransfers { get; private set; }
        private string targetScene, sourceScene, requestedScene;
        private LwsSaveSnapshot sourceSnapshot, movingSnapshot;
        private bool useLastPosition;
        private TextMeshProUGUI title, detail;
        private UnityEngine.UI.Image progress;
        private string failure;
        private TruckTaxiBootstrap arrived;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { instance = null; IsBusy = false; LastStatus = null; CompletedTransfers = 0; }
        public static bool TryTravel(TruckTaxiBootstrap host, string destination, bool returnToLastPosition = true)
        {
            string reason = host?.Persistence?.WorldTransferBlockReason ?? (host?.Persistence == null ? "Career is not ready." : null);
            if (reason == null && (!TruckTaxiWorldCatalog.Contains(destination) || destination == host.gameObject.scene.name))
                reason = "Choose the other available region.";
            if (reason == null && !Application.CanStreamedLevelBeLoaded(destination)) reason = "Destination scene is not included in this build.";
            if (reason != null) { LastStatus = reason; host?.hud?.Alerts?.Publish("world-travel-blocked", reason, TruckTaxiAlertPriority.High); return false; }
            LwsSaveSnapshot source, moving;
            try { source = host.Persistence.CaptureWorldTransfer(); moving = host.Persistence.CaptureWorldTransfer(); }
            catch (Exception e) { LastStatus = e.Message; return false; }
            var checkpoint = host.Persistence.SaveBeforeLeaving();
            if (!checkpoint.Succeeded) { LastStatus = checkpoint.Message; host.hud.Alerts.Publish("world-travel-save", LastStatus, TruckTaxiAlertPriority.High); return false; }
            var root = new GameObject("Truck Taxi regional loading transaction");
            DontDestroyOnLoad(root); instance = root.AddComponent<TruckTaxiWorldTravel>();
            instance.sourceSnapshot = source; instance.movingSnapshot = moving;
            instance.sourceScene = host.gameObject.scene.name; instance.requestedScene = destination;
            instance.useLastPosition = returnToLastPosition;
            instance.BuildLoadingScreen(host.hud);
            host.WorldLife?.ReturnToTruck(); host.SetPaused(true);
            host.Session.AcquireOfferSuppression(instance);
            IsBusy = true;
            instance.StartCoroutine(instance.Travel());
            return true;
        }
        private void BuildLoadingScreen(TruckTaxiHud view)
        {
            var screen = new GameObject("Regional Loading", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            screen.transform.SetParent(transform, false);
            var canvas = screen.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
            var scaler = screen.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
            var panel = view.Panel(screen.GetComponent<RectTransform>(), "Truck Taxi loading", Vector2.zero, Vector2.one);
            var background = panel.GetComponent<UnityEngine.UI.Image>(); if (background != null) background.color = new Color(.025f,.035f,.04f,1);
            view.Text(panel,"Brand",new Vector2(.12f,.72f),new Vector2(.88f,.85f),52).text = "TRUCK TAXI";
            title = view.Text(panel,"Destination",new Vector2(.12f,.52f),new Vector2(.88f,.67f),36);
            detail = view.Text(panel,"Loading status",new Vector2(.12f,.37f),new Vector2(.88f,.49f),25);
            var bar = new GameObject("Actual scene loading progress",typeof(RectTransform),typeof(UnityEngine.UI.Image)); bar.transform.SetParent(panel,false);
            var rect = bar.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.12f,.29f); rect.anchorMax = new Vector2(.88f,.31f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            progress = bar.GetComponent<UnityEngine.UI.Image>(); progress.color = new Color(.2f,.8f,.65f);
            // A sprite-less Image ignores fillAmount. Scale its actual rectangle from the left instead.
            rect.pivot = new Vector2(0, .5f);
            rect.localScale = new Vector3(0, 1, 1);
            view.Text(panel,"Tip",new Vector2(.12f,.13f),new Vector2(.88f,.23f),22).text = "Your career and truck travel with you. Regional weather remains local.";
        }
        private IEnumerator Travel()
        {
            yield return null;
            failure = null;
            yield return LoadRegion(requestedScene);
            if (arrived != null) yield return PrepareRegion(movingSnapshot, useLastPosition);
            bool success = arrived != null && TryApply(arrived, movingSnapshot, useLastPosition, false);
            if (!success)
            {
                string originalFailure = failure ?? "Destination could not be restored.";
                detail.text = "Returning to your original region. " + originalFailure;
                yield return LoadRegion(sourceScene);
                if (arrived != null) yield return PrepareRegion(sourceSnapshot, true);
                if (arrived != null && TryApply(arrived, sourceSnapshot, true, true))
                {
                    IsBusy = false; arrived.SetPaused(false);
                    arrived.hud.Alerts.Publish("world-travel-rollback", "Travel cancelled: " + originalFailure + " Your original career was restored.", TruckTaxiAlertPriority.High);
                    LastStatus = originalFailure; Destroy(gameObject); yield break;
                }
                // The vendor checkpoint remains untouched even if the source cannot initialize.
                IsBusy = false; LastStatus = "Travel failed. Continue your source checkpoint from the main menu. " + failure;
                detail.text = LastStatus;
                yield return new WaitForSecondsRealtime(4);
                Time.timeScale = 1;
                SceneManager.LoadSceneAsync(TruckTaxiMainMenu.SceneName, LoadSceneMode.Single);
                Destroy(gameObject); yield break;
            }
            detail.text = "Finishing regional weather and presentation...";
            float presentationDeadline = Time.realtimeSinceStartup + 30;
            var snowPresentation = arrived.GetComponent<TruckTaxiSnowSurface>();
            while (snowPresentation != null && !snowPresentation.EnvironmentBindingsReady && Time.realtimeSinceStartup < presentationDeadline)
            {
                snowPresentation.Refresh();
                yield return null;
            }
            IsBusy = false; CompletedTransfers++; LastStatus = "Arrived in " + TruckTaxiWorldCatalog.Label(requestedScene);
            arrived.SetPaused(false);
            arrived.Persistence.RequestAutosave("Regional travel completed");
            var saved = arrived.Persistence.SaveService.SaveAutosave(arrived.Persistence.RunProfileId, "Regional travel completed");
            if (!saved.Succeeded) arrived.hud.Alerts.Publish("world-travel-autosave", "Arrived, but autosave failed: " + saved.Message, TruckTaxiAlertPriority.High);
            arrived.hud.Alerts.Publish("world-travel-arrival", LastStatus);
            yield return new WaitForEndOfFrame();
            Destroy(gameObject);
        }
        private IEnumerator PrepareRegion(LwsSaveSnapshot snapshot, bool last)
        {
            Exception error = null;
            bool completed = false;
            float deadline = Time.realtimeSinceStartup + 60; // Matches PrepareInitialWorld's real-time budget.
            TruckTaxiBootstrap host = arrived;
            IEnumerator preparation = RunPreparation(
                () => host.GetComponent<TruckTaxiRegionalWorld>().PrepareInitialWorld(
                    host.Persistence.WorldTransferPreparationPoint(snapshot, last)),
                result => { error ??= result; completed = true; }, () => Time.realtimeSinceStartup, deadline);
            Coroutine running = null;
            try
            {
                try { running = StartCoroutine(preparation); }
                catch (Exception e) { error = e; }
                // Watch independently so native Unity yield instructions cannot suspend the deadline.
                while (error == null && !completed && Time.realtimeSinceStartup < deadline) yield return null;
                if (error == null && !completed) error = new TimeoutException("Timed out preparing regional arrival.");
            }
            finally
            {
                try { if (running != null && !completed) StopCoroutine(running); }
                catch (Exception e) { error ??= e; }
                try { (preparation as IDisposable)?.Dispose(); }
                catch (Exception e) { error ??= e; }
            }
            if (error != null)
            {
                failure = error.GetType().Name + ": " + error.Message;
                arrived = null; // Never apply a partially prepared destination (or rollback source).
                Debug.LogWarning("Regional preparation: " + failure);
            }
        }

        private static IEnumerator RunPreparation(Func<IEnumerator> create, Action<Exception> completed,
            Func<float> realtime, float deadline)
        {
            var pending = new Stack<IEnumerator>();
            Exception error = null;
            try
            {
                try { pending.Push(create() ?? throw new InvalidOperationException("Regional preparation returned no iterator.")); }
                catch (Exception e) { error = e; }
                while (error == null && pending.Count > 0)
                {
                    object yielded = null;
                    bool active = false;
                    try
                    {
                        if (realtime() >= deadline) throw new TimeoutException("Timed out preparing regional arrival.");
                        IEnumerator current = pending.Peek();
                        active = current.MoveNext();
                        if (active) yielded = current.Current;
                        else { pending.Pop(); (current as IDisposable)?.Dispose(); }
                    }
                    catch (Exception e) { error = e; }
                    if (error != null) break;
                    if (!active) continue;
                    // Unity normally advances nested iterators outside the caller's catch block.
                    if (yielded is IEnumerator nested) pending.Push(nested);
                    else yield return yielded;
                }
            }
            finally
            {
                while (pending.Count > 0)
                {
                    try { (pending.Pop() as IDisposable)?.Dispose(); }
                    catch (Exception e) { error ??= e; }
                }
                completed(error);
            }
        }

        private bool TryApply(TruckTaxiBootstrap host, LwsSaveSnapshot snapshot, bool last, bool rollback)
        {
            try
            {
                var result = host.Persistence.ApplyWorldTransfer(snapshot, last, rollback);
                if (!result.Succeeded) { failure = result.Message; return false; }
                return true;
            }
            catch (Exception e) { failure = e.GetType().Name + ": " + e.Message; Debug.LogWarning("Regional transfer: " + failure); return false; }
        }
        private IEnumerator LoadRegion(string scene)
        {
            targetScene = scene; arrived = null;
            title.text = "TRAVELING TO " + TruckTaxiWorldCatalog.Label(scene).ToUpperInvariant();
            detail.text = "Loading terrain, roads and traffic...";
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single); }
            catch (Exception e) { failure = e.Message; }
            if (operation == null) yield break;
            while (!operation.isDone)
            {
                progress.rectTransform.localScale = new Vector3(Mathf.Clamp01(operation.progress / .9f), 1, 1);
                yield return null;
            }
            progress.gameObject.SetActive(false);
            detail.text = "Preparing safe arrival, career, truck and controls...";
            float deadline = Time.realtimeSinceStartup + 90;
            while (Time.realtimeSinceStartup < deadline)
            {
                var host = TruckTaxiBootstrap.Instance;
                if (host != null && host.gameObject.scene.name == scene && host.Ready && host.StartupComplete && host.Persistence?.Initialized == true)
                { arrived = host; break; }
                yield return null;
            }
            if (arrived == null) failure = "Timed out preparing " + TruckTaxiWorldCatalog.Label(scene) + ".";
            progress.gameObject.SetActive(true);
        }
        private void OnDestroy() { if (instance == this) { instance = null; IsBusy = false; } }
    }
}
