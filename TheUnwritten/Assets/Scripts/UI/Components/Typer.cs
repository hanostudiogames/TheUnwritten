using System;
using System.Collections.Generic;
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

            // 줄바꿈 위치를 미리 계산해 원문에 \n 을 삽입한다.
            // 이렇게 하면 단어가 다음 줄로 튀는 현상을 막으면서도
            // 한 줄 안에서의 정렬(center/right 등)은 정상적으로 동작한다.
            string wrappedText = await PreWrapAsync(text);

            typingText?.SetText(string.Empty);
            if (typingText != null)
                typingText.maxVisibleCharacters = int.MaxValue;

            if (_param != null)
                await UniTask.Delay(TimeSpan.FromSeconds(_param.StartDelaySeconds));

            string currentText = "";
            float typingSpeed = _param?.TypingSpeed ?? 0.05f;

            // 정규식을 통해 태그와 텍스트 분리
            MatchCollection matches = Regex.Matches(wrappedText, @"(<[^>]+>|[ \t]+\n|\n|[^<])");

            for (int i = 0; i < matches.Count; i++)
            {
                if (_isEnd)
                {
                    typingText?.SetText(wrappedText);
                    return;
                }

                string part = matches[i].Value;
                currentText += part;

                typingText?.SetText(currentText);

                bool isTag = part.StartsWith("<");
                bool isSpriteTag = part.StartsWith("<sprite");

                // sprite는 딜레이 포함
                if (!isTag || isSpriteTag)
                    await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
            }

            typingText?.SetText(wrappedText);

            if (_param != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_param.EndDelaySeconds));
                _param.CompleteAction?.Invoke();
            }
        }

        // 전체 텍스트를 한 번 렌더해 TMP 가 결정한 wrap 지점에 \n 을 삽입한다.
        // 측정 중에는 maxVisibleCharacters=0 으로 숨겨 깜빡임을 방지한다.
        private async UniTask<string> PreWrapAsync(string text)
        {
            if (typingText == null || string.IsNullOrEmpty(text))
                return text;

            typingText.SetText(text);
            typingText.maxVisibleCharacters = 0;

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            typingText.ForceMeshUpdate();

            var info = typingText.textInfo;
            if (info == null || info.lineCount <= 1)
                return text;

            var insertPositions = new List<int>();
            for (int line = 0; line < info.lineCount - 1; line++)
            {
                int lastCharIdx = info.lineInfo[line].lastCharacterIndex;
                if (lastCharIdx < 0 || lastCharIdx >= info.characterCount)
                    continue;

                int srcIdx = info.characterInfo[lastCharIdx].index;

                // 이미 \n 으로 끝난 라인은 스킵.
                if (srcIdx >= 0 && srcIdx < text.Length && text[srcIdx] == '\n')
                    continue;

                insertPositions.Add(srcIdx + 1);
            }

            if (insertPositions.Count == 0)
                return text;

            // 뒤에서부터 삽입해 앞쪽 인덱스가 밀리지 않도록 한다.
            string result = text;
            for (int i = insertPositions.Count - 1; i >= 0; i--)
            {
                int pos = insertPositions[i];
                if (pos >= 0 && pos <= result.Length)
                    result = result.Insert(pos, "\n");
            }

            return result;
        }
    }
}
