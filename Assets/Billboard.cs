using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Billboard : MonoBehaviour
{
    [SerializeField] Transform target; // player or camera
    [Tooltip("Include front, back, left, right, up, down sprite variant based on rotation")]
    [SerializeField] bool cubeMapBillboard = false;
    [SerializeField] bool lockY;
    [Tooltip("A child object that will rotate to face the target while the base stays as the pivot.")]
    [SerializeField] Transform rotatingChild;
    [Tooltip("Optional animator int parameter used by the cube-map billboard to select the facing sprite.")]
    [SerializeField] string directionParameter = "Direction";

    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (target == null)
        {
            if (Camera.main != null)
            {
                Debug.Log($"no camera assigned, using Main Camera {Camera.main.name}");
                target = Camera.main.transform;
            }
        }

        if (rotatingChild == null && transform.childCount > 0)
        {
            rotatingChild = transform.GetChild(0);
        }
    }

    void Update()
    {

        if (!cubeMapBillboard)
        {
            Vector3 lookDirection = target.position - transform.position;
            if (lockY)
            {
                lookDirection = Vector3.ProjectOnPlane(lookDirection, transform.up);
            }

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, transform.up);
            }
        }
        else
        {
            Vector3 lookDirection = target.position - rotatingChild.position;
            if (lockY)
            {
                lookDirection = Vector3.ProjectOnPlane(lookDirection, transform.up);
            }

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                rotatingChild.rotation = Quaternion.LookRotation(lookDirection.normalized, transform.up);
            }

            UpdateCubeMapAnimator(lookDirection.normalized);
        }
    }

    void UpdateCubeMapAnimator(Vector3 worldDirection)
    {
        if (anim == null || string.IsNullOrEmpty(directionParameter))
            return;

        Vector3 localDirection = transform.InverseTransformDirection(worldDirection).normalized;

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

        if (HasAnimatorParameter(directionParameter))
        {
            anim.SetInteger(directionParameter, directionIndex);
        }
    }

    bool HasAnimatorParameter(string parameterName)
    {
        if (anim == null)
            return false;

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
