using DisabilitySim;
using UnityEngine;

public class VisualSnowController : MonoBehaviour
{
    [SerializeField]
    ConditionManager m_ConditionManager;
    //private ConditionManager conditionManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_ConditionManager.ActiveConditionChanged += OnConditionChanged;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnConditionChanged(VisualCondition condition)
    {
        Debug.Log("Hi. Condition changed.");
    }
}
