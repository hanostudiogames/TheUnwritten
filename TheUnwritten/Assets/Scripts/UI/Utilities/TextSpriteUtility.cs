using UnityEngine;

using System.Text;
using System.Text.RegularExpressions;
using TMPro;

namespace UI.Utilities
{
    public static class TextSpriteUtility
    {
        public static string ConvertToSpriteText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = text.ToUpper();
            var sb = new StringBuilder(text.Length * 8);

            foreach (var c in text)
            {
                if (c == ' ')
                {
                    sb.Append(' ');
                    continue;
                }

                if ((c >= 'A' && c <= 'Z') || c == '?' || c == '!')
                    sb.Append($"<sprite name=\"{c}\">");
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static string ReplaceSpriteAt(string text, int spriteIndex, string replacement)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            int current = 0;
            bool replaced = false;

            return Regex.Replace(text, @"<sprite\s+name=""(.*?)"">", m =>
            {
                if (!replaced && current == spriteIndex)
                {
                    replaced = true;
                    current++;
                    return replacement;
                }

                current++;
                
                return m.Value;
            });
        }

        public static void ReplaceSpriteAt(this TextMeshProUGUI tmp, int index, string replacement)
        {
            if (tmp == null || string.IsNullOrEmpty(tmp.text))
                return;

            tmp.SetText(ReplaceSpriteAt(tmp.text, index, replacement));
        }
    }
}

