using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WeaponSwapUi : MonoBehaviour
{
    [SerializeField] Image[] WeaponCircle;
    public bool selected;
    private int selectedIndex = 0;
    
    void Start()
    {
        // Find all weapon circle images (assuming they are children of this GameObject)
        WeaponCircle = GetComponentsInChildren<Image>();
        
        // Initially hide the UI
        gameObject.SetActive(false);
    }
    
    public void OpenUI()
    {
        gameObject.SetActive(true);
        selected = false;
        selectedIndex = 0;
        
        // Highlight the first weapon by default
        if (WeaponCircle.Length > 0)
        {
            HighlightWeapon(selectedIndex);
        }
    }
    
    public int CloseUI()
    {
        gameObject.SetActive(false);
        return selectedIndex; // index of selected gun
    }
    
    void Update()
    {
        if (!gameObject.activeSelf) return;
        
        // Handle weapon selection with number keys
        for (int i = 0; i < WeaponCircle.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedIndex = i;
                HighlightWeapon(selectedIndex);
            }
        }
        
        // Handle mouse wheel scrolling
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0)
                selectedIndex = (selectedIndex + 1) % WeaponCircle.Length;
            else
                selectedIndex = (selectedIndex - 1 + WeaponCircle.Length) % WeaponCircle.Length;
            
            HighlightWeapon(selectedIndex);
        }
        
        // Select weapon with mouse click or Enter key
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
        {
            selected = true;
        }
        
        // Cancel with right click or Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            selectedIndex = -1; // Return -1 for cancellation
            selected = true;
        }
    }
    
    void OnMouseUpAsButton()
    {
        // This is called when the UI element itself is clicked
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check which weapon circle was clicked
            for (int i = 0; i < WeaponCircle.Length; i++)
            {
                if (hit.collider != null && hit.collider.gameObject == WeaponCircle[i].gameObject)
                {
                    selectedIndex = i;
                    HighlightWeapon(selectedIndex);
                    selected = true;
                    break;
                }
            }
        }

        selected = true;
    }
    
    void HighlightWeapon(int index)
    {
        // Reset all weapons
        for (int i = 0; i < WeaponCircle.Length; i++)
        {
            Color color = WeaponCircle[i].color;
            color.a = 0.5f; // Dim non-selected weapons
            WeaponCircle[i].color = color;
        }
        
        // Highlight the selected weapon
        if (index >= 0 && index < WeaponCircle.Length)
        {
            Color color = WeaponCircle[index].color;
            color.a = 1f; // Full opacity for selected weapon
            WeaponCircle[index].color = color;
        }
    }
}