using UnityEngine;

namespace HouseOf77
{
    public enum EnemyState { Spawning, Pursuit, Windup, Strike, Recovery, Death }

    public sealed class EnemyActor : MonoBehaviour
    {
        RunDirector game;
        SpriteRenderer body, marker, shadow;
        LineRenderer warning, slash;
        MaterialPropertyBlock flash;
        Vector2 knockback, attackDirection, hitDirection;
        float hp, speed, age, stateAge, hitTime, cooldown, baseScale, spawnDelay;
        int kind;
        bool connected;
        public EnemyState State { get; private set; }
        public bool Ranged => kind == 2;
        public bool Alive => gameObject.activeSelf && hp > 0 && State != EnemyState.Spawning;
        public Vector2 Position => transform.position;
        public Vector2 HitPosition => Position + Vector2.up * .5f;
        float Windup => kind == 0 ? .53f : kind == 1 ? .68f : .62f;
        float StrikeLength => kind == 0 ? .16f : kind == 1 ? .33f : .18f;
        float Recovery => kind == 0 ? .48f : kind == 1 ? .68f : .61f;
        Color AttackColor => kind == 2 ? new Color(1, .54f, .23f) : HouseVisuals.Red;

        public void Initialize(RunDirector owner)
        {
            game = owner; flash = new MaterialPropertyBlock();
            body = HouseVisuals.Sprite("Warrior", transform, game.Config.halberdier, game.Config.spriteMaterial, 0);
            shadow = HouseVisuals.Shadow(transform, game.Config);
            marker = HouseVisuals.Sprite("Spawn warning", transform, HouseVisuals.White, game.Config.ringMaterial, -10);
            var ringBlock = new MaterialPropertyBlock(); ringBlock.SetFloat("_Fill", .18f); ringBlock.SetFloat("_Width", .08f); marker.SetPropertyBlock(ringBlock);
            warning = HouseVisuals.Line("Attack telegraph", transform, game.Config.spriteMaterial, new Vector3[2], HouseVisuals.Red, .065f, 240);
            slash = HouseVisuals.Line("Strike arc", transform, game.Config.spriteMaterial, new Vector3[10], HouseVisuals.Red, .06f, 650);
        }
        public void Spawn(Vector2 position, int type, WaveTuning tuning, float warningSeconds = .62f)
        {
            transform.position = position; kind = type; hp = tuning.health * (type == 2 ? 2 : 1);
            speed = tuning.speed * (type == 1 ? 1.24f : type == 2 ? .82f : 1);
            age = stateAge = hitTime = 0; cooldown = Random.Range(.22f, .68f); knockback = Vector2.zero;
            hitDirection = attackDirection = Vector2.right; baseScale = type == 2 ? .99f : .90f;
            spawnDelay = warningSeconds; connected = false; State = EnemyState.Spawning;
            body.sprite = kind == 1 ? game.Config.valkyrie : kind == 2 ? game.Config.elite : game.Config.halberdier;
            body.transform.localPosition = Vector3.zero; body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(baseScale * 1.1f, baseScale * .7f, 1);
            body.color = Color.clear; body.flipX = false; warning.enabled = slash.enabled = false;
            marker.enabled = true; marker.color = new Color(1, .26f, .38f, .8f); shadow.enabled = false;
            flash.Clear(); body.SetPropertyBlock(flash);
        }
        public void Tick(float dt)
        {
            age += dt; stateAge += dt;
            if (State == EnemyState.Death) { AnimateDeath(); return; }
            if (State == EnemyState.Spawning)
            {
                float t = Mathf.Clamp01(stateAge / spawnDelay);
                marker.transform.localScale = Vector3.one * Mathf.Lerp(2.1f, .85f, t);
                body.transform.localScale = new Vector3(baseScale * Mathf.Lerp(1.22f, 1, t), baseScale * Mathf.Lerp(.5f, 1, t), 1);
                body.color = new Color(1,1,1, Mathf.SmoothStep(0,1,t));
                if (t >= 1) { marker.enabled = false; shadow.enabled = true; Enter(EnemyState.Pursuit); }
                return;
            }
            Vector2 gap = game.Player.Position - Position;
            Vector2 direction = gap.sqrMagnitude > .001f ? gap.normalized : attackDirection;
            hitTime = Mathf.Max(0, hitTime-dt);
            cooldown -= dt;
            Vector2 velocity = knockback;
            Vector2 before = Position;
            switch (State)
            {
                case EnemyState.Pursuit:
                    float trigger = kind == 0 ? 1.26f : kind == 1 ? 4.0f : 7.4f;
                    if (cooldown <= 0 && gap.sqrMagnitude <= trigger * trigger && game.IsOnCamera(Position) && game.CanStartAttack(Ranged))
                    {
                        attackDirection = kind == 2 ? (game.Player.Position + Vector2.up * .4f - HitPosition).normalized : direction;
                        connected = false;
                        Enter(EnemyState.Windup); SetWarning(0);
                        if (game.IsOnCamera(Position)) game.Audio.Windup();
                    }
                    else
                    {
                        bool holdRange = kind == 2 && gap.sqrMagnitude < 16;
                        velocity += direction * speed * (holdRange ? -.35f : 1) + Separation() * 2.2f;
                    }
                    break;
                case EnemyState.Windup:
                    SetWarning(stateAge / Windup);
                    if (stateAge >= Windup)
                    {
                        Enter(EnemyState.Strike); warning.enabled = false;
                        if (kind == 2)
                        {
                            game.Shoot(HitPosition, attackDirection, 1, true, 0);
                            game.Effects.Ring(HitPosition, AttackColor, .9f, .27f);
                        }
                        else DrawSlash(0);
                    }
                    break;
                case EnemyState.Strike:
                    velocity += attackDirection * (kind == 1 ? 9.7f : kind == 0 ? 4.5f : 0);
                    if (kind != 2) DrawSlash(stateAge / StrikeLength);
                    break;
                case EnemyState.Recovery:
                    velocity += Separation() * .75f;
                    if (stateAge >= Recovery) { cooldown = kind == 2 ? 2.3f : kind == 1 ? 1.9f : .58f; Enter(EnemyState.Pursuit); }
                    break;
            }
            transform.position = game.ClampToArena(Position + velocity * dt);
            knockback *= Mathf.Exp(-dt*12);
            if (State == EnemyState.Strike)
            {
                if (!connected && kind != 2)
                {
                    Vector2 toPlayer = game.Player.Position - Position;
                    bool inStrike = kind == 1
                        ? HouseVisuals.SegmentDistance(game.Player.Position, before, Position) < .76f
                        : toPlayer.sqrMagnitude < 1.38f * 1.38f && (toPlayer.sqrMagnitude < .16f || Vector2.Dot(toPlayer.normalized, attackDirection) > .58f);
                    if (inStrike) { connected = true; game.Player.Damage(Position); }
                }
                if (stateAge >= StrikeLength) { slash.enabled = false; Enter(EnemyState.Recovery); }
            }
            Animate(State == EnemyState.Pursuit ? direction : attackDirection);
        }
        Vector2 Separation()
        {
            Vector2 separation = Vector2.zero;
            Vector2 here = Position;
            foreach (var other in game.Enemies.Items)
            {
                if (other == this || !other.Alive) continue;
                Vector2 delta = here - other.Position; float sq = delta.sqrMagnitude;
                if (sq < .74f && sq > .0001f) separation += delta.normalized * (1-Mathf.Sqrt(sq)/.86f);
            }
            return separation;
        }
        void Enter(EnemyState state) { State = state; stateAge = 0; }
        void SetWarning(float progress)
        {
            if (kind == 0)
            {
                warning.positionCount = 12; warning.loop = true;
                warning.SetPosition(0, Vector3.zero);
                for (int i = 0; i <= 10; i++)
                    warning.SetPosition(i + 1, HouseVisuals.Rotate(attackDirection, Mathf.Lerp(-54,54,i/10f)) * 2.10f);
            }
            else
            {
                warning.positionCount = 2; warning.loop = false;
                Vector3 origin = kind == 2 ? Vector3.up * .5f : Vector3.zero;
                warning.SetPosition(0, origin); warning.SetPosition(1, origin + (Vector3)(attackDirection * (kind == 1 ? 3.9f : 5.8f)));
            }
            warning.enabled = true; warning.startWidth = warning.endWidth = kind == 1 ? .11f : .057f;
            Color c = AttackColor; c.a = Mathf.Lerp(.45f,.96f,progress); warning.startColor = warning.endColor = c;
        }
        void DrawSlash(float progress)
        {
            slash.enabled = true;
            float t = Mathf.Clamp01(progress);
            float radius = kind == 0 ? Mathf.Lerp(.72f,1.48f,t) : Mathf.Lerp(.7f,1.04f,t);
            Vector2 origin = attackDirection * .27f + Vector2.up * .38f;
            for (int i = 0; i < slash.positionCount; i++)
            {
                float angle = Mathf.Lerp(-80,80,i/(slash.positionCount-1f));
                slash.SetPosition(i, origin + HouseVisuals.Rotate(attackDirection, angle) * radius);
            }
            slash.startWidth = slash.endWidth = Mathf.Lerp(.14f,.035f,t);
            Color c = Color.Lerp(new Color(1,.84f,.68f),AttackColor,t); c.a = 1-t*.72f;
            slash.startColor = slash.endColor = c;
        }
        void Animate(Vector2 facing)
        {
            float stride = Mathf.Sin(age * (kind == 1 ? 16 : 11));
            float sx = 1 + stride*.035f, sy = 1 - stride*.045f;
            float lean = -facing.x * 7 + stride * 4, lift = Mathf.Abs(stride) * .10f;
            Vector2 offset = Vector2.zero;
            if (State == EnemyState.Windup)
            {
                float t = Mathf.SmoothStep(0,1, stateAge/Windup);
                offset = -facing * (.30f*t); lean = facing.x * 23*t;
                sx = 1+.18f*t; sy = 1-.23f*t; lift = .04f;
            }
            else if (State == EnemyState.Strike)
            {
                float t = 1-Mathf.Clamp01(stateAge/StrikeLength);
                offset = facing * (.27f*t); lean = -facing.x*27*t; sx = 1-.06f*t; sy = 1+.19f*t; lift = .12f*t;
            }
            else if (State == EnemyState.Recovery)
            {
                float t = 1-Mathf.Clamp01(stateAge/Recovery);
                lean = -facing.x*14*t; sx = 1+.12f*t; sy = 1-.16f*t; lift = 0;
            }
            float recoil = Mathf.Clamp01(hitTime/.16f);
            offset += hitDirection * (.14f*recoil);
            lean -= hitDirection.x*18*recoil; sx += .13f*recoil; sy -= .13f*recoil;
            body.transform.localPosition = offset + Vector2.up*lift;
            body.transform.localScale = new Vector3(baseScale*sx,baseScale*sy,1);
            body.transform.localRotation = Quaternion.Euler(0,0,lean);
            if (Mathf.Abs(facing.x) > .045f) body.flipX = facing.x < 0;
            body.sortingOrder = 100-Mathf.RoundToInt(Position.y*10);
            body.color = State == EnemyState.Windup ? Color.Lerp(Color.white, new Color(1,.65f,.6f), Mathf.Clamp01(stateAge/Windup)) : Color.white;
            flash.SetFloat("_Flash",recoil*.8f); flash.SetFloat("_Gold",kind == 2 ? .65f : 0); body.SetPropertyBlock(flash);
        }
        void AnimateDeath()
        {
            float t = Mathf.Clamp01(stateAge/.46f);
            float fall = Mathf.Sin(t * Mathf.PI*.5f);
            float side = hitDirection.x < 0 ? 1 : -1;
            body.transform.localPosition = hitDirection*.65f*fall + Vector2.up*Mathf.Sin(t*Mathf.PI)*.39f;
            body.transform.localRotation = Quaternion.Euler(0,0,side*fall*81);
            body.transform.localScale = new Vector3(baseScale*(1+.16f*fall),baseScale*(1-.43f*fall),1);
            body.color = new Color(.91f,.72f,.8f,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.34f,1,t)));
            flash.SetFloat("_Flash",Mathf.Clamp01(1-t*7)); body.SetPropertyBlock(flash);
            if (t >= 1) gameObject.SetActive(false);
        }
        public void Hit(float damage, Vector2 direction)
        {
            if (!Alive) return;
            hp -= damage; hitTime = .16f; hitDirection = direction; knockback = direction * 5.2f;
            game.Effects.Sparks(HitPosition, kind == 2 ? HouseVisuals.Gold : HouseVisuals.Blue, 5);
            game.Effects.Number(HitPosition, Mathf.RoundToInt(damage), damage >= 24 ? HouseVisuals.Gold : Color.white); game.Audio.Impact();
            if (hp <= 0)
            {
                Enter(EnemyState.Death); warning.enabled = marker.enabled = slash.enabled = shadow.enabled = false;
                game.Effects.Sparks(HitPosition, kind == 2 ? HouseVisuals.Gold : new Color(.72f,.52f,1), kind == 2 ? 16 : 9);
                if (game.IsOnCamera(Position)) game.Audio.DefeatEnemy();
                game.Shake(kind == 2 ? .12f : .045f);
                game.Killed(Position, kind == 2);
            }
        }
    }
}
