using UnityEngine;

public class YLockedBillboard : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Settings")]
    public Vector3 upAxis = Vector3.up;
    public bool useCameraAsDefaultTarget = true;
    public bool smoothRotation = false;
    public float smoothSpeed = 10f;

    private void Start()
    {
        if (target == null && useCameraAsDefaultTarget)
        {
            if (Camera.main != null)
                target = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, upAxis);

        if (smoothRotation)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        else
            transform.rotation = targetRotation;
    }
}
