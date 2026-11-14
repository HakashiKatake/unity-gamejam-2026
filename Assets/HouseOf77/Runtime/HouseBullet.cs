using System.Collections.Generic;
using UnityEngine;

namespace HouseOf77
{
    public sealed class HouseBullet : MonoBehaviour
    {
        RunDirector game; SpriteRenderer sprite; LineRenderer trail;
        Vector2 velocity; float damage, life; bool hostile; int pierce;
        readonly List<EnemyActor> hits = new List<EnemyActor>(4);
        public void Initialize(RunDirector owner)
        {
            game = owner; sprite = HouseVisuals.Sprite("Blade",transform,game.Config.blade,game.Config.spriteMaterial,600);
            trail = HouseVisuals.Line("Trail",transform,game.Config.spriteMaterial,new [] {Vector3.zero,Vector3.left*.55f},HouseVisuals.Blue,.045f,599);
        }
        public void Fire(Vector2 position, Vector2 direction, float amount, bool enemy, int piercing)
        {
            transform.position = position; transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            hostile = enemy; damage = amount; pierce = piercing; life = enemy ? 6 : 1.9f;
            velocity = direction * (enemy ? 4.3f : 16); hits.Clear();
            sprite.color = enemy ? HouseVisuals.Red : game.JackpotRemaining > 0 ? HouseVisuals.Gold : HouseVisuals.Blue;
            sprite.transform.localScale = Vector3.one * (enemy ? .7f : 1.15f);
            trail.startColor = sprite.color; trail.endColor = new Color(sprite.color.r,sprite.color.g,sprite.color.b,0);
        }
        public void Tick(float dt)
        {
            life -= dt; if (life <= 0) { gameObject.SetActive(false); return; }
            Vector2 from = transform.position, to = from+velocity*dt; transform.position = to;
            if (Mathf.Abs(to.x) > 17 || Mathf.Abs(to.y) > 10) { gameObject.SetActive(false); return; }
            if (hostile)
            {
                if (HouseVisuals.SegmentDistance(game.Player.Position+Vector2.up*.4f,from,to) < .42f)
                { game.Player.Damage(from); gameObject.SetActive(false); }
                return;
            }
            if (game.Phase == RunPhase.Boss && game.Boss.Alive && HouseVisuals.SegmentDistance(game.Boss.Position,from,to) < 1.8f)
            { game.Boss.Hit(damage); gameObject.SetActive(false); return; }
            foreach (var enemy in game.Enemies.Items)
            {
                if (!enemy.Alive || hits.Contains(enemy)) continue;
                if (HouseVisuals.SegmentDistance(enemy.HitPosition,from,to) > .5f) continue;
                hits.Add(enemy); enemy.Hit(damage,velocity.normalized);
                if (pierce-- <= 0) { gameObject.SetActive(false); return; }
            }
        }
    }
}
