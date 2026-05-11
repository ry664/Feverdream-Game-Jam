using UnityEngine;
using UnityEngine.AddressableAssets;

public class AreaManager : MonoBehaviour
{
    public AssetReference[] previewReferences;
    public AssetReference[] loadReferences;
    
    async void OnTriggerEnter(Collider other)
    {
        foreach (var reference in previewReferences)
        {
            var handle = reference.InstantiateAsync();
            await handle.Task;
        }
        
        foreach (var reference in loadReferences)
        {
            var handle = reference.InstantiateAsync();
            await handle.Task;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        foreach (var reference in previewReferences)
        {
            if (reference.IsValid())
                reference.ReleaseAsset();
        }
        
        foreach (var reference in loadReferences)
        {
            if (reference.IsValid())
                reference.ReleaseAsset();
        }
    }
}