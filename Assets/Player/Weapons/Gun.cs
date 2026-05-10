using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public UnityEvent fire = new();
    public UnityEvent reload = new();

    public int MaxAmmo = 100;
    public int currentAmmo;
    public bool isAutomatic;
    public int magizineSize;
    int currentLoadedAmmo;
    
    float fireCooldown;
    float currentCooldown;
    bool reloading;

    void Start()
    {
        currentCooldown = fireCooldown;
    }

    public virtual void Fire(){

        currentCooldown -= Time.deltaTime;
        if(currentCooldown > fireCooldown || reloading) return;

        if (isAutomatic)
        {
            if (InputSystem.actions["Fire"].IsPressed())
            {
                fire?.Invoke();
            }
        }
        else
        {
            if (InputSystem.actions["Fire"].WasPressedThisFrame())
            {
                fire?.Invoke();
            }
        }
        
    }

    public virtual void Reload()
    {
        if(reloading || currentAmmo < magizineSize) return; // cant load an unfull magizine    only give ammo in multiples of 6 to fit current gun types 1, 6, 6
        
        if (InputSystem.actions["Reload"].WasPressedThisFrame())
        {
            reloading = true;
            reload?.Invoke();
            RemoveAmmo(magizineSize);
            currentLoadedAmmo = magizineSize;
        }
    }
    public void EndReload()
    {
        reloading = false;
    }

    public void AddAmmo(int ammount)
    {
        currentAmmo += Mathf.Clamp(currentAmmo + ammount, 0, MaxAmmo);
    }
    public void RemoveAmmo(int ammount)
    {
        currentAmmo -= Mathf.Clamp(currentAmmo - ammount, 0, MaxAmmo);
    }
}