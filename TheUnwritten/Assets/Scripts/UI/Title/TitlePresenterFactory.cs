using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

using Common;
using Services;
using UI.View;

namespace UI.Title
{
    public class TitlePresenterFactory : UIFactory<TitleView, TitlePresenter>
    {
        // private readonly IGameManager _gameManager = null;
        private readonly IGameInfoService _gameInfoService = null;
        
        public TitlePresenterFactory(UIManager uiManager, IGameInfoService gameInfoService) : base(uiManager)
        {
            // _gameManager = gameManager;
            _gameInfoService = gameInfoService;
        }

        protected override TPresenter OnCreatePresenter<TPresenter>()
        {
            // var model = OnCreateModel<MainModel>();
        
            var presenter = new TitlePresenter(_view, null, _uiManager, _gameInfoService);
            presenter.Activate();

            return presenter as TPresenter;
        }
    }
}