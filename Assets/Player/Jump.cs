using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class Jump : MonoBehaviour
{
    CharacterController controller;
    PlayerController playerController;
    public float jumpHeight;
    public bool isJumping = false;
    public float jumpApexTime = 0.3f;
    public AnimationCurve jumpCurve;
    public float jumpValue { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        InputSystem.actions["Jump"].performed += StartJump;
    }

    void StartJump(InputAction.CallbackContext ctx)
    {
        if (!isJumping && controller.isGrounded)
        {
            StartCoroutine(JumpLoop());
        }
    }

    IEnumerator JumpLoop()
    {
        isJumping = true;
        float jumpTime = 0f;

        while (isJumping)
        {
            float normalizedTime = Mathf.Clamp01(jumpTime / jumpApexTime);
            jumpValue = jumpCurve.Evaluate(normalizedTime) * jumpHeight;
            jumpTime += Time.deltaTime;

            if (controller.isGrounded && jumpTime > 0.1f)
            {
                isJumping = false;
                jumpValue = 0f;
            }

            yield return new WaitForEndOfFrame();
        }
    }

}
