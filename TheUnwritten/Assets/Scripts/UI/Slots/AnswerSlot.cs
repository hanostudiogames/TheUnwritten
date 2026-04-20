using UnityEngine;

using UnityEngine.Localization.Settings;

using TMPro;

using Common;
using UnityEngine.UI;

namespace UI.Slots
{
    public interface IAnswerSlot
    {
        void Activate();
        void Deactivate();
        
        void SetIndex(int index);
        void SetAnswerId(int id);
    }
    
    public class AnswerSlot : Slot<AnswerSlot.Param>, IAnswerSlot
    {
        public class Param : ElementParam
        {
            public IListener Listener { get; private set; } = null;

            public int Index { get; private set; } = 0;
            public int AnswerId { get; private set; } = 0;

            public Param(IListener listener)
            {
                Listener = listener;
            }
            
            public Param WithAnswerId(int id)
            {
                AnswerId = id;
                return this;
            }

            public Param WithIndex(int index)
            {
                Index = index;
                return this;
            }
        }

        public interface IListener
        {
            void OnSelectedAnswer(int id);
        }
        
        [SerializeField] private TextMeshProUGUI indexText = null;
        [SerializeField] private TextMeshProUGUI answerText = null;
        [SerializeField] private Button btn = null;
        
        public override void Initialize(Param param)
        {
            base.Initialize(param);
            
            btn?.onClick.RemoveAllListeners();
            btn?.onClick.AddListener(OnClick);
        }

        private void SetIndexText()
        {
            indexText?.SetText($"{_param?.Index}.");
        }

        private void SetAnswerText()
        {
            var local = LocalizationSettings.StringDatabase.GetLocalizedString("Answer", $"{_param.AnswerId}", LocalizationSettings.SelectedLocale);
            answerText?.SetText(local);
        }

        private void OnClick()
        {
            _param?.Listener?.OnSelectedAnswer(_param.AnswerId);
        }
        
        #region IAnswerSlot

        void IAnswerSlot.SetIndex(int index)
        {
            _param?.WithIndex(index);
            
            SetIndexText();
        }
        
        void IAnswerSlot.SetAnswerId(int id)
        {
            _param?.WithAnswerId(id);
            
            SetAnswerText();
        }
        #endregion
    }

}
