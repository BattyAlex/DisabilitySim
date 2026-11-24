using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DisabilitySim.Editor
{
    /// <summary>
    /// Adds the welcome / controls text to the "Tutorial board" in the open scene.
    /// Run it from Tools > DisabilitySim > Create Tutorial Board Text, then save the scene.
    /// Running it again replaces the old text.
    /// </summary>
    static class TutorialBoardTextCreator
    {
        const string k_BoardName = "Tutorial board";
        const string k_TextName = "Tutorial Board Text";

        // How much of the board face the text may use.
        const float k_FaceFill = 0.85f;

        // Moves the text down by this fraction of the board height (keep below (1 - k_FaceFill) / 2).
        const float k_DownShift = 0.06f;

        // Gap between the board surface and the text so they don't flicker (meters).
        const float k_SurfaceOffset = 0.01f;

        const string k_BoardText =
            "<size=160%><b>Welcome!</b></size>\n" +
            "This room lets you experience how some visual conditions affect everyday life. " +
            "Look around, pick up objects and try reading, then switch conditions and see how it changes.\n" +
            "\n" +
            "<size=130%><b>Controls</b></size>\n" +
            "• <b>Open / close the condition menu:</b> Menu button on the left controller\n" +
            "• <b>Pick a condition:</b> point at the menu and press the trigger\n" +
            "• <b>Grab objects:</b> hold the grip button\n" +
            "• <b>Read the book:</b> pick it up and tap the page edges or the Next / Previous buttons\n" +
            "• <b>Move:</b> left thumbstick\n" +
            "• <b>Turn:</b> right thumbstick";

        [MenuItem("Tools/DisabilitySim/Create Tutorial Board Text")]
        static void CreateBoardText()
        {
            var board = GameObject.Find(k_BoardName);
            if (board == null)
            {
                EditorUtility.DisplayDialog("Tutorial board text", $"Could not find a GameObject named \"{k_BoardName}\" in the open scene.", "OK");
                return;
            }

            var boardRenderer = board.GetComponentInChildren<Renderer>();
            if (boardRenderer == null)
            {
                EditorUtility.DisplayDialog("Tutorial board text", $"\"{k_BoardName}\" has no renderer to measure.", "OK");
                return;
            }

            // Put it next to the board (not under it) so the board's non-uniform scale doesn't stretch the text.
            var parent = board.transform.parent;
            var old = parent != null ? parent.Find(k_TextName) : GameObject.Find(k_TextName)?.transform;
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            var bounds = boardRenderer.bounds;
            var size = bounds.size;

            // The thinnest axis of the board is the direction its face points.
            var normal = size.x <= size.y && size.x <= size.z ? Vector3.right
                : size.z <= size.y ? Vector3.forward
                : Vector3.up;

            // Use the side that faces the player / room.
            var viewer = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            if (Vector3.Dot(viewer - bounds.center, normal) < 0f)
                normal = -normal;

            var up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
            var right = Vector3.Cross(up, normal);
            var faceWidth = Mathf.Abs(Vector3.Dot(size, right));
            var faceHeight = Mathf.Abs(Vector3.Dot(size, up));
            var faceDepth = Mathf.Abs(Vector3.Dot(size, normal));

            var go = new GameObject(k_TextName);
            Undo.RegisterCreatedObjectUndo(go, "Create Tutorial Board Text");
            go.layer = board.layer;
            if (parent != null)
                go.transform.SetParent(parent, false);

            // TextMeshPro reads correctly when looking along its +Z, so +Z points into the board.
            go.transform.SetPositionAndRotation(
                bounds.center + normal * (faceDepth * 0.5f + k_SurfaceOffset) - up * (faceHeight * k_DownShift),
                Quaternion.LookRotation(-normal, up));
            go.transform.localScale = Vector3.one;

            var text = go.AddComponent<TextMeshPro>();
            text.rectTransform.sizeDelta = new Vector2(faceWidth * k_FaceFill, faceHeight * k_FaceFill);
            text.text = k_BoardText;
            text.color = new Color(0.12f, 0.12f, 0.14f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0.5f;
            text.fontSizeMax = 3f;
            text.lineSpacing = 10f;
            text.paragraphSpacing = 6f;

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log($"Created \"{k_TextName}\" on \"{k_BoardName}\". Save the scene to keep it.", go);
        }
    }
}
