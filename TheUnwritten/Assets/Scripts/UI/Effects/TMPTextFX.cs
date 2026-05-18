using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace UI.Effects
{
    public static class TMPTextFX
    {
        #region ===== STATE =====

        private class State
        {
            public float[] shear;
            public float[] fold;
            public float[] fall;
            public float[] rot;
            public float[] shake;
            public float[] melt;
            public float[] pulse;
            public float[] bleed;
            public float[] converge;
            public float[] shadowSpread;
            public float[] suck;
            public float[] letterDrop;
            public float[] letterMissing;
            public float[] letterCorrosion;
            public float[] letterDropShake;

            public Vector3[][] originalVertices;
            public Color32[][] originalColors;
            public int length;

            public Vector2 convergeTarget;
            public Vector2 suckTarget;
            public Vector2[] scatterOffset;
            public Vector2[] shadowSpreadOffset;
            public Vector2[] suckCurveOffset;
            public Vector2[] letterDropOffset;
            public float[] suckSpin;
            public float[] letterDropSpin;
            public Color bleedColor = Color.black;
            public Color shadowSpreadColor = new Color(0.04f, 0.02f, 0.08f, 0.12f);
            public Color corrosionColor = new Color(0.08f, 0.03f, 0.02f, 1f);
        }

        private static readonly Dictionary<TMP_Text, State> stateMap = new();

        private static State GetState(TMP_Text text)
        {
            text.ForceMeshUpdate();

            var textInfo = text.textInfo;
            int count = textInfo.characterCount;

            if (!stateMap.TryGetValue(text, out var state))
            {
                state = new State();
                stateMap[text] = state;
            }

            if (state.length != count || state.letterDrop == null || state.letterDrop.Length != count)
            {
                state.shear = new float[count];
                state.fold = new float[count];
                state.fall = new float[count];
                state.rot = new float[count];
                state.shake = new float[count];
                state.melt = new float[count];
                state.pulse = new float[count];
                state.bleed = new float[count];
                state.converge = new float[count];
                state.shadowSpread = new float[count];
                state.suck = new float[count];
                state.letterDrop = new float[count];
                state.letterMissing = new float[count];
                state.letterCorrosion = new float[count];
                state.letterDropShake = new float[count];
                state.scatterOffset = new Vector2[count];
                state.shadowSpreadOffset = new Vector2[count];
                state.suckCurveOffset = new Vector2[count];
                state.letterDropOffset = new Vector2[count];
                state.suckSpin = new float[count];
                state.letterDropSpin = new float[count];
                state.length = count;

                state.originalVertices = new Vector3[textInfo.meshInfo.Length][];
                state.originalColors = new Color32[textInfo.meshInfo.Length][];

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    var srcV = textInfo.meshInfo[i].vertices;
                    state.originalVertices[i] = new Vector3[srcV.Length];
                    Array.Copy(srcV, state.originalVertices[i], srcV.Length);

                    var srcC = textInfo.meshInfo[i].colors32;
                    state.originalColors[i] = new Color32[srcC.Length];
                    Array.Copy(srcC, state.originalColors[i], srcC.Length);
                }
            }

            return state;
        }

        public static void ClearState(this TMP_Text text)
        {
            stateMap.Remove(text);
        }

        #endregion

        #region ===== APPLY =====

        private static void ApplyAll(TMP_Text text, State state)
        {
            var textInfo = text.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIndex = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;

                var vertices = textInfo.meshInfo[matIndex].vertices;
                var colors = textInfo.meshInfo[matIndex].colors32;
                var original = state.originalVertices[matIndex];
                var originalColors = state.originalColors[matIndex];

                float fold = state.fold[i];
                float shear = state.shear[i];
                float fall = state.fall[i];
                float rot = state.rot[i];
                float shake = state.shake[i];
                float melt = state.melt[i];
                float pulse = state.pulse[i];
                float bleed = state.bleed[i];
                float converge = state.converge[i];
                float shadowSpread = state.shadowSpread[i];
                float suck = state.suck[i];
                float suckEase = suck * suck * (3f - 2f * suck);
                float letterDrop = state.letterDrop[i];
                float letterDropEase = letterDrop * letterDrop * (3f - 2f * letterDrop);
                float letterMissing = state.letterMissing[i];
                float letterCorrosion = state.letterCorrosion[i];
                float letterDropShake = state.letterDropShake[i];

                Vector3 pivot = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;

                // 🔥 converge (pivot을 target + scatter 지점으로 끌어당김)
                Vector2 scatter = (state.scatterOffset != null && i < state.scatterOffset.Length)
                    ? state.scatterOffset[i]
                    : Vector2.zero;
                Vector3 convergeDest = new Vector3(
                    state.convergeTarget.x + scatter.x,
                    state.convergeTarget.y + scatter.y,
                    0f);
                Vector3 convergeOffset = (convergeDest - pivot) * converge;
                Vector2 shadowOffset = (state.shadowSpreadOffset != null && i < state.shadowSpreadOffset.Length)
                    ? state.shadowSpreadOffset[i] * shadowSpread
                    : Vector2.zero;
                Vector2 suckCurveOffset = (state.suckCurveOffset != null && i < state.suckCurveOffset.Length)
                    ? state.suckCurveOffset[i] * Mathf.Sin(suckEase * Mathf.PI)
                    : Vector2.zero;
                Vector3 suckDest = new Vector3(
                    state.suckTarget.x + suckCurveOffset.x,
                    state.suckTarget.y + suckCurveOffset.y,
                    0f);
                Vector3 suckOffset = (suckDest - pivot) * suckEase;
                float suckSpin = state.suckSpin != null && i < state.suckSpin.Length
                    ? state.suckSpin[i]
                    : 0f;
                Vector2 letterDropOffset = (state.letterDropOffset != null && i < state.letterDropOffset.Length)
                    ? state.letterDropOffset[i] * letterDropEase
                    : Vector2.zero;
                float letterDropSpin = state.letterDropSpin != null && i < state.letterDropSpin.Length
                    ? state.letterDropSpin[i]
                    : 0f;

                // 🔥 melt (글자 상단 고정, 하단이 늘어져 내림)
                float topY = charInfo.topLeft.y;
                float bottomY = charInfo.bottomLeft.y;
                float charHeight = Mathf.Max(topY - bottomY, 0.0001f);
                float dripPhase = Mathf.Sin(i * 1.37f) + Mathf.Sin(i * 0.53f) * 0.5f;

                // 🔥 fold ease
                float ft = 1f - Mathf.Pow(1f - fold, 3f);

                // 🔥 rotation
                float angle = rot * 90f + suckSpin * suckEase * 360f + letterDropSpin * letterDropEase * 180f;
                float rad = angle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                // 🔥 shake (시간 기반)
                float shakeX = Mathf.Sin(Time.time * 60f + i) * shake;
                float shakeY = Mathf.Cos(Time.time * 50f + i) * shake;
                float letterShakeX = Mathf.Sin(Time.time * 22f + i * 0.73f) * letterDropShake;
                float letterShakeY = Mathf.Cos(Time.time * 17f + i * 1.11f) * letterDropShake * 0.65f;

                // 🔥 pulse (pivot 기준 균일 스케일)
                float suckScale = Mathf.Lerp(1f, 0.03f, Mathf.Pow(suckEase, 1.15f));
                float pulseScale = (1f + pulse) * suckScale;

                for (int j = 0; j < 4; j++)
                {
                    int idx = vertIndex + j;

                    Vector3 v = original[idx];
                    Vector3 dir = v - pivot;

                    // shear
                    dir.x += dir.y * shear;

                    // rotate
                    float rx = dir.x * cos - dir.y * sin;
                    float ry = dir.x * sin + dir.y * cos;

                    // fall
                    ry -= fall * 50f;

                    // shake 추가 (마지막!)
                    rx += shakeX;
                    ry += shakeY;

                    // melt (상단=0, 하단=1 인 비선형 가중치로 늘어트림)
                    float fromTop = Mathf.Clamp01((topY - v.y) / charHeight);
                    float meltWeight = fromTop * fromTop;
                    ry -= melt * meltWeight * 80f;
                    rx += melt * meltWeight * dripPhase * 6f;

                    // fold — melt를 포함한 최종 수직 변위를 압축 (녹은 글자가 바닥으로 깔림)
                    ry *= (1f - ft);

                    // pulse (pivot 기준 스케일)
                    rx *= pulseScale;
                    ry *= pulseScale;

                    vertices[idx] = pivot + convergeOffset + suckOffset + new Vector3(
                        rx + shadowOffset.x + letterDropOffset.x + letterShakeX,
                        ry + shadowOffset.y + letterDropOffset.y + letterShakeY,
                        0f);

                    // bleed (vertex color를 bleedColor 쪽으로 블렌딩, 알파는 원본 유지)
                    Color finalColor = originalColors[idx];
                    if (bleed > 0f)
                    {
                        Color baseCol = originalColors[idx];
                        Color target = state.bleedColor;
                        target.a = baseCol.a;
                        finalColor = Color.Lerp(baseCol, target, bleed);
                    }

                    if (shadowSpread > 0f)
                    {
                        Color target = state.shadowSpreadColor;
                        target.a = ((Color)originalColors[idx]).a * Mathf.Clamp01(state.shadowSpreadColor.a);
                        finalColor = Color.Lerp(finalColor, target, Mathf.Clamp01(shadowSpread));
                    }

                    if (letterCorrosion > 0f)
                    {
                        Color target = state.corrosionColor;
                        target.a = finalColor.a;
                        finalColor = Color.Lerp(finalColor, target, Mathf.Clamp01(letterCorrosion));

                        float vertexNoise = Noise01(i * 53 + j * 17 + 23);
                        float flicker = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.time * 16f + i * 1.37f + j));
                        float bite = Mathf.Lerp(0.18f, 0.72f, vertexNoise) * Mathf.Lerp(0.65f, 1f, flicker);
                        finalColor.a *= 1f - Mathf.Clamp01(letterCorrosion * bite);
                    }

                    if (suck > 0f)
                    {
                        float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 1f, suckEase));
                        finalColor.a *= 1f - fade;
                    }

                    if (letterDrop > 0f || letterMissing > 0f)
                    {
                        float dropFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 1f, letterDropEase));
                        float alphaLoss = Mathf.Clamp01(Mathf.Max(letterMissing, dropFade));
                        finalColor.a *= 1f - alphaLoss;
                    }

                    colors[idx] = finalColor;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                meshInfo.mesh.colors32 = meshInfo.colors32;
                text.UpdateGeometry(meshInfo.mesh, i);
            }
        }

        #endregion

        #region ===== RESET =====

        public static void ResetFX(this TMP_Text text)
        {
            if (!stateMap.TryGetValue(text, out var state))
                return;

            Array.Clear(state.shear, 0, state.shear.Length);
            Array.Clear(state.fold, 0, state.fold.Length);
            Array.Clear(state.fall, 0, state.fall.Length);
            Array.Clear(state.rot, 0, state.rot.Length);
            Array.Clear(state.shake, 0, state.shake.Length);
            Array.Clear(state.melt, 0, state.melt.Length);
            Array.Clear(state.pulse, 0, state.pulse.Length);
            Array.Clear(state.bleed, 0, state.bleed.Length);
            Array.Clear(state.converge, 0, state.converge.Length);
            Array.Clear(state.shadowSpread, 0, state.shadowSpread.Length);
            Array.Clear(state.suck, 0, state.suck.Length);
            Array.Clear(state.letterDrop, 0, state.letterDrop.Length);
            Array.Clear(state.letterMissing, 0, state.letterMissing.Length);
            Array.Clear(state.letterCorrosion, 0, state.letterCorrosion.Length);
            Array.Clear(state.letterDropShake, 0, state.letterDropShake.Length);
            if (state.scatterOffset != null)
                Array.Clear(state.scatterOffset, 0, state.scatterOffset.Length);
            if (state.shadowSpreadOffset != null)
                Array.Clear(state.shadowSpreadOffset, 0, state.shadowSpreadOffset.Length);
            if (state.suckCurveOffset != null)
                Array.Clear(state.suckCurveOffset, 0, state.suckCurveOffset.Length);
            if (state.letterDropOffset != null)
                Array.Clear(state.letterDropOffset, 0, state.letterDropOffset.Length);
            if (state.suckSpin != null)
                Array.Clear(state.suckSpin, 0, state.suckSpin.Length);
            if (state.letterDropSpin != null)
                Array.Clear(state.letterDropSpin, 0, state.letterDropSpin.Length);

            ApplyAll(text, state);
        }

        #endregion

        #region ===== SINGLE =====

        public static Tween DoFold(this TMP_Text text, float target, float duration)
        {
            var state = GetState(text);
            float current = state.fold.Length > 0 ? state.fold[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.fold.Length; i++)
                    state.fold[i] = x;

                ApplyAll(text, state);
            }, target, duration);
        }

        public static Tween DoShear(this TMP_Text text, float target, float duration)
        {
            var state = GetState(text);
            float current = state.shear.Length > 0 ? state.shear[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.shear.Length; i++)
                    state.shear[i] = x;

                ApplyAll(text, state);
            }, target, duration).SetEase(Ease.InOutSine);
        }

        public static Tween DoShake(this TMP_Text text, float target, float duration)
        {
            var state = GetState(text);
            float current = state.shake.Length > 0 ? state.shake[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.shake.Length; i++)
                    state.shake[i] = x;

                ApplyAll(text, state);
            }, target, duration).SetEase(Ease.InOutSine);
        }

        public static Tween DoMelt(this TMP_Text text, float target, float duration)
        {
            var state = GetState(text);
            float current = state.melt.Length > 0 ? state.melt[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.melt.Length; i++)
                    state.melt[i] = x;

                ApplyAll(text, state);
            }, target, duration).SetEase(Ease.InCubic);
        }

        public static Tween DoPulse(this TMP_Text text, float target, float duration)
        {
            var state = GetState(text);
            float current = state.pulse.Length > 0 ? state.pulse[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.pulse.Length; i++)
                    state.pulse[i] = x;

                ApplyAll(text, state);
            }, target, duration).SetEase(Ease.InOutSine);
        }

        public static Tween DoBleed(this TMP_Text text, float target, float duration, Color? bleedColor = null)
        {
            var state = GetState(text);
            if (bleedColor.HasValue)
                state.bleedColor = bleedColor.Value;

            float current = state.bleed.Length > 0 ? state.bleed[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.bleed.Length; i++)
                    state.bleed[i] = x;

                ApplyAll(text, state);
            }, target, duration).SetEase(Ease.InOutQuad);
        }

        // 불꽃이 글자들을 *부분부분* 핥는 듯한 효과.
        // duration 은 전체 지속 시간이 아니라 0→maxStrength 한 번의 flicker half-cycle 시간이다.
        // 호출부에서 별도 지속 시간 동안 유지한 뒤 Kill/복귀시키는 구조.
        public static Tween DoBleedFlame(this TMP_Text text, float maxStrength, float duration, Color? bleedColor = null)
        {
            var state = GetState(text);
            if (bleedColor.HasValue)
                state.bleedColor = bleedColor.Value;

            float globalStrength = 0f;

            var tween = DOTween.To(() => globalStrength, x => globalStrength = x, maxStrength, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .OnUpdate(() =>
                {
                    float t = Time.time;
                    for (int i = 0; i < state.bleed.Length; i++)
                    {
                        // 글자별 위상차 sin 노이즈 — 시간에 따라 패턴 이동.
                        float n1 = Mathf.Sin(i * 1.7f + t * 3.5f);
                        float n2 = Mathf.Sin(i * 0.7f + t * 5.2f) * 0.5f;
                        float perChar = Mathf.Clamp01((n1 + n2 + 1.5f) / 3f);
                        state.bleed[i] = perChar * globalStrength;
                    }
                    ApplyAll(text, state);
                });

            return tween;
        }

        public static Tween DoShadowSpread(this TMP_Text text, float strength, float duration, Color? shadowColor = null)
        {
            var state = GetState(text);
            state.shadowSpreadColor = shadowColor ?? new Color(0.04f, 0.02f, 0.08f, 0.12f);

            if (strength > 0f)
                BuildShadowSpreadOffsets(text, state);

            float current = state.shadowSpread.Length > 0 ? state.shadowSpread[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.shadowSpread.Length; i++)
                    state.shadowSpread[i] = x;

                ApplyAll(text, state);
            }, strength, duration).SetEase(strength > current ? Ease.OutCubic : Ease.InOutSine);
        }

        private static void BuildShadowSpreadOffsets(TMP_Text text, State state)
        {
            text.ForceMeshUpdate();
            var textInfo = text.textInfo;

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            int visibleCount = 0;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                    continue;

                visibleCount++;
                if (charInfo.bottomLeft.x < min.x) min.x = charInfo.bottomLeft.x;
                if (charInfo.bottomLeft.y < min.y) min.y = charInfo.bottomLeft.y;
                if (charInfo.topRight.x > max.x) max.x = charInfo.topRight.x;
                if (charInfo.topRight.y > max.y) max.y = charInfo.topRight.y;
            }

            Vector2 center = visibleCount > 0
                ? new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f)
                : Vector2.zero;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                {
                    state.shadowSpreadOffset[i] = Vector2.zero;
                    continue;
                }

                Vector2 charCenter = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;
                Vector2 dir = charCenter - center;
                float n1 = Noise01(i * 17 + 3);
                float n2 = Noise01(i * 31 + 9);

                if (dir.sqrMagnitude < 0.001f)
                {
                    float fallbackAngle = n1 * Mathf.PI * 2f;
                    dir = new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle));
                }
                else
                {
                    dir.Normalize();
                }

                float angle = Mathf.Atan2(dir.y, dir.x) + Mathf.Lerp(-0.75f, 0.75f, n1);
                float distance = Mathf.Lerp(48f, 92f, n2);
                state.shadowSpreadOffset[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            }
        }

        private static float Noise01(int seed)
        {
            return Mathf.Repeat(Mathf.Sin(seed * 12.9898f) * 43758.5453f, 1f);
        }

        private static int[] BuildShuffledIndices(int count)
        {
            int[] indices = new int[count];
            for (int i = 0; i < count; i++)
                indices[i] = i;

            for (int i = 0; i < count; i++)
            {
                int rand = UnityEngine.Random.Range(i, count);
                (indices[i], indices[rand]) = (indices[rand], indices[i]);
            }

            return indices;
        }

        private static Vector2 GetTargetPointInLocalSpace(TMP_Text sourceText, TMP_Text targetText)
        {
            Vector3 targetWorld = GetTextAbsorbWorldPoint(targetText);
            Vector3 local = sourceText.transform.InverseTransformPoint(targetWorld);
            return new Vector2(local.x, local.y);
        }

        private static Vector3 GetTextAbsorbWorldPoint(TMP_Text targetText)
        {
            var textInfo = targetText.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
            {
                targetText.ForceMeshUpdate();
                textInfo = targetText.textInfo;
            }

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            int visibleCount = 0;

            if (textInfo != null)
            {
                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    var charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible)
                        continue;

                    visibleCount++;
                    if (charInfo.bottomLeft.x < min.x) min.x = charInfo.bottomLeft.x;
                    if (charInfo.bottomLeft.y < min.y) min.y = charInfo.bottomLeft.y;
                    if (charInfo.topRight.x > max.x) max.x = charInfo.topRight.x;
                    if (charInfo.topRight.y > max.y) max.y = charInfo.topRight.y;
                }
            }

            Vector3 localTarget;
            if (visibleCount > 0)
            {
                localTarget = new Vector3(
                    (min.x + max.x) * 0.5f,
                    min.y + (max.y - min.y) * 0.35f,
                    0f);
            }
            else
            {
                var rectTransform = targetText.transform as RectTransform;
                localTarget = rectTransform != null ? rectTransform.rect.center : Vector3.zero;
            }

            return targetText.transform.TransformPoint(localTarget);
        }

        private static void BuildSuckTrajectory(TMP_Text text, State state)
        {
            var textInfo = text.textInfo;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                {
                    state.suckCurveOffset[i] = Vector2.zero;
                    state.suckSpin[i] = 0f;
                    continue;
                }

                Vector2 charCenter = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;
                Vector2 toTarget = state.suckTarget - charCenter;
                float n1 = Noise01(i * 19 + 5);
                float n2 = Noise01(i * 23 + 11);
                float n3 = Noise01(i * 29 + 17);
                float n4 = Noise01(i * 37 + 7);

                Vector2 dir;
                if (toTarget.sqrMagnitude < 0.001f)
                {
                    float fallbackAngle = n1 * Mathf.PI * 2f;
                    dir = new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle));
                }
                else
                {
                    dir = toTarget.normalized;
                }

                Vector2 perp = new Vector2(-dir.y, dir.x);
                float side = n1 < 0.5f ? -1f : 1f;
                float distance = Mathf.Clamp(toTarget.magnitude, 30f, 420f);
                float curve = Mathf.Lerp(24f, Mathf.Min(160f, distance * 0.42f), n2);

                state.suckCurveOffset[i] = perp * side * curve + dir * Mathf.Lerp(-18f, 12f, n3);
                state.suckSpin[i] = Mathf.Lerp(-1.2f, 1.2f, n4);
            }
        }

        public static Tween DoConverge(this TMP_Text text, Vector2 targetLocal, float target, float duration)
        {
            var state = GetState(text);
            state.convergeTarget = targetLocal;

            float current = state.converge.Length > 0 ? state.converge[0] : 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.converge.Length; i++)
                    state.converge[i] = x;

                ApplyAll(text, state);
            }, target, duration).SetEase(Ease.InCubic);
        }

        public static Tween DoSuckInto(this TMP_Text text, TMP_Text targetText, float duration, float delayStep = 0.012f)
        {
            if (text == null || targetText == null)
                return null;

            var state = GetState(text);
            var textInfo = text.textInfo;
            int count = textInfo.characterCount;
            if (count <= 0)
                return null;

            state.suckTarget = GetTargetPointInLocalSpace(text, targetText);
            BuildSuckTrajectory(text, state);

            int[] indices = BuildShuffledIndices(count);
            float time = 0f;
            float perCharacterDuration = Mathf.Max(0.001f, duration);
            float stagger = Mathf.Max(0f, delayStep);
            float totalDuration = perCharacterDuration + stagger * (count - 1);

            return DOTween.To(() => time, x =>
            {
                time = x;

                for (int order = 0; order < count; order++)
                {
                    int i = indices[order];
                    if (!textInfo.characterInfo[i].isVisible)
                        continue;

                    float t = Mathf.Clamp01((time - order * stagger) / perCharacterDuration);
                    state.suck[i] = t;
                    state.shake[i] = Mathf.Sin(t * Mathf.PI) * Mathf.Lerp(0.4f, 1.6f, Noise01(i * 41 + 13));
                }

                ApplyAll(text, state);
            }, totalDuration, totalDuration).SetEase(Ease.Linear);
        }

        #endregion

        #region ===== LETTER DROP =====

        public static Tween DoLetterDrop(
            this TMP_Text text,
            int minCount,
            int maxCount,
            float duration,
            float damagePressure = 0f,
            float delayStep = 0.035f,
            Color? corrosionColor = null)
        {
            return DoLetterDropInternal(
                text,
                minCount,
                maxCount,
                duration,
                damagePressure,
                delayStep,
                false,
                true,
                corrosionColor);
        }

        public static Tween DoCorrodedLetterDrop(
            this TMP_Text text,
            int minCount,
            int maxCount,
            float duration,
            float damagePressure = 0f,
            float delayStep = 0.05f,
            Color? corrosionColor = null)
        {
            return DoLetterDropInternal(
                text,
                minCount,
                maxCount,
                duration,
                damagePressure,
                delayStep,
                true,
                true,
                corrosionColor);
        }

        public static Tween DoMonsterNearDeathCollapse(this TMP_Text text, float duration)
        {
            return DoLetterDropInternal(
                text,
                int.MaxValue,
                int.MaxValue,
                duration,
                1f,
                0.018f,
                true,
                true,
                new Color(0.03f, 0.01f, 0.06f, 1f));
        }

        private static Tween DoLetterDropInternal(
            TMP_Text text,
            int minCount,
            int maxCount,
            float duration,
            float damagePressure,
            float delayStep,
            bool corrodeFirst,
            bool leaveMissing,
            Color? corrosionColor)
        {
            if (text == null)
                return null;

            var state = GetState(text);
            if (corrosionColor.HasValue)
                state.corrosionColor = corrosionColor.Value;

            int[] indices = SelectLetterDropIndices(text, state, minCount, maxCount);
            if (indices == null || indices.Length == 0)
                return null;

            float pressure = Mathf.Clamp01(damagePressure);
            for (int i = 0; i < indices.Length; i++)
                PrepareLetterDrop(state, indices[i], pressure);

            float time = 0f;
            float perCharacterDuration = Mathf.Max(0.001f, duration);
            float stagger = Mathf.Max(0f, delayStep);
            float totalDuration = perCharacterDuration + stagger * (indices.Length - 1);

            return DOTween.To(() => time, x =>
                {
                    time = x;

                    for (int order = 0; order < indices.Length; order++)
                    {
                        int i = indices[order];
                        if (i < 0 || i >= state.letterDrop.Length)
                            continue;

                        float t = Mathf.Clamp01((time - order * stagger) / perCharacterDuration);
                        if (t <= 0f)
                            continue;

                        float corrosionRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, corrodeFirst ? 0.32f : 0.18f, t));
                        float corrosionFall = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, t));
                        float corrosion = corrosionRise * Mathf.Max(0.35f, corrosionFall);

                        state.letterDrop[i] = t;
                        state.letterCorrosion[i] = corrodeFirst
                            ? Mathf.Clamp01(corrosion)
                            : Mathf.Clamp01(corrosion * 0.55f);
                        state.letterDropShake[i] = Mathf.Sin(t * Mathf.PI) * Mathf.Lerp(0.7f, 2.8f, pressure);

                        if (leaveMissing)
                            state.letterMissing[i] = Mathf.Max(
                                state.letterMissing[i],
                                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1f, t)));
                    }

                    ApplyAll(text, state);
                }, totalDuration, totalDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        int index = indices[i];
                        if (index < 0 || index >= state.letterDrop.Length)
                            continue;

                        state.letterDrop[index] = 0f;
                        state.letterCorrosion[index] = 0f;
                        state.letterDropShake[index] = 0f;

                        if (leaveMissing)
                            state.letterMissing[index] = 1f;
                    }

                    ApplyAll(text, state);
                });
        }

        private static int[] SelectLetterDropIndices(TMP_Text text, State state, int minCount, int maxCount)
        {
            var textInfo = text.textInfo;
            if (textInfo == null || textInfo.characterCount <= 0)
                return new int[0];

            var available = new List<int>();
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                if (state.letterMissing != null &&
                    i < state.letterMissing.Length &&
                    state.letterMissing[i] >= 0.95f)
                    continue;

                available.Add(i);
            }

            if (available.Count == 0)
                return new int[0];

            int lower = Mathf.Clamp(minCount, 0, available.Count);
            int upper = Mathf.Clamp(Mathf.Max(maxCount, lower), lower, available.Count);
            if (upper <= 0)
                return new int[0];

            int count = UnityEngine.Random.Range(lower, upper + 1);
            for (int i = 0; i < available.Count; i++)
            {
                int rand = UnityEngine.Random.Range(i, available.Count);
                (available[i], available[rand]) = (available[rand], available[i]);
            }

            var selected = new int[count];
            for (int i = 0; i < count; i++)
                selected[i] = available[i];

            return selected;
        }

        private static void PrepareLetterDrop(State state, int index, float pressure)
        {
            if (state.letterDrop == null || index < 0 || index >= state.letterDrop.Length)
                return;

            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float lateral = UnityEngine.Random.Range(16f, 52f) * Mathf.Lerp(1f, 1.7f, pressure);
            float fall = UnityEngine.Random.Range(78f, 168f) * Mathf.Lerp(1f, 1.65f, pressure);
            float spin = UnityEngine.Random.Range(0.65f, 1.65f) * Mathf.Lerp(1f, 1.45f, pressure);

            state.letterDrop[index] = 0f;
            state.letterCorrosion[index] = 0f;
            state.letterDropShake[index] = 0f;
            state.letterDropOffset[index] = new Vector2(side * lateral, -fall);
            state.letterDropSpin[index] = side * spin;
        }

        #endregion

        #region ===== RANDOM COLLAPSE =====

        public static Tween DORandomCollapse(this TMP_Text text, float duration, float delayStep)
        {
            var state = GetState(text);
            var textInfo = text.textInfo;

            int count = textInfo.characterCount;

            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;

            for (int i = 0; i < count; i++)
            {
                int rand = UnityEngine.Random.Range(i, count);
                (indices[i], indices[rand]) = (indices[rand], indices[i]);
            }

            float time = 0f;
            float totalDuration = duration + delayStep * (count - 1);

            return DOTween.To(() => time, x =>
            {
                time = x;

                for (int order = 0; order < count; order++)
                {
                    int i = indices[order];
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    float t = Mathf.Clamp01((time - order * delayStep) / duration);

                    state.fall[i] = t;
                    state.rot[i] = t;
                }

                ApplyAll(text, state);
            }, totalDuration, totalDuration).SetEase(Ease.OutCubic);
        }

        #endregion

        #region ===== RANDOM MELT =====

        public static Tween DORandomMelt(this TMP_Text text, float target, float duration, float delayStep)
        {
            var state = GetState(text);
            var textInfo = text.textInfo;

            int count = textInfo.characterCount;

            int[] indices = new int[count];
            for (int i = 0; i < count; i++) 
                indices[i] = i;

            for (int i = 0; i < count; i++)
            {
                int rand = UnityEngine.Random.Range(i, count);
                (indices[i], indices[rand]) = (indices[rand], indices[i]);
            }

            float time = 0f;
            float totalDuration = duration + delayStep * (count - 1);

            return DOTween.To(() => time, x =>
            {
                time = x;

                for (int order = 0; order < count; order++)
                {
                    int i = indices[order];
                    if (!textInfo.characterInfo[i].isVisible) 
                        continue;

                    float t = Mathf.Clamp01((time - order * delayStep) / duration);
                    state.melt[i] = t * t * target;
                }

                ApplyAll(text, state);
            }, totalDuration, totalDuration);
        }

        #endregion

        #region ===== INK MONSTER =====

        // 잉크괴물 등장 연출
        //  Phase 1 (응집): 글자들이 랜덤하게 녹아내리며 중심-하단으로 수렴
        //  Phase 2 (형성): 검정으로 번지며 팽창하고 짧게 흔들린다. 전투 중 호흡은
        //  TMP 버텍스가 아니라 RectTransform 전체 스케일로 별도 처리한다.
        public static Sequence DoInkMonsterAppear(this TMP_Text text, float duration)
        {
            var state = GetState(text);
            var textInfo = text.textInfo;

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            int visibleCount = 0;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) 
                    continue;
                
                visibleCount++;

                var ci = textInfo.characterInfo[i];
                if (ci.bottomLeft.x < min.x) min.x = ci.bottomLeft.x;
                if (ci.bottomLeft.y < min.y) min.y = ci.bottomLeft.y;
                if (ci.topRight.x   > max.x) max.x = ci.topRight.x;
                if (ci.topRight.y   > max.y) max.y = ci.topRight.y;
            }

            Vector2 target = visibleCount > 0
                ? new Vector2((min.x + max.x) * 0.5f, min.y + (max.y - min.y) * 0.3f)
                : Vector2.zero;

            state.convergeTarget = target;

            float t1 = duration * 0.55f;
            float t2 = duration * 0.45f;

            var seq = DOTween.Sequence();

            // Phase 1 — 응집
            seq.Append(text.DORandomMelt(1.2f, t1, 0.025f));
            seq.Join(text.DoConverge(target, 0.75f, t1));

            // Phase 2 — 형성 (검정 번짐 + 팽창 + 짧은 충격)
            seq.Append(text.DoBleed(1f, t2));
            seq.Join(text.DoPulse(0.3f, t2));
            seq.Join(text.DoShake(4f, t2));
            seq.Append(text.DoShake(0f, 0.25f));

            return seq;
        }

        #endregion

        #region ===== RANDOM SHAKE =====

        public static Tween DORandomShake(this TMP_Text text, float strength, float duration, float delayStep)
        {
            var state = GetState(text);
            var textInfo = text.textInfo;

            int count = textInfo.characterCount;

            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;

            for (int i = 0; i < count; i++)
            {
                int rand = UnityEngine.Random.Range(i, count);
                (indices[i], indices[rand]) = (indices[rand], indices[i]);
            }

            float time = 0f;
            float totalDuration = duration + delayStep * (count - 1);

            return DOTween.To(() => time, x =>
            {
                time = x;

                for (int order = 0; order < count; order++)
                {
                    int i = indices[order];
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    float t = Mathf.Clamp01((time - order * delayStep) / duration);
                    float shakeValue = Mathf.Sin(t * Mathf.PI);

                    state.shake[i] = shakeValue * strength;
                }

                ApplyAll(text, state);
            }, totalDuration, totalDuration);
        }

        #endregion
    }
}
