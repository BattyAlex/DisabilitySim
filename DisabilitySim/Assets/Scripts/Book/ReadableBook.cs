using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DisabilitySim
{
    /// <summary>
    /// Puts readable text on the "Book (Open)" model and turns its pages.
    /// While held, trigger on the right hand goes forward and on the left hand goes back.
    /// The pages themselves can be clicked with the free hand: right page for next, left page for previous.
    /// The buttons below the book work both on the table and in the hand. N and B work on a keyboard.
    /// The page curve below matches that specific mesh; the book's spine runs along local Z.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ReadableBook : MonoBehaviour
    {
        // Cross section of a page in meters: x = distance from the spine, y = height.
        static readonly Vector2[] k_Profile =
        {
            new Vector2(0.0000f, -0.0018f),
            new Vector2(0.0289f, 0.0115f),
            new Vector2(0.0632f, 0.0171f),
            new Vector2(0.1254f, 0.0038f),
            new Vector2(0.2675f, -0.0010f),
        };

        const float k_PageBottom = -0.1505f;
        const float k_PageTop = 0.1519f;
        const float k_SheetInnerEdge = 0.01f;
        const int k_SheetColumns = 24;

        // Text column, measured from the spine.
        const float k_TextCenter = 0.163f;
        static readonly Vector2 k_TextSize = new Vector2(0.18f, 0.27f);

        // Text is laid out this much larger and scaled back down, as TMP's auto sizing is too coarse for tiny font sizes.
        const float k_TextLayoutScale = 100f;

        // Small offsets that keep the layers from overlapping.
        const float k_RestingLift = 0.001f;
        const float k_SheetLift = 0.0025f;
        const float k_TextOffset = 0.001f;

        // UV of the paper colour in the Home Set palette texture.
        static readonly Vector2 k_PaperUV = new Vector2(0.048f, 0.877f);

        // Just below the vision effects (Transparent - 1 and up) so they also cover the text.
        const int k_TextRenderQueue = 2990;

        [SerializeField, TextArea(6, 16), Tooltip("One entry per page. The first page is on the left.")]
        string[] m_Pages =
        {
            "<b>The Lighthouse Keeper's Garden</b>\n\nEvery morning, before the gulls were awake, Marta climbed the ninety-two steps to the top of the lighthouse and put out the lamp.\n\nThen she climbed back down and went to see what the wind had done to her garden.",
            "It was a small garden, tucked behind a wall of grey stones. Nobody in the village believed anything could grow there.\n\n\"Too much salt,\" said the baker.\n\n\"Too much wind,\" said the ferryman.\n\nMarta said nothing at all, and planted peas.",
            "The first spring, the peas came up yellow and lay down flat.\n\nThe second spring, she built the wall higher and carried soil up from the valley, one bucket at a time.\n\nBy June there were three green pods. She ate them slowly, sitting on the step.",
            "The third spring, a letter arrived. The lamp would soon run by itself, it said, and a keeper was no longer needed.\n\nMarta read it twice. She folded it into a small square and used it to prop open the garden gate.",
            "On her last night, a storm came in from the west. The new lamp flickered once and went dark.\n\nMarta took the old lantern from its hook. She climbed the ninety-two steps and held the light up to the glass until morning.",
            "At dawn the ferryman knocked on her door. Behind him stood the baker, and behind the baker stood half the village.\n\n\"We saw your light,\" he said. \"We would all be at the bottom of the bay.\"\n\nNobody knew what to say after that.",
            "So Marta showed them the garden.\n\nThey walked between the rows in their wet boots, past the peas and the beans and one very small apple tree.\n\n\"Well,\" said the baker at last. \"I suppose the salt is good for them.\"",
            "Marta stayed. The lamp runs by itself now, most of the time.\n\nBut if you pass the bay before the gulls are awake, you may still see a lantern moving slowly along the wall, stopping now and then to look at the peas.\n\n<align=center>The End</align>",
        };

        [SerializeField, Tooltip("Largest text size. Long pages shrink to fit, down to half of this.")]
        float m_FontSize = 0.16f;
        [SerializeField, Range(0f, 0.5f), Tooltip("Makes the letters thicker. 0 is the normal font weight.")]
        float m_TextThickness = 0.2f;
        [SerializeField]
        Color m_TextColor = new Color(0.02f, 0.02f, 0.02f);
        [SerializeField, Tooltip("How long one page turn takes (seconds).")]
        float m_FlipDuration = 0.6f;
        [SerializeField, Tooltip("Move the book back to where it started when it is let go.")]
        bool m_ReturnWhenReleased = true;
        [SerializeField]
        float m_ReturnDuration = 0.4f;

        XRGrabInteractable m_Grab;
        Material m_TextMaterial;
        Mesh m_SheetMesh;
        Vector3[] m_SheetVertices;
        GameObject m_Sheet;
        TextMeshPro m_LeftText;
        TextMeshPro m_RightText;
        TextMeshPro m_SheetFrontText;
        TextMeshPro m_SheetBackText;
        Button m_PreviousButton;
        Button m_NextButton;
        TMP_Text m_PageLabel;
        GameObject m_PageTouchAreas;
        InputAction m_NextAction;
        InputAction m_PreviousAction;

        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        Coroutine m_ReturnRoutine;
        Coroutine m_FlipRoutine;
        float m_FlipProgress; // 0 = turning sheet lies on the right, 1 = on the left
        int m_Spread; // index of the open pair of pages

        int SpreadCount => Mathf.Max(1, (m_Pages.Length + 1) / 2);

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            gameObject.AddComponent<BookHoldTransformer>();
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;

            BuildPages();
            BuildButtons();
            BuildPageTouchAreas();
            ShowSpread();

            m_NextAction = new InputAction("Next Page", InputActionType.Button, "<Keyboard>/n");
            m_NextAction.performed += _ => NextPage();
            m_PreviousAction = new InputAction("Previous Page", InputActionType.Button, "<Keyboard>/b");
            m_PreviousAction.performed += _ => PreviousPage();
        }

        void OnEnable()
        {
            m_Grab.activated.AddListener(OnActivated);
            m_Grab.selectEntered.AddListener(OnGrabbed);
            m_Grab.selectExited.AddListener(OnReleased);
            m_NextAction.Enable();
            m_PreviousAction.Enable();
            ShowSpread();
        }

        void OnDisable()
        {
            m_Grab.activated.RemoveListener(OnActivated);
            m_Grab.selectEntered.RemoveListener(OnGrabbed);
            m_Grab.selectExited.RemoveListener(OnReleased);
            m_NextAction.Disable();
            m_PreviousAction.Disable();

            // Stopped coroutines never finish, so settle the page here.
            StopAllCoroutines();
            m_PageTouchAreas.SetActive(false);
            m_ReturnRoutine = null;
            m_FlipRoutine = null;
            if (m_Sheet != null)
                m_Sheet.SetActive(false);
        }

        void OnDestroy()
        {
            m_NextAction?.Dispose();
            m_PreviousAction?.Dispose();
            Destroy(m_TextMaterial);
            Destroy(m_SheetMesh);
        }

        public void NextPage() => Flip(1);

        public void PreviousPage() => Flip(-1);

        void OnActivated(ActivateEventArgs args)
        {
            if (args.interactorObject.handedness == InteractorHandedness.Left)
                PreviousPage();
            else
                NextPage();
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_PageTouchAreas.SetActive(true);
            if (m_ReturnRoutine != null)
                StopCoroutine(m_ReturnRoutine);
            m_ReturnRoutine = null;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            m_PageTouchAreas.SetActive(m_Grab.isSelected);
            if (m_ReturnWhenReleased && !m_Grab.isSelected && !args.isCanceled && isActiveAndEnabled)
                m_ReturnRoutine = StartCoroutine(ReturnHome());
        }

        IEnumerator ReturnHome()
        {
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            for (var time = 0f; time < m_ReturnDuration; time += Time.deltaTime)
            {
                var t = Mathf.SmoothStep(0f, 1f, time / m_ReturnDuration);
                transform.SetPositionAndRotation(Vector3.Lerp(startPosition, m_HomePosition, t), Quaternion.Slerp(startRotation, m_HomeRotation, t));
                yield return null;
            }

            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            m_ReturnRoutine = null;
        }

        void Flip(int direction)
        {
            var target = m_Spread + direction;
            if (m_FlipRoutine != null || !isActiveAndEnabled || target < 0 || target >= SpreadCount)
                return;

            m_FlipRoutine = StartCoroutine(FlipTo(target));
        }

        IEnumerator FlipTo(int target)
        {
            var forward = target > m_Spread;
            var lower = Mathf.Min(m_Spread, target);
            m_Spread = target;
            m_FlipProgress = forward ? 0f : 1f;

            // The turning sheet carries the right page of the earlier pair and the left page of the later one.
            m_Sheet.SetActive(true);
            SetPage(m_LeftText, lower * 2);
            SetPage(m_SheetFrontText, lower * 2 + 1);
            SetPage(m_SheetBackText, lower * 2 + 2);
            SetPage(m_RightText, lower * 2 + 3);
            UpdateSheet();
            UpdateButtons();

            for (var time = 0f; time < m_FlipDuration; time += Time.deltaTime)
            {
                var t = Mathf.SmoothStep(0f, 1f, time / m_FlipDuration);
                m_FlipProgress = forward ? t : 1f - t;
                UpdateSheet();
                yield return null;
            }

            m_FlipRoutine = null;
            ShowSpread();
        }

        void ShowSpread()
        {
            m_Sheet.SetActive(false);
            SetPage(m_LeftText, m_Spread * 2);
            SetPage(m_RightText, m_Spread * 2 + 1);
            UpdateButtons();
        }

        void UpdateButtons()
        {
            m_PreviousButton.interactable = m_Spread > 0;
            m_NextButton.interactable = m_Spread < SpreadCount - 1;
            m_PageLabel.text = $"{m_Spread + 1} / {SpreadCount}";
        }

        void SetPage(TextMeshPro text, int page)
        {
            text.text = page < m_Pages.Length ? m_Pages[page] : string.Empty;
            text.ForceMeshUpdate();
        }

        static float PageHeight(float fromSpine)
        {
            for (var i = 1; i < k_Profile.Length; i++)
            {
                if (fromSpine <= k_Profile[i].x)
                    return Mathf.Lerp(k_Profile[i - 1].y, k_Profile[i].y, Mathf.InverseLerp(k_Profile[i - 1].x, k_Profile[i].x, fromSpine));
            }

            return k_Profile[k_Profile.Length - 1].y;
        }

        /// <summary>
        /// Position on a page, turned around the spine: progress 0 rests on the right, 1 on the left.
        /// </summary>
        static Vector3 PagePoint(float fromSpine, float z, float progress, float lift, float normalOffset)
        {
            var height = PageHeight(fromSpine) + lift;
            var radius = Mathf.Sqrt(fromSpine * fromSpine + height * height);
            var angle = Mathf.Atan2(height, fromSpine);
            angle = Mathf.Lerp(angle, Mathf.PI - angle, progress);

            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);
            return new Vector3(radius * cos - sin * normalOffset, radius * sin + cos * normalOffset, z);
        }

        void BuildPages()
        {
            m_TextMaterial = new Material(TMP_Settings.defaultFontAsset.material) { renderQueue = k_TextRenderQueue };
            // TMP blurs text whose normals point away from the viewer, and warping leaves the normals unchanged.
            m_TextMaterial.SetFloat("_PerspectiveFilter", 0f);
            m_TextMaterial.SetFloat("_FaceDilate", m_TextThickness);

            m_LeftText = CreateText("Left Page Text", transform);
            m_LeftText.OnPreRenderText += info => WarpText(info, true, 1f, k_RestingLift, 0f);
            m_RightText = CreateText("Right Page Text", transform);
            m_RightText.OnPreRenderText += info => WarpText(info, false, 0f, k_RestingLift, 0f);

            m_Sheet = new GameObject("Turning Page", typeof(MeshFilter), typeof(MeshRenderer));
            m_Sheet.layer = gameObject.layer;
            m_Sheet.transform.SetParent(transform, false);
            m_Sheet.GetComponent<MeshRenderer>().sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
            BuildSheetMesh();
            m_Sheet.GetComponent<MeshFilter>().sharedMesh = m_SheetMesh;

            m_SheetFrontText = CreateText("Front Text", m_Sheet.transform);
            m_SheetFrontText.OnPreRenderText += info => WarpText(info, false, m_FlipProgress, k_SheetLift, k_TextOffset);
            m_SheetBackText = CreateText("Back Text", m_Sheet.transform);
            m_SheetBackText.OnPreRenderText += info => WarpText(info, true, m_FlipProgress, k_SheetLift, -k_TextOffset);
        }

        TextMeshPro CreateText(string objectName, Transform parent)
        {
            var go = new GameObject(objectName);
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshPro>();
            text.rectTransform.localScale = Vector3.one / k_TextLayoutScale;
            text.rectTransform.sizeDelta = k_TextSize * k_TextLayoutScale;
            text.fontSharedMaterial = m_TextMaterial;
            text.color = m_TextColor;
            text.enableAutoSizing = true;
            text.fontSizeMin = m_FontSize * 0.5f * k_TextLayoutScale;
            text.fontSizeMax = m_FontSize * k_TextLayoutScale;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }

        // Text is laid out flat by TMP, then every vertex is moved onto the curved page.
        static void WarpText(TMP_TextInfo info, bool leftPage, float progress, float lift, float normalOffset)
        {
            var centerZ = (k_PageBottom + k_PageTop) / 2f;
            for (var i = 0; i < info.materialCount; i++)
            {
                var vertices = info.meshInfo[i].vertices;
                for (var v = 0; v < info.meshInfo[i].vertexCount; v++)
                {
                    var flat = vertices[v] / k_TextLayoutScale;
                    var fromSpine = leftPage ? k_TextCenter - flat.x : k_TextCenter + flat.x;
                    vertices[v] = PagePoint(fromSpine, centerZ + flat.y, progress, lift, normalOffset) * k_TextLayoutScale;
                }
            }
        }

        void BuildSheetMesh()
        {
            // Two copies of the same grid, one facing each way.
            var perSide = (k_SheetColumns + 1) * 2;
            m_SheetVertices = new Vector3[perSide * 2];
            var uvs = new Vector2[perSide * 2];
            for (var i = 0; i < uvs.Length; i++)
                uvs[i] = k_PaperUV;

            var triangles = new int[k_SheetColumns * 12];
            for (int c = 0, t = 0; c < k_SheetColumns; c++)
            {
                var a = c * 2;
                var b = perSide + c * 2;
                foreach (var index in new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3, b, b + 2, b + 1, b + 2, b + 3, b + 1 })
                    triangles[t++] = index;
            }

            m_SheetMesh = new Mesh { name = "Turning Page" };
            m_SheetMesh.MarkDynamic();
            m_SheetMesh.vertices = m_SheetVertices;
            m_SheetMesh.uv = uvs;
            m_SheetMesh.triangles = triangles;
        }

        void UpdateSheet()
        {
            var perSide = m_SheetVertices.Length / 2;
            var outerEdge = k_Profile[k_Profile.Length - 1].x;
            for (var c = 0; c <= k_SheetColumns; c++)
            {
                var fromSpine = Mathf.Lerp(k_SheetInnerEdge, outerEdge, c / (float)k_SheetColumns);
                var bottom = PagePoint(fromSpine, k_PageBottom, m_FlipProgress, k_SheetLift, 0f);
                var top = PagePoint(fromSpine, k_PageTop, m_FlipProgress, k_SheetLift, 0f);
                m_SheetVertices[c * 2] = m_SheetVertices[perSide + c * 2] = bottom;
                m_SheetVertices[c * 2 + 1] = m_SheetVertices[perSide + c * 2 + 1] = top;
            }

            m_SheetMesh.vertices = m_SheetVertices;
            m_SheetMesh.RecalculateNormals();
            m_SheetMesh.RecalculateBounds();

            m_SheetFrontText.ForceMeshUpdate();
            m_SheetBackText.ForceMeshUpdate();
        }

        void BuildButtons()
        {
            // Lies flat just below the bottom edge of the book.
            var rect = CreateCanvas("Page Buttons", new Vector2(400f, 44f), new Vector3(0f, 0f, -0.2f));

            m_PreviousButton = CreateButton("Previous", rect, -130f);
            m_PreviousButton.onClick.AddListener(PreviousPage);
            m_NextButton = CreateButton("Next", rect, 130f);
            m_NextButton.onClick.AddListener(NextPage);

            // Dark label plate so the number is readable against any table.
            var plate = CreateUIObject("Page Number", rect);
            ((RectTransform)plate.transform).sizeDelta = new Vector2(100f, 40f);
            var plateImage = plate.AddComponent<Image>();
            plateImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            plateImage.raycastTarget = false;
            m_PageLabel = CreateLabel(plate.transform, new Vector2(100f, 40f));
        }

        // Nearly invisible buttons over both pages. Only there while the book is held, as they would block grabbing it.
        void BuildPageTouchAreas()
        {
            var rect = CreateCanvas("Page Touch Areas", new Vector2(540f, 304f), new Vector3(0f, 0.025f, 0f));
            m_PageTouchAreas = rect.gameObject;
            CreateTouchArea("Left Page", rect, -135f).onClick.AddListener(PreviousPage);
            CreateTouchArea("Right Page", rect, 135f).onClick.AddListener(NextPage);
            m_PageTouchAreas.SetActive(false);
        }

        static Button CreateTouchArea(string objectName, Transform parent, float x)
        {
            var go = CreateUIObject(objectName, parent);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(266f, 304f);
            rect.anchoredPosition = new Vector2(x, 0f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = go.AddComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = colors.selectedColor = colors.disabledColor = new Color(1f, 1f, 1f, 0.01f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.16f);
            button.colors = colors;
            return button;
        }

        // Canvas lying flat on the book. 1 canvas unit = 1 mm.
        RectTransform CreateCanvas(string objectName, Vector2 size, Vector3 position)
        {
            var go = CreateUIObject(objectName, transform);
            go.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * 0.001f;
            rect.localPosition = position;
            rect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return rect;
        }

        static Button CreateButton(string label, Transform parent, float x)
        {
            var go = CreateUIObject(label, parent);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(130f, 40f);
            rect.anchoredPosition = new Vector2(x, 0f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = go.AddComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = colors.selectedColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.95f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 0.95f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            button.colors = colors;

            CreateLabel(go.transform, rect.sizeDelta).text = label;
            return button;
        }

        static TMP_Text CreateLabel(Transform parent, Vector2 size)
        {
            var text = CreateUIObject("Label", parent).AddComponent<TextMeshProUGUI>();
            text.rectTransform.sizeDelta = size;
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static GameObject CreateUIObject(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
