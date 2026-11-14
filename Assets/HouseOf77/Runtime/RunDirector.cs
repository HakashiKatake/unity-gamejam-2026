using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace HouseOf77
{
    public sealed class RunDirector : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        public GameConfig Config => config;
        public RunRules Rules { get; private set; }
        public HousePlayer Player { get; private set; }
        public HouseBoss Boss { get; private set; }
        public HouseEffects Effects { get; private set; }
        public HouseAudio Audio { get; private set; }
        public HouseUI UI { get; private set; }
        public Camera GameCamera { get; private set; }
        public HousePool<EnemyActor> Enemies { get; private set; }
        public HousePool<HouseBullet> Bullets { get; private set; }
        public HousePool<HousePickup> Pickups { get; private set; }
        public RunPhase Phase { get; private set; } = RunPhase.Title;
        public bool Paused { get; private set; }
        public bool CombatActive => !Paused && (Phase == RunPhase.Wave || Phase == RunPhase.Boss);
        public bool ScreenShake { get; private set; }
        public bool Won { get; private set; }
        public int Wave { get; private set; }
        public int Kills { get; private set; }
        public float WaveRemaining { get; private set; }
        public float CombatSeconds { get; private set; }
        public float JackpotRemaining { get; private set; }
        public int LastSpin { get; private set; }
        public bool LastMatch { get; private set; }
        public int BestScore => PlayerPrefs.GetInt("House77.Best",0);
        public int Score => Kills*10+Rules.Jackpots*777+(Won ? 7777 : 0);
        public bool Reveal => Phase != RunPhase.Wave || WaveRemaining <= config.revealSeconds;
        public System.Random SpinRandom { get; private set; }
        System.Random offerRandom, targetRandom;
        float spawnTimer, rushTimer, jackpotPulse, shake, bannerTimer;
        int killProgress, chipsSpawned, killChain;
        public int RushProgress => killChain;
        Material arenaMaterial;
        HouseCamera followCamera;
        Transform world;
        int screenWidth, screenHeight;
        public string Banner { get; private set; }

        void Start()
        {
            if (!config) { Debug.LogError("House of 77 needs its GameConfig asset. Run House of 77/Prepare Game in the editor."); enabled = false; return; }
            Application.targetFrameRate = 60;
            Time.timeScale = 1; Rules = new RunRules();
            ScreenShake = PlayerPrefs.GetInt("House77.Shake",1) == 1;
            GameCamera = Camera.main;
            if (!GameCamera) { var camera = new GameObject("Main Camera"); camera.tag = "MainCamera"; GameCamera = camera.AddComponent<Camera>(); camera.AddComponent<AudioListener>(); }
            followCamera = new HouseCamera(GameCamera, config);
            GameCamera.backgroundColor = new Color(.018f,.021f,.035f); GameCamera.clearFlags = CameraClearFlags.SolidColor;
            FitCamera();
            world = new GameObject("Casino arena").transform; world.SetParent(transform);
            BuildArena();
            Effects = new GameObject("Effects").AddComponent<HouseEffects>(); Effects.transform.SetParent(transform); Effects.Initialize(config);
            Audio = gameObject.AddComponent<HouseAudio>(); Audio.Initialize(config);
            Player = new GameObject("Player — Bladekeeper").AddComponent<HousePlayer>(); Player.transform.SetParent(world); Player.Initialize(this); Player.ResetPlayer();
            Enemies = new HousePool<EnemyActor>(config.enemyCapacity,"Warrior",world,e => e.Initialize(this));
            Bullets = new HousePool<HouseBullet>(config.projectileCapacity,"Projectile",world,b => b.Initialize(this));
            Pickups = new HousePool<HousePickup>(48,"Chip",world,p => p.Initialize(this));
            Boss = new GameObject("The House").AddComponent<HouseBoss>(); Boss.transform.SetParent(world); Boss.Initialize(this);
            if (!EventSystem.current)
            {
                var events = new GameObject("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
            }
            UI = new GameObject("House of 77 — Interface").AddComponent<HouseUI>(); UI.Initialize(this);
            Player.gameObject.SetActive(false); UI.ShowTitle(); Audio.SetScene(Phase);
        }
        void BuildArena()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad); floor.name = "Engraved casino floor"; floor.transform.SetParent(world);
            floor.transform.position = new Vector3(0,0,2); floor.transform.localScale = new Vector3(35,20,1);
            Destroy(floor.GetComponent<Collider>()); arenaMaterial = new Material(config.arenaMaterial); floor.GetComponent<MeshRenderer>().sharedMaterial = arenaMaterial;
            var h = config.arenaHalfSize;
            HouseVisuals.Line("Inlaid boundary",world,config.spriteMaterial,new [] {new Vector3(-h.x-.4f,-h.y-.4f),new Vector3(h.x+.4f,-h.y-.4f),new Vector3(h.x+.4f,h.y+.4f),new Vector3(-h.x-.4f,h.y+.4f)},new Color(.55f,.39f,.17f),.045f,-40,true);
            HouseVisuals.Line("Outer violet rail",world,config.spriteMaterial,new [] {new Vector3(-h.x-.62f,-h.y-.62f),new Vector3(h.x+.62f,-h.y-.62f),new Vector3(h.x+.62f,h.y+.62f),new Vector3(-h.x-.62f,h.y+.62f)},new Color(.25f,.15f,.4f),.10f,-40,true);
            for (int i = 0; i < 8; i++)
            {
                float x = -13.6f+i*3.9f;
                HouseVisuals.Line("Gold rail inset",world,config.spriteMaterial,new [] {new Vector3(x,-7.66f),new Vector3(x+1.7f,-7.66f)},HouseVisuals.Gold*.55f,.04f,-30);
                HouseVisuals.Line("Gold rail inset",world,config.spriteMaterial,new [] {new Vector3(x,7.66f),new Vector3(x+1.7f,7.66f)},HouseVisuals.Gold*.55f,.04f,-30);
            }
            var center = new GameObject("House seal"); center.transform.SetParent(world);
            HouseVisuals.Circle("Seal",center.transform,config.spriteMaterial,1.4f,new Color(.4f,.3f,.13f,.5f),.025f,-30);
            var seal = center.AddComponent<TextMesh>(); seal.text = "77"; seal.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); seal.fontSize = 110; seal.characterSize = .17f; seal.fontStyle = FontStyle.Bold; seal.anchor = TextAnchor.MiddleCenter; seal.color = new Color(.37f,.28f,.14f,.5f);
            var sr = center.GetComponent<MeshRenderer>(); sr.sharedMaterial = seal.font.material; sr.sortingOrder = -25;
        }
        public void StartRun()
        {
            ClearCombat(); Rules = new RunRules();
            int seed = Environment.TickCount;
            SpinRandom = new System.Random(seed); targetRandom = new System.Random(seed ^ 771); offerRandom = new System.Random(seed ^ 7777);
            Wave = Kills = 0; CombatSeconds = JackpotRemaining = 0; Won = false; LastMatch = false; LastSpin = 0;
            Paused = false; Time.timeScale = 1; Player.ResetPlayer(); followCamera.Reset(Player.Position); BeginNextEncounter();
        }
        public void BeginNextEncounter()
        {
            if (Wave > 0 && (Phase != RunPhase.Intermission || !Rules.Spun)) return;
            ClearCombat(); Wave++; Player.SetInput(true);
            if (Wave <= 4)
            {
                Phase = RunPhase.Wave; Rules.BeginWave(targetRandom); WaveRemaining = config.waveSeconds;
                spawnTimer = .35f; rushTimer = 16f; killProgress = chipsSpawned = killChain = 0;
                Announce("WAVE 0"+Wave+"  /  "+new [] {"THE WELCOME","RAISE THE STAKES","GILDED COMPANY","ALL IN"}[Wave-1]);
            }
            else
            {
                Phase = RunPhase.Boss; killChain = 0; Boss.Begin(); Announce("FINAL ENCOUNTER  /  THE HOUSE");
            }
            if (LastMatch) { JackpotRemaining = config.jackpotSeconds; jackpotPulse = 0; Effects.Jackpot(Player.Position); Audio.Reward(config.jackpotSound); }
            else JackpotRemaining = 0;
            UI.ShowCombat(); NotifyHud(); Audio.SetScene(Phase);
        }
        void Update()
        {
            if (!UI) return;
            if (screenWidth != Screen.width || screenHeight != Screen.height) FitCamera();
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame && (Phase == RunPhase.Wave || Phase == RunPhase.Boss)) TogglePause();
                if (Keyboard.current.enterKey.wasPressedThisFrame && Phase == RunPhase.Title && !UI.CreditsOpen) StartRun();
            }
            if (Paused) { UI.Tick(Time.unscaledDeltaTime); return; }
            float dt = Time.deltaTime;
            Effects.Tick(dt); if (bannerTimer > 0) { bannerTimer -= dt; if (bannerTimer <= 0) Banner = ""; }
            if (CombatActive)
            {
                CombatSeconds += dt; Player.Tick(dt);
                if (!CombatActive) { UI.Tick(Time.unscaledDeltaTime); return; }
                if (JackpotRemaining > 0)
                {
                    JackpotRemaining = Mathf.Max(0,JackpotRemaining-dt); jackpotPulse -= dt;
                    if (jackpotPulse <= 0) { jackpotPulse = 2; for (int i = 0; i < 7; i++) Shoot(Player.Position+Vector2.up*.5f,HouseVisuals.Rotate(Vector2.right,i*360f/7),config.bladeDamage*Rules.DamageMultiplier,false,1); Effects.Ring(Player.Position,HouseVisuals.Gold,2,.45f); }
                }
                if (Phase == RunPhase.Wave)
                {
                    float before = WaveRemaining; WaveRemaining -= dt; spawnTimer -= dt; rushTimer -= dt;
                    if (before > config.revealSeconds && Reveal) { Announce("YOUR LUCKY NUMBER IS "+Rules.Target); Effects.Ring(Player.Position,HouseVisuals.Gold,3,.7f); }
                    if (WaveRemaining <= 0) { OpenIntermission(); UI.Tick(Time.unscaledDeltaTime); return; }
                    if (spawnTimer <= 0)
                    {
                        spawnTimer = config.waves[Wave-1].spawnInterval * (WaveRemaining < 30 ? .8f : 1);
                        int type = Wave >= 3 && UnityEngine.Random.value < .16f ? 2 : Wave >= 2 && UnityEngine.Random.value < .32f ? 1 : 0;
                        SpawnEnemy(type);
                    }
                    if (rushTimer <= 0 && WaveRemaining > 6)
                    {
                        rushTimer = 16 + UnityEngine.Random.Range(-2f, 2f);
                        for (int i = 0; i < 2 + Wave / 2; i++) SpawnEnemy(i == 0 && Wave >= 2 ? 1 : 0);
                        if (string.IsNullOrEmpty(Banner)) Announce("THE HOUSE SENDS A RUSH");
                    }
                }
                else Boss.Tick(dt);
                foreach (var enemy in Enemies.Items) { if (!CombatActive) break; if (enemy.gameObject.activeSelf) enemy.Tick(dt); }
                foreach (var bullet in Bullets.Items) { if (!CombatActive) break; if (bullet.gameObject.activeSelf) bullet.Tick(dt); }
                foreach (var chip in Pickups.Items) if (chip.gameObject.activeSelf) chip.Tick(dt);
            }
            arenaMaterial.SetFloat("_Luck",JackpotRemaining > 0 || Won ? 1 : 0);
            if (Player.gameObject.activeSelf) arenaMaterial.SetVector("_Focus", Player.Position + Vector2.up * .45f);
            arenaMaterial.SetFloat("_FocusStrength", Player.gameObject.activeSelf ? 1 : 0);
            UI.Tick(Time.unscaledDeltaTime);
        }
        void LateUpdate()
        {
            if (!CombatActive || !Player) return;
            float dt = Time.deltaTime;
            shake = Mathf.MoveTowards(shake, 0, dt * 1.0f);
            followCamera.Tick(Player.Position, Player.Aim, Phase == RunPhase.Boss ? Boss.Position : (Vector2?)null, dt, ScreenShake ? shake : 0);
        }
        public bool IsOnCamera(Vector2 point) => followCamera.IsVisible(point, .25f);
        public bool CanStartAttack(bool ranged)
        {
            int attacks = 0;
            foreach (var enemy in Enemies.Items)
            {
                if (!enemy.Alive || (enemy.State != EnemyState.Windup && enemy.State != EnemyState.Strike)) continue;
                if (ranged && enemy.Ranged) return false;
                if (++attacks >= 3) return false;
            }
            return true;
        }
        public void SpawnEnemy(int kind)
        {
            var enemy = Enemies.Get(); if (!enemy) return;
            Vector2 p = Vector2.zero, best = Vector2.zero;
            float bestScore = float.NegativeInfinity;
            Vector2 view = Vector2.Scale(followCamera.HalfView, new Vector2(.91f,.73f));
            for (int attempt = 0; attempt < 22; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2);
                Vector2 offset = new Vector2(Mathf.Cos(angle) * view.x, Mathf.Sin(angle) * view.y);
                p = ClampToArena(followCamera.Center + offset);
                float distance = Vector2.Distance(p, Player.Position);
                if (distance < 3.7f) continue;
                if (Phase == RunPhase.Boss && (p - Boss.Position).sqrMagnitude < 5.2f) continue;
                bool occupied = false;
                foreach (var other in Enemies.Items)
                    if (other != enemy && other.gameObject.activeSelf && (p - other.Position).sqrMagnitude < 1.15f) { occupied = true; break; }
                float score = -Mathf.Abs(distance - 5.6f) - (occupied ? 5 : 0);
                if (score > bestScore) { bestScore = score; best = p; }
                if (!occupied && distance < 7.5f) break;
            }
            if (float.IsNegativeInfinity(bestScore)) { enemy.gameObject.SetActive(false); return; }
            enemy.Spawn(best,kind,config.waves[Mathf.Clamp(Wave-1,0,3)]);
        }
        public void Shoot(Vector2 position, Vector2 direction, float damage, bool hostile, int pierce)
        {
            var bullet = Bullets.Get(); if (bullet) bullet.Fire(position,direction,damage,hostile,pierce);
        }
        public void Killed(Vector2 position, bool elite)
        {
            Kills++; killProgress += elite ? 4 : 1;
            if (++killChain >= 7)
            {
                killChain = 0;
                Vector2 origin = Player.Position + Vector2.up * .45f;
                for (int i = 0; i < 7; i++) Shoot(origin, HouseVisuals.Rotate(Player.Aim, i * (360f/7)), config.bladeDamage * Rules.DamageMultiplier * 1.5f, false, 1);
                Effects.Ring(Player.Position, HouseVisuals.Gold, 3.3f, .5f);
                Effects.Sparks(origin, HouseVisuals.Gold, 16); Shake(.105f); Audio.Reward(config.streakSound);
                Announce("LUCKY SEVEN  /  BLADE BURST");
            }
            while (killProgress >= 2 && chipsSpawned < 21)
            {
                killProgress -= 2; chipsSpawned++; var chip = Pickups.Get(); if (chip) chip.Spawn(position+UnityEngine.Random.insideUnitCircle*.3f); else Rules.AwardChips(1);
            }
            NotifyHud();
        }
        void OpenIntermission()
        {
            foreach (var chip in Pickups.Items) if (chip.gameObject.activeSelf) chip.Collect();
            Phase = RunPhase.Intermission; Player.SetInput(false); JackpotRemaining = 0;
            foreach (var enemy in Enemies.Items) if (enemy.gameObject.activeSelf) Effects.Sparks(enemy.Position,new Color(.5f,.3f,.8f),3);
            Enemies.Clear(); Bullets.Clear(); UI.ShowIntermission(Rules.Offers(offerRandom)); Audio.SetScene(Phase);
        }
        public bool BuyUpgrade(UpgradeKind kind)
        {
            if (Phase != RunPhase.Intermission || !Rules.Buy(kind)) return false;
            if (kind == UpgradeKind.Vitality || kind == UpgradeKind.Heal) Player.Heal(2);
            NotifyHud(); return true;
        }
        public bool Rig() { if (Phase != RunPhase.Intermission) return false; bool result = Rules.Rig(); if (result) NotifyHud(); return result; }
        public bool Spin()
        {
            if (Phase != RunPhase.Intermission || Rules.Spun) return false;
            LastSpin = Rules.Spin(SpinRandom); LastMatch = LastSpin == Rules.Target;
            Player.Heal(LastMatch ? 2 : 1); Audio.Play(config.reelSound); NotifyHud(); return true;
        }
        public void EndRun(bool win)
        {
            if (!CombatActive) return;
            Won = win; Phase = RunPhase.Results; Player.SetInput(false); JackpotRemaining = 0;
            Enemies.Clear(); Bullets.Clear(); Pickups.Clear(); Boss.gameObject.SetActive(false);
            if (!win) Player.ShowDeath(); else { Effects.Jackpot(Player.Position); Audio.Play(config.jackpotSound); }
            if (Score > BestScore) { PlayerPrefs.SetInt("House77.Best",Score); PlayerPrefs.Save(); }
            Banner = ""; UI.ShowResults(); Audio.SetScene(Phase);
        }
        public void ReturnToTitle()
        {
            Paused = false; Time.timeScale = 1; ClearCombat(); Phase = RunPhase.Title; Player.SetInput(false); Player.gameObject.SetActive(false); UI.ShowTitle(); Audio.SetScene(Phase);
        }
        void ClearCombat()
        {
            if (Enemies == null) return;
            Enemies.Clear(); Bullets.Clear(); Pickups.Clear(); Effects.Clear(); Audio.Clear(); Boss.gameObject.SetActive(false); shake = 0; Banner = "";
        }
        public void TogglePause()
        {
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1; Player.SetInput(!Paused); UI.ShowPause(Paused); Audio.SetScene(Phase, Paused);
        }
        void OnApplicationFocus(bool focus) { if (!focus && CombatActive) TogglePause(); }
        void OnDestroy() { Time.timeScale = 1; if (arenaMaterial) Destroy(arenaMaterial); }
        public void ToggleShake() { ScreenShake = !ScreenShake; PlayerPrefs.SetInt("House77.Shake",ScreenShake ? 1 : 0); PlayerPrefs.Save(); }
        public void Shake(float amount) { shake = Mathf.Max(shake,amount); }
        public void Announce(string text) { Banner = text; bannerTimer = 3; }
        public void NotifyHud() { if (UI) UI.Refresh(); }
        void FitCamera()
        {
            screenWidth = Screen.width; screenHeight = Screen.height;
            float ratio = (float)screenWidth / Mathf.Max(1,screenHeight), target = HouseCamera.Aspect;
            if (ratio < target) { float height=ratio/target; GameCamera.rect=new Rect(0,(1-height)/2,1,height); }
            else { float width=target/ratio; GameCamera.rect=new Rect((1-width)/2,0,width,1); }
        }
        public Vector2 ClampToArena(Vector2 position) => new Vector2(Mathf.Clamp(position.x,-config.arenaHalfSize.x,config.arenaHalfSize.x),Mathf.Clamp(position.y,-config.arenaHalfSize.y,config.arenaHalfSize.y));
        // Editor smoke checks exercise the real transition methods without changing shipping pacing.
        #if UNITY_EDITOR
        public void DebugFinishWave() { if (Phase == RunPhase.Wave) OpenIntermission(); }
        #endif
    }
}
