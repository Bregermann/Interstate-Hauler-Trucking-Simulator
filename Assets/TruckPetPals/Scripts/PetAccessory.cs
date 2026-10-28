using UnityEngine;
namespace LWS.TruckPetPals
{
    public sealed class PetAccessory : MonoBehaviour
    {
        public string[] ids;
        public GameObject[] models;
        public string RequestedId { get; private set; }
        public string ResolvedId { get; private set; }
        public void Select(string id)
        {
            RequestedId=id;int index=System.Array.IndexOf(ids,id);if(index<0)index=0;
            ResolvedId=ids[index];for(int i=0;i<models.Length;i++)models[i].SetActive(i==index);
        }
    }
}
