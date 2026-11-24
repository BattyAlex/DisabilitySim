using UnityEngine;

namespace DisabilitySim
{
    /// <summary>
    /// A <see cref="VisualCondition"/> that simulates a type of color vision deficiency.
    /// Applied on screen by <see cref="ColorBlindnessEffect"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Color Blindness", menuName = "DisabilitySim/Color Blindness Condition")]
    public class ColorBlindnessCondition : VisualCondition
    {
        public enum ColorBlindnessType
        {
            Protanopia,
            Deuteranopia,
            Tritanopia,
            Achromatopsia,
        }

        [SerializeField]
        ColorBlindnessType m_Type;

        [SerializeField, Range(0f, 1f), Tooltip("0 = normal vision, 1 = full deficiency.")]
        float m_Severity = 1f;

        public ColorBlindnessType Type => m_Type;

        public float Severity => m_Severity;

        /// <summary>
        /// Rows of the 3x3 simulation matrix for linear RGB.
        /// Protanopia/Deuteranopia/Tritanopia use Machado, Oliveira &amp; Fernandes (2009) at severity 1.0.
        /// </summary>
        public Vector3[] GetMatrixRows()
        {
            switch (m_Type)
            {
                case ColorBlindnessType.Protanopia:
                    return new[]
                    {
                        new Vector3(0.152286f, 1.052583f, -0.204868f),
                        new Vector3(0.114503f, 0.786281f, 0.099216f),
                        new Vector3(-0.003882f, -0.048116f, 1.051998f),
                    };
                case ColorBlindnessType.Deuteranopia:
                    return new[]
                    {
                        new Vector3(0.367322f, 0.860646f, -0.227968f),
                        new Vector3(0.280085f, 0.672501f, 0.047413f),
                        new Vector3(-0.011820f, 0.042940f, 0.968881f),
                    };
                case ColorBlindnessType.Tritanopia:
                    return new[]
                    {
                        new Vector3(1.255528f, -0.076749f, -0.178779f),
                        new Vector3(-0.078411f, 0.930809f, 0.147602f),
                        new Vector3(0.004733f, 0.691367f, 0.303900f),
                    };
                default: // Achromatopsia: luminance only
                    var luminance = new Vector3(0.2126f, 0.7152f, 0.0722f);
                    return new[] { luminance, luminance, luminance };
            }
        }
    }
}
