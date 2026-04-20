using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

using Common;

namespace Tables
{
    public class TableInitializer
    {
        public async UniTask InitializeAsync(AddressableManager addressableManager)
        {
            await addressableManager.LoadAssetAsync<ScriptableObject>("Table", 
                handle => 
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        var scriptableObject = handle.Result;
                        if (scriptableObject == null) 
                            return;

                        var typeName = $"Tables.Containers.{scriptableObject.name}Container";
                        var type = System.Type.GetType(typeName);
                        if (type != null)
                        {
                            Debug.Log(typeName);
                            
                            var instance = System.Activator.CreateInstance(type);
                            if (instance is IInitializableContainer tableContainer)
                                tableContainer.Initialize(scriptableObject);
                        }
                    }
                });
        }
    }
}

