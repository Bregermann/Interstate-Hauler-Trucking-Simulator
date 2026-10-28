using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LWS.TruckPetPals
{
    [Serializable] public sealed class PetCareCard
    {
        public RectTransform root;
        public TMP_Text identity, detail;
        public Image[] fills;
    }
    public sealed class PetCareHUD : MonoBehaviour
    {
        public PetPalsMatch match;
        public PetCareCard[] cards;
        public TMP_Text saveNotice;
        float next;
        void Update()
        {
            if (Time.unscaledTime < next) return; next = Time.unscaledTime + .15f;
            bool show = match.Live && !match.PhotoActive;
            int views = match.drivingViews.rally.Humans;
            for (int i = 0; i < cards.Length; i++)
            {
                bool active = show && (views > 0 ? i < views : i < match.setup.count); cards[i].root.gameObject.SetActive(active); if (!active) continue;
                int owner = views > 0 ? match.drivingViews.rally.owners[i] : i; var pet = match.companions[owner]; if (pet.Record == null) continue;
                var record = pet.Record; var card = cards[i];
                if (views > 0)
                {
                    var rect = match.drivingViews.rally.views[i].view.rect; card.root.anchorMin = card.root.anchorMax = rect.min; card.root.anchoredPosition = new Vector2(216, 84); card.root.sizeDelta = new Vector2(400, 148);
                }
                else { card.root.anchorMin = card.root.anchorMax = new Vector2(.5f, 0); card.root.anchoredPosition = new Vector2((i - (match.setup.count - 1) * .5f) * 456, 142); card.root.sizeDelta = new Vector2(438, 148); }
                card.identity.text = match.setup.players[owner].name + " · " + record.name + " · BOND " + record.BondLevel;
                card.identity.enableAutoSizing = true;card.identity.fontSizeMax = 22;card.identity.fontSizeMin = 14;
                card.identity.color = match.setup.players[owner].color;
                card.detail.text = pet.Mood == PetMood.Bathing ? pet.Bath.Step + " " + pet.Bath.Progress.ToString("P0") + " · " + pet.Hint : pet.TrainingCue ? "ACTION NOW · GENTLE TRAINING CUE" : pet.Mood.ToString().ToUpperInvariant() + (record.leashed ? " · LEASHED" : " · FREE ROAM");
                float[] needs = { record.hunger, record.cleanliness, record.happiness, record.energy, record.affection };
                for (int n = 0; n < card.fills.Length; n++) card.fills[n].fillAmount = Mathf.Clamp01(needs[n] / 100);
            }
            if (saveNotice != null)
            {
                saveNotice.gameObject.SetActive(show && match.petProfiles.ReadOnly);
                saveNotice.text = "PET DATA READ ONLY · ORIGINAL SAVE RETAINED";
            }
        }
    }
}
