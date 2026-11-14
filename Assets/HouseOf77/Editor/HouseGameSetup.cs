using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace HouseOf77.Editor
{
    public static class HouseGameSetup
    {
        public const string ScenePath = "Assets/HouseOf77/Scenes/HouseOf77.unity";
        const string ConfigPath = "Assets/HouseOf77/Configuration/HouseOf77.asset";
        const string BladeRoot = "Assets/Elementals_metal_bladekeeper_FREE_v1.1/PNG animations/";
        [MenuItem("House of 77/Prepare Game")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/HouseOf77/Configuration"); Directory.CreateDirectory("Assets/HouseOf77/Scenes");
            AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (!config) { config = ScriptableObject.CreateInstance<GameConfig>(); AssetDatabase.CreateAsset(config,ConfigPath); }
            config.idle = Frames("01_idle"); config.run = Frames("02_run"); config.cast = Frames("05_projectile_cast");
            config.roll = Frames("04_roll"); config.hit = Frames("12_take_hit"); config.death = Frames("13_death");
            config.halberdier = Import("Assets/Human Warriors [FREE]/Original Color/Halberdier.png",32,new Vector2(.5f,.12f));
            config.valkyrie = Import("Assets/Human Warriors [FREE]/Original Color/Valkyrie.png",32,new Vector2(.5f,.12f));
            config.elite = Import("Assets/Human Warriors [FREE]/Endesca/Halberdier.png",32,new Vector2(.5f,.12f));
            config.machine = Import("Assets/Pixel Fantasy Slot Machine/Slot Machine/slot-machine1.png",160,new Vector2(.5f,.5f));
            config.symbols = Enumerable.Range(1,4).Select(i => Import("Assets/Pixel Fantasy Slot Machine/Slot Machine/slot-symbol"+i+".png",32,new Vector2(.5f,.5f))).ToArray();
            string icon = "Assets/HollowCreek_SurvivalIcons_Vol1_Free/icons/";
            config.icons = new [] {Import(icon+"weapons/katana.png",16,new Vector2(.5f,.5f)),Import(icon+"materials/gears.png",16,new Vector2(.5f,.5f)),Import(icon+"weapons/crossbow.png",16,new Vector2(.5f,.5f)),Import(icon+"tools/nails.png",16,new Vector2(.5f,.5f)),Import(icon+"medical/herb.png",16,new Vector2(.5f,.5f)),Import(icon+"tools/compass.png",16,new Vector2(.5f,.5f))};
            config.titleLayers = Enumerable.Range(1,5).Select((i) => {
                string[] names={"sky","mountains","hills","forest","foreground"};
                return Import("Assets/nashas_parallax_worlds_free_castle_town_and_dungeon_gate_v1.0/dungeon_gate/320x180/layer_"+i+"_"+names[i-1]+".png",32,new Vector2(.5f,.5f));
            }).ToArray();
            config.blade = Import(BladeRoot+"projectile_and_trap/projectile_throw/projectile_throw.png",32,new Vector2(.5f,.5f));
            // A cropped sprite subasset references the original image. No new texture is generated.
            if (!AssetDatabase.LoadAllAssetsAtPath(ConfigPath).Any(a => a.name == "Blade projectile crop"))
            {
                var texture = config.blade.texture; var pixels = texture.GetPixels32(); int x0=texture.width,y0=texture.height,x1=0,y1=0;
                for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) if(pixels[y*texture.width+x].a>8) {x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
                var sprite=Sprite.Create(texture,new Rect(x0,y0,x1-x0+1,y1-y0+1),new Vector2(.5f,.5f),32); sprite.name="Blade projectile crop";
                AssetDatabase.AddObjectToAsset(sprite,config); config.blade=sprite;
            }
            else config.blade = AssetDatabase.LoadAllAssetsAtPath(ConfigPath).OfType<Sprite>().First(a => a.name == "Blade projectile crop");
            config.arenaMaterial = Material("Arena","HouseOf77/Arena");
            config.spriteMaterial = Material("SpriteFlash","HouseOf77/SpriteFlash");
            config.ringMaterial = Material("Ring","HouseOf77/Ring");
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            var current = SceneManager.GetActiveScene();
            if (current.isDirty) throw new InvalidOperationException("Save the current scene before preparing the game.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera = new GameObject("Main Camera"); camera.tag="MainCamera";
            var c = camera.AddComponent<Camera>(); c.orthographic=true; c.orthographicSize=9; c.transform.position=new Vector3(0,0,-10); c.backgroundColor=new Color(.02f,.025f,.04f); camera.AddComponent<AudioListener>();
            var root = new GameObject("HOUSE OF 77"); var director = root.AddComponent<RunDirector>();
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var serialized = new SerializedObject(director); serialized.FindProperty("config").objectReferenceValue=config; serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director); EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var oldScenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList(); oldScenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));
            // Preserve the existing scene list, but ship only the new game's scene by default.
            for (int i=1;i<oldScenes.Count;i++) oldScenes[i].enabled=false;
            EditorBuildSettings.scenes=oldScenes.ToArray();
            PlayerSettings.productName="House of 77"; PlayerSettings.companyName="Saurabh and Prince";
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720; PlayerSettings.runInBackground=false;
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.memorySize=256;
            AssetDatabase.SaveAssets();
            Debug.Log("HOUSE OF 77 ready: "+ScenePath);
        }
        static Sprite[] Frames(string folder)
        {
            return Directory.GetFiles(BladeRoot+folder,"*.png").OrderBy(path => {
                string name=Path.GetFileNameWithoutExtension(path); int start=name.LastIndexOf('_')+1;
                return int.TryParse(name.Substring(start),out int n) ? n : 0;
            }).Select(path=>Import(path.Replace('\\','/'),32,new Vector2(.5f,.04f))).ToArray();
        }
        static Sprite Import(string path, float ppu, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing imported art",path);
            bool dirty=importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single || importer.filterMode!=FilterMode.Point || importer.spritePixelsPerUnit!=ppu || !importer.isReadable || importer.spritePivot!=pivot;
            if(dirty)
            {
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.filterMode=FilterMode.Point;
                importer.spritePixelsPerUnit=ppu; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=true; importer.isReadable=true;
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment=(int)SpriteAlignment.Custom; settings.spritePivot=pivot; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Material Material(string name, string shaderName)
        {
            string path="Assets/HouseOf77/Configuration/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find(shaderName); if(!shader) throw new InvalidOperationException("Missing shader "+shaderName);
            if(!material) {material=new Material(shader);AssetDatabase.CreateAsset(material,path);} else material.shader=shader;
            EditorUtility.SetDirty(material); return material;
        }
        [MenuItem("House of 77/Build WebGL")]
        public static void BuildWeb()
        {
            Build(BuildTarget.WebGL,"Builds/WebGL");
        }
        [MenuItem("House of 77/Build Windows")]
        public static void BuildWindows() { Build(BuildTarget.StandaloneWindows64,"Builds/Windows/HouseOf77.exe"); }
        static void Build(BuildTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new []{ScenePath},locationPathName=path,target=target,options=BuildOptions.None });
            bool filesExist=target==BuildTarget.WebGL ? File.Exists(Path.Combine(path,"index.html")) : File.Exists(path);
            if(report.summary.result!=BuildResult.Succeeded || report.summary.totalErrors>0 || !filesExist) throw new Exception("House of 77 export incomplete: "+report.summary.totalErrors+" errors; playable output present: "+filesExist);
            Debug.Log("HOUSE OF 77 BUILD OK: "+path+" / "+report.summary.totalSize+" bytes");
        }
    }
}
