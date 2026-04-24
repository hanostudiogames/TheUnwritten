using System.Collections.Generic;

using Common;
using Tables.Records;
using UI.Slots;

namespace UI.Main
{
    public class SceneEventDispatcher
    {
        private readonly Dictionary<SceneEventType, ISceneEventHandler> _handlers = new();

        public SceneEventDispatcher Register(ISceneEventHandler handler)
        {
            if (handler == null)
                return this;

            _handlers[handler.EventType] = handler;
            return this;
        }

        public void Dispatch(EventRecord record, SceneModeContext context, IDialogueSlot slot, int act, int scene)
        {
            if (record == null)
                return;

            var type = (SceneEventType)record.EventId;
            if (_handlers.TryGetValue(type, out var handler))
                handler.Handle(record, context, slot, act, scene);
        }
    }
}
