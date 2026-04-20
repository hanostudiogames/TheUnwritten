using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

using Common;
using DG.Tweening;
using UI.Slots;

namespace Common
{
    public interface IUIManager
    {
        TElement Create<TElement>(RectTransform rootRectTr, out bool initialize) where TElement : Element;
        
        void RegisterDimensionHandler<TType>(Action<bool> handler);

        UniTask FadeInOutAsync(Func<UniTask> taskFunc, Action onComplete);
    }
    
    public class UIManager : MonoBehaviour, IUIManager
    {
        [SerializeField] private Camera uiCamera = null;
        [SerializeField] private Canvas canvas = null;
        [SerializeField] private RectTransform viewRootRectTr = null;
        [SerializeField] private RectTransformDimensionHandler dimensionHandler = null;
        [SerializeField] private Image dimmedImage = null;

        private readonly Dictionary<System.Type, GameObject> _cachedPrefabs = new ();
        private ObjectPooler _objectPooler = null;
        
        public async UniTask InitializeAsync(AddressableManager addressableManager, ObjectPooler objectPooler)
        {
            if (addressableManager == null)
                return;

            _objectPooler = objectPooler;
            
            await addressableManager.LoadAssetAsync<GameObject>("UI", 
                handle => 
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        GameObject prefab = handle.Result;
                        if (prefab == null) 
                            return;

                        if (prefab.TryGetComponent<Common.Element>(out var element))
                        {
                            var type = element.GetType();
                            _cachedPrefabs[type] = prefab;
                        }
                    }
                });
        }

        public void StackUICamera(Camera mainCamera)
        {
            if (mainCamera == null)
                return;
            
            var cameraData = mainCamera.GetUniversalAdditionalCameraData();
            cameraData?.cameraStack.Add(uiCamera);
        }

        public TElement Create<TElement>(RectTransform rootRectTr, out bool initialize) where TElement : Element
        {
            initialize = false;
            
            var element = _objectPooler?.Get<TElement>(typeof(TElement).Name, viewRootRectTr);
            if (element == null)
            {
                if (_cachedPrefabs.TryGetValue(typeof(TElement), out var prefab))
                {
                    var gameObj = Instantiate(prefab);
                    if (gameObj)
                    {
                        initialize = true;
                        
                        element = gameObj.GetComponent<TElement>();
                        // element?.Initialize();
                        
                        _objectPooler?.Add(element);
                    }
                }
            }

            if (element != null)
            {
                if(rootRectTr)
                    element.transform.SetParent(rootRectTr, false);
                else
                    element.transform.SetParent(viewRootRectTr, false);
            }
            
            return element;
        }

        public void RegisterDimensionHandler<TType>(Action<bool> handler) => dimensionHandler?.RegisterDimensionHandler<TType>(handler);
        public void UnregisterDimensionHandler<TType>() => dimensionHandler?.UnregisterDimensionHandler<TType>();

        public async UniTask FadeInOutAsync(Func<UniTask> taskFunc, Action onComplete)
        {
            if (dimmedImage == null)
                return;

            float duration = 3f;
            
            await dimmedImage.DOFade(0, 0);
            // await UniTask.Yield();
            
            dimmedImage.gameObject.SetActive(true);
            
            await dimmedImage.DOFade(1f, duration);
            
            if(taskFunc != null)
                await taskFunc.Invoke();
            
            await dimmedImage.DOFade(0, duration);
            dimmedImage.gameObject.SetActive(false);
            
            onComplete?.Invoke();
        }
    }
}

