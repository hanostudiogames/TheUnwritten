using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Components
{
    [ExecuteAlways]
    public class DecipherGlyphProgressBar : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float progress = 0f;
        [SerializeField] private Image fillImage = null;
        [SerializeField] private Image[] glowImages = Array.Empty<Image>();
        [SerializeField] private float[] glyphCompleteThresholds = { 0.18f, 0.38f, 0.57f, 0.74f, 0.94f };
        [SerializeField] private Color fillColor = new(0.12f, 0.85f, 0.78f, 1f);
        [SerializeField, Range(0f, 1f)] private float completedGlowAlpha = 0.22f;
        [SerializeField, Range(0f, 1f)] private float activeGlowAlpha = 0.16f;
        [SerializeField, Range(0f, 1f)] private float activeGlowPulseAlpha = 0.12f;
        [SerializeField, Range(0f, 1f)] private float pulseAlpha = 1f;
        [SerializeField] private Color completedGlowColor = new(0.1f, 0.92f, 0.78f, 1f);
        [SerializeField] private Color activeGlowColor = new(0.12f, 1f, 0.78f, 1f);
        [SerializeField] private Color flashGlowColor = new(0.86f, 1f, 0.9f, 1f);
        [SerializeField] private float activeGlowPulseSpeed = 8f;
        [SerializeField] private float pulseDuration = 0.3f;
        [SerializeField] private float pulseScale = 1.22f;
        [SerializeField] private bool animatePreviewInEditor = false;
        [SerializeField] private float previewSeconds = 4f;

        private bool[] _completed = Array.Empty<bool>();
        private double[] _pulseStartedAt = Array.Empty<double>();
        private Vector3[] _baseScales = Array.Empty<Vector3>();

        public float Progress => progress;

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            ApplyProgress(true);
        }

        private void OnEnable()
        {
            EnsureState();
            ApplyProgress(false);
        }

        private void OnValidate()
        {
            progress = Mathf.Clamp01(progress);
            completedGlowAlpha = Mathf.Clamp01(completedGlowAlpha);
            activeGlowAlpha = Mathf.Clamp01(activeGlowAlpha);
            activeGlowPulseAlpha = Mathf.Clamp01(activeGlowPulseAlpha);
            pulseAlpha = Mathf.Clamp01(pulseAlpha);
            activeGlowPulseSpeed = Mathf.Max(0f, activeGlowPulseSpeed);
            pulseDuration = Mathf.Max(0.01f, pulseDuration);
            pulseScale = Mathf.Max(1f, pulseScale);
            previewSeconds = Mathf.Max(0.1f, previewSeconds);

            EnsureState();
            ApplyProgress(true);
        }

        private void Update()
        {
            EnsureState();

            if (!Application.isPlaying && animatePreviewInEditor)
            {
                var t = Mathf.Repeat((float)(Now() / previewSeconds), 1f);
                progress = t;
                ApplyProgress(true);
            }

            ApplyGlow();

#if UNITY_EDITOR
            if (!Application.isPlaying && (animatePreviewInEditor || HasActivePulse()))
                EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        [ContextMenu("Progress/Reset")]
        private void ResetProgress()
        {
            progress = 0f;
            ApplyProgress(false);
        }

        [ContextMenu("Progress/Next Glyph")]
        private void PreviewNextGlyph()
        {
            for (var i = 0; i < GlyphCount; i++)
            {
                var threshold = GetGlyphThreshold(i);
                if (progress < threshold - 0.0001f)
                {
                    SetProgress(threshold);
                    return;
                }
            }

            SetProgress(1f);
        }

        [ContextMenu("Progress/Complete")]
        private void CompleteProgress()
        {
            SetProgress(1f);
        }

        private int GlyphCount => glowImages?.Length ?? 0;

        private void EnsureState()
        {
            var count = GlyphCount;
            if (_completed.Length != count)
                _completed = new bool[count];
            if (_pulseStartedAt.Length != count)
            {
                _pulseStartedAt = new double[count];
                for (var i = 0; i < _pulseStartedAt.Length; i++)
                    _pulseStartedAt[i] = -1d;
            }
            if (_baseScales.Length != count)
                _baseScales = new Vector3[count];

            for (var i = 0; i < count; i++)
            {
                if (glowImages[i] == null)
                    continue;

                if (_baseScales[i] == Vector3.zero)
                    _baseScales[i] = glowImages[i].rectTransform.localScale;
            }
        }

        private void ApplyProgress(bool playCompletionPulse)
        {
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0;
                fillImage.fillAmount = progress;
                fillImage.color = fillColor;
            }

            var count = GlyphCount;
            if (count == 0)
                return;

            for (var i = 0; i < count; i++)
            {
                var threshold = GetGlyphThreshold(i);
                var isComplete = progress >= threshold - 0.0001f;

                if (playCompletionPulse && isComplete && !_completed[i])
                    StartPulse(i);

                _completed[i] = isComplete;
            }

            ApplyGlow();
        }

        private float GetGlyphThreshold(int index)
        {
            if (glyphCompleteThresholds != null &&
                index >= 0 &&
                index < glyphCompleteThresholds.Length &&
                glyphCompleteThresholds[index] > 0f)
            {
                return Mathf.Clamp01(glyphCompleteThresholds[index]);
            }

            var count = Mathf.Max(1, GlyphCount);
            return (index + 1f) / count;
        }

        private void StartPulse(int index)
        {
            if (index < 0 || index >= _pulseStartedAt.Length)
                return;

            _pulseStartedAt[index] = Now();
        }

        private void ApplyGlow()
        {
            var now = Now();
            for (var i = 0; i < GlyphCount; i++)
            {
                var glow = glowImages[i];
                if (glow == null)
                    continue;

                var pulse = GetPulse(i, now);
                var isActiveGlyph = IsActiveGlyph(i);
                var activePulse = isActiveGlyph
                    ? 0.5f + 0.5f * Mathf.Sin((float)now * activeGlowPulseSpeed + i * 0.73f)
                    : 0f;
                var alpha = Mathf.Max(_completed[i] ? completedGlowAlpha : 0f, pulse * pulseAlpha);
                if (isActiveGlyph)
                    alpha = Mathf.Max(alpha, activeGlowAlpha + activePulse * activeGlowPulseAlpha);

                var color = Color.Lerp(completedGlowColor, flashGlowColor, pulse);
                if (isActiveGlyph && pulse <= 0f)
                    color = Color.Lerp(activeGlowColor, flashGlowColor, activePulse * 0.35f);
                color.a = alpha;
                glow.color = color;

                var baseScale = _baseScales[i] == Vector3.zero ? Vector3.one : _baseScales[i];
                glow.rectTransform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, pulse);
            }
        }

        private bool IsActiveGlyph(int index)
        {
            if (index < 0 || index >= GlyphCount || _completed[index])
                return false;

            var previousThreshold = index <= 0 ? 0f : GetGlyphThreshold(index - 1);
            var threshold = GetGlyphThreshold(index);
            return progress > previousThreshold + 0.0001f && progress < threshold - 0.0001f;
        }

        private float GetPulse(int index, double now)
        {
            if (index < 0 || index >= _pulseStartedAt.Length || _pulseStartedAt[index] < 0d)
                return 0f;

            var elapsed = (float)(now - _pulseStartedAt[index]);
            var normalized = Mathf.Clamp01(elapsed / pulseDuration);
            if (normalized >= 1f)
            {
                _pulseStartedAt[index] = -1d;
                return 0f;
            }

            if (normalized < 0.08f)
                return 1f;

            var decay = 1f - Mathf.InverseLerp(0.08f, 1f, normalized);
            return Mathf.Pow(decay, 2.6f);
        }

        private bool HasActivePulse()
        {
            for (var i = 0; i < _pulseStartedAt.Length; i++)
            {
                if (_pulseStartedAt[i] >= 0d)
                    return true;
            }

            return false;
        }

        private static double Now()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return EditorApplication.timeSinceStartup;
#endif
            return Time.unscaledTimeAsDouble;
        }
    }
}
