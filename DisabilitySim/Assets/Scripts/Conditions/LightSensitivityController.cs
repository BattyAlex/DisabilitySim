using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DisabilitySim
{
    public class LightSensitivityController : MonoBehaviour
    {
        static readonly int k_BrightnessId = Shader.PropertyToID("_Brightness");
        static readonly float[] k_Levels = { -0.25f, 0f, 0.35f, 0.7f };
        static readonly string[] k_Labels = { "Dim", "Normal", "Bright", "Brighter" };
        const float k_TransitionSpeed = 0.3f;

        [SerializeField] ConditionManager m_ConditionManager;
        [SerializeField] VisualCondition m_LightSensitivity;
        [SerializeField] FullScreenPassRendererFeature[] m_RendererFeatures;
        [SerializeField] Material m_ExposureMaterial;
        [SerializeField] RectTransform m_MenuContent;
        [SerializeField] TMP_Text m_MenuTextStyle;

        Material m_RuntimeMaterial;
        GameObject m_Controls;
        TMP_Text m_Status;
        TMP_Text m_PauseLabel;
        Button m_Decrease;
        Button m_Increase;
        float m_Brightness;
        int m_Level;
        bool m_Paused;
        bool m_Selected;

        void OnEnable()
        {
            if (m_ConditionManager == null || m_LightSensitivity == null ||
                m_ExposureMaterial == null || m_MenuContent == null || m_MenuTextStyle == null ||
                m_RendererFeatures == null || m_RendererFeatures.Length == 0)
            {
                Debug.LogError("Light Sensitivity is missing a scene reference.", this);
                enabled = false;
                return;
            }

            m_RuntimeMaterial = new Material(m_ExposureMaterial);
            foreach (var feature in m_RendererFeatures)
            {
                if (feature != null)
                    feature.passMaterial = m_RuntimeMaterial;
            }

            if (m_Controls == null)
                BuildControls();

            m_ConditionManager.ActiveConditionChanged += OnConditionChanged;
            OnConditionChanged(m_ConditionManager.ActiveCondition);
        }

        void OnDisable()
        {
            if (m_RuntimeMaterial == null)
                return;

            m_ConditionManager.ActiveConditionChanged -= OnConditionChanged;
            m_Controls.SetActive(false);
            SetRendering(false);
            foreach (var feature in m_RendererFeatures)
            {
                if (feature != null && feature.passMaterial == m_RuntimeMaterial)
                    feature.passMaterial = m_ExposureMaterial;
            }

            Destroy(m_RuntimeMaterial);
            m_RuntimeMaterial = null;
            m_Brightness = 0f;
        }

        void OnDestroy()
        {
            if (m_Controls != null)
                Destroy(m_Controls);
        }

        void OnConditionChanged(VisualCondition condition)
        {
            m_Selected = condition == m_LightSensitivity;
            m_Controls.SetActive(m_Selected);
            ResetExposure();
        }

        void Update()
        {
            if (m_Paused)
                return;

            float target = m_Selected ? k_Levels[m_Level] : 0f;
            // Limit each frame's step so a long frame cannot cause a brightness jump.
            float step = k_TransitionSpeed * Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float previous = m_Brightness;
            m_Brightness = Mathf.MoveTowards(m_Brightness, target, step);
            if (m_Brightness == previous)
                return;

            m_RuntimeMaterial.SetFloat(k_BrightnessId, m_Brightness);
            SetRendering(m_Brightness != 0f);
        }

        void SetRendering(bool active)
        {
            foreach (var feature in m_RendererFeatures)
            {
                if (feature != null && feature.isActive != active)
                    feature.SetActive(active);
            }
        }

        void ChangeLevel(int step)
        {
            m_Level = Mathf.Clamp(m_Level + step, 0, k_Levels.Length - 1);
            UpdateControls();
        }

        void ResetExposure()
        {
            m_Level = 0;
            m_Paused = false;
            UpdateControls();
        }

        void UpdateControls()
        {
            string status = m_Paused ? "Paused. Target" : "Target";
            m_Status.text = $"{status}: {k_Labels[m_Level]} ({m_Level + 1}/{k_Levels.Length})";
            m_PauseLabel.text = m_Paused ? "Resume" : "Pause";
            m_Decrease.interactable = m_Level > 0;
            m_Increase.interactable = m_Level < k_Levels.Length - 1;
        }

        void BuildControls()
        {
            var layout = CreateElement<VerticalLayoutGroup>("Light Sensitivity Controls", m_MenuContent);
            m_Controls = layout.gameObject;
            m_Controls.transform.SetSiblingIndex(m_MenuTextStyle.transform.GetSiblingIndex() + 1);
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;

            m_Status = CreateLabel("Exposure status", layout.transform);
            var row = CreateElement<HorizontalLayoutGroup>("Exposure buttons", layout.transform);
            row.spacing = 6f;
            m_Decrease = CreateButton("Dimmer", row.transform, () => ChangeLevel(-1));
            m_Increase = CreateButton("Brighter", row.transform, () => ChangeLevel(1));
            var pause = CreateButton("Pause", row.transform, () =>
            {
                m_Paused = !m_Paused;
                UpdateControls();
            });
            m_PauseLabel = pause.GetComponentInChildren<TMP_Text>();
            CreateButton("Reset", row.transform, ResetExposure);
        }

        Button CreateButton(string label, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var image = CreateElement<Image>(label, parent);
            image.color = new Color(0.35f, 0.35f, 0.35f);
            image.gameObject.AddComponent<LayoutElement>().minHeight = 36f;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = CreateLabel(label, image.transform);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        TMP_Text CreateLabel(string name, Transform parent)
        {
            var text = CreateElement<TextMeshProUGUI>(name, parent);
            text.font = m_MenuTextStyle.font;
            text.fontSize = 18f;
            text.raycastTarget = false;
            return text;
        }

        static T CreateElement<T>(string name, Transform parent) where T : Component
        {
            var element = new GameObject(name, typeof(RectTransform));
            element.layer = parent.gameObject.layer;
            element.transform.SetParent(parent, false);
            return element.AddComponent<T>();
        }
    }
}
