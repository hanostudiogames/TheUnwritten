using UnityEngine;
using TMPro;

namespace UI.Components
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextEffect : MonoBehaviour
    {
        private TMP_Text _tmpText;

        [Header("Wave")]
        public float waveAmplitude = 5f;
        public float waveFrequency = 0.05f;
        public float waveSpeed = 10f;

        [Header("Shake")]
        public float shakeStrength = 2f;
        public float shakeSpeed = 50f;

        private void Awake()
        {
            _tmpText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            _tmpText.ForceMeshUpdate();
            TMP_TextInfo textInfo = _tmpText.textInfo;

            foreach (TMP_LinkInfo linkInfo in textInfo.linkInfo)
            {
                string id = linkInfo.GetLinkID();

                for (int i = linkInfo.linkTextfirstCharacterIndex;
                     i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength;
                     i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    if (!charInfo.isVisible)
                        continue;

                    int materialIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;

                    Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                    Vector3 offset = Vector3.zero;

                    // 🎯 Wave
                    if (id == "wave")
                    {
                        float xPos = vertices[vertexIndex].x;
                        float wave = Mathf.Sin(Time.time * waveSpeed + xPos * waveFrequency) * waveAmplitude;
                        offset += new Vector3(0, wave, 0);
                    }

                    // 🎯 Shake
                    else if (id == "shake")
                    {
                        float time = Time.time * shakeSpeed;

                        offset += new Vector3(
                            Mathf.Sin(time + i) * shakeStrength,
                            Mathf.Cos(time * 0.8f + i) * shakeStrength,
                            0);
                    }

                    // 적용
                    vertices[vertexIndex + 0] += offset;
                    vertices[vertexIndex + 1] += offset;
                    vertices[vertexIndex + 2] += offset;
                    vertices[vertexIndex + 3] += offset;
                }
            }

            // 적용
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                _tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}