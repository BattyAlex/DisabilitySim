using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DisabilitySim
{
    /// <summary>
    /// Holds the list of available conditions and which one is currently active.
    /// Visual effects should listen to <see cref="ActiveConditionChanged"/> (or the UnityEvent)
    /// and turn themselves on/off. A null condition means normal vision.
    /// </summary>
    public class ConditionManager : MonoBehaviour
    {
        [SerializeField]
        List<VisualCondition> m_Conditions = new List<VisualCondition>();

        [SerializeField]
        UnityEvent<VisualCondition> m_OnConditionChanged = new UnityEvent<VisualCondition>();

        public IReadOnlyList<VisualCondition> Conditions => m_Conditions;

        public VisualCondition ActiveCondition { get; private set; }

        public UnityEvent<VisualCondition> OnConditionChanged => m_OnConditionChanged;

        public event Action<VisualCondition> ActiveConditionChanged;

        public void SetCondition(VisualCondition condition)
        {
            if (condition == ActiveCondition)
                return;

            ActiveCondition = condition;
            Debug.Log($"Active condition: {(condition != null ? condition.DisplayName : "None")}", this);

            ActiveConditionChanged?.Invoke(condition);
            m_OnConditionChanged.Invoke(condition);
        }

        public void ClearCondition() => SetCondition(null);
    }
}
