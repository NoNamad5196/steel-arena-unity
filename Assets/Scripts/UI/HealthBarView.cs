using UnityEngine;
using UnityEngine.UI;

namespace ProtocolArena.UI
{
    internal sealed class HealthBarView
    {
        private readonly Image fill;
        private readonly Text label;

        public HealthBarView(Transform parent, string name, Color color, Vector2 position)
        {
            GameObject root = RuntimeUIFactory.CreatePanel(parent, name, new Color(0.02f, 0.025f, 0.04f, 1f));
            RuntimeUIFactory.SetRect(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, new Vector2(470f, 58f));
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);

            fill = RuntimeUIFactory.CreateImage(root.transform, "Fill", color);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = new Vector2(5f, 5f);
            fill.rectTransform.offsetMax = new Vector2(-5f, -5f);

            label = RuntimeUIFactory.CreateText(root.transform, "Label", string.Empty, 21, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.Stretch(label.rectTransform);
        }

        public void SetValue(string owner, float current, float maximum)
        {
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            fill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            label.text = $"{owner}  {current:0} / {maximum:0}";
        }
    }
}
