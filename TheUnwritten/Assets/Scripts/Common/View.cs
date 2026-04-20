using UnityEngine;

namespace Common
{
    public interface IView<TPresenter>
    {
        void Initialize(TPresenter presenter);
    }
    
    public abstract class View : Element
    {
    
    }
    
    public abstract class View<TPresenter> : View, IView<TPresenter> where TPresenter : class
    {
        protected TPresenter _presenter = null;

        public virtual void Initialize(TPresenter presenter)
        {
            base.Initialize();
            
            _presenter = presenter;
        }
    }
}
