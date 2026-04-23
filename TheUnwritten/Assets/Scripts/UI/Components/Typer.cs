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

        // 실시간 서술 개입(⑤) 슬롯 태그. 예: <slot_1>, <slot_2>
        private static readonly Regex SlotRegex = new(@"<slot_\d+>", RegexOptions.Compiled);
        private static readonly Regex PartRegex = new(@"(<[^>]+>|[ \t]+\n|\n|[^<])", RegexOptions.Compiled);

        [SerializeField] private TextMeshProUGUI typingText = null;

        private Param _param = null;
        private string _template = string.Empty;
        private readonly Dictionary<string, string> _slotValues = new();

        public TextMeshProUGUI TMP => typingText;
        public IReadOnlyDictionary<string, string> SlotValues => _slotValues;

        public void Initialize(Param param)
        {
            _param = param;
        }

        public bool HasSlot(string slotName)
        {
            return !string.IsNullOrEmpty(slotName) && _slotValues.ContainsKey(slotName);
        }

        public bool IsSlotFilled(string slotName)
        {
            return _slotValues.TryGetValue(slotName, out var v) && !string.IsNullOrEmpty(v);
        }

        public bool HasAnyEmptySlot()
        {
            foreach (var kv in _slotValues)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    return true;
            }
            return false;
        }

        public string FirstEmptySlot()
        {
            foreach (var kv in _slotValues)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    return kv.Key;
            }
            return null;
        }

        public async UniTask TypeTextAsync(string text)
        {
            if (typingText == null)
                return;

            _template = text ?? string.Empty;
            _slotValues.Clear();
            foreach (Match m in SlotRegex.Matches(_template))
            {
                var name = m.Value.Substring(1, m.Value.Length - 2);
                if (!_slotValues.ContainsKey(name))
                    _slotValues[name] = string.Empty;
            }

            var rendered = RenderTemplate();

            if (typingText.alignment == TextAlignmentOptions.Center)
            {
                typingText?.SetText(string.Empty);

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

                if (_param != null)
                    await UniTask.Delay(TimeSpan.FromSeconds(_param.StartDelaySeconds));

                string currentText = "";
                float typingSpeed = _param?.TypingSpeed ?? 0.05f;

                var matches = PartRegex.Matches(rendered);
                for (int i = 0; i < matches.Count; ++i)
                {
                    string part = matches[i].Value;
                    currentText += part;

                    typingText?.SetText(currentText);

                    bool isTag = part.StartsWith("<");
                    bool isSpriteTag = part.StartsWith("<sprite");

                    if (!isTag || isSpriteTag)
                        await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
                }

                typingText?.SetText(RenderTemplate());
            }
            else
            {
                typingText.SetText(rendered);
                typingText.maxVisibleCharacters = 0;

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                typingText?.ForceMeshUpdate();

                if (_param != null)
                    await UniTask.Delay(TimeSpan.FromSeconds(_param.StartDelaySeconds));

                int total = typingText?.textInfo?.characterCount ?? 0;
                float typingSpeed = _param?.TypingSpeed ?? 0.05f;

                for (int i = 1; i <= total; i++)
                {
                    if (typingText != null)
                        typingText.maxVisibleCharacters = i;

                    await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
                }

                if (typingText != null)
                    typingText.maxVisibleCharacters = total;
            }

            if (_param != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_param.EndDelaySeconds));
                _param.CompleteAction?.Invoke();
            }
        }

        // 이미 타이핑이 끝난 Typer 의 특정 슬롯 위치에 text 를 한 글자씩 채워넣는다.
        // Center/비Center 모두 동일하게 SetText 재빌드 방식으로 처리한다 (중간 삽입이라 maxVisibleCharacters 불가).
        public async UniTask TypeIntoSlotAsync(string slotName, string text)
        {
            if (typingText == null)
                return;

            if (string.IsNullOrEmpty(slotName) || !_slotValues.ContainsKey(slotName))
                return;

            text ??= string.Empty;

            float typingSpeed = _param?.TypingSpeed ?? 0.05f;

            _slotValues[slotName] = string.Empty;
            RefreshVisibleText();
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

            var matches = PartRegex.Matches(text);
            string current = string.Empty;
            for (int i = 0; i < matches.Count; ++i)
            {
                string part = matches[i].Value;
                current += part;
                _slotValues[slotName] = current;
                RefreshVisibleText();

                bool isTag = part.StartsWith("<");
                bool isSpriteTag = part.StartsWith("<sprite");
                if (!isTag || isSpriteTag)
                    await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
            }

            _slotValues[slotName] = text;
            RefreshVisibleText();
        }

        private void RefreshVisibleText()
        {
            if (typingText == null)
                return;

            typingText.SetText(RenderTemplate());
            typingText.ForceMeshUpdate();
            typingText.maxVisibleCharacters = typingText.textInfo.characterCount;
        }

        private string RenderTemplate()
        {
            if (string.IsNullOrEmpty(_template))
                return string.Empty;

            if (_slotValues.Count == 0)
                return _template;

            return SlotRegex.Replace(_template, m =>
            {
                var name = m.Value.Substring(1, m.Value.Length - 2);
                return _slotValues.TryGetValue(name, out var v) ? v : string.Empty;
            });
        }
    }
}
