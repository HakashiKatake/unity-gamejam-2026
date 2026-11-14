using UnityEngine;

namespace HouseOf77
{
    [CreateAssetMenu(menuName = "House of 77/Game configuration")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Run tuning")]
        public float waveSeconds = 75;
        public float revealSeconds = 30;
        public float jackpotSeconds = 20;
        public float playerSpeed = 5.8f;
        public float bladeDamage = 12;
        public float shotsPerSecond = 4.5f;
        public float bossHealth = 5000;
        public int enemyCapacity = 60;
        public int projectileCapacity = 250;
        public Vector2 arenaHalfSize = new Vector2(14.8f, 7.1f);
        public WaveTuning[] waves = {
            new WaveTuning { spawnInterval = 1.0f, health = 22, speed = 1.4f },
            new WaveTuning { spawnInterval = .78f, health = 29, speed = 1.55f },
            new WaveTuning { spawnInterval = .63f, health = 35, speed = 1.7f },
            new WaveTuning { spawnInterval = .52f, health = 40, speed = 1.85f }
        };
        [Header("Imported artwork")]
        public Sprite[] idle, run, cast, roll, hit, death;
        public Sprite blade, halberdier, valkyrie, elite, machine;
        public Sprite[] symbols, icons, titleLayers;
        [Header("Handwritten shaders")]
        public Material arenaMaterial, spriteMaterial, ringMaterial;
        [Header("Human recordings — assign clips here")]
        public AudioClip castSound, impactSound, chipSound, reelSound, jackpotSound, damageSound, bossSound;
    }
    [System.Serializable]
    public struct WaveTuning
    {
        public float spawnInterval, health, speed;
    }
}
