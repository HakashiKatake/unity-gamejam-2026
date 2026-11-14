using UnityEngine;

namespace HouseOf77
{
    public sealed class HouseAudio : MonoBehaviour
    {
        AudioSource source; GameConfig config; float impactCooldown, castCooldown;
        public bool Muted { get; private set; }
        public void Initialize(GameConfig value)
        {
            config = value; source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; source.volume = .55f; Muted = PlayerPrefs.GetInt("House77.Mute", 0) == 1; source.mute = Muted;
        }
        public void ToggleMute() { Muted = !Muted; source.mute = Muted; PlayerPrefs.SetInt("House77.Mute", Muted ? 1 : 0); PlayerPrefs.Save(); }
        public void Play(AudioClip clip, float volume = 1) { if (clip) source.PlayOneShot(clip, volume); }
        public void Cast() { if (Time.unscaledTime < castCooldown) return; castCooldown = Time.unscaledTime + .09f; Play(config.castSound, .38f); }
        public void Impact() { if (Time.unscaledTime < impactCooldown) return; impactCooldown = Time.unscaledTime + .055f; Play(config.impactSound, .5f); }
        public void Clear() { source.Stop(); }
    }
}
