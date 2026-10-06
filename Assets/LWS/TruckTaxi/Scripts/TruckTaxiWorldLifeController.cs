using System.Collections.Generic;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiPlayerControlMode { Driving, OnFoot, SeatedInteraction, RestroomQte, Sleeping, SpecialInteraction }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiWorldLifeController : MonoBehaviour
    {
        [Header("On-foot movement")]
        [Min(1f)] public float walkingSpeed = 4.2f;
        [Min(1f)] public float runningSpeed = 6.4f;
        [Range(0.1f, 1f)] public float mouseSensitivity = .15f;
        [Range(20f, 180f)] public float gamepadLookDegreesPerSecond = 110f;
        [Min(1f)] public float returnToTruckDistance = 4.5f;
        [Tooltip("Hold Interact to complete restroom timing without rapid taps.")]
        public bool simplifiedRestroomTiming;
        public TruckTaxiPlayerControlMode Mode { get; private set; } = TruckTaxiPlayerControlMode.Driving;
        public bool IsOnFoot => Mode == TruckTaxiPlayerControlMode.OnFoot;
        public bool InventoryOpen => inventoryPanel != null && inventoryPanel.gameObject.activeSelf;
        public bool DamageOpen => damagePanel != null && damagePanel.gameObject.activeSelf;
        public Transform FocusRoot => temporaryPanel != null && temporaryPanel.gameObject.activeSelf ? temporaryPanel :
            DamageOpen ? damagePanel : InventoryOpen ? inventoryPanel : null;
        public string Feedback { get; private set; } = string.Empty;
        public Camera FootCamera => footCamera;
        public Transform FootTransform => footBody != null ? footBody.transform : null;
        public TruckTaxiBootstrap Host => host;
        public TruckTaxiGasStationPoint ActiveNozzleStation { get; private set; }

        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private Rigidbody truckBody;
        private GameObject footBody;
        private CharacterController controller;
        private Camera footCamera;
        private Camera truckCamera;
        private AudioListener footListener, truckListener;
        private readonly List<LwsKeyboardGamepadTruckInputSource> drivingSources = new List<LwsKeyboardGamepadTruckInputSource>();
        private RectTransform inventoryPanel, damagePanel, temporaryPanel;
        private TextMeshProUGUI prompt, inventoryDetails, basketText, damageHeading;
        private int damagePage;
        private readonly Dictionary<TruckTaxiNeedsItem, int> basket = new Dictionary<TruckTaxiNeedsItem, int>();
        private TruckTaxiStorePoint basketStore;
        private TruckTaxiWorldInteractable aimed;
        private float pitch, verticalSpeed, feedbackUntil;
        private string restroomId;
        private bool restroomBowel;
        private float restroomProgress;
        private int restroomCues;
        private float simplifiedRestroomHold;
        private Vector3 preSeatPosition;
        private bool seatedAtDiner;
        private TruckTaxiRoadPepChallenge ambientChallenge;
        private readonly Collider[] exitHits = new Collider[24];

        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud ownerHud)
        {
            if (host != null) return;
            host = owner; hud = ownerHud; truckBody = owner.Player.GetComponent<Rigidbody>();
            owner.Player.GetComponentsInChildren(true, drivingSources);
            BuildFootRig(); BuildHud();
        }
        private void BuildFootRig()
        {
            footBody = new GameObject("Truck Taxi On-Foot Player");
            footBody.transform.SetParent(transform, false);
            controller = footBody.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = .32f; controller.center = new Vector3(0, .9f, 0);
            controller.stepOffset = .32f; controller.slopeLimit = 48f;
            var eye = new GameObject("First-person view"); eye.transform.SetParent(footBody.transform, false);
            eye.transform.localPosition = new Vector3(0, 1.62f, 0);
            footCamera = eye.AddComponent<Camera>(); footCamera.depth = 100;
            footListener = eye.AddComponent<AudioListener>(); footListener.enabled = false;
            footCamera.enabled = false; footBody.SetActive(false);
        }
        private void BuildHud()
        {
            prompt = hud.Text(hud.Root, "World interaction prompt", new Vector2(.29f, .12f), new Vector2(.71f, .19f), 25);
            prompt.alignment = TextAlignmentOptions.Center; prompt.raycastTarget = false;
            inventoryPanel = hud.Panel(hud.Root, "Driver inventory and basket", new Vector2(.19f, .12f), new Vector2(.81f, .88f));
            hud.Text(inventoryPanel, "Inventory heading", new Vector2(.05f, .9f), new Vector2(.95f, .98f), 32).text = "DRIVER INVENTORY";
            inventoryDetails = hud.Text(inventoryPanel, "Selected item", new Vector2(.54f, .5f), new Vector2(.95f, .88f), 21);
            basketText = hud.Text(inventoryPanel, "Shopping basket", new Vector2(.54f, .31f), new Vector2(.95f, .49f), 20);
            hud.Button(inventoryPanel, "CHECKOUT", new Vector2(.54f, .01f), new Vector2(.73f, .08f), () => Checkout());
            hud.Button(inventoryPanel, "CANCEL BASKET", new Vector2(.25f, .01f), new Vector2(.51f, .08f), () =>
            { basket.Clear(); basketStore = null; RefreshInventory(); });
            hud.Button(inventoryPanel, "CLOSE", new Vector2(.76f, .01f), new Vector2(.95f, .08f), CloseInventory);
            inventoryPanel.gameObject.SetActive(false);
            damagePanel = hud.Panel(hud.Root, "Truck condition inspection", new Vector2(.18f, .1f), new Vector2(.82f, .9f));
            damageHeading = hud.Text(damagePanel, "Condition heading", new Vector2(.05f, .89f), new Vector2(.95f, .98f), 30);
            hud.Button(damagePanel, "PREVIOUS", new Vector2(.05f, .02f), new Vector2(.28f, .09f), () => { damagePage = Mathf.Max(0, damagePage - 1); RefreshDamage(); });
            hud.Button(damagePanel, "NEXT", new Vector2(.38f, .02f), new Vector2(.61f, .09f), () => { damagePage++; RefreshDamage(); });
            hud.Button(damagePanel, "CLOSE", new Vector2(.72f, .02f), new Vector2(.95f, .09f), CloseDamage);
            damagePanel.gameObject.SetActive(false);
            temporaryPanel = hud.Panel(hud.Root, "World interaction choices", new Vector2(.3f, .25f), new Vector2(.7f, .75f));
            temporaryPanel.gameObject.SetActive(false);
            prompt.gameObject.SetActive(false);
        }
        private void Update()
        {
            if (host?.Ready != true || hud?.UIInput == null) return;
            var input = hud.UIInput;
            if (Mode == TruckTaxiPlayerControlMode.RestroomQte)
            {
                if (!host.Paused)
                {
                    if (input.Inventory.WasPressedThisFrame())
                    { simplifiedRestroomTiming = !simplifiedRestroomTiming; simplifiedRestroomHold = 0; }
                    if (simplifiedRestroomTiming)
                    {
                        simplifiedRestroomHold = input.WorldInteract.IsPressed() ? simplifiedRestroomHold + Time.deltaTime : 0;
                        prompt.gameObject.SetActive(true);
                        prompt.text = "HOLD [" + input.Hint(input.WorldInteract) + "] TO FINISH  " +
                            (Mathf.Clamp01(simplifiedRestroomHold / 2f) * 100).ToString("0") +
                            "%  [" + input.Hint(input.Inventory) + "] STANDARD TIMING";
                        if (simplifiedRestroomHold >= 2f) FinishRestroom(true);
                        return;
                    }
                    restroomProgress += Time.deltaTime / 7f;
                    prompt.gameObject.SetActive(true);
                    prompt.text = (restroomBowel ? "BOWEL" : "BLADDER") + "  " + (restroomProgress * 100).ToString("0") +
                        "%  TAP [" + input.Hint(input.WorldInteract) + "] ON EACH MARK  " + restroomCues +
                        "/3  [" + input.Hint(input.Inventory) + "] SIMPLIFIED";
                    if (restroomProgress >= 1f) FinishRestroom(restroomCues == 3);
                }
                return;
            }
            if (Mode == TruckTaxiPlayerControlMode.SpecialInteraction)
            {
                if (!host.Paused && ambientChallenge != null)
                {
                    float look = input.OnFootLook.ReadValue<Vector2>().x;
                    ambientChallenge.Tick(Time.deltaTime, look * (input.OnFootLook.activeControl?.device is Gamepad ?
                        Time.deltaTime * .8f : .006f), 0, input.WorldInteract.WasPressedThisFrame());
                    prompt.gameObject.SetActive(true);
                    prompt.text = "ROAD PEP - HOLD THE MARK, THEN TAP [" + input.Hint(input.WorldInteract) +
                        "]  " + (ambientChallenge.Elapsed / TruckTaxiRoadPepChallenge.TotalSeconds * 100).ToString("0") + "%";
                    if (ambientChallenge.Finished)
                    {
                        host.DriverNeeds.State.CompleteRoadPep(ambientChallenge.Score);
                        ShowFeedback("ROAD PEP " + ambientChallenge.Rating + " - THE COMEDOWN WILL FOLLOW");
                        ambientChallenge = null;
                        Mode = TruckTaxiPlayerControlMode.OnFoot;
                    }
                }
                return;
            }
            if (Mode == TruckTaxiPlayerControlMode.Driving)
            {
                if (input.ExitTruck.WasPressedThisFrame()) TryExitTruck();
                return;
            }
            if (host.Paused) { SetCursor(false); return; }
            if (InventoryOpen || DamageOpen) { SetCursor(false); return; }
            if (Mode != TruckTaxiPlayerControlMode.OnFoot) return;
            SetCursor(true);
            MoveAndLook(input);
            AimAtInteractable();
            if (input.ExitTruck.WasPressedThisFrame() && Vector3.Distance(footBody.transform.position, host.Player.transform.position) <= returnToTruckDistance)
                TryEnterTruck();
            else if (aimed != null && aimed.CanUse(this))
            {
                if (aimed.HoldToUse)
                {
                    if (input.WorldInteract.IsPressed()) aimed.Hold(this, Time.deltaTime);
                    else host.Fuel?.StopManualFueling();
                }
                else if (input.WorldInteract.WasPressedThisFrame()) aimed.Use(this);
            }
            if (input.Inventory.WasPressedThisFrame()) OpenInventory();
            if (input.InspectTruck.WasPressedThisFrame()) OpenDamage();
            if (prompt != null)
            {
                prompt.gameObject.SetActive(true);
                prompt.text = Time.unscaledTime < feedbackUntil ? Feedback : aimed != null && aimed.CanUse(this)
                    ? "[" + input.Hint(input.WorldInteract) + "] " + aimed.Prompt : "[" + input.Hint(input.ExitTruck) + "] ENTER TRUCK NEAR CAB";
            }
        }
        private void MoveAndLook(TruckTaxiUIInput input)
        {
            Vector2 look = input.OnFootLook.ReadValue<Vector2>();
            bool pad = input.OnFootLook.activeControl?.device is Gamepad;
            float gain = pad ? gamepadLookDegreesPerSecond * Time.deltaTime : mouseSensitivity;
            footBody.transform.Rotate(Vector3.up, look.x * gain);
            pitch = Mathf.Clamp(pitch - look.y * gain, -85, 85);
            footCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 axis = Vector2.ClampMagnitude(input.OnFootMove.ReadValue<Vector2>(), 1);
            Vector3 horizontal = (footBody.transform.right * axis.x + footBody.transform.forward * axis.y) *
                (input.OnFootSprint.IsPressed() ? runningSpeed : walkingSpeed);
            verticalSpeed = controller.isGrounded ? -1.5f : Mathf.Max(-25, verticalSpeed - 20 * Time.deltaTime);
            controller.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
        private void AimAtInteractable()
        {
            aimed = null;
            if (!Physics.Raycast(footCamera.transform.position, footCamera.transform.forward, out RaycastHit hit, 4.5f, ~0, QueryTriggerInteraction.Collide)) return;
            var candidate = hit.collider.GetComponentInParent<TruckTaxiWorldInteractable>();
            if (candidate != null && hit.distance <= candidate.interactionRange && candidate.CanUse(this)) aimed = candidate;
        }
        public bool TryExitTruck()
        {
            if (Mode != TruckTaxiPlayerControlMode.Driving || host?.CanResume != true || host.Paused ||
                host.Fuel?.IsRescuing == true || host.Roadside?.IsRecovering == true || host.FastTravel?.IsTravelling == true ||
                host.Races?.MenuOpen == true || host.CustomContent?.IsPlaying == true ||
                truckBody == null || truckBody.linearVelocity.magnitude > .45f)
            { ShowFeedback("Stop safely before exiting the truck."); return false; }
            Vector3 position = Vector3.zero;
            if (!FindSafeExit(out position)) { ShowFeedback("No clear ground beside the cab."); return false; }
            truckCamera = Camera.main;
            truckListener = truckCamera != null ? truckCamera.GetComponent<AudioListener>() : null;
            footBody.SetActive(true); controller.enabled = false;
            footBody.transform.SetPositionAndRotation(position, Quaternion.Euler(0, host.Player.transform.eulerAngles.y, 0));
            controller.enabled = true; verticalSpeed = pitch = 0;
            foreach (var source in drivingSources) if (source != null) source.AcquireDrivingInputSuppression(this);
            if (truckCamera != null) truckCamera.enabled = false;
            footCamera.enabled = true;
            if (truckListener != null) truckListener.enabled = false;
            footListener.enabled = true;
            Mode = TruckTaxiPlayerControlMode.OnFoot;
            SetCursor(true);
            ShowFeedback("ON FOOT");
            return true;
        }
        private bool FindSafeExit(out Vector3 position)
        {
            position = default;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 origin = host.Player.transform.TransformPoint(new Vector3(2.35f * side, 3f, .25f));
                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit ground, 7f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (ground.collider.transform.IsChildOf(host.Player.transform) || ground.normal.y < .7f) continue;
                Vector3 candidate = ground.point + Vector3.up * .06f;
                int count = Physics.OverlapCapsuleNonAlloc(candidate + Vector3.up * .36f, candidate + Vector3.up * 1.5f,
                    .31f, exitHits, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < count; i++)
                    if (exitHits[i] != null && exitHits[i] != ground.collider &&
                        !exitHits[i].transform.IsChildOf(host.Player.transform)) { blocked = true; break; }
                if (blocked) continue;
                position = candidate; return true;
            }
            return false;
        }
        public bool TryEnterTruck()
        {
            if (!IsOnFoot || InventoryOpen || DamageOpen || host.Paused || footBody == null ||
                Vector3.Distance(footBody.transform.position, host.Player.transform.position) > returnToTruckDistance) return false;
            ReturnToTruck();
            return true;
        }
        public void ReturnToTruck()
        {
            host.Fuel?.StopManualFueling();
            ActiveNozzleStation = null;
            if (footCamera != null) footCamera.enabled = false;
            if (truckCamera != null) truckCamera.enabled = true;
            if (footListener != null) footListener.enabled = false;
            if (truckListener != null) truckListener.enabled = true;
            foreach (var source in drivingSources) if (source != null) source.ReleaseDrivingInputSuppression(this);
            if (footBody != null) footBody.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
            if (damagePanel != null) damagePanel.gameObject.SetActive(false);
            if (temporaryPanel != null) temporaryPanel.gameObject.SetActive(false);
            seatedAtDiner = false; ambientChallenge = null;
            aimed = null; if (prompt != null) prompt.gameObject.SetActive(false);
            Mode = TruckTaxiPlayerControlMode.Driving; SetCursor(false);
        }
        private void SetCursor(bool locked)
        {
            CursorLockMode mode = locked ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != mode) Cursor.lockState = mode;
            if (Cursor.visible == locked) Cursor.visible = !locked;
        }
        public void ShowFeedback(string message)
        { Feedback = message ?? string.Empty; feedbackUntil = Time.unscaledTime + 3f; }
        public bool HandleHudInput(TruckTaxiUIInput input)
        {
            if (Mode == TruckTaxiPlayerControlMode.RestroomQte)
            {
                if (input.Cancel.WasPressedThisFrame()) FinishRestroom(false);
                else if (!simplifiedRestroomTiming && input.WorldInteract.WasPressedThisFrame())
                {
                    float mark = (restroomCues + 1) * .25f;
                    if (Mathf.Abs(restroomProgress - mark) > .065f) FinishRestroom(false);
                    else restroomCues++;
                }
                return true;
            }
            if (Mode == TruckTaxiPlayerControlMode.SpecialInteraction)
            {
                if (input.Cancel.WasPressedThisFrame())
                { ambientChallenge = null; Mode = TruckTaxiPlayerControlMode.OnFoot; ShowFeedback("ENCOUNTER CANCELLED"); }
                return true;
            }
            if (temporaryPanel != null && temporaryPanel.gameObject.activeSelf)
            {
                if (input.Cancel.WasPressedThisFrame()) CloseTemporaryInteraction();
                else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
                return true;
            }
            if (DamageOpen)
            {
                if (input.Cancel.WasPressedThisFrame() || input.InspectTruck.WasPressedThisFrame()) CloseDamage();
                else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
                return true;
            }
            if (!InventoryOpen) return false;
            if (input.Cancel.WasPressedThisFrame() || input.Inventory.WasPressedThisFrame()) CloseInventory();
            else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
            return true;
        }
        public bool StartRestroom(string stableToiletId, bool bowel)
        {
            if (!IsOnFoot || string.IsNullOrWhiteSpace(stableToiletId) || host?.DriverNeeds?.State == null) return false;
            if (bowel && host.DriverNeeds.State.BowelPressure < .2f)
            { ShowFeedback("NO BOWEL BREAK NEEDED YET"); return false; }
            restroomId = stableToiletId; restroomBowel = bowel;
            restroomProgress = 0; restroomCues = 0; simplifiedRestroomHold = 0;
            Mode = TruckTaxiPlayerControlMode.RestroomQte;
            ShowFeedback(bowel ? "BOWEL BREAK" : "BLADDER BREAK");
            return true;
        }
        private void FinishRestroom(bool success)
        {
            if (Mode != TruckTaxiPlayerControlMode.RestroomQte) return;
            if (success)
            {
                if (restroomBowel) host.DriverNeeds.State.RelieveBowel();
                else host.DriverNeeds.State.UseBathroom(true);
                if (restroomId.EndsWith(".toilet", System.StringComparison.Ordinal) ||
                    restroomId.EndsWith(".porta", System.StringComparison.Ordinal))
                    host.DriverNeeds.State.RecordToiletUse(restroomId);
            }
            Mode = TruckTaxiPlayerControlMode.OnFoot;
            ShowFeedback(success ? "RELIEVED - TOILET LOG UPDATED" : "MISSED THE TIMING - TRY AGAIN");
        }
        public void OpenInventory()
        {
            if (!IsOnFoot || inventoryPanel == null) return;
            inventoryPanel.gameObject.SetActive(true); RefreshInventory(); SetCursor(false);
        }
        public void CloseInventory()
        { if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false); SetCursor(IsOnFoot && !host.Paused); }
        public void OpenDamage()
        {
            if (!IsOnFoot || damagePanel == null || host?.ComponentDamage == null ||
                Vector3.Distance(FootTransform.position, host.Player.transform.position) > 7f)
            { ShowFeedback("APPROACH THE TRUCK TO INSPECT IT"); return; }
            damagePanel.gameObject.SetActive(true); damagePage = 0; RefreshDamage(); SetCursor(false);
        }
        public void CloseDamage()
        { if (damagePanel != null) damagePanel.gameObject.SetActive(false); SetCursor(IsOnFoot && !host.Paused); }
        public void OpenDiner(Transform seat)
        {
            if (!IsOnFoot || seat == null) return;
            preSeatPosition = footBody.transform.position;
            seatedAtDiner = true;
            controller.enabled = false;
            footBody.transform.position = seat.TransformPoint(new Vector3(0, 0, 1.3f));
            controller.enabled = true;
            Mode = TruckTaxiPlayerControlMode.SeatedInteraction;
            OpenTemporaryPanel("ROADSIDE DINER", "ORDER MEAL", () =>
            {
                var needs = host.DriverNeeds;
                ShowFeedback(needs.EatHere() ? "MEAL SERVED" : needs.Feedback);
            });
        }
        public void OpenCabSleep()
        {
            if (!IsOnFoot) return;
            Mode = TruckTaxiPlayerControlMode.SeatedInteraction;
            OpenTemporaryPanel("CAB REST", "SLEEP 2 HOURS", () => SleepInCab(2));
            hud.Button(temporaryPanel, "SLEEP 4 HOURS", new Vector2(.12f, .35f), new Vector2(.88f, .48f), () => SleepInCab(4));
            hud.Button(temporaryPanel, "SLEEP 8 HOURS", new Vector2(.12f, .19f), new Vector2(.88f, .32f), () => SleepInCab(8));
        }
        public void OpenAmbientChoice(TruckTaxiAmbientCharacter source)
        {
            if (!IsOnFoot || source == null || host.DriverNeeds?.State == null) return;
            Mode = TruckTaxiPlayerControlMode.SeatedInteraction;
            if (source.Role == TruckTaxiAmbientRole.Panhandler)
            {
                string choiceId = source.StableId;
                OpenTemporaryPanel("SPARE CHANGE?", "GIVE $2.00", () =>
                {
                    if (host.Session.TrySpend(200))
                    {
                        host.DriverNeeds.State.RecordAmbientChoice(choiceId + ".gave");
                        ShowFeedback("YOU GAVE SPARE CHANGE"); CloseTemporaryInteraction();
                    }
                    else ShowFeedback("NOT ENOUGH CASH");
                });
                hud.Button(temporaryPanel, "DECLINE", new Vector2(.12f, .35f), new Vector2(.88f, .48f), () =>
                {
                    host.DriverNeeds.State.RecordAmbientChoice(choiceId + ".declined");
                    CloseTemporaryInteraction();
                });
            }
            else
            {
                string choiceId = source.StableId;
                OpenTemporaryPanel("ROADSIDE STRANGER", "TRY FICTIONAL ROAD PEP", () =>
                {
                    host.DriverNeeds.State.RecordAmbientChoice(choiceId + ".accepted");
                    temporaryPanel.gameObject.SetActive(false);
                    Mode = TruckTaxiPlayerControlMode.SpecialInteraction;
                    ambientChallenge = new TruckTaxiRoadPepChallenge();
                    SetCursor(true);
                });
                hud.Button(temporaryPanel, "WALK AWAY", new Vector2(.12f, .35f), new Vector2(.88f, .48f), () =>
                {
                    host.DriverNeeds.State.RecordAmbientChoice(choiceId + ".declined");
                    CloseTemporaryInteraction();
                });
            }
        }
        private void SleepInCab(int hours)
        {
            var needs = host.DriverNeeds;
            bool rested = needs.TryTruckSleep(hours);
            ShowFeedback(rested ? "RESTED IN THE CAB" : needs.Feedback);
            if (rested) CloseTemporaryInteraction();
        }
        private void OpenTemporaryPanel(string title, string command, System.Action action)
        {
            for (int i = temporaryPanel.childCount - 1; i >= 0; i--) Destroy(temporaryPanel.GetChild(i).gameObject);
            hud.Text(temporaryPanel, "Interaction title", new Vector2(.08f, .76f), new Vector2(.92f, .94f), 30).text = title;
            hud.Button(temporaryPanel, command, new Vector2(.12f, .52f), new Vector2(.88f, .7f), action);
            hud.Button(temporaryPanel, "STAND / CLOSE", new Vector2(.12f, .04f), new Vector2(.88f, .17f), CloseTemporaryInteraction);
            temporaryPanel.gameObject.SetActive(true); SetCursor(false);
        }
        public void CloseTemporaryInteraction()
        {
            if (temporaryPanel != null) temporaryPanel.gameObject.SetActive(false);
            if (Mode == TruckTaxiPlayerControlMode.SeatedInteraction)
            {
                controller.enabled = false;
                if (seatedAtDiner) footBody.transform.position = preSeatPosition;
                controller.enabled = true;
                seatedAtDiner = false;
                Mode = TruckTaxiPlayerControlMode.OnFoot;
            }
            SetCursor(IsOnFoot && !host.Paused);
        }
        public bool CheckInMotel(TruckTaxiServicePoint point)
        {
            if (!IsOnFoot || point == null || !point.Supports(TruckTaxiServiceCapability.Sleep) ||
                host.DriverNeeds?.State == null || string.IsNullOrWhiteSpace(point.stableId) ||
                point.stableId.Length > 128) return false;
            var body = host.Player.GetComponent<Rigidbody>();
            if (body == null || !point.CanUse(body.position, body.linearVelocity.magnitude,
                TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond))
            { ShowFeedback("PARK IN THE MOTEL BAY FIRST"); return false; }
            double now = host.Environment.Clock.CurrentSnapshot.totalGameSeconds;
            if (host.DriverNeeds.State.HasMotelAccess(point.stableId, now))
            { ShowFeedback("ROOM KEY STILL VALID"); return true; }
            long cost = System.Math.Max(2500, point.SleepCostCents(8));
            if (!host.Session.TrySpend(cost)) { ShowFeedback("NOT ENOUGH CASH FOR A ROOM"); return false; }
            host.DriverNeeds.State.RentMotel(point.stableId, now, 12 * 3600);
            ShowFeedback("ROOM RENTED FOR 12 HOURS  " + TruckTaxiHud.Money(cost));
            return true;
        }
        public bool SleepAtMotel(TruckTaxiServicePoint point)
        {
            if (!IsOnFoot || point == null || host.DriverNeeds?.State == null ||
                !host.DriverNeeds.State.HasMotelAccess(point.stableId,
                    host.Environment.Clock.CurrentSnapshot.totalGameSeconds))
            { ShowFeedback("CHECK IN AT THE MOTEL DESK FIRST"); return false; }
            host.DriverNeeds.RefreshProximity();
            bool rested = host.DriverNeeds.TrySleep(8, true);
            ShowFeedback(rested ? "RESTED AT MOTEL" : host.DriverNeeds.Feedback);
            return rested;
        }
        private void RefreshDamage()
        {
            if (damagePanel == null || host?.ComponentDamage == null) return;
            var all = new List<TruckTaxiDamagePart>(host.ComponentDamage.Parts.Values);
            all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            const int pageSize = 8;
            damagePage = Mathf.Clamp(damagePage, 0, Mathf.Max(0, (all.Count - 1) / pageSize));
            damageHeading.text = "TRUCK CONDITION  " + (damagePage + 1) + "/" + Mathf.Max(1, (all.Count + pageSize - 1) / pageSize);
            var previous = damagePanel.Find("Condition rows"); if (previous != null) Destroy(previous.gameObject);
            var rows = TruckTaxiHud.Rect(damagePanel, "Condition rows", new Vector2(.05f, .12f), new Vector2(.95f, .88f));
            for (int i = damagePage * pageSize; i < Mathf.Min(all.Count, (damagePage + 1) * pageSize); i++)
            {
                int row = i - damagePage * pageSize;
                var part = all[i];
                float top = 1f - row / (float)pageSize;
                float bottom = 1f - (row + .9f) / pageSize;
                hud.Text(rows, part.Id, new Vector2(0, bottom), new Vector2(.72f, top), 21).text =
                    part.DisplayName + "  " + (part.Condition * 100).ToString("0") + "%  " + part.State;
                if (part.RepairCostCents > 0)
                    hud.Button(rows, "REPAIR " + TruckTaxiHud.Money(part.RepairCostCents),
                        new Vector2(.74f, bottom), new Vector2(1f, top), () =>
                        {
                            ShowFeedback(host.ComponentDamage.TryRepair(part.Id) ? part.DisplayName + " REPAIRED" :
                                "PARK AT A REPAIR SERVICE AND CHECK FUNDS");
                            RefreshDamage();
                        });
            }
        }
        public void AddToBasket(TruckTaxiStorePoint store, TruckTaxiNeedsItem item)
        {
            if (!IsOnFoot || store == null || !store.Sells(item)) return;
            if (basketStore != null && basketStore != store) basket.Clear();
            basketStore = store;
            basket.TryGetValue(item, out int quantity);
            if (quantity >= 99) { ShowFeedback("BASKET FULL"); return; }
            basket[item] = quantity + 1;
            ShowFeedback("ADDED " + TruckTaxiNeedsItems.Find(item).Name + " TO BASKET");
            RefreshInventory();
        }
        public void ToggleNozzle(TruckTaxiGasStationPoint station)
        {
            if (!IsOnFoot || station == null) return;
            if (ActiveNozzleStation == station)
            { ActiveNozzleStation = null; host.Fuel?.StopManualFueling(); ShowFeedback("NOZZLE RETURNED"); }
            else { ActiveNozzleStation = station; ShowFeedback("TAKE NOZZLE TO TRUCK FILLER"); }
        }
        public bool PumpFromNozzle(float seconds)
        {
            if (ActiveNozzleStation == null || FootTransform == null ||
                Vector3.Distance(FootTransform.position, ActiveNozzleStation.Position) > 12f)
            { ShowFeedback("NOZZLE HOSE DOES NOT REACH"); return false; }
            if (host.Player?.TruckControlController?.CurrentState.parkingBrakeOn != true)
            { ShowFeedback("SET THE PARKING BRAKE BEFORE FUELING"); return false; }
            bool success = host.Fuel != null && host.Fuel.PumpManual(ActiveNozzleStation, seconds);
            if (!success) ShowFeedback(host.Fuel?.Feedback ?? "PUMP UNAVAILABLE");
            return success;
        }
        public void RemoveFromBasket(TruckTaxiNeedsItem item)
        {
            if (!basket.TryGetValue(item, out int quantity)) return;
            if (quantity <= 1) basket.Remove(item); else basket[item] = quantity - 1;
            RefreshInventory();
        }
        public bool Checkout()
        {
            var state = host?.DriverNeeds?.State;
            if (!IsOnFoot || state == null || basketStore == null || basket.Count == 0) return false;
            if (Vector3.Distance(FootTransform.position, basketStore.transform.position) > 20f)
            { ShowFeedback("RETURN TO THE STORE REGISTER"); return false; }
            long total = 0;
            foreach (var pair in basket)
            {
                var item = TruckTaxiNeedsItems.Find(pair.Key);
                if (item == null || !item.Purchasable || !basketStore.Sells(pair.Key) ||
                    state.Count(pair.Key) + pair.Value > 99)
                { ShowFeedback("ITEM UNAVAILABLE OR INVENTORY FULL"); return false; }
                total += (long)item.PriceCents * pair.Value;
            }
            if (!host.Session.TrySpend(total)) { ShowFeedback("NOT ENOUGH CASH"); return false; }
            foreach (var pair in basket) state.AddItem(pair.Key, pair.Value);
            basket.Clear(); ShowFeedback("PURCHASED " + TruckTaxiHud.Money(total)); RefreshInventory();
            return true;
        }
        private void RefreshInventory()
        {
            if (inventoryPanel == null || host?.DriverNeeds?.State == null) return;
            var state = host.DriverNeeds.State;
            inventoryDetails.text = "HUNGER " + (state.Hunger * 100).ToString("0") + "%\nTHIRST " +
                (state.Thirst * 100).ToString("0") + "%\nBLADDER " + (state.Pressure * 100).ToString("0") +
                "%\nBOWEL " + (state.BowelPressure * 100).ToString("0") + "%\n\nTOILETS USED " +
                state.ToiletsDiscovered + "/" + (host.PhysicalFacilities?.TotalToilets ?? 0);
            var lines = new System.Text.StringBuilder("BASKET\n");
            foreach (var pair in basket) lines.Append(pair.Value).Append(" x ").Append(TruckTaxiNeedsItems.Find(pair.Key).Name).Append('\n');
            basketText.text = lines.ToString();
            var oldBasket = inventoryPanel.Find("Basket removal buttons");
            if (oldBasket != null) Destroy(oldBasket.gameObject);
            var basketButtons = TruckTaxiHud.Rect(inventoryPanel, "Basket removal buttons", new Vector2(.54f, .1f), new Vector2(.95f, .3f));
            int basketRow = 0;
            foreach (var pair in basket)
            {
                var item = pair.Key;
                float bottom = 1f - (basketRow + 1) / 6f;
                hud.Button(basketButtons, "RETURN 1 " + TruckTaxiNeedsItems.Find(item).Name,
                    new Vector2(0, bottom), new Vector2(1, bottom + .15f), () => RemoveFromBasket(item));
                if (++basketRow >= 6) break;
            }
            var oldGrid = inventoryPanel.Find("Item grid");
            if (oldGrid != null) Destroy(oldGrid.gameObject);
            var grid = TruckTaxiHud.Rect(inventoryPanel, "Item grid", new Vector2(.04f, .08f), new Vector2(.51f, .87f));
            int index = 0;
            foreach (var entry in TruckTaxiNeedsItems.All)
            {
                int count = state.Count(entry.Item);
                if (count <= 0) continue;
                int column = index % 3, row = index / 3;
                float x = column / 3f, y = 1f - (row + 1) / 6f;
                var item = entry.Item;
                hud.Button(grid, entry.Name + " x" + count, new Vector2(x + .01f, y + .02f),
                    new Vector2(x + .31f, y + .155f), () =>
                    {
                        inventoryDetails.text = entry.Name + "\n" + entry.StableId + "\n" +
                            (host.DriverNeeds.ConsumeItem(item) ? "USED" : "CANNOT USE NOW");
                        RefreshInventory();
                    });
                index++;
            }
        }
        private void OnDestroy()
        {
            foreach (var source in drivingSources) if (source != null) source.ReleaseDrivingInputSuppression(this);
            if (truckCamera != null) truckCamera.enabled = true;
            if (truckListener != null) truckListener.enabled = true;
            if (host?.Fuel != null) host.Fuel.StopManualFueling();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
