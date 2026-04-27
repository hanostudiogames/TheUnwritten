using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

using Common;
using Data;
using Tables;
using Tables.Containers;
using Tables.Records;
using UI.Cards;
using UI.Slots;

namespace UI.Main
{
    public interface IMainPresenter
    {
        
    }
    
    public class MainPresenter : Presenter<MainView, MainModel>, IMainPresenter,
        ISceneListener
    {
        private readonly UIFactory _uiFactory = null;
        private readonly IGameManager _gameManager = null;
        private readonly SceneModeContext _sceneModeContext = null;
        
        private DialoguePostAction _dialoguePostAction = null;
        private Dictionary<SceneModeType, SceneMode> _sceneModes = null;
        private SceneMode _sceneMode = null;
        
        public MainPresenter(MainView view, MainModel model, 
            IGameManager gameManager, 
            UIManager uiManager, 
            SlotInteractionHandler slotInteractionHandler) : base (view, model)
        {
            _view = view;

            _gameManager = gameManager;
            _uiFactory = new UIFactory(uiManager);
            
            var cardController = new CardController(view.CardFanSpread);
            slotInteractionHandler?.SetCardController(cardController);
            cardController.SetListener(slotInteractionHandler);

            var cardInventory = new CardInventory();
            // TEMP: 기존 씬 회귀 테스트용 시드. 추후 Scene 1-1~1-3 에 CardGrant 이벤트를 저작해 대체.
            cardInventory.AddCard(1); // flame
            cardInventory.AddCard(3); // seal

            _sceneModeContext = new SceneModeContext(_view, slotInteractionHandler, cardController, _uiFactory, cardInventory);

            _sceneModes = new();
            
            
            uiManager?.RegisterDimensionHandler<MainPresenter>(OnDimensionChanged);
        }

        public override void Activate()
        {
            base.Activate();
            
            _view.FadeLibraryAsync(0, 0).Forget();
        }

        private async UniTask PlayAsync(int act, int scene)
        {
            if (_view == null)
                return;
            
            var sceneRecord = ActTableContainer.Instance?.GetSceneRecord(act, scene);
            if (sceneRecord == null)
                return;

            var sceneMode = CreateSceneMode(sceneRecord.SceneModeType);
            if (sceneMode == null)
                return;

            if (_dialoguePostAction == null)
                _dialoguePostAction = new();
            
            _sceneMode = sceneMode;

            await sceneMode.PlayAsync(act, scene, sceneRecord, _dialoguePostAction);
        }

        private SceneMode CreateSceneMode(SceneModeType sceneModeType)
        {
            if (_sceneModes == null)
                return null;

            SceneMode sceneMode = null;
            if (_sceneModes.TryGetValue(sceneModeType, out sceneMode))
                return sceneMode;

            switch (sceneModeType)
            {
                case SceneModeType.Normal:
                {
                    sceneMode = new NormalSceneMode(_sceneModeContext);
                    break;
                }
                
                case SceneModeType.Battle:
                {
                    sceneMode = new BattleSceneMode(_sceneModeContext);
                    break;
                }
            }

            if (sceneMode == null)
                return null;
   
            _sceneModes[sceneModeType] = sceneMode;
            
            return sceneMode;
        }

        #region ISceneListener

        async UniTask ISceneListener.OnStartSceneAsync(int act, int scene)
        {
            if (act == 1 && scene < 3)
                await _view.FadeLibraryAsync(0.45f, 3f);
            
            await PlayAsync(act, scene);
        }

        async UniTask ISceneListener.OnEndSceneAsync(int act, int scene)
        {
            if (act == 1 && scene == 1)
                await _view.FadeLibraryAsync(0, 3f);
        }
        #endregion
        
        protected override void OnDimensionChanged(bool isPortrait)
        {
            base.OnDimensionChanged(isPortrait);

            if (_view == null)
                return;

            float scrollPositionY = 0;
            if (_sceneMode != null)
                scrollPositionY = _sceneMode.ScrollPositionY;
            
            _view?.OnDimensionChanged(isPortrait, scrollPositionY);
        }
    }
}
