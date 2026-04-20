using UnityEngine;

using Common;

namespace UI.Slots
{
    public abstract class Slot : Element
    {
        
    }
    
    public abstract class Slot<TParam> : Slot where TParam : ElementParam
    {
        protected TParam _param = null;

        public virtual void Initialize(TParam param)
        {
            _param = param;
        }
    }
}

