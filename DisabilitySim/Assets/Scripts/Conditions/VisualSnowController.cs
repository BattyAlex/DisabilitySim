using DisabilitySim;
using UnityEngine;

public class VisualSnowController : MonoBehaviour
{
    [SerializeField]
    ConditionManager m_ConditionManager;
    
    public Shader material;
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

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(material == null)
        {
            Debug.Log("Material not found.");
            Graphics.Blit(source, destination);
            return;
        }

        //Graphics.Blit(source, destination, material);
    }

    void OnConditionChanged(VisualCondition condition)
    {
        
        Debug.Log("Hi. Condition changed to:" + condition);
        if (Camera.main != null)
        {
            
        }
        
    }
}
