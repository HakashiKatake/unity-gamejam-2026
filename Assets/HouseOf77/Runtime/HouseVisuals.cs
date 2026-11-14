using UnityEngine;

namespace HouseOf77
{
    public static class HouseVisuals
    {
        public static readonly Color Gold = new Color(1f, .77f, .35f);
        public static readonly Color Blue = new Color(.38f, .85f, 1);
        public static readonly Color Red = new Color(1, .3f, .43f);
        static Sprite white;
        public static Sprite White
        {
            get
            {
                if (!white) white = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), Texture2D.whiteTexture.width);
                return white;
            }
        }
        public static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Material material, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = material; sr.sortingOrder = order;
            return sr;
        }
        public static LineRenderer Line(string name, Transform parent, Material material, Vector3[] points, Color color, float width, int order = -50, bool loop = false)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var l = go.AddComponent<LineRenderer>(); l.useWorldSpace = false; l.sharedMaterial = material;
            l.startColor = l.endColor = color; l.startWidth = l.endWidth = width; l.positionCount = points.Length;
            l.SetPositions(points); l.loop = loop; l.sortingOrder = order; return l;
        }
        public static LineRenderer Circle(string name, Transform parent, Material material, float radius, Color color, float width, int order = -50)
        {
            var p = new Vector3[96];
            for (int i = 0; i < p.Length; i++) { float a = i * Mathf.PI * 2 / p.Length; p[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius; }
            return Line(name, parent, material, p, color, width, order, true);
        }
        public static SpriteRenderer Shadow(Transform parent, GameConfig config)
        {
            var s = Sprite("Ground shadow", parent, White, config.ringMaterial, -20);
            s.transform.localScale = new Vector3(1.05f, .35f, 1); s.color = new Color(0, 0, 0, .48f);
            var b = new MaterialPropertyBlock(); b.SetFloat("_Fill", 1); s.SetPropertyBlock(b); return s;
        }
        public static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad; return new Vector2(vector.x * Mathf.Cos(a) - vector.y * Mathf.Sin(a), vector.x * Mathf.Sin(a) + vector.y * Mathf.Cos(a));
        }
        public static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a; float t = Mathf.Clamp01(Vector2.Dot(point - a, d) / Mathf.Max(.0001f, d.sqrMagnitude)); return Vector2.Distance(point, a + d * t);
        }
    }
    public sealed class HousePool<T> where T : Component
    {
        public readonly T[] Items;
        public HousePool(int size, string name, Transform parent, System.Action<T> initialize)
        {
            Items = new T[size];
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject(name + " " + i); go.transform.SetParent(parent, false);
                T item = go.AddComponent<T>(); initialize(item); go.SetActive(false); Items[i] = item;
            }
        }
        public T Get()
        {
            for (int i = 0; i < Items.Length; i++) if (!Items[i].gameObject.activeSelf) { Items[i].gameObject.SetActive(true); return Items[i]; }
            return null;
        }
        public void Clear() { foreach (var item in Items) item.gameObject.SetActive(false); }
    }
}
