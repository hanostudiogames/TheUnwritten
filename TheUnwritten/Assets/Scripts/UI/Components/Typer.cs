using System;
using System.Text.RegularExpressions;
using UnityEngine;

using Cysharp.Threading.Tasks;
using TMPro;

namespace UI.Components
{
    public class Typer : MonoBehaviour
    {
        public class Param
        {
            public float TypingSpeed { get; private set; } = 0.05f;
            public float StartDelaySeconds { get; private set; } = 0;
            public float EndDelaySeconds { get; private set; } = 0;
            public Action CompleteAction { get; private set; } = null;

            public Param(Action completeAction)
            {
                CompleteAction = completeAction;
            }

            public Param WithStartDelaySeconds(float startDelaySeconds)
            {
                StartDelaySeconds = startDelaySeconds;
                return this;
            }
            
            public Param WithEndDelaySeconds(float endDelaySeconds)
            {
                EndDelaySeconds = endDelaySeconds;
                return this;
            }

            public Param WithTypingSpeed(float typingSpeed)
            {
                TypingSpeed = typingSpeed;
                return this;
            }
        }
        
        [SerializeField] private TextMeshProUGUI typingText = null;

        // private float _typingSpeed = 0.05f;
        private bool _isEnd = false;

        private Param _param = null;
        
        public TextMeshProUGUI TMP => typingText;

        public void Initialize(Param param)
        {
            _param = param;
        }
        
        public async UniTask TypeTextAsync(string text)
        {
            _isEnd = false;
            typingText?.SetText(string.Empty);
            
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
           
            if(_param != null)
                await UniTask.Delay(TimeSpan.FromSeconds(_param.StartDelaySeconds));
            
            string currentText = "";
            float typingSpeed = _param?.TypingSpeed ?? 0.05f;

            // 정규식을 통해 태그와 텍스트 분리
            MatchCollection matches = Regex.Matches(text, @"(<[^>]+>|[ \t]+\n|\n|[^<])");

            for (int i = 0; i < matches.Count; i++)
            {
                if (_isEnd)
                {
                    typingText?.SetText(text);
                    return;
                }

                string part = matches[i].Value;
                currentText += part;

                // 🔥 이거 반드시 있어야 함
                typingText?.SetText(currentText);

                bool isTag = part.StartsWith("<");
                bool isSpriteTag = part.StartsWith("<sprite");

                // sprite는 딜레이 포함
                if (!isTag || isSpriteTag)
                    await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
            }

            typingText?.SetText(text);

            if (_param != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_param.EndDelaySeconds));
                _param.CompleteAction?.Invoke();
            }
        }

        // public void End(string text)
        // {
        //     _isEnd = true;
        //     tmpTMP?.SetText(text);
        // }
    }
}