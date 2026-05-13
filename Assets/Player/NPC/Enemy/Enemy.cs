using UnityEngine;
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    Health health;

    void Start()
    {
        health = GetComponent<Health>();
        health.OnDeath.AddListener(Die);
    }
    void Die()
    {
        Destroy(this);
    }
}