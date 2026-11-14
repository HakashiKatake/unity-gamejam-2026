using UnityEngine;

namespace HouseOf77
{
    public sealed class HousePickup : MonoBehaviour
    {
        RunDirector game; SpriteRenderer sprite; float age;
        public void Initialize(RunDirector owner)
        {
            game = owner; sprite = HouseVisuals.Sprite("Chip",transform,HouseVisuals.White,game.Config.ringMaterial,-1);
            var block = new MaterialPropertyBlock(); block.SetFloat("_Fill",.65f); block.SetFloat("_Width",.18f); sprite.SetPropertyBlock(block); sprite.color = HouseVisuals.Gold;
            transform.localScale = new Vector3(.24f,.24f,1);
        }
        public void Spawn(Vector2 position) { transform.position = position; age = 0; }
        public void Tick(float dt)
        {
            age += dt; float distance = Vector2.Distance(transform.position,game.Player.Position);
            if (distance < 2.1f) transform.position = Vector2.MoveTowards(transform.position,game.Player.Position,dt*(5+age*2));
            sprite.transform.localPosition = Vector3.up * Mathf.Sin(age*5)*.12f;
            if (distance < .48f) Collect();
        }
        public void Collect()
        {
            if (!gameObject.activeSelf) return;
            int gained = game.Rules.AwardChips(1);
            if (gained > 0) { game.Audio.Chip(); game.Effects.Sparks(transform.position,HouseVisuals.Gold,2); game.NotifyHud(); }
            gameObject.SetActive(false);
        }
    }
}
