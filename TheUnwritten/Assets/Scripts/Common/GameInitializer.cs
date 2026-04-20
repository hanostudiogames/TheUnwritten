using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Data;
using Repositories;
using Scenes;
using Services;
using Tables;

namespace Common
{
    public static class GameInitializer
    {
        private static AddressableManager _addressableManager = null;
        private static UIManager _uiManager = null;
        
        private static UniTaskCompletionSource _initializeCompleteSource = new UniTaskCompletionSource();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            Debug.Log("OnBeforeSceneLoad");
            _addressableManager = new AddressableManager();
            InitializeCommonAsync(_addressableManager).Forget();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            Debug.Log("OnAfterSceneLoad");
            InitializeAsync().Forget();
        }
        
        private static async UniTask InitializeCommonAsync(AddressableManager addressableManager)
        {
            IList<GameObject> prefabs = await addressableManager.LoadAssetsAsync<GameObject>("Common");

            if (prefabs == null || prefabs.Count == 0) 
                return;

            ObjectPooler objectPooler = null;
            foreach (var prefab in prefabs)
            {
                if(objectPooler == null)
                    objectPooler = TryInstantiate<ObjectPooler>(prefab);
                
                if(_uiManager == null)
                    _uiManager = TryInstantiate<UIManager>(prefab);
            }
            
            if(objectPooler != null)
                GameObject.DontDestroyOnLoad(objectPooler);

            if (_uiManager != null)
            {
                GameObject.DontDestroyOnLoad(_uiManager);
                await _uiManager.InitializeAsync(addressableManager, objectPooler);
            }
            
            _initializeCompleteSource?.TrySetResult();
        }

        private static TMonoBehaviour TryInstantiate<TMonoBehaviour>(GameObject prefab) 
            where TMonoBehaviour : MonoBehaviour
        {
            if (prefab.TryGetComponent<TMonoBehaviour>(out _))
            {
                var gameObj = GameObject.Instantiate(prefab);
                if (gameObj.TryGetComponent<TMonoBehaviour>(out var monoBehaviour))
                    return monoBehaviour;
            }

            return null;
        }

        private static async UniTask InitializeAsync()
        {
            var tableInitializer = new TableInitializer();
            await tableInitializer.InitializeAsync(_addressableManager);

            var repositoryLocator = new RepositoryLocator();
            await repositoryLocator.InitializeAsync();
            
            var serviceLocator = new ServiceLocator(repositoryLocator);
            
            await _initializeCompleteSource.Task;
            
            var sceneInitializer = GameObject.FindFirstObjectByType<SceneInitializer>();
            sceneInitializer?.Initialize(_addressableManager, _uiManager, serviceLocator);

            _addressableManager = null;
            _uiManager = null;
            _initializeCompleteSource = null;
        }
    }
}
