using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProtocolArena.UI
{
    internal static class RuntimeUIFactory
    {
        private static Font font;
        public static readonly Color Background = new Color(0.018f, 0.025f, 0.045f, 1f);
        public static readonly Color Panel = new Color(0.045f, 0.06f, 0.095f, 0.98f);
        public static readonly Color Cyan = new Color(0.18f, 0.88f, 1f, 1f);
        public static readonly Color Orange = new Color(1f, 0.65f, 0.2f, 1f);

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    string[] koreanFonts =
                    {
                        "Apple SD Gothic Neo",
                        "Noto Sans CJK KR",
                        "Noto Sans KR",
                        "Malgun Gothic",
                        "Arial Unicode MS"
                    };
                    font = UnityEngine.Font.CreateDynamicFontFromOSFont(koreanFonts, 28);
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = color;
            Stretch(gameObject.GetComponent<RectTransform>());
            return gameObject;
        }

        public static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick, Color accent, int fontSize = 24)
        {
            GameObject gameObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            Button button = gameObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            ApplyButtonAccent(button, accent);
            Text text = gameObject.GetComponentInChildren<Text>();
            text.font = Font;
            text.text = label;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        public static void ApplyButtonAccent(Button button, Color accent)
        {
            button.targetGraphic.color = Color.white;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(accent.r * 0.55f, accent.g * 0.55f, accent.b * 0.55f, 1f);
            colors.highlightedColor = accent;
            colors.selectedColor = accent;
            colors.pressedColor = new Color(accent.r * 0.75f, accent.g * 0.75f, accent.b * 0.75f, 1f);
            button.colors = colors;
            Outline outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static RawImage CreateRawImage(Transform parent, string name, Texture texture, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            gameObject.transform.SetParent(parent, false);
            RawImage image = gameObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            return image;
        }

        public static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
