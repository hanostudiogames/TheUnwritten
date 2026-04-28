using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Common;
using UnityEngine;

using Cysharp.Threading.Tasks;
using TMPro;
using UI.Effects;

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
            public TextRevealMode RevealMode { get; private set; } = TextRevealMode.Character;
            public bool HasRevealModeOverride { get; private set; } = false;

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

            public Param WithRevealMode(TextRevealMode revealMode)
            {
                RevealMode = revealMode;
                HasRevealModeOverride = true;
                return this;
            }
        }

        // 실시간 서술 개입(⑤) 슬롯 태그. 예: <slot_1>, <slot_2>
        private static readonly Regex SlotRegex = new(@"<slot_\d+>", RegexOptions.Compiled);
        private static readonly Regex PartRegex = new(@"(<[^>]+>|[ \t]+\n|\n|[^<])", RegexOptions.Compiled);

        [SerializeField] private TextMeshProUGUI typingText = null;
        // [SerializeField] private RevealMode revealMode = RevealMode.Character;
        [SerializeField] private int smoothRevealFadeCharacters = 6;

        private Param _param = null;
        private string _template = string.Empty;
        private readonly Dictionary<string, string> _slotValues = new();

        public TextMeshProUGUI TMP => typingText;
        // public IReadOnlyDictionary<string, string> SlotValues => _slotValues;

        public void Initialize(Param param)
        {
            _param = param;
        }

        public string FirstEmptySlot()
        {
            foreach (var pair in _slotValues)
            {
                if (string.IsNullOrEmpty(pair.Value))
                    return pair.Key;
            }
            
            return null;
        }

        public async UniTask TypeTextAsync(string text)
        {
            if (typingText == null)
                return;

            typingText.ClearState();

            _template = text ?? string.Empty;
            _slotValues.Clear();
            
            foreach (Match m in SlotRegex.Matches(_template))
            {
                var name = m.Value.Substring(1, m.Value.Length - 2);
                if (!_slotValues.ContainsKey(name))
                    _slotValues[name] = string.Empty;
            }

            var rendered = RenderTemplate();

            if (GetRevealMode() == TextRevealMode.SmoothLeftToRight)
            {
                await RevealTextLeftToRightAsync(rendered);
            }
            else if (typingText.alignment == TextAlignmentOptions.Center)
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
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
                    }
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
                    {
                        typingText.maxVisibleCharacters = i;
                    }

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

            typingText.ClearState();

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
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed));
                }
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
            SetAllCharacterAlpha(255);
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

        private TextRevealMode GetRevealMode()
        {
            return _param != null && _param.HasRevealModeOverride
                ? _param.RevealMode
                : TextRevealMode.SmoothLeftToRight;
        }

        private async UniTask RevealTextLeftToRightAsync(string rendered)
        {
            float originalCanvasAlpha = typingText.canvasRenderer.GetAlpha();
            bool wasEnabled = typingText.enabled;
            int fadeWindow = Mathf.Max(1, smoothRevealFadeCharacters);
            int revealableCount = CountRevealableParts(rendered);

            typingText.enabled = false;
            typingText.canvasRenderer.SetAlpha(0f);
            typingText.SetText(BuildAlphaRevealText(rendered, 0f, fadeWindow));
            typingText.maxVisibleCharacters = int.MaxValue;
            typingText.ForceMeshUpdate(true, true);

            typingText.enabled = wasEnabled;
            typingText.canvasRenderer.SetAlpha(0f);

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            typingText.canvasRenderer.SetAlpha(0f);

            if (_param != null)
                await UniTask.Delay(TimeSpan.FromSeconds(_param.StartDelaySeconds));

            if (revealableCount <= 0)
            {
                typingText.canvasRenderer.SetAlpha(originalCanvasAlpha);
                typingText.SetText(rendered);
                return;
            }

            float typingSpeed = _param?.TypingSpeed + 0.05f ?? 0.1f;
            float duration = Mathf.Max(0.001f, typingSpeed * revealableCount);

            typingText.canvasRenderer.SetAlpha(originalCanvasAlpha);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float head = Mathf.Lerp(0f, revealableCount + fadeWindow, elapsed / duration);
                typingText.SetText(BuildAlphaRevealText(rendered, head, fadeWindow));
                typingText.ForceMeshUpdate();
                
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            typingText.SetText(rendered);
            typingText.ForceMeshUpdate();
            typingText.canvasRenderer.SetAlpha(originalCanvasAlpha);
        }

        private int CountRevealableParts(string rendered)
        {
            int count = 0;
            var matches = PartRegex.Matches(rendered ?? string.Empty);

            for (int i = 0; i < matches.Count; i++)
            {
                if (IsRevealablePart(matches[i].Value))
                    count++;
            }

            return count;
        }

        private string BuildAlphaRevealText(string rendered, float head, int fadeWindow)
        {
            var builder = new StringBuilder((rendered?.Length ?? 0) * 2);
            var matches = PartRegex.Matches(rendered ?? string.Empty);
            int visibleIndex = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                string part = matches[i].Value;
                if (!IsRevealablePart(part))
                {
                    builder.Append(part);
                    continue;
                }

                bool isDash = IsDashPart(part);
                float characterFadeWindow = isDash ? 1f : fadeWindow;
                float revealOffset = isDash ? 1f : 0f;
                byte alpha = (byte)Mathf.RoundToInt(
                    Mathf.Clamp01((head - visibleIndex + revealOffset) / characterFadeWindow) * 255f);

                builder.Append("<alpha=#");
                builder.Append(alpha.ToString("X2"));
                builder.Append(">");
                builder.Append(part);
                visibleIndex++;
            }

            builder.Append("<alpha=#FF>");
            return builder.ToString();
        }

        private static bool IsRevealablePart(string part)
        {
            if (string.IsNullOrEmpty(part))
                return false;

            if (part.StartsWith("<"))
                return part.StartsWith("<sprite", StringComparison.OrdinalIgnoreCase);

            return !string.IsNullOrWhiteSpace(part);
        }

        private static bool IsDashPart(string part)
        {
            return part.Length == 1 && IsDashCharacter(part[0]);
        }

        private int CountVisibleCharacters()
        {
            if (typingText == null || typingText.textInfo == null)
                return 0;

            int count = 0;
            var characters = typingText.textInfo.characterInfo;
            for (int i = 0; i < typingText.textInfo.characterCount; i++)
            {
                if (characters[i].isVisible)
                    count++;
            }

            return count;
        }

        private void ApplyLeftToRightAlpha(float head, int fadeWindow)
        {
            if (typingText == null || typingText.textInfo == null)
                return;

            int visibleIndex = 0;
            var textInfo = typingText.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var character = textInfo.characterInfo[i];
                if (!character.isVisible)
                    continue;

                bool isDash = IsDashCharacter(character.character);
                float characterFadeWindow = isDash ? 1f : fadeWindow;
                float revealOffset = isDash ? 1f : 0f;
                byte alpha = (byte)(Mathf.Clamp01((head - visibleIndex + revealOffset) / characterFadeWindow) * 255);
                SetCharacterAlpha(character, alpha);
                visibleIndex++;
            }

            ApplyCharacterAlphaChanges();
        }

        private static bool IsDashCharacter(char character)
        {
            return character == '-' ||
                   character == '\u2013' ||
                   character == '\u2014' ||
                   character == '\u2212';
        }

        private void SetAllCharacterAlpha(byte alpha, bool forceMeshUpdate = true)
        {
            if (typingText == null)
                return;

            if (forceMeshUpdate)
                typingText.ForceMeshUpdate();

            var textInfo = typingText.textInfo;
            if (textInfo == null)
                return;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var character = textInfo.characterInfo[i];
                if (character.isVisible)
                    SetCharacterAlpha(character, alpha);
            }

            ApplyCharacterAlphaChanges();
        }

        private void ApplyCharacterAlphaChanges()
        {
            if (typingText == null || typingText.textInfo == null)
                return;

            var textInfo = typingText.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                if (meshInfo.mesh == null || meshInfo.colors32 == null)
                    continue;

                meshInfo.mesh.colors32 = meshInfo.colors32;
                typingText.UpdateGeometry(meshInfo.mesh, i);
            }
        }

        private void SetCharacterAlpha(TMP_CharacterInfo character, byte alpha)
        {
            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;
            var colors = typingText.textInfo.meshInfo[materialIndex].colors32;

            colors[vertexIndex + 0].a = alpha;
            colors[vertexIndex + 1].a = alpha;
            colors[vertexIndex + 2].a = alpha;
            colors[vertexIndex + 3].a = alpha;
        }
    }
}
