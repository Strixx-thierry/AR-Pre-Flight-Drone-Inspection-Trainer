using System;
using System.Collections;
using UnityEngine;
#if DT_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace DroneTrainer.Data
{
    /// <summary>
    /// Asset management for the heavy 3D drone models (Addressables, group "Drones").
    /// Only the selected drone is loaded into memory for a training session, and it is
    /// released again when the session scene is left — important on mobile memory budgets.
    /// Falls back to a direct prefab reference if Addressables is not installed.
    /// </summary>
    public sealed class DroneAssetLoader : IDisposable
    {
#if DT_ADDRESSABLES
        AsyncOperationHandle<GameObject> handle;
#endif
        GameObject instance;

        public GameObject Instance => instance;

        public IEnumerator Load(DroneDefinition def, Transform parent = null)
        {
#if DT_ADDRESSABLES
            if (!string.IsNullOrEmpty(def.address))
            {
                handle = Addressables.LoadAssetAsync<GameObject>(def.address);
                yield return handle;
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    instance = UnityEngine.Object.Instantiate(handle.Result, parent);
                    instance.name = def.id;
                    Debug.Log($"[Assets] Loaded '{def.address}' via Addressables");
                    yield break;
                }
                Debug.LogError($"[Assets] Addressables load failed for '{def.address}': {handle.OperationException?.Message}");
            }
#endif
            if (def.prefab)
            {
                instance = UnityEngine.Object.Instantiate(def.prefab, parent);
                instance.name = def.id;
            }
            yield break;
        }

        /// <summary>Destroys the instance and releases the loaded asset so its memory can be freed.</summary>
        public void Dispose()
        {
            if (instance) UnityEngine.Object.Destroy(instance);
            instance = null;
#if DT_ADDRESSABLES
            if (handle.IsValid())
            {
                Addressables.Release(handle);
                Debug.Log("[Assets] Released drone asset");
            }
            handle = default;
#endif
        }
    }
}
