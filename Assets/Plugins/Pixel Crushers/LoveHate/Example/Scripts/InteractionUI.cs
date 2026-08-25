using UnityEngine;
using System.Collections;

namespace PixelCrushers.LoveHate.Example
{

    /// <summary>
    /// This script maintains the faction info window in the lower
    /// right of the example scene.
    /// </summary>
    public class InteractionUI : MonoBehaviour
    {

        public CanvasGroup introCanvasGroup;

        public RectTransform interactionPanel;

        public UnityEngine.UI.Text npcSummaryText;

        public UnityEngine.UI.Button flatterButton;
        public UnityEngine.UI.Button insultButton;
        public UnityEngine.UI.Button giveButton;
        public UnityEngine.UI.Button stealButton;

        private IEnumerator Start()
        {
            UIUtility.RequireEventSystem(null);
            SetInteractionPanel(false);

            // Wait for intro canvas to close:
            float elapsed = 0;
            while (elapsed < 5)
            {
                if (IsInterruptKeyDown()) break;
                yield return null;
                elapsed += GameTime.deltaTime;
            }
            if (introCanvasGroup != null && introCanvasGroup.gameObject != null)
            {
                while (introCanvasGroup.alpha > 0.05f)
                {
                    if (IsInterruptKeyDown()) break;
                    yield return null;
                    introCanvasGroup.alpha -= GameTime.deltaTime;
                }
                introCanvasGroup.gameObject.SetActive(false);
            }
        }

        private bool IsInterruptKeyDown()
        {
            return InputDeviceManager.IsKeyDown(KeyCode.Escape) ||
                InputDeviceManager.IsKeyDown(KeyCode.Return) ||
                InputDeviceManager.IsKeyDown(KeyCode.Space) ||
                InputDeviceManager.DefaultGetMouseButtonDown(0) ||
                Mathf.Abs(InputDeviceManager.GetAxis("Vertical")) > 0.1 ||
                Mathf.Abs(InputDeviceManager.GetAxis("Horizontal")) > 0.1;
        }

        public void SetInteractionPanel(bool value)
        {
            if (interactionPanel != null && interactionPanel.gameObject != null)
            {
                interactionPanel.gameObject.SetActive(value);
            }
        }

    }

}
