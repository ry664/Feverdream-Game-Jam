using UnityEngine;
using UnityEngine.Events;
public class ContactEvent : TriggerEvent
{
    public bool InTrigger {get; private set;}

    void OnCollisionEnter(Collision other)
    {
        if (filterByLayer && other.gameObject.layer != targetLayer) return;
        if (filterByTag   && !other.transform.CompareTag(targetTag)) return;
        InTrigger = true;
        OnEnterTrigger?.Invoke();
        
    }
    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.layer != targetLayer) return;
        if (!other.transform.CompareTag(targetTag)) return;
        InTrigger = false;
        OnExitTrigger?.Invoke();
    }
}