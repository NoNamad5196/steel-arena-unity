using UnityEngine;
using UnityEngine.UI;

namespace ProtocolArena.UI
{
    internal sealed class FighterView
    {
        private readonly RectTransform root;
        private readonly Image body;
        private readonly Color baseColor;
        private readonly float attackDirection;
        private float attackTimer;
        private float hitTimer;
        private float healTimer;

        public FighterView(Transform parent, string name, Color color, bool facesRight)
        {
            baseColor = color;
            attackDirection = facesRight ? 1f : -1f;
            var rootObject = new GameObject(name, typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            root = rootObject.GetComponent<RectTransform>();
            RuntimeUIFactory.SetRect(root, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(130f, 170f));

            body = RuntimeUIFactory.CreateImage(root, "Body", color);
            RuntimeUIFactory.SetRect(body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(82f, 95f));
            Image head = RuntimeUIFactory.CreateImage(root, "Head", Color.Lerp(color, Color.white, 0.22f));
            RuntimeUIFactory.SetRect(head.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(62f, 52f));
            Image visor = RuntimeUIFactory.CreateImage(head.transform, "Visor", new Color(0.02f, 0.04f, 0.07f, 1f));
            RuntimeUIFactory.SetRect(visor.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(44f, 13f));
            CreateLimb(root, "Left Arm", color, new Vector2(-54f, 81f), new Vector2(26f, 64f));
            CreateLimb(root, "Right Arm", color, new Vector2(54f, 81f), new Vector2(26f, 64f));
            CreateLimb(root, "Left Leg", color, new Vector2(-23f, 19f), new Vector2(28f, 62f));
            CreateLimb(root, "Right Leg", color, new Vector2(23f, 19f), new Vector2(28f, 62f));
        }

        public void SetPosition(float worldX)
        {
            float normalized = Mathf.InverseLerp(-3.7f, 3.7f, worldX);
            root.anchorMin = new Vector2(normalized, 0f);
            root.anchorMax = new Vector2(normalized, 0f);
            root.anchoredPosition = new Vector2(0f, 28f);
        }

        public void SetFlash(float amount)
        {
            float hit = Mathf.Clamp01(amount);
            float heal = Mathf.Clamp01(healTimer * 4f);
            body.color = Color.Lerp(Color.Lerp(baseColor, Color.white, hit), new Color(0.35f, 1f, 0.48f, 1f), heal);
        }

        public void PlayAttack()
        {
            attackTimer = 0.24f;
        }

        public void PlayHit()
        {
            hitTimer = 0.22f;
        }

        public void PlayHeal()
        {
            healTimer = 0.48f;
        }

        public void Tick(float deltaTime, float flash)
        {
            attackTimer = Mathf.MoveTowards(attackTimer, 0f, deltaTime);
            hitTimer = Mathf.MoveTowards(hitTimer, 0f, deltaTime);
            healTimer = Mathf.MoveTowards(healTimer, 0f, deltaTime);

            float attackPhase = attackTimer <= 0f ? 0f : Mathf.Sin((1f - attackTimer / 0.24f) * Mathf.PI);
            float shake = hitTimer <= 0f ? 0f : Mathf.Sin(hitTimer * 115f) * 10f * (hitTimer / 0.22f);
            float heal = healTimer <= 0f ? 0f : Mathf.Sin((1f - healTimer / 0.48f) * Mathf.PI);
            root.anchoredPosition = new Vector2(attackPhase * 42f * attackDirection + shake, 28f + heal * 10f);
            root.localScale = Vector3.one * (1f + Mathf.Clamp01(flash) * 0.06f + heal * 0.12f);
            SetFlash(flash);
        }

        private static void CreateLimb(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            Image limb = RuntimeUIFactory.CreateImage(parent, name, Color.Lerp(color, Color.black, 0.12f));
            RuntimeUIFactory.SetRect(limb.rectTransform, new Vector2(0.5f, 0f), position, size);
        }
    }
}
