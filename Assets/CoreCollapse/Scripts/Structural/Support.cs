using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.CoreCollapse.Scripts.Structural
{
    public class Support : MonoBehaviour
    {
        [Serializable] public class OnCollapseEvent : UnityEvent { }

        [SerializeField] Support[] supportedBy;
        [SerializeField] float supportCapacity;
        [SerializeField] float supportWeight;
        [SerializeField] protected OnCollapseEvent onCollapse;

        [Header("EXPERIMENTAL")] 
        [SerializeField]
        bool includeSameLevel;

        IList<Support> supporting = new List<Support>();
        Collapser collapser;

        bool IsStanding => !collapser.HasCollapsed;

        void Start()
        {
            collapser = GetComponent<Collapser>();
            foreach (var s in supportedBy)
                s.RegisterSupportedStructure(this);

        }

        public void AddSupport(Support support)
        {
            var tempSupports = new List<Support>(supportedBy);
            tempSupports.Add(support);

            supportedBy = tempSupports.ToArray();
        }

        void RegisterSupportedStructure(Support structure) => 
            supporting.Add(structure);

        public void OnCollapse()
        {
            foreach (var s in supporting.Where(sup => sup.IsStanding)) 
                s.OnSupportCollapsed();
            onCollapse.Invoke();
        }

        void OnSupportCollapsed()
        {
            var supCap = supportedBy.Where(s => s.IsStanding).Sum(s => s.supportCapacity);
            if (supportWeight > supCap)
                collapser.ReceiveAllDamage();
        }

        void OnDrawGizmos()
        {
            if (supportedBy != null && supportedBy.Any())
                foreach (var s in supportedBy)
                    Debug.DrawLine(transform.position, s.transform.position, Color.green);
        }

        public void ClearSupports() =>
            supportedBy = Array.Empty<Support>();
    }
}
