using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadAir
{
    [DefaultExecutionOrder(-190)]
    [DisallowMultipleComponent]
    public sealed class DeadAirEndingDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class EndingConfig
        {
            public DeadAirEndingId endingId = DeadAirEndingId.Exit17;
            public string title = "EXIT 17";
            [TextArea] public string description = "You made it off the route.";
            public float fadeSeconds = 1.25f;
            public float holdSeconds = 4f;
        }

        [Serializable]
        public sealed class EndingEligibilityRule
        {
            public DeadAirEndingId endingId = DeadAirEndingId.Lost;
            public string choiceId = "CHOICE_01";
            public DeadAirChoiceOutcome requiredOutcome = DeadAirChoiceOutcome.None;
        }

        [SerializeField] private List<EndingConfig> endings = new List<EndingConfig>();
        [SerializeField] private List<EndingEligibilityRule> eligibilityRules = new List<EndingEligibilityRule>();
        [SerializeField] private DeadAirEndingId fallbackEnding = DeadAirEndingId.Lost;
        [SerializeField] private bool buildRuntimeOverlay = true;

        private CanvasGroup _overlay;
        private Text _titleText;
        private Text _bodyText;
        private Coroutine _endingRoutine;

        public DeadAirEndingId CurrentEnding { get; private set; } = DeadAirEndingId.None;
        public IReadOnlyList<EndingConfig> Endings => endings;
        public IReadOnlyList<EndingEligibilityRule> EligibilityRules => eligibilityRules;

        private void Awake()
        {
            EnsureDefaultEndings();
            if (buildRuntimeOverlay)
            {
                BuildOverlay();
            }

            ResetEnding();
        }

        public void PlayEnding(DeadAirEndingId endingId)
        {
            EnsureDefaultEndings();
            if (_endingRoutine != null)
            {
                StopCoroutine(_endingRoutine);
            }

            CurrentEnding = endingId;
            _endingRoutine = StartCoroutine(EndingRoutine(ResolveConfig(endingId)));
        }

        public DeadAirEndingId ResolveEndingFromChoices(DeadAirStoryDirector story)
        {
            EnsureDefaultEndings();
            if (story == null)
            {
                return fallbackEnding;
            }

            for (int i = 0; i < eligibilityRules.Count; i++)
            {
                EndingEligibilityRule rule = eligibilityRules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.choiceId))
                {
                    continue;
                }

                if (story.TryGetChoice(rule.choiceId, out DeadAirChoiceOutcome outcome) &&
                    outcome == rule.requiredOutcome)
                {
                    return rule.endingId;
                }
            }

            return fallbackEnding;
        }

        public void PlayResolvedEnding(DeadAirStoryDirector story)
        {
            PlayEnding(ResolveEndingFromChoices(story));
        }

        public void ResetEnding()
        {
            CurrentEnding = DeadAirEndingId.None;
            if (_endingRoutine != null)
            {
                StopCoroutine(_endingRoutine);
                _endingRoutine = null;
            }

            if (_overlay != null)
            {
                _overlay.alpha = 0f;
                _overlay.blocksRaycasts = false;
            }
        }

        private IEnumerator EndingRoutine(EndingConfig config)
        {
            BuildOverlay();
            _titleText.text = config.title;
            _bodyText.text = config.description + "\n\nTRY AGAIN\nPress Enter to restart.";
            _overlay.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < config.fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _overlay.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, config.fadeSeconds));
                yield return null;
            }

            yield return new WaitForSecondsRealtime(config.holdSeconds);
            while (!WasRestartPressed())
            {
                yield return null;
            }

            DeadAirGameManager.Instance?.RestartRun();
        }

        private EndingConfig ResolveConfig(DeadAirEndingId endingId)
        {
            for (int i = 0; i < endings.Count; i++)
            {
                if (endings[i] != null && endings[i].endingId == endingId)
                {
                    return endings[i];
                }
            }

            return endings[0];
        }

        private void EnsureDefaultEndings()
        {
            if (endings.Count == 0)
            {
                endings.Add(new EndingConfig { endingId = DeadAirEndingId.Exit17, title = "EXIT 17", description = "The exit sign is real. This time." });
                endings.Add(new EndingConfig { endingId = DeadAirEndingId.TrustDispatch, title = "TRUST DISPATCH", description = "You kept the radio alive. Something kept listening back." });
                endings.Add(new EndingConfig { endingId = DeadAirEndingId.Lost, title = "LOST", description = "The highway loops until the signal eats the horizon." });
                endings.Add(new EndingConfig { endingId = DeadAirEndingId.TrustNoOne, title = "TRUST NO ONE", description = "You ignored every voice and found the only road that was not hungry." });
            }

            EnsureEndingConfig(DeadAirEndingId.SuckedIntoVoid, "SUCKED INTO THE VOID", "You left the marked road and the dark took the rig.");

            if (eligibilityRules.Count == 0)
            {
                eligibilityRules.Add(new EndingEligibilityRule { endingId = DeadAirEndingId.Exit17, choiceId = "CHOICE_03_EXIT17", requiredOutcome = DeadAirChoiceOutcome.Exit17 });
                eligibilityRules.Add(new EndingEligibilityRule { endingId = DeadAirEndingId.TrustNoOne, choiceId = "CHOICE_03_EXIT17", requiredOutcome = DeadAirChoiceOutcome.TrustNoOne });
                eligibilityRules.Add(new EndingEligibilityRule { endingId = DeadAirEndingId.TrustDispatch, choiceId = "CHOICE_01", requiredOutcome = DeadAirChoiceOutcome.TrustDispatch });
            }
        }

        private void EnsureEndingConfig(DeadAirEndingId endingId, string title, string description)
        {
            for (int i = 0; i < endings.Count; i++)
            {
                if (endings[i] != null && endings[i].endingId == endingId)
                {
                    return;
                }
            }

            endings.Add(new EndingConfig { endingId = endingId, title = title, description = description });
        }

        private void BuildOverlay()
        {
            if (_overlay != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject("Dead Air Ending Overlay", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7600;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            _overlay = canvasObject.AddComponent<CanvasGroup>();
            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;

            Image image = canvasObject.AddComponent<Image>();
            image.color = Color.black;

            RectTransform titleRect = CreateRect(canvasObject.transform, "Ending Title", new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 96f));
            _titleText = titleRect.gameObject.AddComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 54;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = Color.white;

            RectTransform bodyRect = CreateRect(canvasObject.transform, "Ending Body", new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 220f));
            _bodyText = bodyRect.gameObject.AddComponent<Text>();
            _bodyText.font = font;
            _bodyText.fontSize = 24;
            _bodyText.alignment = TextAnchor.UpperCenter;
            _bodyText.color = new Color(0.84f, 0.9f, 0.88f, 1f);
            _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static bool WasRestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return);
#endif
        }
    }
}
