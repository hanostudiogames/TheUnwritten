using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

using Cysharp.Threading.Tasks;

namespace Common
{
    public class AddressableManager
    {
        public async UniTask LoadAssetAsync<T>(string labelKey, System.Action<AsyncOperationHandle<T>> action)
        {
            var locations = await Addressables.LoadResourceLocationsAsync(labelKey);
            var tasks = new List<UniTask>();

            foreach (var location in locations)
            {
                var handle = Addressables.LoadAssetAsync<T>(location);
                
                tasks.Add(
                    handle.ToUniTask().ContinueWith(_ => {
                    action?.Invoke(handle);
                }));
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        // 라벨(Label)이나 주소를 통해 여러 에셋을 한꺼번에 로드하는 함수
        public async UniTask<IList<T>> LoadAssetsAsync<T>(string key) where T : Object
        {
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(key, null);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result; // IList<T> 반환

            Debug.LogError($"[AddressableManager] '{key}' 로드 실패.");
            return null;
        }
    
        public AsyncOperationHandle<T> GetLoadAssetHandle<T>(string key) where T : Object
        {
            return Addressables.LoadAssetAsync<T>(key);
        }
    }
}

