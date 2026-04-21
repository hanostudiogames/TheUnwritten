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

            public Vector3[][] originalVertices;
            public Color32[][] originalColors;
            public int length;

            public Vector2 convergeTarget;
            public Vector2[] scatterOffset;
            public Color bleedColor = Color.black;
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

            if (state.length != count)
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
                state.scatterOffset = new Vector2[count];
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

                // 🔥 melt (글자 상단 고정, 하단이 늘어져 내림)
                float topY = charInfo.topLeft.y;
                float bottomY = charInfo.bottomLeft.y;
                float charHeight = Mathf.Max(topY - bottomY, 0.0001f);
                float dripPhase = Mathf.Sin(i * 1.37f) + Mathf.Sin(i * 0.53f) * 0.5f;

                // 🔥 fold ease
                float ft = 1f - Mathf.Pow(1f - fold, 3f);

                // 🔥 rotation
                float angle = rot * 90f;
                float rad = angle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                // 🔥 shake (시간 기반)
                float shakeX = Mathf.Sin(Time.time * 60f + i) * shake;
                float shakeY = Mathf.Cos(Time.time * 50f + i) * shake;

                // 🔥 pulse (pivot 기준 균일 스케일)
                float pulseScale = 1f + pulse;

                for (int j = 0; j < 4; j++)
                {
                    int idx = vertIndex + j;

                    Vector3 v = original[idx];
                    Vector3 dir = v - pivot;

                    // fold
                    dir.y *= (1f - ft);

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

                    // pulse (pivot 기준 스케일)
                    rx *= pulseScale;
                    ry *= pulseScale;

                    vertices[idx] = pivot + convergeOffset + new Vector3(rx, ry, 0f);

                    // bleed (vertex color를 bleedColor 쪽으로 블렌딩, 알파는 원본 유지)
                    if (bleed > 0f)
                    {
                        Color baseCol = originalColors[idx];
                        Color target = state.bleedColor;
                        target.a = baseCol.a;
                        colors[idx] = Color.Lerp(baseCol, target, bleed);
                    }
                    else
                    {
                        colors[idx] = originalColors[idx];
                    }
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
            if (state.scatterOffset != null)
                Array.Clear(state.scatterOffset, 0, state.scatterOffset.Length);

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

        public static Tween DoShake(this TMP_Text text, float strength, float duration)
        {
            var state = GetState(text);
            float current = 0f;

            return DOTween.To(() => current, x =>
            {
                current = x;
                for (int i = 0; i < state.shake.Length; i++)
                    state.shake[i] = x;

                ApplyAll(text, state);
            }, strength, duration).SetEase(Ease.InOutSine);
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
        //  Phase 2 (형성): 검정으로 번지며 팽창 + 떨림 → 덩어리 실루엣이 "숨쉬는" 상태
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
            seq.Join(text.DoConverge(target, 1f, t1));

            // Phase 2 — 형성 (검정 번짐 + 팽창 + 떨림)
            seq.Append(text.DoBleed(1f, t2));
            seq.Join(text.DoPulse(0.3f, t2));
            seq.Join(text.DoShake(4f, t2));

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