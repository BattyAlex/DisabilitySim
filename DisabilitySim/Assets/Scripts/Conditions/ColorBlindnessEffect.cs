using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DisabilitySim
{
    /// <summary>
    /// Turns a URP Full Screen Pass Renderer Feature on while a <see cref="ColorBlindnessCondition"/>
    /// is active, and feeds the condition's matrix to the feature's material.
    /// The feature's Pass Material should use the "DisabilitySim/ColorBlindness" shader.
    /// </summary>
    public class ColorBlindnessEffect : MonoBehaviour
    {
        static readonly int k_Row0 = Shader.PropertyToID("_Row0");
        static readonly int k_Row1 = Shader.PropertyToID("_Row1");
        static readonly int k_Row2 = Shader.PropertyToID("_Row2");
        static readonly int k_Severity = Shader.PropertyToID("_Severity");

        [SerializeField]
        ConditionManager m_ConditionManager;

        [SerializeField, Tooltip("Full Screen Pass Renderer Feature on the URP Renderer asset. Leave it disabled in the asset.")]
        FullScreenPassRendererFeature m_Feature;

        void OnEnable()
        {
            if (m_ConditionManager == null || m_Feature == null)
            {
                Debug.LogError("ColorBlindnessEffect needs a ConditionManager and a Full Screen Pass Renderer Feature.", this);
                enabled = false;
                return;
            }

            m_ConditionManager.ActiveConditionChanged += Apply;
            Apply(m_ConditionManager.ActiveCondition);
        }

        void OnDisable()
        {
            if (m_ConditionManager != null)
                m_ConditionManager.ActiveConditionChanged -= Apply;

            // The feature lives on an asset, so its state outlives Play mode. Always reset it.
            if (m_Feature != null)
                m_Feature.SetActive(false);
        }

        void Apply(VisualCondition condition)
        {
            if (condition is ColorBlindnessCondition colorBlindness && m_Feature.passMaterial != null)
            {
                var rows = colorBlindness.GetMatrixRows();
                var material = m_Feature.passMaterial;
                material.SetVector(k_Row0, rows[0]);
                material.SetVector(k_Row1, rows[1]);
                material.SetVector(k_Row2, rows[2]);
                material.SetFloat(k_Severity, colorBlindness.Severity);
                m_Feature.SetActive(true);
            }
            else
            {
                m_Feature.SetActive(false);
            }
        }
    }
}
