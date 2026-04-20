using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Services;
using Tables.Containers;
using Tables.Containers;

namespace Common
{
    public interface ISceneListener
    {
        UniTask OnStartSceneAsync(int act, int scene);
        UniTask OnEndSceneAsync();
    }

    public interface IGameManager
    {
        // GameMode GameMode { get; }
        
        void AddSceneListener(ISceneListener listener);
        // void RegisterModeHandler<TType>(Action<GameMode> action);
    }
    
    public class GameManager : IGameManager
    {
        // private readonly ManagementManager _managementManager = null;
        private readonly EventManager _eventManager = null;
        private readonly IGameInfoService _gameInfoService = null;
        private readonly IUIManager _uiManager = null;

        private int _act = 0;
        private int _scene = 0;
        private readonly HashSet<ISceneListener> _sceneListeners = new();

        // private readonly Dictionary<Type, Action<GameMode>> _modeHandlers = new();
        
        // public GameMode GameMode { get; private set; } = GameMode.None;
        
        public GameManager(IGameInfoService gameInfoService, IUIManager uiManager)
        {
            // _managementManager = new ManagementManager();
            _eventManager = new();
            
            _gameInfoService = gameInfoService;
            _uiManager = uiManager;
        }
        
        public void Initialize()
        {
            
        }

        public async UniTask StartActAsync()
        {
            ++_act;
            ++_scene;
            
            // if(HasDialogue())
            
            // await UniTask.Delay(TimeSpan.FromSeconds(1f));
            await NotifyStartSceneAsync();
            // await _uiManager.FadeInOutAsync(, () => { });
            // await NotifyStartSceneAsync();
        }
        
        // public async UniTask StartTurnAsync()
        // {
        //     ++_turn;
        //     
        //     if(HasDialogue(false))
        //         SetMode(GameMode.Narrative);
        //
        //     await UniTask.Delay(TimeSpan.FromSeconds(1f));
        //     await _uiManager.FadeOutDimmedAsync();
        //     await NotifyStartTurnAsync();
        //
        //
        //     SetMode(GameMode.Governance);
        // }

        // public async UniTask EndTurnAsync()
        // {
        //     if(HasDialogue(true))
        //         SetMode(GameMode.Narrative);
        //     // 종료 스토리
        // }

        public void AddSceneListener(ISceneListener listener)
        {
            if (listener == null) 
                return;

            _sceneListeners?.Add(listener);
        }
        
        public void RemoveSceneListener(ISceneListener listener) => _sceneListeners?.Remove(listener);

        private async UniTask NotifyStartSceneAsync()
        {
            await UniTask.WhenAll(_sceneListeners
                .Where(listener => listener != null)
                .Select(listener => listener.OnStartSceneAsync(_act, _scene)));
        }

        private bool HasDialogue()
        {
            var sceneRecord = ActTableContainer.Instance.GetSceneRecord(_act, _scene);
            if (sceneRecord == null)
                return false;
            
            var dialogues = sceneRecord.DialogueRecords;
            if (dialogues == null)
                return false;

            return dialogues.Length > 0;
        }

        // private void SetMode(GameMode mode)
        // {
        //     GameMode = mode;
        //     NotifyModeChanged();
        // }

        // public void RegisterModeHandler<TType>(Action<GameMode> handler)
        // {
        //     if (!_modeHandlers.ContainsKey(typeof(TType)))
        //         _modeHandlers[typeof(TType)] = handler;
        // }

        // private void NotifyModeChanged()
        // {
        //     if (_modeHandlers == null)
        //         return;
        //     
        //     foreach (var modeHandler in _modeHandlers.Values)
        //     {
        //         modeHandler?.Invoke(GameMode);
        //     }
        // }
    }
}

