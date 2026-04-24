using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

using Common;
using Services;

namespace UI.Main
{
    public class MainPresenterFactory : UIFactory<MainView, MainPresenter>
    {
        private readonly IGameManager _gameManager = null;

        protected override TPresenter OnCreatePresenter<TPresenter>()
        {
            var model = OnCreateModel<MainModel>();
            var presenter = new MainPresenter(_view, model, _gameManager, _uiManager, 
                new SlotInteractionHandler());
            
            presenter.Activate();

            return presenter as TPresenter;
        }

        public MainPresenterFactory(IGameManager gameManager, UIManager uiManager) : base(uiManager)
        {
            _gameManager = gameManager;
        }
    }
}
