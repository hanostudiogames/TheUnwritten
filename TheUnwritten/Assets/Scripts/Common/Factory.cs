using UnityEngine;

using Cysharp.Threading.Tasks;

using Common;

namespace Common
{
    public abstract class Factory
    {
        protected abstract TElement OnCreate<TElement>(Transform rootTr, out bool initialize) where TElement : Element;
        
        protected virtual TPresenter OnCreatePresenter<TPresenter>() where TPresenter : class
        {
            return null;
        }
        
        protected TModel OnCreateModel<TModel>() where TModel : class, new()
        {
            return new TModel();
        }
    }
}