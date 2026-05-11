using DialogueEditor;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(ZoneEvent))]
public class NPC : MonoBehaviour
{
    ZoneEvent trigger;
    void Start()
    {
        trigger = GetComponent<ZoneEvent>();

        InputSystem.actions["Interact"].performed += ctx =>
        {
            if(trigger.InTrigger) 
            {
                ConversationManager.Instance.StartConversation(GetComponent<NPCConversation>());
            }
        };

        trigger.OnExitTrigger.AddListener(() => {ConversationManager.Instance.EndConversation();});
    }
}
