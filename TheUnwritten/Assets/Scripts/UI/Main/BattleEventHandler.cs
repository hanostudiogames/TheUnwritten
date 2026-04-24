using Common;
using Tables.Records;
using UI.Slots;

namespace UI.Main
{
    public class BattleEventHandler : ISceneEventHandler
    {
        public SceneEventType EventType => SceneEventType.Battle;

        public void Handle(EventRecord record, SceneModeContext context, IDialogueSlot slot, int act, int scene)
        {
            if (record == null || context == null || slot == null)
                return;

            context.SetPayload(SceneModeType.Battle, new BattleModePayload
            {
                Act = act,
                Scene = scene,
                EventId = record.EventId,
                SlotId = record.SlotId,
                DialogueSlot = slot,
                MonsterTMP = record.IsMonster ? slot.TMP : null
            });
        }
    }
}
