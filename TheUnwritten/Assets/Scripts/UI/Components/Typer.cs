using System;
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

            // 전체 텍스트를 먼저 세팅하고 maxVisibleCharacters 로 점진 공개.
            // 이렇게 하면 TMP 가 최종 레이아웃으로 미리 wrapping 을 확정하므로
            // 타이핑 도중 단어가 다음 줄로 튀는 현상이 발생하지 않는다.
            if (typingText != null)
            {
                typingText.SetText(text);
                typingText.maxVisibleCharacters = 0;
            }

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            typingText?.ForceMeshUpdate();

            if (_param != null)
                await UniTask.Delay(TimeSpan.FromSeconds(_param.StartDelaySeconds));

            float typingSpeed = _param?.TypingSpeed ?? 0.05f;
            int total = typingText?.textInfo?.characterCount ?? 0;

            for (int i = 1; i <= total; i++)
            {
                if (_isEnd)
                {
                    if (typingText != null)
                        typingText.maxVisibleCharacters = total;
                    return;
                }

                if (typingText != null)
                    typingText.maxVisibleCharacters = i;

                await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
            }

            if (typingText != null)
                typingText.maxVisibleCharacters = total;

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