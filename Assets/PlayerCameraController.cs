using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;

    [Header("Look Settings")]
    public float mouseSensitivity = 1.5f;
    public float verticalLimit = 89f;
    public bool lockCursor = true;

    [Header("Strafe Tilt")]
    public float strafeTiltAngle = 8f;
    public float strafeTiltSpeed = 6f;

    private float pitch;
    private float yaw;
    private float currentTilt;
    private float tiltVelocity;

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        pitch = transform.localEulerAngles.x;
        yaw = playerBody != null ? playerBody.eulerAngles.y : transform.eulerAngles.y;
    }

    private void Update()
    {
        Vector2 lookInput = ReadLookInput();
        Vector2 moveInput = ReadMoveInput();

        if (lookInput.sqrMagnitude > 0.0001f)
        {
            yaw += lookInput.x * mouseSensitivity;
            pitch -= lookInput.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -verticalLimit, verticalLimit);
        }

        float targetTilt = -moveInput.x * strafeTiltAngle;
        currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, 1f / strafeTiltSpeed);

        if (playerBody != null)
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);

        transform.localRotation = Quaternion.Euler(pitch, 0f, currentTilt);
    }

    private Vector2 ReadLookInput()
    {
        InputAction lookAction = GetLookAction();
        return lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private Vector2 ReadMoveInput()
    {
        InputAction moveAction = GetMoveAction();
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private InputAction GetLookAction()
    {
        if (InputSystem.actions == null)
            return null;

        try
        {
            return InputSystem.actions["Look"];
        }
        catch
        {
            return null;
        }
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

    private void OnDisable()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
