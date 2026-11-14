using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace HouseOf77
{
    public sealed class HousePlayer : MonoBehaviour
    {
        RunDirector game; SpriteRenderer sprite; MaterialPropertyBlock block; LineRenderer reticle;
        InputActionMap controls; InputAction move, attack, dash;
        Vector2 dashDirection = Vector2.right, lastMove = Vector2.right;
        float dashTime, dashCooldown, protection, fireTimer, animationTime, castTime, hitTime, trailTime;
        bool requireAttackRelease;
        public Vector2 Position => transform.position;
        public Vector2 Aim { get; private set; } = Vector2.right;
        public float DashReady => 1 - Mathf.Clamp01(dashCooldown / game.Rules.DashCooldown);
        public int Health { get; private set; }
        public bool Invincible => protection > 0 || dashTime > 0;
        public void Initialize(RunDirector owner)
        {
            game = owner; block = new MaterialPropertyBlock();
            sprite = HouseVisuals.Sprite("Bladekeeper", transform, game.Config.idle[0], game.Config.spriteMaterial, 0);
            HouseVisuals.Shadow(transform, game.Config);
            reticle = HouseVisuals.Circle("Blade aiming reticle",game.transform,game.Config.spriteMaterial,.15f,new Color(.6f,.9f,1,.7f),.025f,2000);
            reticle.gameObject.SetActive(false);
            // Reuse the existing project's Player actions without changing the imported asset.
            controls = InputSystem.actions.FindActionMap("Player", true);
            move = controls.FindAction("Move", true); attack = controls.FindAction("Attack", true); dash = controls.FindAction("Jump", true);
            controls.Disable();
        }
        public void SetInput(bool active) { requireAttackRelease = true; if (active) controls.Enable(); else controls.Disable(); if(reticle) reticle.gameObject.SetActive(active); }
        public void ResetPlayer()
        {
            transform.position = new Vector3(0, -2.2f); Health = game.Rules.MaxHealth;
            dashTime = dashCooldown = protection = fireTimer = animationTime = castTime = hitTime = trailTime = 0;
            gameObject.SetActive(true); sprite.color = Color.white; sprite.sprite = game.Config.idle[0]; SetInput(false);
        }
        public void Heal(int amount) { Health = Mathf.Min(Health + amount, game.Rules.MaxHealth); game.NotifyHud(); }
        public void Tick(float dt)
        {
            dashCooldown = Mathf.Max(0, dashCooldown-dt); protection = Mathf.Max(0, protection-dt);
            fireTimer -= dt; castTime -= dt; hitTime -= dt; animationTime += dt;
            Vector2 input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1);
            if (input.sqrMagnitude > .01f) lastMove = input.normalized;
            if (Mouse.current != null)
            {
                Vector2 target = game.GameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                reticle.transform.position = target;
                Vector2 direction = target - (Position + Vector2.up * .6f);
                if (direction.sqrMagnitude > .001f) Aim = direction.normalized;
            }
            if (dash.WasPressedThisFrame() && dashCooldown <= 0)
            {
                dashDirection = input.sqrMagnitude > .01f ? input.normalized : lastMove;
                dashTime = .18f; dashCooldown = game.Rules.DashCooldown;
                game.Effects.Ring(Position, HouseVisuals.Blue, .9f, .22f);
            }
            if (dashTime > 0)
            {
                dashTime -= dt; input = dashDirection * 3.7f; trailTime -= dt;
                if (trailTime <= 0) { trailTime = .035f; game.Effects.Sparks(Position, HouseVisuals.Blue, 2); }
            }
            transform.position = game.ClampToArena(Position + input * game.Config.playerSpeed * dt);
            if (!attack.IsPressed()) requireAttackRelease = false;
            if (!requireAttackRelease && attack.IsPressed() && fireTimer <= 0 && (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject()))
            {
                fireTimer = 1 / (game.Config.shotsPerSecond * game.Rules.FireRateMultiplier);
                float damage = game.Config.bladeDamage * game.Rules.DamageMultiplier * (game.JackpotRemaining > 0 ? 2 : 1);
                game.Shoot(Position + Vector2.up * .6f + Aim * .35f, Aim, damage, false, game.Rules.Upgrades[3]);
                if (game.Rules.Upgrades[2] > 0)
                {
                    game.Shoot(Position + Vector2.up * .6f, HouseVisuals.Rotate(Aim, 14), damage * .4f, false, game.Rules.Upgrades[3]);
                    game.Shoot(Position + Vector2.up * .6f, HouseVisuals.Rotate(Aim, -14), damage * .4f, false, game.Rules.Upgrades[3]);
                }
                castTime = .19f; game.Audio.Cast(); game.Effects.Sparks(Position + Vector2.up * .6f + Aim * .5f, HouseVisuals.Blue, 1);
            }
            Sprite[] frames = dashTime > 0 ? game.Config.roll : hitTime > 0 ? game.Config.hit : castTime > 0 ? game.Config.cast : input.sqrMagnitude > .01f ? game.Config.run : game.Config.idle;
            sprite.sprite = frames[(int)(animationTime * (castTime > 0 ? 18 : 11)) % frames.Length];
            sprite.flipX = Aim.x < 0; sprite.sortingOrder = 100 - Mathf.RoundToInt(Position.y * 10);
            sprite.color = protection > 0 && Mathf.Sin(animationTime*45) > 0 ? new Color(1,1,1,.45f) : Color.white;
            block.SetFloat("_Flash", Mathf.Clamp01(hitTime * 5)); block.SetFloat("_Gold", game.JackpotRemaining > 0 ? .65f : 0); sprite.SetPropertyBlock(block);
        }
        public void Damage(Vector2 source)
        {
            if (!game.CombatActive || Invincible) return;
            Health--; protection = .9f; hitTime = .22f;
            transform.position = game.ClampToArena(Position + (Position-source).normalized * .5f);
            game.Effects.Ring(Position, HouseVisuals.Red, 1.2f); game.Effects.Sparks(Position + Vector2.up*.5f, HouseVisuals.Red, 10);
            game.Audio.Play(game.Config.damageSound); game.Shake(.15f); game.NotifyHud();
            if (Health <= 0) game.EndRun(false);
        }
        public void ShowDeath() { SetInput(false); sprite.sprite = game.Config.death[game.Config.death.Length-1]; }
    }
}
