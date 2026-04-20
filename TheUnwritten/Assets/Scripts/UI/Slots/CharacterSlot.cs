using UnityEngine;
using UnityEngine.Localization.Settings;

using TMPro;

using Common;
using Common.Models;
using Data;
using Tables.Records;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace UI.Slots
{
    public interface ICharacterSlot
    {
        void SetRelationship(int relationship);
        
        void OnDimensionChanged(bool isPortrait);
    }
    
    public class CharacterSlot : Slot<CharacterSlot.Param>, ICharacterSlot
    {
        public class Param : ElementParam
        {
            public CharacterRecord CharacterRecord { get; private set; } = null;
            public int Relationship { get; private set; } = 0;

            public Param(CharacterRecord characterRecord)
            {
                CharacterRecord = characterRecord;
            }

            public Param WithRelationship(int relationship)
            {
                Relationship = relationship;
                return this;
            }
        }
        
        [SerializeField] private TextMeshProUGUI characterNameText = null;
        [SerializeField] private TextMeshProUGUI relationshipText = null;
        [SerializeField] private Image roleIconImage = null;
        [SerializeField] private SpriteAtlas iconSpriteAltas = null;

        public override void Initialize(Param param)
        {
            base.Initialize(param);
            
            characterNameText?.SetText(CharacterName);
            
            var relationship = 0;
            if(param != null)
                relationship = param.Relationship;
            
            SetRelationshipText(relationship);
            // SetRoleIconImage();
        }

        private string CharacterName
        {
            get
            {
                var localKey = _param?.CharacterRecord?.NameLocalKey;
                if (string.IsNullOrEmpty(localKey))
                    return string.Empty;
                
                return LocalizationSettings.StringDatabase.GetLocalizedString("Character", localKey, LocalizationSettings.SelectedLocale);
            }
        }

        public void SetRelationship(int relationship)
        {
            _param?.WithRelationship(relationship);
            SetRelationshipText(relationship);
        }

        private void SetRelationshipText(int relationship)
        {
            relationshipText?.SetText($"{relationship}");
        }

        // private void SetRoleIconImage()
        // {
        //     if (roleIconImage ==null)
        //         return;
        //     
        //     roleIconImage.gameObject.SetActive(false);
        //     
        //     var characterRecord = _param?.CharacterRecord;
        //     if (characterRecord == null)
        //         return;
        //
        //     var iconSprite = iconSpriteAltas.GetSprite($"{characterRecord.Role}");
        //     if (iconSprite == null)
        //         return;
        //
        //     roleIconImage.sprite = iconSprite;
        //     roleIconImage.gameObject.SetActive(true);
        // }
        

        void ICharacterSlot.OnDimensionChanged(bool isPortrait)
        {
            characterNameText?.SetText(isPortrait ? string.Empty : CharacterName);
        }
    }
}

