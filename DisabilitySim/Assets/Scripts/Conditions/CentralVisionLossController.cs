using DisabilitySim;
using UnityEngine;
using UnityEngine.UI;

public class CentralVisionLossController : MonoBehaviour
{
    static readonly int k_RadiusId = Shader.PropertyToID("_Radius");

    [SerializeField]
    ConditionManager m_ConditionManager;
    [SerializeField, Tooltip("The condition asset that turns this effect on.")]
    VisualCondition centralVisionLossCondition;
    [SerializeField, Tooltip("Canvas used to display the slider and the text.")]
    Canvas blackBoardText;
    [SerializeField, Tooltip("The quad in front of the camera that draws the blind spot.")]
    GameObject centralVisionLossEffect;
    [SerializeField, Tooltip("Slider for setting the size of the blind spot.")]
    Slider scotomaSize;
    [SerializeField, Tooltip("Smallest blind spot radius, in degrees from the centre of view.")]
    float minValue = 3f;
    [SerializeField, Tooltip("Largest blind spot radius, in degrees from the centre of view.")]
    float maxValue = 30f;

    Material m_Material;

    void Start()
    {
        m_ConditionManager.ActiveConditionChanged += OnConditionChanged;

        m_Material = centralVisionLossEffect.GetComponent<Renderer>().material;

        scotomaSize.minValue = minValue;
        scotomaSize.maxValue = maxValue;
        scotomaSize.value = m_Material.GetFloat(k_RadiusId);
        scotomaSize.onValueChanged.AddListener(value => m_Material.SetFloat(k_RadiusId, value));

        OnConditionChanged(m_ConditionManager.ActiveCondition);
    }

    void OnDestroy()
    {
        if (m_ConditionManager != null)
            m_ConditionManager.ActiveConditionChanged -= OnConditionChanged;
    }

    void OnConditionChanged(VisualCondition condition)
    {
        var active = condition != null && condition == centralVisionLossCondition;
        blackBoardText.gameObject.SetActive(active);
        centralVisionLossEffect.SetActive(active);
    }
}
