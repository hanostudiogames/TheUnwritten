using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public class RectTransformDimensionHandler : MonoBehaviour
    {
        private Dictionary<System.Type, Action<bool>> _dimensionHandlers = new (); 
        
        private void OnRectTransformDimensionsChange()
        {
            NotifyDimensionHandlers();
        }
        
        public void RegisterDimensionHandler<TType>(Action<bool> handler)
        {
            if (!_dimensionHandlers.ContainsKey(typeof(TType)))
                _dimensionHandlers[typeof(TType)] = handler;
        }

        public void UnregisterDimensionHandler<TType>()
        {
            _dimensionHandlers?.Remove(typeof(TType));
        }
        
        private void NotifyDimensionHandlers()
        {
            if (_dimensionHandlers == null)
                return;

            bool isPortrait = Screen.width < Screen.height;
            
            foreach (var handler in _dimensionHandlers.Values)
            {
                handler?.Invoke(isPortrait);
            }
        }
    }
}

