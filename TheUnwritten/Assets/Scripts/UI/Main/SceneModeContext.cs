using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;
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
        public int SlotId = 0;
        public IDialogueSlot DialogueSlot = null;
        public TextMeshProUGUI MonsterTMP = null;

        // 몬스터 등장(IsMonster 캡처) 시점에 시작되어 전투 종료 시 까지 살아있는
        // 호흡 펄스 트윈. 씬 전환을 가로질러 유지되도록 페이로드에 함께 실어 보낸다.
        // public Tween BreathingTween = null;
    }

    public class SceneModeContext
    {
        public MainView View { get; }
        public ICardSelectionHandler CardSelectionHandler { get; }
        public CardController CardController { get; }
        public UIFactory UIFactory { get; }
        // public IBattleCardInput BattleCardInput { get; }
        public CardInventory CardInventory { get; }

        private readonly Dictionary<SceneModeType, ISceneModePayload> _payloads = new();

        public SceneModeContext(
            MainView view,
            ICardSelectionHandler cardSelectionHandler,
            CardController cardController,
            UIFactory uiFactory,
            CardInventory cardInventory)
        {
            View = view;
            CardSelectionHandler = cardSelectionHandler;
            CardController = cardController;
            UIFactory = uiFactory;
            CardInventory = cardInventory;
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
