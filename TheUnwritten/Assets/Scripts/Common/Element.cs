using UnityEngine;

namespace Common
{
    public class ElementParam
    {
        
    }
    
    public class Element : MonoBehaviour, IPoolable
    {
        public GameObject GameObject => gameObject;

        string IPoolable.Key => GetType().Name;
        
        bool IPoolable.IsActive
        {
            get
            {
                if (!gameObject)
                    return false;
                
                return gameObject.activeSelf;
            }
        }
        
        public virtual void Initialize()
        {
            
        }

        public virtual void Activate()
        {
            gameObject.SetActive(true);
        }

        public virtual void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}

