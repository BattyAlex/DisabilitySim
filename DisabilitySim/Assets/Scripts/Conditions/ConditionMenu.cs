using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DisabilitySim
{
    /// <summary>
    /// World-space menu that lists the conditions from a <see cref="ConditionManager"/> as radio buttons.
    /// Hidden by default; the toggle action shows it in front of the user's head and hides it again.
    /// </summary>
    public class ConditionMenu : MonoBehaviour
    {
        [SerializeField]
        ConditionManager m_ConditionManager;

        [Header("UI")]
        [SerializeField, Tooltip("The object that is shown/hidden (usually the Canvas).")]
        GameObject m_Panel;

        [SerializeField, Tooltip("Inactive row that is cloned once per condition.")]
        Toggle m_OptionTemplate;

        [SerializeField]
        ToggleGroup m_OptionGroup;

        [SerializeField]
        TMP_Text m_DescriptionText;

        [SerializeField]
        string m_NoneLabel = "None (normal vision)";

        [Header("Showing / hiding")]
        [SerializeField]
        InputActionProperty m_ToggleMenuAction = new InputActionProperty(CreateDefaultToggleAction());

        [SerializeField]
        bool m_StartVisible;

        [SerializeField, Tooltip("Defaults to the main camera.")]
        Transform m_Head;

        [SerializeField, Tooltip("How far in front of the head the menu appears (meters).")]
        float m_Distance = 0.8f;

        [SerializeField, Tooltip("Height of the menu origin relative to the head (meters).")]
        float m_VerticalOffset = -0.15f;

        readonly List<(Toggle toggle, VisualCondition condition)> m_Options = new List<(Toggle, VisualCondition)>();

        public bool IsVisible => m_Panel != null && m_Panel.activeSelf;

        static InputAction CreateDefaultToggleAction()
        {
            var action = new InputAction("Toggle Condition Menu", InputActionType.Button);
            action.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            action.AddBinding("<Keyboard>/m");
            return action;
        }

        void OnEnable()
        {
            var action = m_ToggleMenuAction.action;
            if (action == null)
                return;

            action.performed += OnTogglePerformed;
            action.Enable();
        }

        void OnDisable()
        {
            var action = m_ToggleMenuAction.action;
            if (action == null)
                return;

            action.performed -= OnTogglePerformed;
            // Actions from an asset are owned by the InputActionManager, only disable our own.
            if (m_ToggleMenuAction.reference == null)
                action.Disable();
        }

        void Start()
        {
            if (m_ConditionManager == null)
            {
                Debug.LogError("ConditionMenu has no ConditionManager assigned.", this);
                enabled = false;
                return;
            }

            BuildOptions();
            m_ConditionManager.ActiveConditionChanged += OnActiveConditionChanged;
            OnActiveConditionChanged(m_ConditionManager.ActiveCondition);

            if (m_StartVisible)
                Show();
            else
                Hide();
        }

        void OnDestroy()
        {
            if (m_ConditionManager != null)
                m_ConditionManager.ActiveConditionChanged -= OnActiveConditionChanged;
        }

        public void Show()
        {
            PlaceInFrontOfHead();
            m_Panel.SetActive(true);
        }

        public void Hide() => m_Panel.SetActive(false);

        public void ToggleVisible()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        void OnTogglePerformed(InputAction.CallbackContext context) => ToggleVisible();

        void BuildOptions()
        {
            m_OptionTemplate.gameObject.SetActive(false);

            CreateOption(m_NoneLabel, null);
            foreach (var condition in m_ConditionManager.Conditions)
            {
                if (condition != null)
                    CreateOption(condition.DisplayName, condition);
            }
        }

        void CreateOption(string label, VisualCondition condition)
        {
            var toggle = Instantiate(m_OptionTemplate, m_OptionTemplate.transform.parent);
            toggle.name = $"Option ({label})";
            toggle.gameObject.SetActive(true);

            var text = toggle.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;

            toggle.group = m_OptionGroup;
            toggle.SetIsOnWithoutNotify(condition == m_ConditionManager.ActiveCondition);
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    m_ConditionManager.SetCondition(condition);
            });

            m_Options.Add((toggle, condition));
        }

        void OnActiveConditionChanged(VisualCondition active)
        {
            foreach (var (toggle, condition) in m_Options)
                toggle.SetIsOnWithoutNotify(condition == active);

            if (m_DescriptionText != null)
                m_DescriptionText.text = active != null ? active.Description : string.Empty;
        }

        void PlaceInFrontOfHead()
        {
            var head = m_Head;
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
            if (head == null)
                return;

            // Only use the yaw so the menu stays upright even if the user is looking up/down.
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(head.forward.y < 0f ? head.up : -head.up, Vector3.up);
            forward.Normalize();

            var position = head.position + forward * m_Distance + Vector3.up * m_VerticalOffset;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }
    }
}
