

using Cysharp.Threading.Tasks;

using Common;
using TMPro;

namespace UI.Main
{
    public interface IBattleController
    {
        UniTask PlayBattleAsync();
        void SetTargetTMP(TextMeshProUGUI targetTMP);
        void SetDialogueTMP(TextMeshProUGUI dialogueTMP);
    }
    
    public class BattleController : IBattleController, ISceneListener
    {
        private TextMeshProUGUI _targetTMP = null;
        private TextMeshProUGUI _dialogueTMP = null;
        
        private UniTaskCompletionSource _battleCompletionSource = null;
        
        public BattleController(IGameManager gameManager)
        {
            // gameManager.reget;
            gameManager?.AddSceneListener(this);
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

        async UniTask IBattleController.PlayBattleAsync()
        {
            // _targetTMP = targetTMP;

            _battleCompletionSource = new();

            await _battleCompletionSource.Task;
        }
        #endregion

        UniTask ISceneListener.OnStartSceneAsync(int act, int scene)
        {
            return UniTask.CompletedTask;
        }

        UniTask ISceneListener.OnEndSceneAsync(int act, int scene)
        {
            return UniTask.CompletedTask;
        }
    }

}
