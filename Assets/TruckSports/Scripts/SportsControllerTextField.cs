using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
namespace LWS.TruckSports
{
    // Register once per loaded scene (including inactive pages), not by global Update scans.
    [DefaultExecutionOrder(-500), DisallowMultipleComponent, RequireComponent(typeof(TMP_InputField))]
    public sealed class SportsControllerTextField : MonoBehaviour, ISelectHandler, ISubmitHandler
    {
        TMP_InputField field;
        bool activateOnSelect;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { SceneManager.sceneLoaded -= Loaded; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += Loaded; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initial() { for (int i = 0; i < SceneManager.sceneCount; i++) Loaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive); }
        static void Loaded(Scene scene, LoadSceneMode mode)
        { if (scene.isLoaded) foreach (var root in scene.GetRootGameObjects()) BindUnder(root.transform); }
        public static void BindUnder(Transform root)
        {
            foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                bool shared = input.GetComponentInParent<SportsMenuView>(true) != null || input.GetComponentInParent<LWS.TruckBilliards.BilliardsMenus>(true) != null;
                if (shared && input.GetComponent<SportsControllerTextField>() == null) input.gameObject.AddComponent<SportsControllerTextField>();
            }
        }
        void Awake() { field = GetComponent<TMP_InputField>(); activateOnSelect = field.shouldActivateOnSelect; field.shouldActivateOnSelect = false; }
        void OnDestroy() { if (field != null) field.shouldActivateOnSelect = activateOnSelect; }
        void OnDisable() { SportsControllerKeyboard.CancelFor(field); }
        void Update()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject) OpenFromPad();
        }
        public void OnSelect(BaseEventData data) { OpenFromPad(); }
        public void OnSubmit(BaseEventData data) { if (OpenFromPad()) data?.Use(); }
        bool OpenFromPad()
        {
            if (field == null || !field.IsInteractable() || field.readOnly || field.isFocused || SportsControllerKeyboard.InputBlocked) return false;
            var pad = SportsControllerKeyboard.SubmittingPad();
            return pad != null && SportsControllerKeyboard.Open(field, pad);
        }
    }
}
