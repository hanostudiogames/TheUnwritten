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

            public Vector3[][] originalVertices;
            public int length;
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
                state.length = count;

                state.originalVertices = new Vector3[textInfo.meshInfo.Length][];

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    var src = textInfo.meshInfo[i].vertices;
                    state.originalVertices[i] = new Vector3[src.Length];
                    Array.Copy(src, state.originalVertices[i], src.Length);
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
                var original = state.originalVertices[matIndex];

                float fold = state.fold[i];
                float shear = state.shear[i];
                float fall = state.fall[i];
                float rot = state.rot[i];
                float shake = state.shake[i];

                Vector3 pivot = (charInfo.bottomLeft + charInfo.topRight) * 0.5f;

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

                    vertices[idx] = pivot + new Vector3(rx, ry, 0f);
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
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