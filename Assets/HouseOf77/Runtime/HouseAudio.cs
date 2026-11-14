using UnityEngine;

namespace HouseOf77
{
    public sealed class HouseAudio : MonoBehaviour
    {
        AudioSource effects;
        readonly AudioSource[] music = new AudioSource[2];
        GameConfig config;
        int activeMusic;
        float musicVolume, impactCooldown, castCooldown, windupCooldown, deathCooldown, chipCooldown, duckUntil;
        public bool Muted { get; private set; }
        public AudioClip CurrentMusic => music[activeMusic].clip;

        public void Initialize(GameConfig value)
        {
            config = value;
            Muted = PlayerPrefs.GetInt("House77.Mute", 0) == 1;
            effects = NewSource(false, .64f);
            music[0] = NewSource(true, 0); music[1] = NewSource(true, 0);
        }
        AudioSource NewSource(bool loop, float volume)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0;
            source.volume = volume; source.mute = Muted; return source;
        }
        public void SetScene(RunPhase phase, bool paused = false)
        {
            bool runTrack = phase == RunPhase.Wave || phase == RunPhase.Boss || phase == RunPhase.Intermission;
            var clip = runTrack ? config.combatMusic : config.menuMusic;
            musicVolume = paused ? .065f : phase == RunPhase.Intermission ? .13f : phase == RunPhase.Boss ? .32f : runTrack ? .25f : .30f;
            if (CurrentMusic != clip)
            {
                activeMusic = 1 - activeMusic;
                music[activeMusic].Stop();
                music[activeMusic].clip = clip;
                music[activeMusic].volume = 0;
            }
            if (clip && !music[activeMusic].isPlaying) music[activeMusic].Play();
        }
        void Update()
        {
            if (!effects) return;
            float budget = Time.unscaledDeltaTime * .64f;
            for (int i = 0; i < music.Length; i++)
            {
                float target = i == activeMusic ? musicVolume * (Time.unscaledTime < duckUntil ? .48f : 1) : 0;
                music[i].volume = Mathf.MoveTowards(music[i].volume, target, budget);
                if (i != activeMusic && music[i].volume == 0 && music[i].isPlaying) music[i].Stop();
            }
        }
        public void ToggleMute()
        {
            Muted = !Muted; effects.mute = Muted;
            foreach (var source in music) source.mute = Muted;
            PlayerPrefs.SetInt("House77.Mute", Muted ? 1 : 0); PlayerPrefs.Save();
        }
        public void Play(AudioClip clip, float volume = 1) { if (clip) effects.PlayOneShot(clip, volume); }
        public void Cast() { if (Time.unscaledTime < castCooldown) return; castCooldown = Time.unscaledTime + .09f; Play(config.castSound, .28f); }
        public void Impact() { if (Time.unscaledTime < impactCooldown) return; impactCooldown = Time.unscaledTime + .08f; Play(config.impactSound, .42f); }
        public void Windup() { if (Time.unscaledTime < windupCooldown) return; windupCooldown = Time.unscaledTime + .45f; Play(config.enemyWindupSound, .47f); }
        public void DefeatEnemy() { if (Time.unscaledTime < deathCooldown) return; deathCooldown = Time.unscaledTime + .12f; Play(config.deathSound, .44f); }
        public void Chip() { if (Time.unscaledTime < chipCooldown) return; chipCooldown = Time.unscaledTime + .065f; Play(config.chipSound,.43f); }
        public void Reward(AudioClip clip) { Play(clip, .86f); duckUntil = Time.unscaledTime + .75f; }
        public void Clear() { effects.Stop(); duckUntil = 0; }
    }
}
