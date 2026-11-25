using DisabilitySim;
using UnityEngine;
using UnityEngine.UI;

public class VisualSnowController : MonoBehaviour
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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_ConditionManager.ActiveConditionChanged += OnConditionChanged;

        migraineIntensity.minValue = minValue;
        migraineIntensity.maxValue = maxValue;

        visualSnowMaterial.SetFloat("_visualSnowAlphaValue", 0.2f);
        migraineIntensity.value = visualSnowMaterial.GetFloat("_visualSnowAlphaValue");
    }

    // Update is called once per frame
    void Update()
    {
        if(blackBoardText.isActiveAndEnabled)
        {
            visualSnowMaterial.SetFloat("_visualSnowAlphaValue", migraineIntensity.value);
        }
        
        // I need to make it so that the plane follows the camera... Camera.main.transform?
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
