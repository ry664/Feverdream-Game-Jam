using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManger : MonoBehaviour
{
    [SerializeField] WeaponSwapUi ui;
    public IWeapon[] EquipedWeapons;

    public void Start()
    {
        InputSystem.actions["SwapWeapon"].performed += StartWeaponSwap;
    }
    void StartWeaponSwap(InputAction.CallbackContext ctx)
    {
        StartCoroutine(SwapWeapon());
    }
    void Update()
    {
        
    }

    public IEnumerator SwapWeapon()   // half life alyx
    {
        ui.OpenUI();
        // turn on bullet time slowdown
        Time.timeScale = 0.5f;
        yield return new WaitUntil(() => true);
        // disable old weapon
        // enable new weapon
        Time.timeScale = 1;
        ui.CloseUI();
    }
}