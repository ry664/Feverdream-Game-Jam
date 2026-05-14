using UnityEngine;

[RequireComponent(typeof(Gun))]
public class GlobalAmmo : MonoBehaviour // global ammo counter insted of individual ammo types
{
    public Gun connectedGun;
    public static int currentAmmo = 0;
    public void AddAmmo(int ammount)
    {
        currentAmmo = Mathf.Clamp(currentAmmo + ammount, 0, connectedGun.MaxAmmo);
        connectedGun.currentAmmo = currentAmmo;
        Debug.Log(currentAmmo + "add");
    }
    public void RemoveAmmo(int ammount)
    {
        currentAmmo = Mathf.Clamp(currentAmmo - ammount, 0, connectedGun.MaxAmmo);
        connectedGun.currentAmmo = currentAmmo;
    }
    

    void Start()
    {
        connectedGun = GetComponent<Gun>();
        currentAmmo = connectedGun.MaxAmmo;
        connectedGun.currentAmmo = currentAmmo;
    }

}