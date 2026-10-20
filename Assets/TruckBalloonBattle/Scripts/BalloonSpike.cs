using UnityEngine;
namespace LWS.TruckBalloonBattle
{
    public sealed class BalloonSpike : MonoBehaviour
    {
        public BalloonMatch match; public int slot;
        public BoxCollider contact; public Renderer lamp; public Transform spear;
        MaterialPropertyBlock color;
        public void Pose()
        {
            bool live = match.Live && match.Countdown <= 0 && match.rules.Alive(slot);
            bool extended = live && match.rules.Extended(slot);
            float age = match.rules.AttackAge(slot);
            bool warning = live && age >= 0 && age < match.rules.telegraphSeconds;
            contact.enabled = extended;
            spear.localScale = new Vector3(1, 1, extended ? 1 : .22f);
            color ??= new MaterialPropertyBlock();
            var tint = warning ? Color.yellow : extended ? Color.red : match.rules.Cooldown(slot) > 0 ? new Color(.25f, .12f, .5f) : Color.cyan;
            color.SetColor("_BaseColor", tint); color.SetColor("_EmissionColor", tint * (warning || extended ? 3 : .4f)); lamp.SetPropertyBlock(color);
        }
    }
}
