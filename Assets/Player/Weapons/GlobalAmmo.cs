using UnityEngine;

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