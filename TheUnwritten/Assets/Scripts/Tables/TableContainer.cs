using UnityEngine;

namespace Tables
{
    public interface IInitializableContainer
    {
        void Initialize(ScriptableObject scriptableObject);
    }

    public class TableContainer<TContainer, TTable> : IInitializableContainer 
        where TContainer : class, new()
        where TTable : ScriptableObject
    {
        public static TContainer Instance { get; private set; } = null;

        protected TTable _table = null;

        void IInitializableContainer.Initialize(ScriptableObject scriptableObject)
        {
            Instance = this as TContainer;
            _table = scriptableObject as TTable;

            Initialize();
        }

        protected virtual void Initialize()
        {
            
        }
    }
}
