using Common;
using Tables.Records;
using UI.Slots;

namespace UI.Main
{
    public interface ISceneEventHandler
    {
        SceneEventType EventType { get; }

        void Handle(EventRecord record, SceneModeContext context, IDialogueSlot slot, int act, int scene);
    }
}
