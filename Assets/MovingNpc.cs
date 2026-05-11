using Unity.VisualScripting;
using UnityEngine;

public class NPCMovment : MonoBehaviour
{
    
}
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    
}
public class Health : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    public float selfHealSpeed;
    public void TakeDamage(int ammount)
    {
        
    }
    public void Heal(int ammount)
    {
        
    }
}
public class DamageSource : MonoBehaviour
{
    [SerializeField] DamageType damageType;
    [SerializeField] bool continous;

    void Start()
    {
        switch (damageType)
        {
            case DamageType.Contact:
            GetComponent<ContactEvent>();
            break;
            case DamageType.TriggerBox:
            GetComponent<ZoneEvent>();
            break;
        }  
    }
    void Update()
    {
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
