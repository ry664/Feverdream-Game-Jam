using UnityEngine;
using UnityEngine.Events;
public class Health : MonoBehaviour
{
    public int maxHealth;
    public int currentHealth;
    public float selfHealSpeed;
    public UnityEvent OnDeath = new();
    public void TakeDamage(int ammount)
    {
        currentHealth -=  Mathf.Clamp(ammount, 0, maxHealth);
        if(currentHealth == 0)
        {
            OnDeath?.Invoke();
        }
    }
    public void Heal(int ammount)
    {
        currentHealth += Mathf.Clamp(ammount, 0, maxHealth);
    }
    void Start()
    {
        
    }
}