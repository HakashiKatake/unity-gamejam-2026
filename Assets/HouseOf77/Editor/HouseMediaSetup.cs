using System;
using UnityEditor;
using UnityEngine;

namespace HouseOf77.Editor
{
    public static class HouseMediaSetup
    {
        const string Root = "Assets/HouseOf77/Media/";

        [MenuItem("House of 77/Assign Licensed Fonts and Audio")]
        public static void Assign()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/HouseOf77/Configuration/HouseOf77.asset");
            if (!config) throw new InvalidOperationException("House of 77 needs its GameConfig asset.");
            config.displayFont = Require<Font>("Fonts/CinzelDecorative-Black.ttf");
            config.bodyFont = Require<Font>("Fonts/BarlowSemiCondensed-Regular.ttf");
            config.strongFont = Require<Font>("Fonts/BarlowSemiCondensed-SemiBold.ttf");
            config.menuMusic = Music("Deadly Roulette.mp3");
            config.combatMusic = Music("Darkling.mp3");
            if (!config.castSound) config.castSound = Sound("cloth2.ogg");
            if (!config.impactSound) config.impactSound = Sound("impactMetal_light_002.ogg");
            if (!config.chipSound) config.chipSound = Sound("chips-collide-2.ogg");
            if (!config.reelSound) config.reelSound = Sound("chip-lay-2.ogg");
            if (!config.jackpotSound) config.jackpotSound = Sound("jingles_STEEL07.ogg");
            if (!config.damageSound) config.damageSound = Sound("chop.ogg");
            if (!config.bossSound) config.bossSound = Sound("impactMetal_heavy_001.ogg");
            if (!config.dashSound) config.dashSound = Sound("card-shove-1.ogg");
            if (!config.enemyWindupSound) config.enemyWindupSound = Sound("drawKnife2.ogg");
            if (!config.deathSound) config.deathSound = Sound("chop.ogg");
            if (!config.streakSound) config.streakSound = Sound("chips-collide-2.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        static AudioClip Music(string name)
        {
            string relative = "Audio/" + name;
            var importer = AssetImporter.GetAtPath(Root + relative) as AudioImporter;
            if (!importer) throw new InvalidOperationException("Missing music asset: " + name);
            var settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.CompressedInMemory || settings.compressionFormat != AudioCompressionFormat.Vorbis || Math.Abs(settings.quality - .56f) > .01f)
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .56f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return Require<AudioClip>(relative);
        }
        static AudioClip Sound(string name) => Require<AudioClip>("Audio/" + name);
        static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Root + path);
            if (!asset) throw new InvalidOperationException("Missing licensed media: " + path);
            return asset;
        }
    }
}
