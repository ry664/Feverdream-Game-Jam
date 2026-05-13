using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
public class DamageSource : MonoBehaviour
{
    [SerializeField] DamageType damageType;
    [SerializeField] bool continous;
    [SerializeField] int damage;

    public UnityEvent OnTakeDamage;

    TriggerEvent trigger;

    void Start()
    {
        switch (damageType)
        {
            case DamageType.Contact:
            trigger = GetComponent<ContactEvent>();
            break;

            case DamageType.TriggerBox:
            trigger = GetComponent<ZoneEvent>();
            break;
        } 
        if (!continous)
        {
            trigger.OnEnterTrigger.AddListener(DoDamage);
        }
    }
    void FixedUpdate()
    {
        if (trigger.InTrigger && continous)
        {
            DoDamage();
        }
    }

    void DoDamage()
    {
        if(continous)
        if(trigger.EffectedObject.TryGetComponent(out Health health))
        {
            health.TakeDamage(damage);
        }
    }


    void OnValidate()
    {
        switch (damageType)
        {
            case DamageType.Contact:
            this.GetOrAddComponent<ContactEvent>();
            if(TryGetComponent<ZoneEvent>(out var trigger)) DestroyImmediate(trigger);
            break;
            case DamageType.TriggerBox:
            this.GetOrAddComponent<ZoneEvent>();
            if(TryGetComponent<ContactEvent>(out var contact)) DestroyImmediate(contact);
            break;
        }
    }
    public enum DamageType
    {
        Contact,
        TriggerBox
    }
}