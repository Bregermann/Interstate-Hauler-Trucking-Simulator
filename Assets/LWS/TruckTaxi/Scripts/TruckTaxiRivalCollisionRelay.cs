using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRivalCollisionRelay : MonoBehaviour
    {
        private TruckTaxiRivalPopulation population;
        private string rivalId;
        private int generation;

        public void Initialize(TruckTaxiRivalPopulation owner, string id, int incarnation)
        { population = owner; rivalId = id; generation = incarnation; }

        private void OnCollisionEnter(Collision collision) => population?.OnRivalCollision(rivalId, generation, collision);
        private void OnDisable() { population = null; rivalId = null; }
    }
}
