using UnityEngine;

namespace DisabilitySim
{
    /// <summary>
    /// Data for one selectable visual condition. The visual effect itself is added later;
    /// for now this only holds what the selection menu needs to show.
    /// </summary>
    [CreateAssetMenu(fileName = "New Visual Condition", menuName = "DisabilitySim/Visual Condition")]
    public class VisualCondition : ScriptableObject
    {
        [SerializeField]
        string m_DisplayName;

        [SerializeField, TextArea(2, 5)]
        string m_Description;

        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;

        public string Description => m_Description;
    }
}
