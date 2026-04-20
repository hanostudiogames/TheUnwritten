using System;
using UnityEngine;

using Cysharp.Threading.Tasks;

using Services;
using Common;

namespace Scenes
{
    public abstract class SceneInitializer : Element
    {
        protected UIManager _uiManager = null;
        protected IServiceLocator _serviceLocator = null;
        // private AddressableManager _addressableManager = null;
        
        public void Initialize(AddressableManager addressableManager, UIManager uiManager, IServiceLocator serviceLocator)
        {
            // _addressableManager = addressableManager;
            _uiManager = uiManager;
            _serviceLocator = serviceLocator;
            
            OnInitializeAsync().Forget();
        }

        protected virtual async UniTask OnInitializeAsync()
        {
            var mainCamera = Camera.main;
            
            _uiManager?.StackUICamera(mainCamera);
        }
    }
}

