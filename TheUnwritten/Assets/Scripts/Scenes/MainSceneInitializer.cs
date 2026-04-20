using UnityEngine;

using Cysharp.Threading.Tasks;

using Common;
using Services;
using TMPro.Examples;
using UI;
using UI.Main;
using UI.Title;
using UI.View;

namespace Scenes
{
    public class MainSceneInitializer : SceneInitializer
    {
        protected override async UniTask OnInitializeAsync()
        {
            await base.OnInitializeAsync();
            Debug.Log("MainScene.InitializeAsync()");

            // _uiManager.FadeOutDimmedAsync().Forget();
            //
            var factory = new TitlePresenterFactory(_uiManager, _serviceLocator.Get<GameInfoService>());
            var titlePresenter = factory.Create();
            // titleView?.AddListener(this);

            // OpenMainView();
            // gameManager.StartTurnAsync().Forget();
            
            await UniTask.CompletedTask;
        }
    }
}

