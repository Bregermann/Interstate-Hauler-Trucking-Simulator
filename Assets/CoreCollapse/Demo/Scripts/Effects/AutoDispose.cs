using UnityEngine;

namespace Assets.CoreCollapse.Demo.Scripts.Effects
{
    public class AutoDispose : MonoBehaviour
    {
        [SerializeField] float lifetime;

        void Start() => 
            Destroy(this.gameObject, lifetime);
    }
}
