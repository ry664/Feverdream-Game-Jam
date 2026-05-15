using System;
using System.Collections;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Gun))]
public class GrappleGun : MonoBehaviour
{
    Gun connectedGun;
    CharacterController controller;
    Rigidbody rb;
    [SerializeField] Transform raycastSource;

    [SerializeField] float grabTime;
    float grabtimer;
    [SerializeField] float Targetdistance;
    [SerializeField] Velocity grabSpeed;

    bool isGrappling = false;
    [SerializeField] float grappleGoalOffset; 
    [SerializeField] Velocity grappleSpeed;
    void Start()
    {
        connectedGun = GetComponent<Gun>();
        Transform parent = transform.parent;

        rb = parent.GetComponentInParent<Rigidbody>();
        controller = parent.GetComponentInParent<CharacterController>();
        connectedGun.fire.AddListener(CheckHits);
    }

    IEnumerator Grapple(RaycastHit hit)
    {
        Debug.Log("a");
        isGrappling = true;
        controller.enabled = false;
        rb.isKinematic = false;
        while(Vector3.Distance(hit.point, raycastSource.position) > grappleGoalOffset || grabtimer < grabTime)
        {
            grabtimer += Time.fixedDeltaTime;
            rb.maxLinearVelocity = grappleSpeed.max;
            Vector3 direction = (hit.point - raycastSource.transform.position).normalized;
            rb.AddForce(direction, ForceMode.Impulse);
            yield return new WaitForFixedUpdate();
        }
        grabtimer = 0;
        rb.isKinematic = true;
        rb.maxLinearVelocity = Mathf.Infinity;
        controller.enabled = true;
        
        isGrappling = false;

        
    }
    IEnumerator Grab(RaycastHit hit){
        yield return null;
    }

    void CheckHits(){
        Debug.Log("aa");
        if(isGrappling) return;
        if(Physics.Raycast(raycastSource.position, raycastSource.forward,out RaycastHit hit, 1000)){
            Debug.Log(hit.collider);
            if(hit.collider.CompareTag("Grabable")){
                
                StartCoroutine(Grab(hit));
            }
            else if(hit.collider.CompareTag("Grappleable")){
                StartCoroutine(Grapple(hit));
            }
        }
    }


}
[Serializable]
public struct Velocity{
    public float acceleration;
    public float min;
    public float max;
}
