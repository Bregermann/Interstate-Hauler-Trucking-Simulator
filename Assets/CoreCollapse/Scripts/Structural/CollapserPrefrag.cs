using UnityEngine;

namespace Assets.CoreCollapse.Scripts.Structural
{
    public class CollapserPrefrag : Collapser
    {
        [SerializeField] GameObject collapsedPrefab;
        
        protected override void Collapse()
        {
            base.Collapse();
            Instantiate(collapsedPrefab, transform.position, transform.rotation);
            gameObject.SetActive(false);
        }
    }
}