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
            if (mainPresenter != null)
            {
                gameManager.AddSceneListener(mainPresenter);

                // ⑤ 실시간 서술 개입 전담 핸들러. 활성 다이얼로그의 <slot_N> 에 선택된 카드 이름을 채워넣는다.
                var slotInteractionHandler = new SlotInteractionHandler();
                mainPresenter.SetCardSelectionHandler(slotInteractionHandler);
                mainPresenter.CardController.SetListener(slotInteractionHandler);
            }

            gameManager.StartActAsync(actIndex, sceneIndex).Forget();

            return UniTask.CompletedTask;
        }
    }
}

