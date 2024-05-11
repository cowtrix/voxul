using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul.Utilities
{
    [Serializable]
    public struct SerializableGradient
    {
        [Serializable]
        public struct ColorKey
        {
            [ColorUsage(false, true)]
            public Color Color;
            public float Time;

            public ColorKey(GradientColorKey k)
            {
                Color = k.color;
                Time = k.time;
            }
            public GradientColorKey ToColorKey() => new GradientColorKey { color = Color, time = Time };
        }

        [Serializable]
        public struct AlphaKey
        {
            [Range(0, 1)]
            public float Alpha;
            public float Time;

            public AlphaKey(GradientAlphaKey k)
            {
                Alpha = k.alpha;
                Time = k.time;
            }
            public GradientAlphaKey ToAlphaKey() => new GradientAlphaKey { alpha = Alpha, time = Time };
        }

        public ColorKey[] colorKeys;
        public AlphaKey[] alphaKeys;
        public GradientMode mode;

        public SerializableGradient(Gradient gradient)
        {
            colorKeys = gradient.colorKeys.Select(s => new ColorKey(s)).Distinct(c => c.Color).ToArray();
            alphaKeys = gradient.alphaKeys.Select(s => new AlphaKey(s)).Distinct(c => c.Alpha).ToArray();
            mode = gradient.mode;
        }

        public SerializableGradient(Color c)
        {
            colorKeys = new ColorKey[] { new ColorKey { Color = c } };
            alphaKeys = new AlphaKey[] { new AlphaKey { Alpha = c.a } };
            mode = GradientMode.Blend;
        }

        public Gradient ToGradient() => new Gradient
        {
            colorKeys = colorKeys == null ? new GradientColorKey[0] : colorKeys.Select(s => s.ToColorKey()).Distinct(c => c.color).ToArray(),
            alphaKeys = alphaKeys == null ? new GradientAlphaKey[0] : alphaKeys.Select(s => s.ToAlphaKey()).Distinct(c => c.alpha).ToArray(),
            mode = mode,
        };

        public override bool Equals(object obj)
        {
            return obj is SerializableGradient gradient &&
                   EqualityComparer<ColorKey[]>.Default.Equals(colorKeys, gradient.colorKeys) &&
                   EqualityComparer<AlphaKey[]>.Default.Equals(alphaKeys, gradient.alphaKeys) &&
                   mode == gradient.mode;
        }

        public Color Evaluate(float value)
        {
            var alpha = 1f;
            if (alphaKeys.Any())
            {
                AlphaKey lastAlphaKey = alphaKeys[0];
                alpha = lastAlphaKey.Alpha;
                for (int i = 1; i < alphaKeys.Length; i++)
                {
                    var k = alphaKeys[i];
                    if (k.Time >= value)
                    {
                        var fracTime = Mathf.Clamp01((value - lastAlphaKey.Time) / (k.Time - lastAlphaKey.Time));
                        alpha = Mathf.Lerp(lastAlphaKey.Alpha, k.Alpha, fracTime);
                    }
                    lastAlphaKey = k;
                }
            }
            
            ColorKey lastColorKey = colorKeys[0];
            Color color = lastColorKey.Color;
            for (int i = 1; i < colorKeys.Length; i++)
            {
                var k = colorKeys[i];
                if (k.Time >= value)
                {
                    var fracTime = Mathf.Clamp01((value - lastColorKey.Time) / (k.Time - lastColorKey.Time));
                    color = Color.Lerp(lastColorKey.Color, k.Color, fracTime);
                }
                lastColorKey = k;
            }
            return color.WithAlpha(alpha);
        }

        public override int GetHashCode()
        {
            int hashCode = 1860550585;
            hashCode = hashCode * -1521134295 + EqualityComparer<ColorKey[]>.Default.GetHashCode(colorKeys);
            hashCode = hashCode * -1521134295 + EqualityComparer<AlphaKey[]>.Default.GetHashCode(alphaKeys);
            hashCode = hashCode * -1521134295 + mode.GetHashCode();
            return hashCode;
        }
    }
}