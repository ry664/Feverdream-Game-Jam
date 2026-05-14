using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManger : MonoBehaviour
{
    [SerializeField] WeaponSwapUi ui;
    public IWeapon[] AvalibleWeapons;
    public int selectedIndex = 0;
    public IWeapon equipedWeapon;

    public bool activeSwap;

    public void Start()
    {
        InputSystem.actions["SwapWeapon"].performed += StartWeaponSwap;
        AvalibleWeapons = GetComponentsInChildren<IWeapon>();
        
        if(selectedIndex == -1)
        {
            
        }
        else {equipedWeapon = AvalibleWeapons[selectedIndex];}
        print(equipedWeapon);
    }
    void StartWeaponSwap(InputAction.CallbackContext ctx)
    {
        if(activeSwap) return;
        StartCoroutine(SwapWeapon());
    }

    public IEnumerator SwapWeapon()   // half life alyx
    {
        activeSwap = true;
        ui.OpenUI();
        Time.timeScale = 0.5f;
        yield return new WaitUntil(() => ui.selected); // hover over gui 
        Time.timeScale = 1;

        int closeIndex = ui.CloseUI();
        if(closeIndex != selectedIndex)
        {
            equipedWeapon.OnUnequip();
            selectedIndex = closeIndex;
            equipedWeapon = AvalibleWeapons[selectedIndex];
            equipedWeapon.Onequip();
        }
        activeSwap = false;
    }
}