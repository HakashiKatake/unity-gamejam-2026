using UnityEngine;

namespace HouseOf77
{
    public sealed class EnemyActor : MonoBehaviour
    {
        RunDirector game; SpriteRenderer body, marker; LineRenderer warning; MaterialPropertyBlock flash;
        Vector2 knockback, chargeDirection; float hp, speed, age, spawnTime, hitTime, abilityTime, chargeTime, deathTime;
        int kind; bool charging;
        public bool Alive => gameObject.activeSelf && hp > 0 && spawnTime <= 0;
        public Vector2 Position => transform.position;
        public Vector2 HitPosition => Position + Vector2.up * .5f;
        public void Initialize(RunDirector owner)
        {
            game = owner; flash = new MaterialPropertyBlock();
            body = HouseVisuals.Sprite("Warrior", transform, game.Config.halberdier, game.Config.spriteMaterial, 0);
            HouseVisuals.Shadow(transform, game.Config);
            marker = HouseVisuals.Sprite("Spawn warning", transform, HouseVisuals.White, game.Config.ringMaterial, -10);
            marker.color = new Color(1, .25f, .4f, .7f);
            warning = HouseVisuals.Line("Charge warning", transform, game.Config.spriteMaterial, new Vector3[2], HouseVisuals.Red, .065f, -5);
        }
        public void Spawn(Vector2 position, int type, WaveTuning tuning)
        {
            transform.position = position; kind = type; hp = tuning.health * (type == 2 ? 2 : 1);
            speed = tuning.speed * (type == 1 ? 1.2f : type == 2 ? .72f : 1);
            age = 0; spawnTime = .8f; deathTime = hitTime = chargeTime = 0; knockback = Vector2.zero;
            charging = false; abilityTime = Random.Range(1.5f, 3.5f);
            body.sprite = kind == 1 ? game.Config.valkyrie : kind == 2 ? game.Config.elite : game.Config.halberdier;
            body.transform.localScale = Vector3.one * (type == 2 ? 1.1f : 1);
            body.color = Color.clear; body.flipX = false; warning.enabled = false; marker.enabled = true;
            flash.Clear(); body.SetPropertyBlock(flash);
        }
        public void Tick(float dt)
        {
            age += dt;
            if (hp <= 0)
            {
                deathTime += dt; body.color = new Color(1, .7f, .5f, 1-deathTime/.25f);
                body.transform.localScale *= Mathf.Exp(-dt*3); if (deathTime >= .25f) gameObject.SetActive(false); return;
            }
            if (spawnTime > 0)
            {
                spawnTime -= dt; marker.transform.localScale = Vector3.one * (1.3f + .18f*Mathf.Sin(age*18));
                body.color = new Color(1,1,1,Mathf.Clamp01(1-spawnTime/.8f));
                if (spawnTime <= 0) marker.enabled = false; return;
            }
            Vector2 direction = (game.Player.Position-Position).normalized;
            hitTime = Mathf.Max(0,hitTime-dt); abilityTime -= dt;
            if (kind == 1)
            {
                if (abilityTime <= 0 && !charging)
                {
                    charging = true; chargeTime = .75f; chargeDirection = direction; warning.enabled = true;
                    warning.SetPosition(0, Vector3.zero); warning.SetPosition(1, (Vector3)(chargeDirection*4.5f));
                }
                if (charging)
                {
                    chargeTime -= dt;
                    if (chargeTime <= 0)
                    {
                        warning.enabled = false; transform.position = game.ClampToArena(Position + chargeDirection*speed*3.8f*dt);
                        if (chargeTime <= -.5f) { charging = false; abilityTime = 3.8f; }
                    }
                }
            }
            if (kind == 2 && abilityTime <= 0)
            {
                game.Shoot(HitPosition, (game.Player.Position + Vector2.up*.4f-HitPosition).normalized, 1, true, 0);
                game.Effects.Ring(HitPosition, HouseVisuals.Red, .7f, .25f); abilityTime = 3.6f;
            }
            Vector2 separation = Vector2.zero;
            foreach (var other in game.Enemies.Items)
            {
                if (other == this || !other.Alive) continue;
                Vector2 delta = Position-other.Position; float sq = delta.sqrMagnitude;
                if (sq < .65f && sq > .0001f) separation += delta.normalized * (1-Mathf.Sqrt(sq)/.81f);
            }
            Vector2 velocity = (charging ? Vector2.zero : direction * speed) + separation * 2 + knockback;
            transform.position = game.ClampToArena(Position + velocity * dt); knockback *= Mathf.Exp(-dt*12);
            body.flipX = direction.x < 0;
            body.transform.localPosition = new Vector3(0, .05f+Mathf.Abs(Mathf.Sin(age*8))*.09f);
            body.transform.localRotation = Quaternion.Euler(0,0,Mathf.Sin(age*7)*3);
            body.sortingOrder = 100-Mathf.RoundToInt(Position.y*10);
            body.color = Color.white; flash.SetFloat("_Flash", hitTime*8); flash.SetFloat("_Gold",kind == 2 ? .65f : 0); body.SetPropertyBlock(flash);
            if (Vector2.Distance(Position,game.Player.Position) < .66f) game.Player.Damage(Position);
        }
        public void Hit(float damage, Vector2 direction)
        {
            if (!Alive) return;
            hp -= damage; hitTime = .12f; knockback = direction * 4;
            game.Effects.Sparks(HitPosition, kind == 2 ? HouseVisuals.Gold : HouseVisuals.Blue, 3);
            game.Effects.Number(HitPosition, Mathf.RoundToInt(damage), damage >= 24 ? HouseVisuals.Gold : Color.white); game.Audio.Impact();
            if (hp <= 0)
            {
                warning.enabled = marker.enabled = false;
                game.Effects.Sparks(HitPosition, kind == 2 ? HouseVisuals.Gold : new Color(.65f,.45f,1), kind == 2 ? 15 : 7);
                game.Killed(Position, kind == 2); deathTime = 0;
            }
        }
    }
}
