using UnityEngine;

namespace Common
{
    public abstract class Presenter
    {
        public abstract void Activate();
    }
    
    public abstract class Presenter<TView, TModel> : Presenter where TView : Element where TModel : class
    {
        protected TView _view = null;
        protected TModel _model = null;

        protected Presenter(TView view, TModel model)
        {
            _view = view;
            _model = model;
        }

        public override void Activate()
        {
            _view?.Activate();

            OnDimensionChanged(Screen.width < Screen.height);
        }

        protected virtual void OnDimensionChanged(bool isPortrait)
        {
            
        }
    }
}
