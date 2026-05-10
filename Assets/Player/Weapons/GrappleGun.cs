using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Gun))]
public class GrappleGun : MonoBehaviour
{
    [SerializeField] Transform raycastOrign;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void CastAndCheckHits()
    {
        if (Physics.Raycast(raycastOrign.position, raycastOrign.forward, out RaycastHit hit, 1000, LayerMask.NameToLayer("GrappleInteractable")))
        {
            string tag = hit.collider.tag;

            if(tag == "Grappleable")    { HitGrappleable(hit.point); }
            else if (tag == "Grabable") { HitGrabable(hit.collider.transform); }
        }
    }

    void HitGrappleable(Vector3 hitPoint)
    {
        PlayGrappleAnim(false);

        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            return;
        }

        Vector3 direction = hitPoint - rb.position;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        float grappleSpeed = Mathf.Clamp(direction.magnitude * 3f, 20f, 60f);
        Vector3 grappleVelocity = direction.normalized * grappleSpeed;

        rb.linearVelocity += grappleVelocity / 10f;
    }

    void HitGrabable(Transform hitEntity)
    {
        PlayGrappleAnim(true);
        StartCoroutine(PullTowards(hitEntity)); 
    }

    void PlayGrappleAnim(bool grabOrGrapple)
    {
        string animClip = grabOrGrapple ? "Grab" : "Grapple";
        // animator.play(animclip)
    }

    IEnumerator PullTowards(Transform hitEntity)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3.Lerp(hitEntity.position, raycastOrign.position, i/6);  // weird probably
            yield return new WaitForFixedUpdate(); 
        }  
    }
}
