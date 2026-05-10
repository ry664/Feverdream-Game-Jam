using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class Sprint : MonoBehaviour
{
    PlayerController controller;
    public float sprintSpeed;
    public bool useSuperSprint;
    public float superSprintTime;
    public float superSprintSpeed;

    public float targetSpeed {get; private set;}

    float sprintTime;

    void Start()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (InputSystem.actions["Sprint"].IsPressed())
        {
            sprintTime += Time.deltaTime;
            if(useSuperSprint && sprintTime > superSprintTime)
            {
                targetSpeed = superSprintSpeed;
            }
            else
            {
                targetSpeed = sprintSpeed;
            }
            
        }
        else
        {
            targetSpeed = controller.moveSpeed;
            sprintTime = 0;
        }
    }
}