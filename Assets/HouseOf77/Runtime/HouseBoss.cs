using UnityEngine;

namespace HouseOf77
{
    public sealed class HouseBoss : MonoBehaviour
    {
        RunDirector game; SpriteRenderer body; LineRenderer beam; MaterialPropertyBlock flash;
        float hp, timer, hitTime, beamTime; int attack; Vector2 beamEnd;
        TextMesh[] reels;
        public bool Alive => gameObject.activeSelf && hp > 0;
        public Vector2 Position => transform.position;
        public float HealthFraction => Mathf.Clamp01(hp/game.Config.bossHealth);
        public bool BeamWarning => beamTime > .35f;
        public void Initialize(RunDirector owner)
        {
            game = owner; flash = new MaterialPropertyBlock();
            body = HouseVisuals.Sprite("Possessed jackpot",transform,game.Config.machine,game.Config.spriteMaterial,0);
            body.transform.localScale = Vector3.one*.8f;
            reels = new TextMesh[3];
            for(int i=0;i<3;i++)
            {
                var reel=new GameObject("Possessed reel "+i); reel.transform.SetParent(transform,false); reel.transform.localPosition=new Vector3(-.7f+i*.72f,-.15f,-.01f);
                var text=reel.AddComponent<TextMesh>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=70; text.characterSize=.15f;
                text.fontStyle=FontStyle.Bold; text.anchor=TextAnchor.MiddleCenter; text.color=new Color(.12f,.10f,.20f); text.text="7";
                var renderer=reel.GetComponent<MeshRenderer>(); renderer.sharedMaterial=text.font.material; renderer.sortingOrder=1; reels[i]=text;
            }
            var shadow = HouseVisuals.Shadow(transform,game.Config); shadow.transform.localScale = new Vector3(4,1,1);
            beam = HouseVisuals.Line("House beam",transform,game.Config.spriteMaterial,new Vector3[2],HouseVisuals.Red,.08f,400);
            beam.enabled = false; gameObject.SetActive(false);
        }
        public void Begin()
        {
            transform.position = new Vector3(0,3.4f); hp = game.Config.bossHealth; timer = 2; attack = 0; beamTime = hitTime = 0;
            beam.enabled = false; body.color = Color.white; gameObject.SetActive(true); game.Audio.Play(game.Config.bossSound);
        }
        public void Tick(float dt)
        {
            if (!Alive) return;
            timer -= dt; hitTime = Mathf.Max(0,hitTime-dt);
            body.transform.localPosition = Vector3.up * Mathf.Sin(game.CombatSeconds*2)*.07f;
            for(int i=0;i<3;i++) reels[i].text = HealthFraction < .5f ? ((int)(game.CombatSeconds*4+i*3)%8).ToString() : "7";
            flash.SetFloat("_Flash",hitTime*8); flash.SetFloat("_Gold",HealthFraction < .5f ? .5f : .1f); body.SetPropertyBlock(flash);
            if (beamTime > 0)
            {
                beamTime -= dt;
                bool firing = beamTime <= .35f;
                beam.startWidth = beam.endWidth = firing ? 1.15f : .07f;
                beam.startColor = beam.endColor = firing ? new Color(1,.7f,.65f,.85f) : new Color(1,.3f,.43f,.55f+.3f*Mathf.Sin(beamTime*20));
                if (firing && HouseVisuals.SegmentDistance(game.Player.Position+Vector2.up*.3f,Position,beamEnd) < .82f) game.Player.Damage(Position);
                if (beamTime <= 0) beam.enabled = false;
            }
            if (timer > 0) return;
            timer = HealthFraction < .5f ? 2.1f : 2.8f;
            Vector2 muzzle = Position + Vector2.down*1.25f;
            Vector2 direction = (game.Player.Position-muzzle).normalized;
            switch (attack++%3)
            {
                case 0:
                    for (int i = -3; i <= 3; i++) game.Shoot(muzzle,HouseVisuals.Rotate(direction,i*16),1,true,0);
                    game.Effects.Ring(muzzle,HouseVisuals.Red,1.2f,.4f); break;
                case 1:
                    beamTime = 1.35f; beamEnd = Position + (game.Player.Position-Position).normalized*25;
                    beam.SetPosition(0,Vector3.zero); beam.SetPosition(1,(Vector3)(beamEnd-Position)); beam.enabled = true; break;
                case 2:
                    for (int i = 0; i < 3; i++) game.SpawnEnemy(i == 2 ? 1 : 0);
                    game.Effects.Ring(Position,HouseVisuals.Gold,3,.7f); break;
            }
            if (Vector2.Distance(game.Player.Position,Position) < 1.8f) game.Player.Damage(Position);
        }
        public void Hit(float damage)
        {
            if (!Alive) return;
            hp -= damage; hitTime = .1f; game.Effects.Sparks(Position+Random.insideUnitCircle,HouseVisuals.Gold,3); game.Audio.Impact();
            game.Effects.Number(Position,Mathf.RoundToInt(damage),HouseVisuals.Gold);
            if (hp <= 0) { beam.enabled = false; game.Effects.Jackpot(Position); game.EndRun(true); }
        }
    }
}
