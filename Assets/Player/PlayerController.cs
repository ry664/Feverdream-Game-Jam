using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float moveAcceleration = 25f;
    public float moveDeceleration = 20f;
    public float gravityStrength = -9.81f;
    public LayerMask gravityLayerMask = ~0;

    public Vector3 upDirection = Vector3.up; // direction to orient "down" to  

    public CharacterController controller;
    public Transform foot;

    float AcelerationTimer;
    float decelerationTimer;


    bool hasJump;
    Jump jump;
    bool hasCrouch;
    bool hasSprint;
    Sprint sprint;


    void Start()
    {
        hasJump   = TryGetComponent(out jump);
        hasCrouch = TryGetComponent<Crouch>(out _);
        hasSprint = TryGetComponent(out sprint);
    }

    void Update()
    {
        MovePlayer();
        RotatePlayerWithGravity();
        DontFallIfNoGround();
    }
    void RotatePlayerWithGravity()
    {
        float rotationSpeed = 0.1f;
        if (upDirection == Vector3.zero)
            return;

        Vector3 targetUp = upDirection.normalized;
        float angle = Vector3.Angle(transform.up, targetUp);
        if (angle > 0.01f)
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, targetUp);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(transform.right, targetUp);
            }

            if (forward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, targetUp);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed);
            }
            else
            {
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed);
            }
        }
    }
    void DontFallIfNoGround()
    {
        if(!Physics.Raycast(foot.position, upDirection, 100))
        {
            upDirection = Vector3.zero;
        }
    }

    void MovePlayer(){
        float targetSpeed = hasSprint ? sprint.targetSpeed : moveSpeed;

        float yInput;
        if(hasJump){
            if(jump.isJumping){
                yInput = jump.jumpValue;
            }
            else {yInput = gravityStrength;}
        }
        else {
            yInput = gravityStrength;
        }

        Vector2 input = InputSystem.actions["Move"].ReadValue<Vector2>().normalized;

        Vector3 moveInput = new(input.x * targetSpeed, yInput, input.y * targetSpeed);
        controller.Move(transform.TransformDirection(moveInput) * Time.deltaTime);  // should be local space movement and falling
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GravitySource"))
        {
            upDirection = other.transform.up;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("GravitySource"))
        {
            upDirection = Vector3.zero;
        }
    }
}
