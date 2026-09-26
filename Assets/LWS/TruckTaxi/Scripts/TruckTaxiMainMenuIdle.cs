using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiMainMenuIdle : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private string stateName = "Idle_Normal";
        [SerializeField, Range(0,1)] private float phase;
        private Vector3 visualPosition;
        public void Configure(Animator target,string state,float normalizedPhase,Transform visualRoot = null)
        {
            animator=target;
            visual=visualRoot;
            stateName=state;
            phase=normalizedPhase;
            if(visual!=null) visualPosition=visual.localPosition;
        }
        private void Awake()
        {
            if(visual!=null) visualPosition=visual.localPosition;
        }
        private void Start()
        {
            if(animator!=null && animator.runtimeAnimatorController!=null && animator.HasState(0,Animator.StringToHash(stateName)))
                animator.Play(stateName,0,phase);
        }
        private void LateUpdate()
        {
            if(visual==null) return;
            float time=Time.unscaledTime*(1.4f+phase)+phase*6.28f;
            visual.localPosition=visualPosition+Vector3.up*(Mathf.Sin(time*2f)*.025f);
            visual.localRotation=Quaternion.Euler(0,Mathf.Sin(time*.7f)*3f,Mathf.Sin(time)*4f);
        }
    }
}
