
namespace Common
{
    // public enum DialoguePostActionType
    // {
    //     None,
    //     
    //     DoShearAllTMP,
    //     DoFoldAllTMP,
    //     
    //     DoShear,
    //     CollapseSingleParagraph,
    // }

    public enum SceneModeType
    {
        None,

        Normal,
        Battle,
    }

    public enum DialogueActionType
    {
        None,

        Shear,
        Fold,
        RandomShake,
        Shake,
        Melt,
        RandomMelt,

        RandomCollapse,

        Pulse,
        Bleed,
        Converge,
        InkMonsterAppear,
        SuckIntoMonster,
    }
   
    public enum TextRevealMode
    {
        Character,
        SmoothLeftToRight
    }
}
