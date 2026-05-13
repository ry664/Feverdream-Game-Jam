using UnityEngine;
using UnityEngine.Events;
public class ContactEvent : TriggerEvent
{
    void OnCollisionEnter(Collision other)
    {
        if (filterByLayer && other.gameObject.layer != targetLayer) return;
        if (filterByTag   && !other.transform.CompareTag(targetTag)) return;
        EffectedObject = other.transform;
        OnEnterTrigger?.Invoke();
        
    }
    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.layer != targetLayer) return;
        if (!other.transform.CompareTag(targetTag)) return;
        EffectedObject = null;
        OnExitTrigger?.Invoke();
    }
}