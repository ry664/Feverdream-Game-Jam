using UnityEngine;
using UnityEngine.Events;
public class TriggerEvent : MonoBehaviour
{
    [SerializeField] protected bool filterByTag;
    [SerializeField] protected string targetTag;
    [SerializeField] protected bool filterByLayer;
    [SerializeField] protected int targetLayer;

    public UnityEvent OnEnterTrigger;
    public UnityEvent OnExitTrigger;
}