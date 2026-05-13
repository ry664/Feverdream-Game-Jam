using UnityEngine;
using UnityEngine.Events;
public class TriggerEvent : MonoBehaviour
{
    public Transform EffectedObject {get; protected set;}
    [SerializeField] protected bool filterByTag;
    [SerializeField] protected string targetTag;
    [SerializeField] protected bool filterByLayer;
    [SerializeField] protected int targetLayer;

    public bool InTrigger {get; protected set;}

    public UnityEvent OnEnterTrigger;
    public UnityEvent OnExitTrigger;
}