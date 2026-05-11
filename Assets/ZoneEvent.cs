using UnityEngine;
using UnityEngine.Events;
public class ZoneEvent : TriggerEvent
{
    public bool InTrigger {get; private set;}

    void OnTriggerEnter(Collider other)
    {
        if (filterByLayer && other.gameObject.layer != targetLayer) return;
        if (filterByTag   && !other.CompareTag(targetTag)) return;
        InTrigger = true;
        OnEnterTrigger?.Invoke();
        
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != targetLayer) return;
        if (!other.CompareTag(targetTag)) return;
        InTrigger = false;
        OnExitTrigger?.Invoke();
    }

}