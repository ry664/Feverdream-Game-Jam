using UnityEngine;
[RequireComponent(typeof(Collider))]
public class ZoneEvent : TriggerEvent
{

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (filterByLayer && other.gameObject.layer != targetLayer) return;
        if (filterByTag   && !other.CompareTag(targetTag)) return;
        InTrigger = true;
        EffectedObject = other.transform;
        OnEnterTrigger?.Invoke();
        
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != targetLayer) return;
        if (!other.CompareTag(targetTag)) return;
        InTrigger = false;
        EffectedObject = null;
        OnExitTrigger?.Invoke();
    }

}