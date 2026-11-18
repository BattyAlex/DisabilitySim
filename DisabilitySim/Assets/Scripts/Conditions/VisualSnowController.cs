using DisabilitySim;
using UnityEngine;
using UnityEngine.UI;

public class VisualSnowController : MonoBehaviour
{
    [SerializeField]
    ConditionManager m_ConditionManager;
    public Canvas blackBoardText;
    public GameObject visualSnowEffect;
    public Material test;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_ConditionManager.ActiveConditionChanged += OnConditionChanged;
    }

    // Update is called once per frame
    void Update()
    {
        
        // I need to make it so that the plane follows the camera... Camera.main.transform?
    }

    void OnConditionChanged(VisualCondition condition)
    {
        
        Debug.Log("Hi. Condition changed to:" + condition);
        if(condition.DisplayName.Equals("Visual Snow"))
        {
            Debug.Log("Visual Snow in effect");
            blackBoardText.gameObject.SetActive(true);
            visualSnowEffect.SetActive(true);

            test.SetFloat("visualSnowAlphaValue", 0.9f);
        }
        else
        {
           blackBoardText.gameObject.SetActive(false); 
           visualSnowEffect.SetActive(false);
        }
        
    }

    
}
