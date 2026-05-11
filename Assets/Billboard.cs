using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Billboard : MonoBehaviour {
    [SerializeField] private BillboardType billboardType;
    [SerializeField] private bool useCubeMap = false;
    [SerializeField] private Transform rotatingChild;

    [Header("Lock Rotation")]
    [SerializeField] private bool lockX;
    [SerializeField] private bool lockY;
    [SerializeField] private bool lockZ;

    [SerializeField] private string directionParameter = "Direction";

    private Vector3 originalRotation;
    private Animator anim;

    public enum BillboardType { LookAtCamera, CameraForward };

    private void Awake() {
        originalRotation = transform.rotation.eulerAngles;
        anim = GetComponent<Animator>();
        if (rotatingChild == null && transform.childCount > 0) {
            rotatingChild = transform.GetChild(0);
        }
    }

    // Use Late update so everything should have finished moving.
    void LateUpdate() {
        Transform targetTransform = useCubeMap && rotatingChild != null ? rotatingChild : transform;

        // There are two ways people billboard things.
        switch (billboardType) {
            case BillboardType.LookAtCamera:
                targetTransform.LookAt(Camera.main.transform.position, Vector3.up);
                break;
            case BillboardType.CameraForward:
                targetTransform.forward = -Camera.main.transform.forward;
                break;
            default:
                break;
        }

        // Modify the rotation in Euler space to lock certain dimensions.
        Vector3 rotation = targetTransform.rotation.eulerAngles;
        if (lockX) { rotation.x = originalRotation.x; }
        if (lockY) { rotation.y = originalRotation.y; }
        if (lockZ) { rotation.z = originalRotation.z; }
        targetTransform.rotation = Quaternion.Euler(rotation);

        // Update cube map animator if enabled
        if (useCubeMap && anim != null) {
            UpdateCubeMapAnimator();
        }
    }

    private void UpdateCubeMapAnimator() {
        if (string.IsNullOrEmpty(directionParameter))
            return;

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 localDirection = transform.InverseTransformDirection(cameraForward).normalized;

        float forward = Vector3.Dot(localDirection, Vector3.forward);
        float right = Vector3.Dot(localDirection, Vector3.right);
        float up = Vector3.Dot(localDirection, Vector3.up);
        float back = Vector3.Dot(localDirection, Vector3.back);
        float left = Vector3.Dot(localDirection, Vector3.left);
        float down = Vector3.Dot(localDirection, Vector3.down);

        int directionIndex = 0;
        float maxDot = forward;

        if (right > maxDot) { maxDot = right; directionIndex = 1; }
        if (back > maxDot) { maxDot = back; directionIndex = 2; }
        if (left > maxDot) { maxDot = left; directionIndex = 3; }
        if (up > maxDot) { maxDot = up; directionIndex = 4; }
        if (down > maxDot) { maxDot = down; directionIndex = 5; }

        if (HasAnimatorParameter(directionParameter)) {
            anim.SetInteger(directionParameter, directionIndex);
        }
    }

    private bool HasAnimatorParameter(string parameterName) {
        if (anim == null)
            return false;

        foreach (AnimatorControllerParameter parameter in anim.parameters) {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
