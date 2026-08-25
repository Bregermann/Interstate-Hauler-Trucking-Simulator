using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [DisallowMultipleComponent]
    public sealed class DeadAirSign : MonoBehaviour
    {
        [SerializeField] private DeadAirSignKind kind = DeadAirSignKind.RoadSign;
        [SerializeField] private string line1 = "EXIT 17";
        [SerializeField] private string line2;
        [SerializeField] private string line3;
        [SerializeField] private Color signColor = new Color(0.03f, 0.18f, 0.08f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Vector2 sizeMeters = new Vector2(5f, 2.3f);
        [SerializeField, TextArea] private string notes = "Editable diegetic road sign.";

        private TextMesh _text;

        public string DisplayText => string.Join("\n", new[] { line1, line2, line3 }).Trim();

        public void Configure(DeadAirSignKind signKind, string text)
        {
            kind = signKind;
            line1 = text;
            line2 = string.Empty;
            line3 = string.Empty;
            RefreshText();
        }

        private void Reset()
        {
            RefreshText();
        }

        private void OnValidate()
        {
            RefreshText();
        }

        private void RefreshText()
        {
            Transform textTransform = transform.Find("SIGN_TEXT");
            if (textTransform == null)
            {
                textTransform = new GameObject("SIGN_TEXT").transform;
                textTransform.SetParent(transform, false);
                textTransform.localPosition = new Vector3(0f, 0f, -0.03f);
                textTransform.localRotation = Quaternion.identity;
            }

            _text = textTransform.GetComponent<TextMesh>() ?? textTransform.gameObject.AddComponent<TextMesh>();
            _text.text = DisplayText;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = textColor;
            _text.fontSize = 72;
            _text.characterSize = 0.06f;
        }

        private void OnDrawGizmos()
        {
            if (!DeadAirTriggerZone.ShowDeadAirGizmos)
            {
                return;
            }

            Gizmos.color = signColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(sizeMeters.x, sizeMeters.y, 0.08f));
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(sizeMeters.x, sizeMeters.y, 0.08f));
            Gizmos.matrix = Matrix4x4.identity;
#if UNITY_EDITOR
            Handles.color = textColor;
            Handles.Label(transform.position + Vector3.up * (sizeMeters.y + 0.5f), $"{kind}\n{DisplayText}\n{notes}");
#endif
        }
    }
}
