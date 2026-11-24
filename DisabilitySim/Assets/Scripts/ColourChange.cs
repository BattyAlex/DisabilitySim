using UnityEngine;

public class ColourChange : MonoBehaviour
{
    [SerializeField]
    public Color pebbleColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.GetComponent<Renderer>().material.color = pebbleColor;
    }
}
