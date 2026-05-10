using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GravitySource : MonoBehaviour
{
    public new Collider collider;

    public void Start()
    {
        collider = GetComponent<Collider>();
    }
}