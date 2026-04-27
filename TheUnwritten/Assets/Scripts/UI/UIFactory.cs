using UnityEngine;

using Cysharp.Threading.Tasks;

using Common;
using UI.Slots;

namespace UI
{
    public class UIFactory : Factory
    {
        protected readonly UIManager _uiManager = null;
        
        public UIFactory(UIManager uiManager)
        {
            _uiManager = uiManager;
        }
        
        protected override TElement OnCreate<TElement>(Transform rootTr, out bool initialize)
        {
            initialize = false;
            
            RectTransform rootRectTr = null;
            if (rootTr != null)
                rootRectTr = rootTr.GetComponent<RectTransform>();
            
            return _uiManager?.Create<TElement>(rootRectTr, out initialize);
        }
        
        public TElement Create<TElement>(RectTransform rootRectTr)  where TElement : Element
        {
            var element = OnCreate<TElement>(rootRectTr, out var initialize);
            element?.Activate();
            
            return element;
        }

        public TElement Create<TElement, TParam>(RectTransform rootRectTr, TParam param = null)  where TElement : Element where TParam : ElementParam
        {
            var element = OnCreate<TElement>(rootRectTr, out var initialize);
            if (initialize)
            {
                if(element is Slot<TParam> slot)
                    slot.Initialize(param);
            }
            
            element?.Activate();
            
            return element;
        }
    }
    
    public abstract class UIFactory<TView, TPresenter> : UIFactory where TView : Element where TPresenter : Presenter
    {
        protected TView _view = null;
        
        public UIFactory(UIManager uiManager) : base(uiManager)
        {
            
        }

        public TPresenter Create()
        {
            _view = OnCreate<TView>(null, out var initialize);
            var presenter = OnCreatePresenter<TPresenter>();
            
            if (initialize && _view is View<TPresenter> view)
                view.Initialize(presenter);

            presenter?.Activate();
            
            return presenter;
        }
    }
}
