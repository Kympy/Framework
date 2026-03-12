using System.Collections.Generic;
using UnityEngine;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        // ── 팔레트 ────────────────────────────────────────────────────
        private static readonly Color BG_COLOR = new Color(0.13f, 0.13f, 0.15f);
        private static readonly Color GRID_FINE = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color GRID_COARSE = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color INSPECTOR_BG = new Color(0.17f, 0.17f, 0.20f);
        private static readonly Color BORDER_DEFAULT = new Color(0.40f, 0.40f, 0.50f);
        private static readonly Color BORDER_SELECTED = new Color(0.25f, 0.75f, 1.00f);
        private static readonly Color BORDER_PATH = Color.orangeRed;
        private static readonly Color CONNECTION_PLAYING = Color.orangeRed;
        private static readonly Color CONNECTION_DEFAULT = new Color(0.4f, 0.95f, 0.45f, 0.8f);
        private static readonly Color CONNECTION_CONDITION_TRUE = new Color(0.4f, 0.55f, 0.9f, 0.8f);
        private static readonly Color CONNECTION_CONDITION_FALSE = new Color(0.95f, 0.45f, 0.45f, 0.8f);
        private static readonly Color CONNECTION_CHOICE = new Color(1f, 0.85f, 0.25f, 0.8f);

        private static readonly Dictionary<DialogueNodeType, Color> NODE_COLORS =
            new Dictionary<DialogueNodeType, Color>
            {
                { DialogueNodeType.Start, new Color(0.15f, 0.48f, 0.22f) },
                { DialogueNodeType.Character, new Color(0.28f, 0.42f, 0.30f) },
                { DialogueNodeType.Narration, new Color(0.38f, 0.28f, 0.48f) },
                { DialogueNodeType.ChapterEnd, new Color(0.50f, 0.18f, 0.18f) },
                { DialogueNodeType.Condition, new Color(0.45f, 0.35f, 0.12f) },
            };

        private static readonly Dictionary<DialogueNodeType, string> NODE_ICONS =
            new Dictionary<DialogueNodeType, string>
            {
                { DialogueNodeType.Start, "▶ START" },
                { DialogueNodeType.Character, "🗣 CHARACTER" },
                { DialogueNodeType.Narration, "📖 NARRATION" },
                { DialogueNodeType.ChapterEnd, "■ CHAPTER END" },
                { DialogueNodeType.Condition, "? CONDITION" },
            };
    }
}
