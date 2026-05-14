using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour, IWeapon
{
    public UnityEvent fire = new();
    public UnityEvent reload = new();

    public int MaxAmmo = 100;
    public int currentAmmo;
    [SerializeField] bool isAutomatic;
    [SerializeField] int magizineSize;
    [SerializeField] float reloadTime;
    int currentLoadedAmmo;
    
    [SerializeField] float fireCooldown;
    float currentCooldown;
    bool reloading;
    bool UsingGlobalAmmo => globalAmmo != null;
    GlobalAmmo globalAmmo;

    public bool UsingAnimator => gunAnimator != null;
    Animator gunAnimator;
    public string reloadAnimation;
    public string fireAnimation;

    void Start()
    {
        TryGetComponent(out gunAnimator);
        TryGetComponent(out globalAmmo);
        currentCooldown = fireCooldown;
    }
    void Update()
    {
        Fire();
        Reload();
    }

    public virtual void Fire(){

        currentCooldown -= Time.deltaTime;
        if(currentCooldown > 0 || reloading || currentLoadedAmmo < 1) return;

        if (isAutomatic)
        {
            if (InputSystem.actions["Fire"].IsPressed())
            {
                fire?.Invoke();
                if (UsingAnimator)
                {
                    gunAnimator.Play("Fire");
                }
                currentCooldown = fireCooldown;
                currentLoadedAmmo--;
            }
        }
        else
        {
            if (InputSystem.actions["Fire"].WasPressedThisFrame())
            {
                fire?.Invoke();
                if (UsingAnimator)
                {
                    gunAnimator.Play("Fire");
                }
                currentCooldown = fireCooldown;
                currentLoadedAmmo--;
            }
        }
        
    }

    public virtual void Reload()
    {
        if(reloading || currentAmmo < magizineSize) return; // cant load an unfull magizine    only give ammo in multiples of 6 to fit current gun types 1, 6, 6
        
        if (InputSystem.actions["Reload"].WasPressedThisFrame())
        {
            reloading = true;
            if (UsingAnimator)
            {
                gunAnimator.Play("Reload");
            }
            else
            {
                Invoke(nameof(EndReload), reloadTime);
            }
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
        if (UsingGlobalAmmo)
        {
            globalAmmo.AddAmmo(ammount);
        }
        else 
        {
            currentAmmo += Mathf.Clamp(currentAmmo + ammount, 0, MaxAmmo);
        }
    }
    public void RemoveAmmo(int ammount)
    {
        if (UsingGlobalAmmo)
        {
            globalAmmo.RemoveAmmo(ammount);
        }
        else 
        {
            currentAmmo -= Mathf.Clamp(currentAmmo - ammount, 0, MaxAmmo);
        }
    }

    public void Onequip()
    {
        if (UsingAnimator)
        {
            gunAnimator.Play("Equip");
        }
        gameObject.SetActive(true);
    }

    public void OnUnequip()
    {
        if (UsingAnimator)
        {
           gunAnimator.Play("Unequip"); 
        }
        gameObject.SetActive(false);
    }
}