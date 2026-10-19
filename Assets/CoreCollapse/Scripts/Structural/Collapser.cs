using UnityEngine;
using UnityEngine.Events;
using System;

namespace Assets.CoreCollapse.Scripts.Structural
{
    [RequireComponent(typeof(Support))]
    public abstract class Collapser : MonoBehaviour
    {
        [SerializeField] protected float startingHp;
        protected float currentHp;
        protected Support support;

        public bool HasCollapsed => currentHp <= 0;
        public float HealthRatio => currentHp / startingHp;

        void Start()
        {
            currentHp = startingHp;
            support = GetComponent<Support>();
        }

        public virtual void ReceiveDamage(float damage)
        {
            currentHp -= damage;
            if (HasCollapsed && gameObject.activeInHierarchy)
                Collapse();
        }

        protected virtual void Collapse() => 
            support.OnCollapse();
        
        public virtual void ReceiveAllDamage() => ReceiveDamage(currentHp);
    }
}
