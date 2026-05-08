using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float moveAcceleration = 25f;
    public float moveDeceleration = 20f;
    public float jumpHeight = 1.8f;
    public float gravityStrength = 9.81f;
    public float groundedRayDistance = 0.25f;
    public float groundStickForce = 4f;
    public LayerMask gravityLayerMask = ~0;

    public Vector3 WorldSpaceCurrentDownVector { get; private set; } = Vector3.down;

    private CharacterController characterController;
    private Vector3 currentMoveVelocity;
    private Vector3 velocity;
    private bool isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        InputAction moveAction = GetMoveAction();
        if (moveAction != null)
            moveAction.Enable();

        InputAction jumpAction = GetJumpAction();
        if (jumpAction != null)
            jumpAction.Enable();
    }

    private void OnDisable()
    {
        InputAction moveAction = GetMoveAction();
        if (moveAction != null)
            moveAction.Disable();

        InputAction jumpAction = GetJumpAction();
        if (jumpAction != null)
            jumpAction.Disable();
    }

    private void Update()
    {
        UpdateGravityDirection();
        GroundCheck();

        Vector2 input = ReadMoveInput();
        Vector3 desiredMove = GetMoveDirection(input);
        Vector3 targetMoveVelocity = desiredMove * moveSpeed;

        float accel = input.sqrMagnitude > 0.001f ? moveAcceleration : moveDeceleration;
        currentMoveVelocity = Vector3.MoveTowards(currentMoveVelocity, targetMoveVelocity, accel * Time.deltaTime);

        if (isGrounded)
        {
            currentMoveVelocity = Vector3.ProjectOnPlane(currentMoveVelocity, WorldSpaceCurrentDownVector.normalized);
        }

        if (isGrounded && ReadJumpInput())
        {
            velocity = -WorldSpaceCurrentDownVector.normalized * Mathf.Sqrt(2f * gravityStrength * jumpHeight);
        }
        else if (!isGrounded)
        {
            velocity += WorldSpaceCurrentDownVector.normalized * gravityStrength * Time.deltaTime;
        }
        else
        {
            velocity = Vector3.ProjectOnPlane(velocity, WorldSpaceCurrentDownVector.normalized);
        }

        Vector3 finalMotion = currentMoveVelocity + velocity;
        if (isGrounded && !ReadJumpInput())
        {
            finalMotion += WorldSpaceCurrentDownVector.normalized * groundStickForce;
        }

        characterController.Move(finalMotion * Time.deltaTime);

        AlignUpWithGravity();
    }

    private Vector2 ReadMoveInput()
    {
        InputAction moveAction = GetMoveAction();
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private bool ReadJumpInput()
    {
        InputAction jumpAction = GetJumpAction();
        return jumpAction != null && jumpAction.triggered;
    }

    private InputAction GetMoveAction()
    {
        if (InputSystem.actions == null)
            return null;

        try
        {
            return InputSystem.actions["Move"];
        }
        catch
        {
            return null;
        }
    }

    private InputAction GetJumpAction()
    {
        if (InputSystem.actions == null)
            return null;

        try
        {
            return InputSystem.actions["Jump"];
        }
        catch
        {
            return null;
        }
    }

    private void UpdateGravityDirection()
    {
        Vector3 rayOrigin = transform.position + transform.up * 0.1f;
        Vector3 rayDirection = WorldSpaceCurrentDownVector.normalized;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, groundedRayDistance + 0.5f, gravityLayerMask, QueryTriggerInteraction.Ignore))
        {
            WorldSpaceCurrentDownVector = -hit.normal;
        }
        else
        {
            WorldSpaceCurrentDownVector = Vector3.down;
        }
    }

    private void GroundCheck()
    {
        Vector3 rayOrigin = transform.position + transform.up * 0.1f;
        isGrounded = Physics.Raycast(rayOrigin, WorldSpaceCurrentDownVector.normalized, groundedRayDistance + 0.1f, gravityLayerMask, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetMoveDirection(Vector2 input)
    {
        Vector3 up = -WorldSpaceCurrentDownVector.normalized;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);
        forward.Normalize();
        Vector3 right = Vector3.Cross(up, forward).normalized;
        return right * input.x + forward * input.y;
    }

    private void AlignUpWithGravity()
    {
        Vector3 targetUp = -WorldSpaceCurrentDownVector.normalized;
        if (targetUp.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + transform.up * 0.1f, transform.position + transform.up * 0.1f + WorldSpaceCurrentDownVector.normalized * (groundedRayDistance + 0.5f));
    }
}
