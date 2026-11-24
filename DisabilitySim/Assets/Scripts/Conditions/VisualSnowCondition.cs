using UnityEngine;
using UnityEngine.UI;

namespace DisabilitySim
{
    public class VisualSnowCondition : VisualCondition
    {
        [SerializeField]
        ConditionManager m_ConditionManager;
        [SerializeField, Tooltip("Canvas used to display the slider and the text.")]
        Canvas blackBoardText;
        [SerializeField, Tooltip("The plane responsible for creating the visual effect.")]
        GameObject visualSnowEffect;
        [SerializeField, Tooltip("The material used by the visual effect.")]
        Material visualSnowMaterial;
        [SerializeField, Tooltip("Slider for setting the migraine's intensity.")]
        Slider migraineIntensity;
        [SerializeField, Tooltip("Minimum value of the slider. If changed, the shader's values must be changed as well.")]
        float minValue = 0.05f;
        [SerializeField, Tooltip("Maximum value of the slider. If changed, the shader's values must be changed as well.")]
        float maxValue = 0.9f;

        void Start()
        {
            m_ConditionManager.ActiveConditionChanged += OnConditionChanged;

            migraineIntensity.minValue = minValue;
            migraineIntensity.maxValue = maxValue;

            migraineIntensity.value = visualSnowMaterial.GetFloat("_visualSnowAlphaValue");
        }

        void Update()
        {
            if(blackBoardText.isActiveAndEnabled)
            {
                visualSnowMaterial.SetFloat("_visualSnowAlphaValue", migraineIntensity.value);
            }
        }

        void OnConditionChanged(VisualCondition condition)
        {
            if(condition != null && condition.DisplayName.Equals("Visual Snow"))
            {
                blackBoardText.gameObject.SetActive(true);
                visualSnowEffect.SetActive(true);
            }
            else
            {
            blackBoardText.gameObject.SetActive(false); 
            visualSnowEffect.SetActive(false);
            }
        }
    }

    
}