using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public int MaxAmmo = 100;
    public int currentAmmo;
    public bool isAutomatic;
    public int magizineSize;
    float fireCooldown;
    float currentCooldown;

    void Start()
    {
        currentCooldown = fireCooldown;
    }

    public virtual void Fire(){
        if (isAutomatic)
        {
            if(InputSystem.actions["Fire"].per)
        }
        else
        {
            
        }
    }

    public virtual void Reload(){}


    public void AddAmmo(int ammount)
    {
        currentAmmo += Mathf.Clamp(currentAmmo + ammount, 0, MaxAmmo);
    }
    public void RemoveAmmo(int ammount)
    {
        currentAmmo -= Mathf.Clamp(currentAmmo - ammount, 0, MaxAmmo);
    }

}

[RequireComponent(typeof(Gun))]
public class GlobalAmmo : MonoBehaviour // global ammo counter insted of individual ammo types
{
    public Gun connectedGun;
    public static int currentAmmo = 0;
    public void AddAmmo(int ammount)
    {
        currentAmmo += Mathf.Clamp(currentAmmo + ammount, 0, connectedGun.MaxAmmo);
    }
    public void RemoveAmmo(int ammount)
    {
        currentAmmo -= Mathf.Clamp(currentAmmo - ammount, 0, connectedGun.MaxAmmo);
    }

    void Start()
    {
        connectedGun = GetComponent<Gun>();
        connectedGun.currentAmmo = currentAmmo;
    }

}
public class MeleeWeapon : MonoBehaviour
{
    
}
public interface IWeapon
{
    public void Onequip(); // set up input events
    public void OnUnequip();
}