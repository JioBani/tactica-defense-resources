using UnityEngine;

namespace Common.Scripts.BubbleMessage
{
    public struct BubbleMessageParams
    {
        public readonly Color? Color;
        public readonly float? FontSize;
        public readonly float? Duration;
        public readonly float? FloatDistance;

        public BubbleMessageParams(
            Color? color = null,
            float? fontSize = null,
            float? duration = null,
            float? floatDistance = null
        )
        {
            Color = color;
            FontSize = fontSize;
            Duration = duration;
            FloatDistance = floatDistance;
        }
    }
}
