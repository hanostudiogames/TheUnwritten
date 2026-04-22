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
// #if UNITY_EDITOR
        public int actIndex = 0;
        public int sceneIndex = 0;        
// #endif
        
        protected override async UniTask OnInitializeAsync()
        {
            await base.OnInitializeAsync();
            Debug.Log("MainScene.InitializeAsync()");

            if (actIndex > 0 && actIndex > 0)
                OpenMainViewAsync().Forget();
            else
            {
                var factory = new TitlePresenterFactory(_uiManager, _serviceLocator.Get<GameInfoService>());
                factory.Create();
            }

            await UniTask.CompletedTask;
        }
        
        private UniTask OpenMainViewAsync()
        {
            var gameManager = new GameManager(_serviceLocator.Get<GameInfoService>(), _uiManager);
            var factory = new MainPresenterFactory(gameManager, _uiManager);
            var mainPresenter = factory.Create();
            if(mainPresenter != null)
            {
                gameManager.AddSceneListener(mainPresenter);

                // BattleManager 는 MainPresenter 와 형제 계층으로 동작하며 동일 View/Model/UIFactory 를 공유한다.
                // 전투 씬(SceneRecord.IsBattle == true) 에서만 실제 동작하고, 일반 씬에서는 조기 반환한다.
                var battleManager = new BattleManager(
                    mainPresenter.View,
                    mainPresenter.Model,
                    gameManager,
                    mainPresenter.UIFactory);
                gameManager.AddSceneListener(battleManager);
            }

            gameManager.StartActAsync(actIndex, sceneIndex).Forget();

            return UniTask.CompletedTask;
        }
    }
}

