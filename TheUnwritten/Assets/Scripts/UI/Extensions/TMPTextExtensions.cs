using System;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

using DG.Tweening;
using TMPro;

namespace UI.Extensions
{
    public static class TMPTextExtensions
    {
        #region ===== ANIMATE (SHAKE + SKEW) =====

        public static async UniTask AnimateCharacterAsync(
            this TextMeshProUGUI tmp,
            int charIndex,
            float duration,
            float shakeStrength = 10f,
            float frequency = 50f,
            float skewStrength = 5f,
            Action onComplete = null)
        {
            if (tmp == null) 
                return;

            float elapsed = 0f;

            tmp.ForceMeshUpdate();
            var textInfo = tmp.textInfo;

            if (charIndex >= textInfo.characterCount) 
                return;

            var charInfo = textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) 
                return;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            var vertices = textInfo.meshInfo[matIndex].vertices;

            Vector3[] original = new Vector3[4];
            for (int i = 0; i < 4; i++)
                original[i] = vertices[vertIndex + i];

            while (elapsed < duration)
            {
                float t = Time.time * frequency;

                Vector3 shake = new(
                    Mathf.Sin(t) * shakeStrength,
                    Mathf.Cos(t * 0.8f) * shakeStrength,
                    0);

                float skew = Mathf.Sin(t * 0.5f) * skewStrength;

                vertices[vertIndex + 0] = original[0] + shake;
                vertices[vertIndex + 3] = original[3] + shake;

                vertices[vertIndex + 1] = original[1] + shake + new Vector3(skew, 0);
                vertices[vertIndex + 2] = original[2] + shake + new Vector3(skew, 0);

                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

                await UniTask.Yield();
                elapsed += Time.deltaTime;
            }

            for (int i = 0; i < 4; i++)
                vertices[vertIndex + i] = original[i];

            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            onComplete?.Invoke();
        }

        #endregion
    }
}