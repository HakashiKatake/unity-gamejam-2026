using UnityEngine;

namespace HouseOf77
{
    public sealed class HouseEffects : MonoBehaviour
    {
        HousePool<HouseEffect> pool;
        HousePool<HouseDamageText> numbers;
        ParticleSystem coins;
        public void Initialize(GameConfig config)
        {
            pool = new HousePool<HouseEffect>(160, "Effect", transform, fx => fx.Initialize(config));
            numbers = new HousePool<HouseDamageText>(24, "Damage", transform, fx => fx.Initialize());
            var go = new GameObject("Jackpot coins"); go.transform.SetParent(transform);
            coins = go.AddComponent<ParticleSystem>();
            var main = coins.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 150;
            main.startLifetime = .85f; main.startSpeed = 5; main.startSize = .075f;
            main.startColor = HouseVisuals.Gold; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .3f;
            var emission = coins.emission; emission.enabled = false;
            var shape = coins.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .4f;
            var renderer = coins.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = config.spriteMaterial; renderer.sortingOrder = 1500;
            coins.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void Ring(Vector2 position, Color color, float radius = 1, float life = .4f)
        {
            var fx = pool.Get(); if (fx) fx.Activate(position, Vector2.zero, color, radius, life, true);
        }
        public void Sparks(Vector2 position, Color color, int count = 6)
        {
            for (int i = 0; i < count; i++)
            {
                var fx = pool.Get(); if (!fx) break;
                fx.Activate(position, Random.insideUnitCircle.normalized * Random.Range(1.5f, 4.5f), color, Random.Range(.055f, .13f), Random.Range(.18f, .45f), false);
            }
        }
        public void Number(Vector2 position, int value, Color color)
        {
            var number = numbers.Get(); if (number) number.Activate(position, value, color);
        }
        public void Jackpot(Vector2 position)
        {
            Ring(position, HouseVisuals.Gold, 7, .85f); Sparks(position, HouseVisuals.Gold, 40);
            coins.transform.position = position; coins.Emit(90);
        }
        public void Tick(float dt)
        {
            foreach (var fx in pool.Items) if (fx.gameObject.activeSelf) fx.Tick(dt);
            foreach (var number in numbers.Items) if (number.gameObject.activeSelf) number.Tick(dt);
        }
        public void Clear()
        {
            pool.Clear(); numbers.Clear(); coins.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    public sealed class HouseEffect : MonoBehaviour
    {
        SpriteRenderer sprite; Material ring, spark; float age, life, size; bool isRing; Vector2 velocity; Color color;
        public void Initialize(GameConfig config)
        {
            ring = config.ringMaterial; spark = config.spriteMaterial;
            sprite = HouseVisuals.Sprite("Shape", transform, HouseVisuals.White, ring, 1500);
        }
        public void Activate(Vector2 position, Vector2 movement, Color tint, float radius, float duration, bool circle)
        {
            transform.position = position; transform.rotation = Quaternion.identity;
            velocity = movement; color = tint; size = radius; life = duration; age = 0; isRing = circle;
            sprite.sharedMaterial = circle ? ring : spark; sprite.color = tint; Tick(0);
        }
        public void Tick(float dt)
        {
            age += dt; if (age >= life) { gameObject.SetActive(false); return; }
            float t = age / life; transform.position += (Vector3)(velocity * dt); velocity *= Mathf.Exp(-dt * 4);
            transform.localScale = isRing ? Vector3.one * Mathf.Lerp(.15f, size * 2, t) : new Vector3(size * (1-t), size * 2.5f * (1-t), 1);
            if (!isRing) transform.Rotate(0, 0, dt * 140);
            sprite.color = new Color(color.r, color.g, color.b, color.a * (1-t));
        }
    }
    public sealed class HouseDamageText : MonoBehaviour
    {
        TextMesh text; float life; Color color;
        public void Initialize()
        {
            text = gameObject.AddComponent<TextMesh>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 40; text.characterSize = .09f; text.anchor = TextAnchor.MiddleCenter; text.fontStyle = FontStyle.Bold;
            var r = GetComponent<MeshRenderer>(); r.sharedMaterial = text.font.material; r.sortingOrder = 1800;
        }
        public void Activate(Vector2 position, int value, Color tint)
        {
            transform.position = position + Vector2.up * .8f; text.text = value.ToString(); color = tint; text.color = tint; life = .65f;
        }
        public void Tick(float dt)
        {
            life -= dt; if (life <= 0) { gameObject.SetActive(false); return; }
            transform.position += Vector3.up * dt * .9f; text.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(life * 3));
        }
    }
}
