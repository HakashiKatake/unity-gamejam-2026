using UnityEngine;

namespace HouseOf77
{
    // Camera offsets never feed back into the tracked position, so shake cannot accumulate drift.
    public sealed class HouseCamera
    {
        public const float Aspect = 16f / 9f;
        readonly Camera camera;
        readonly GameConfig config;
        Vector2 focus;
        float zoom, shakePhase;
        public Vector2 Center => focus;
        public Vector2 HalfView => new Vector2(zoom * Aspect, zoom);

        public HouseCamera(Camera camera, GameConfig config)
        {
            this.camera = camera; this.config = config;
            camera.orthographic = true; camera.aspect = Aspect;
            Reset(Vector2.zero);
        }
        public void Reset(Vector2 player)
        {
            zoom = config.combatZoom; focus = Confine(player + Vector2.up * .55f, zoom);
            shakePhase = 0; Apply(Vector2.zero);
        }
        public void Tick(Vector2 player, Vector2 aim, Vector2? boss, float dt, float shake)
        {
            Vector2 desired = player + Vector2.up * .55f + aim * .32f;
            float desiredZoom = config.combatZoom;
            if (boss.HasValue)
            {
                Vector2 gap = boss.Value - player;
                desired += Vector2.ClampMagnitude(gap * .22f, 1.9f);
                desiredZoom += Mathf.Clamp(gap.magnitude * .13f, .45f, 1.55f);
            }
            zoom = Mathf.Lerp(zoom, desiredZoom, 1 - Mathf.Exp(-3.8f * dt));
            focus = Confine(Vector2.Lerp(focus, desired, 1 - Mathf.Exp(-config.cameraFollowRate * dt)), zoom);
            shakePhase += dt * 88;
            Apply(new Vector2(Mathf.Sin(shakePhase * 1.17f), Mathf.Sin(shakePhase * .79f)) * shake);
        }
        public bool IsVisible(Vector2 point, float padding = 0)
        {
            Vector2 offset = point - focus, half = HalfView;
            return Mathf.Abs(offset.x) <= half.x + padding && Mathf.Abs(offset.y) <= half.y + padding;
        }
        Vector2 Confine(Vector2 point, float size)
        {
            // Show the rail and a sliver past it while keeping the entire hero inside the HUD-safe frame.
            Vector2 limits = config.arenaHalfSize + new Vector2(1.65f, 2.45f) - new Vector2(size * Aspect, size);
            float x = Mathf.Max(0, limits.x), y = Mathf.Max(0, limits.y);
            return new Vector2(Mathf.Clamp(point.x, -x, x), Mathf.Clamp(point.y, -y, y));
        }
        void Apply(Vector2 offset)
        {
            camera.orthographicSize = zoom;
            camera.transform.position = new Vector3(focus.x + offset.x, focus.y + offset.y, -10);
        }
    }
}
