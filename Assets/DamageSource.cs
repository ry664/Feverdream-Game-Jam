using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
public class DamageSource : MonoBehaviour
{
    [SerializeField] DamageType damageType;
    [SerializeField] bool continous;

    public UnityEvent OnTakeDamage;

    TriggerEvent trigger;

    void Start()
    {
        switch (damageType)
        {
            case DamageType.Contact:
            trigger = GetComponent<ContactEvent>();
            trigger.OnEnterTrigger.AddListener(DoContactDamage);
            break;
            case DamageType.TriggerBox:
            trigger = GetComponent<ZoneEvent>();
            break;
        }  
    }
    void Update()
    {
        if(!continous) return;

        switch (damageType)
        {
            case DamageType.Contact:
            break;
            case DamageType.TriggerBox:
            break;
        }
    }

    void DoContactDamage(){}
    void DoTriggerDamage(){}

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