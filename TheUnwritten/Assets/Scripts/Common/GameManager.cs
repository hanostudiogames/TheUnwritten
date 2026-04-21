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
        UniTask OnEndSceneAsync(int act, int scene);
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

        public async UniTask StartActAsync(int act, int scene)
        {
            var actTableContainer = ActTableContainer.Instance;
            if (actTableContainer == null)
                return;
            
            var sceneRecord = actTableContainer.GetSceneRecord(act, scene);
            if (sceneRecord != null)
            {
                _act = act;
                _scene = scene;
            
                await NotifyStartSceneAsync();
                await NotifyEndSceneAsync();
            }

            int nextAct = _act;
            int nextScene = 0;
            if (!actTableContainer.HasNextScene(nextAct, _scene, out nextScene))
            {
                if (!actTableContainer.HasNextAct(act, out nextAct))
                    return;
                
                _scene = 0;
                if (!actTableContainer.HasNextScene(nextAct, _scene, out nextScene))
                    return;
            }
            
            StartActAsync(nextAct, nextScene).Forget();
        }

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
        
        private async UniTask NotifyEndSceneAsync()
        {
            await UniTask.WhenAll(_sceneListeners
                .Where(listener => listener != null)
                .Select(listener => listener.OnEndSceneAsync(_act, _scene)));
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
    }
}

