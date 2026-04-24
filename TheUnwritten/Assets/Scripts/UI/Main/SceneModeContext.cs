using System.Collections.Generic;
using UnityEngine;

using TMPro;

using Common;
using UI.Cards;
using UI.Slots;

namespace UI.Main
{
    public interface ISceneModePayload
    {
    }

    public class BattleModePayload : ISceneModePayload
    {
        public int Act = 0;
        public int Scene = 0;
        public int EventId = 0;
        public int SlotId = 0;
        public IDialogueSlot DialogueSlot = null;
        public TextMeshProUGUI MonsterTMP = null;
    }

    public class SceneModeContext
    {
        public MainView View { get; }
        public ICardSelectionHandler CardSelectionHandler { get; }
        public CardController CardController { get; }
        public UIFactory UIFactory { get; }
        // public IBattleCardInput BattleCardInput { get; }
        public SceneEventDispatcher EventDispatcher { get; }

        private readonly Dictionary<SceneModeType, ISceneModePayload> _payloads = new();

        public SceneModeContext(
            MainView view,
            ICardSelectionHandler cardSelectionHandler,
            CardController cardController,
            UIFactory uiFactory,
            SceneEventDispatcher eventDispatcher)
        {
            View = view;
            CardSelectionHandler = cardSelectionHandler;
            CardController = cardController;
            UIFactory = uiFactory;
            EventDispatcher = eventDispatcher;
        }

        public void SetPayload<T>(SceneModeType sceneModeType, T payload) where T : class, ISceneModePayload
        {
            if (payload == null)
            {
                _payloads.Remove(sceneModeType);
                return;
            }

            _payloads[sceneModeType] = payload;
        }

        public T GetPayload<T>(SceneModeType sceneModeType) where T : class, ISceneModePayload
        {
            if (_payloads.TryGetValue(sceneModeType, out var payload))
                return payload as T;

            return null;
        }

        public void ClearPayload(SceneModeType sceneModeType)
        {
            _payloads.Remove(sceneModeType);
        }
    }
}
