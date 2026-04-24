

using Cysharp.Threading.Tasks;

using Common;
using Tables.Records;
using TMPro;
using UI.Slots;

namespace UI.Main
{
    public interface IBattleCardInput
    {
        UniTask<CardRecord> RequestCardAsync(int slotId, IDialogueSlot activeSlot);
    }

    public interface IBattleController
    {
        UniTask PlayBattleAsync(int eventId, int slotId, IDialogueSlot activeSlot);
        void SetTargetTMP(TextMeshProUGUI targetTMP);
        void SetDialogueTMP(TextMeshProUGUI dialogueTMP);
    }
    
    public class BattleController : IBattleController
    {
        private TextMeshProUGUI _targetTMP = null;
        private TextMeshProUGUI _dialogueTMP = null;
        private IBattleCardInput _cardInput = null;
        
        private UniTaskCompletionSource _battleCompletionSource = null;
        
        public BattleController(IGameManager gameManager)
        {
            // gameManager.reget;
            // gameManager?.AddSceneListener(this);
        }

        public void BindCardInput(IBattleCardInput cardInput)
        {
            _cardInput = cardInput;
        }

        #region IBattleController
        void IBattleController.SetTargetTMP(TextMeshProUGUI targetTMP)
        {
            _targetTMP = targetTMP;
        }

        void IBattleController.SetDialogueTMP(TextMeshProUGUI dialogueTMP)
        {
            _dialogueTMP = dialogueTMP;
        }

        async UniTask IBattleController.PlayBattleAsync(int eventId, int slotId, IDialogueSlot activeSlot)
        {
            _battleCompletionSource = new();

            if (slotId > 0 && _cardInput != null)
                await _cardInput.RequestCardAsync(slotId, activeSlot);

            _battleCompletionSource.TrySetResult();
            await _battleCompletionSource.Task;
        }
        #endregion

        // UniTask ISceneListener.OnStartSceneAsync(int act, int scene)
        // {
        //     return UniTask.CompletedTask;
        // }
        //
        // UniTask ISceneListener.OnEndSceneAsync(int act, int scene)
        // {
        //     return UniTask.CompletedTask;
        // }
    }

}
