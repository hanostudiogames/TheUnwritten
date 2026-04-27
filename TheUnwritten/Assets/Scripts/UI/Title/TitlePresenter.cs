using UnityEngine;

using Cysharp.Threading.Tasks;

using Common;
using Services;
using UI.Main;

namespace UI.Title
{
    public interface ITitlePresenter
    {
        void OpenMainView();
    }
    
    public class TitlePresenter : Presenter<TitleView, Model>, ITitlePresenter
    {
        private readonly UIManager _uiManager = null;
        private readonly IGameInfoService _gameInfoService = null;
        
        public TitlePresenter(TitleView view, Model model, UIManager uiManager, IGameInfoService gameInfoService) : base(view, model)
        {
            _uiManager = uiManager;
            _gameInfoService = gameInfoService;
            
            uiManager?.RegisterDimensionHandler<TitlePresenter>(OnDimensionChanged);
        }
        
        public override void Activate()
        {
            base.Activate();
        }

        public void OpenMainView()
        {
            _uiManager.FadeInOutAsync(
                async () => { await OpenMainViewAsync(); }, 
                null).Forget();
        }
        
        private UniTask OpenMainViewAsync()
        {
            var gameManager = new GameManager(_gameInfoService, _uiManager);
            var factory = new MainPresenterFactory(gameManager, _uiManager);
            var mainPresenter = factory.Create();
            if(mainPresenter != null)
                gameManager.AddSceneListener(mainPresenter);

            gameManager.StartActAsync(0, 0).Forget();
            
            return UniTask.CompletedTask;
        }

        protected override void OnDimensionChanged(bool isPortrait)
        {
            base.OnDimensionChanged(isPortrait);
            
            _view?.OnDimensionChanged(isPortrait);
        }
    }
}
