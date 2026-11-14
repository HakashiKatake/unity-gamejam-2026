using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace HouseOf77.Editor
{
    public static class HouseGameChecks
    {
        [MenuItem("House of 77/Verify Scene References")]
        public static void VerifyScene()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<RunDirector>();
            Require(game && game.Config,"Scene has a serialized GameConfig");
            var c=game.Config;
            foreach(var frames in new[]{c.idle,c.run,c.cast,c.roll,c.hit,c.death}) Require(frames!=null && frames.Length>0 && frames.All(s=>s),"Animation frames exist");
            Require(c.blade && c.machine && c.halberdier && c.valkyrie && c.elite,"Combat artwork exists");
            Require(c.waves.Length==4 && c.waves.All(w=>w.spawnInterval>0 && w.health>0 && w.speed>0),"Four valid wave definitions");
            Require(c.waveSeconds>c.revealSeconds && c.jackpotSeconds>0,"Timers are valid");
            foreach(var material in new[]{c.spriteMaterial,c.arenaMaterial,c.ringMaterial})
            {
                Require(material && material.shader,"Shader materials exist");
                Require(!ShaderUtil.GetShaderMessages(material.shader).Any(m=>m.severity.ToString()=="Error"),"Shaders compile without errors");
            }
            Debug.Log("HOUSE OF 77: scene, animations, combat sprites, tuning and shaders verified.");
        }

        // Runs against the real player, UI, pools, and encounter controller in Play mode.
        // Temporary virtual devices are always removed. No shipping cheat component is added.
        public static string Smoke()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<RunDirector>();
            Require(EditorApplication.isPlaying && game && game.UI,"Game must be running");
            var passed=new List<string>();
            var keyboard=InputSystem.AddDevice<Keyboard>("House77VerificationKeyboard");
            var mouse=InputSystem.AddDevice<Mouse>("House77VerificationMouse");
            bool oldBackground=Application.runInBackground;
            var oldInputBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            var oldBackgroundBehavior=InputSystem.settings.backgroundBehavior;
            bool hadBest=PlayerPrefs.HasKey("House77.Best"); int oldBest=PlayerPrefs.GetInt("House77.Best",0);
            try
            {
                Application.runInBackground=true;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);game.StartRun();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D)); InputSystem.Update();
                var start=game.Player.Position; game.Player.Tick(.1f);
                Require(Vector2.Distance(start,game.Player.Position)>.4f,"WASD moves the real player"); passed.Add("WASD movement");
                int health=game.Player.Health; game.Player.Damage(game.Player.Position+Vector2.left); game.Player.Damage(game.Player.Position+Vector2.left);
                Require(game.Player.Health==health-1,"Damage protection prevents repeated contact hits"); passed.Add("damage and invulnerability");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); InputSystem.Update(); game.Player.Tick(.02f);
                Require(game.Player.DashReady<1 && game.Player.Invincible,"Space triggers dash and protection"); passed.Add("dash input and cooldown");
                game.StartRun(); InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                var aim=game.GameCamera.WorldToScreenPoint(new Vector3(5,-1.6f,0));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(aim.x,aim.y)}); InputSystem.Update(); game.Player.Tick(.02f);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(aim.x,aim.y),buttons=1}); InputSystem.Update(); game.Player.Tick(.02f);
                Require(game.Bullets.Items.Any(b=>b.gameObject.activeSelf),"Mouse aim and fire produce a projectile"); passed.Add("mouse aim and casting");
                var enemy=game.Enemies.Get(); enemy.Spawn(new Vector2(1.5f,-2.2f),0,new WaveTuning{health=1,speed=0,spawnInterval=1}); enemy.Tick(.9f);
                foreach(var bullet in game.Bullets.Items) if(bullet.gameObject.activeSelf)bullet.Tick(.12f);
                Require(game.Kills==1,"A real blade hits and kills a spawned enemy"); passed.Add("projectile collision and enemy death");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(aim.x,aim.y)}); InputSystem.Update();
                game.Rules.AwardChips(21); game.DebugFinishWave();
                Require(game.Phase==RunPhase.Intermission && !InputSystem.actions.FindActionMap("Player").enabled,"Intermission disables combat controls");
                int money=game.Rules.Chips;
                var card=game.UI.GetComponentsInChildren<Button>().First(b=>b.name==""); card.onClick.Invoke(); card.onClick.Invoke();
                Require(game.Rules.Purchased && game.Rules.Chips==money-7,"An upgrade card charges exactly once"); passed.Add("shop click guard and input isolation");
                Require(game.Rig() && game.Rules.Chance==.30,"Rig purchase changes displayed odds"); passed.Add("paid odds transaction");
                var spin=game.UI.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("SPIN FOR")); spin.onClick.Invoke(); spin.onClick.Invoke();
                Require(game.Rules.Spun,"Spin resolves once");
                var next=game.UI.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("CONTINUE")); next.onClick.Invoke();
                Require(game.Phase==RunPhase.Intermission,"Continue is locked during reel animation"); game.UI.Tick(2.1f); next.onClick.Invoke();
                Require(game.Wave==2 && game.Phase==RunPhase.Wave,"Resolved spin advances to the next wave"); passed.Add("reel animation lock and next wave");
                Require(game.Rules.Upgrades.Sum()==1,"Purchased upgrade survives a wave transition");
                if(game.LastMatch)Require(game.JackpotRemaining==game.Config.jackpotSeconds,"Jackpot begins at next encounter");
                game.TogglePause(); float jackpot=game.JackpotRemaining;
                Require(game.Paused && !game.CombatActive && Time.timeScale==0,"Pause freezes combat");
                Require(game.JackpotRemaining==jackpot,"Pause preserves jackpot timer"); game.TogglePause(); passed.Add("pause and resume");
                while(game.Wave<=4){game.DebugFinishWave();game.Spin();game.BeginNextEncounter();}
                Require(game.Phase==RunPhase.Boss && game.Boss.Alive,"Fourth spin always leads to boss");
                bool sawFan=false,sawBeam=false,sawSummon=false;
                for(int i=0;i<36;i++)
                {
                    game.Boss.Tick(.3f);
                    sawFan |= game.Bullets.Items.Any(b=>b.gameObject.activeSelf);
                    sawBeam |= game.Boss.BeamWarning;
                    sawSummon |= game.Enemies.Items.Any(e=>e.gameObject.activeSelf);
                }
                Require(sawFan,"Boss fires its fan");Require(sawBeam,"Boss telegraphs its beam");Require(sawSummon,"Boss summons warriors");
                passed.Add("all waves and boss attack cycle");
                game.Boss.Hit(game.Config.bossHealth+1); Require(game.Won && game.Phase==RunPhase.Results,"Defeating boss opens victory"); passed.Add("victory");
                game.StartRun();Require(game.Rules.Chips==7 && game.Rules.Upgrades.Sum()==0 && game.Player.Health==7 && game.JackpotRemaining==0 && !game.Bullets.Items.Any(b=>b.gameObject.activeSelf),"Retry clears all run state"); passed.Add("clean retry");
                for(int i=0;i<7;i++){game.Player.Tick(1);game.Player.Damage(game.Player.Position+Vector2.left);}
                Require(game.Phase==RunPhase.Results && !game.Won,"Zero health opens defeat"); passed.Add("defeat");
                return JsonUtility.ToJson(new SmokeReport{passed=true,checks=passed.ToArray()},true);
            }
            finally
            {
                Application.runInBackground=oldBackground;
                InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
                InputSystem.settings.editorInputBehaviorInPlayMode=oldInputBehavior;
                InputSystem.settings.backgroundBehavior=oldBackgroundBehavior;
                if(hadBest)PlayerPrefs.SetInt("House77.Best",oldBest);else PlayerPrefs.DeleteKey("House77.Best");PlayerPrefs.Save();
                game.ReturnToTitle();
            }
        }
        [Serializable] sealed class SmokeReport { public bool passed; public string[] checks; }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}

        [MenuItem("House of 77/Capture Preview (Play mode)")]
        public static void CapturePreview() { Capture("/tmp/house77-preview.png"); }
        public static void Capture(string path)
        {
            var game=UnityEngine.Object.FindAnyObjectByType<RunDirector>();Require(game && game.UI,"Run the game before capturing");
            var canvas=game.UI.GetComponent<Canvas>();var camera=game.GameCamera;var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;
            var oldRT=camera.targetTexture;var oldRect=camera.rect;var oldActive=RenderTexture.active;
            var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {canvas.renderMode=mode;canvas.worldCamera=oldCamera;camera.targetTexture=oldRT;camera.rect=oldRect;RenderTexture.active=oldActive;UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);Canvas.ForceUpdateCanvases();}
        }
    }
}
