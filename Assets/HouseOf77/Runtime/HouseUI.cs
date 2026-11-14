using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HouseOf77
{
    public sealed class HouseUI : MonoBehaviour
    {
        static readonly Color Ink = new Color(.025f,.032f,.055f,.97f);
        static readonly Color Card = new Color(.065f,.075f,.105f,.98f);
        static readonly Color Muted = new Color(.63f,.66f,.72f);
        static readonly Color Cream = new Color(.95f,.91f,.81f);
        static readonly string[] Names = { "SHARPEN", "QUICKCAST", "SPLIT SHOT", "PIERCE", "VITALITY", "SECOND WIND", "RESTORE" };
        static readonly string[] Descriptions = {
            "+20% blade damage\nMake every hit count.", "+15% casting speed\nKeep the pressure on.", "Two extra angled blades\n40% damage per side blade.", "Pierce one more enemy\nCut through the crowd.", "+1 maximum health\nRestore two health now.", "20% shorter dash cooldown\nAlways an escape route.", "Restore two health\nA little breathing room."
        };
        RunDirector game; Font font, displayFont, strongFont; RectTransform root;
        GameObject title, hud, intermission, results, pause, credits;
        Text health, wave, time, chips, target, pity, banner, jackpot, bossName, footer, best;
        Text shopChips, shopTarget, odds, spinStatus, reelLeft, reelMiddle, reelRight, resultTitle, resultStats;
        Text muteLabel, shakeLabel, titleMute, streak;
        Image waveFill, dashFill, jackpotFill, bossFill, streakFill;
        GameObject jackpotPanel, bossPanel;
        Button rigButton, spinButton, continueButton;
        readonly List<Button> upgradeButtons = new List<Button>();
        readonly List<Text> upgradeLabels = new List<Text>();
        readonly List<Image> upgradeIcons = new List<Image>();
        readonly List<UpgradeKind> offers = new List<UpgradeKind>();
        readonly List<RectTransform> titleArt = new List<RectTransform>();
        float reelTime, reelTick, refreshTime; bool presenting;

        public void Initialize(RunDirector owner)
        {
            game = owner; font = owner.Config.bodyFont ? owner.Config.bodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            displayFont = owner.Config.displayFont ? owner.Config.displayFont : font;
            strongFont = owner.Config.strongFont ? owner.Config.strongFont : font;
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.dynamicPixelsPerUnit = 3f;
            gameObject.AddComponent<GraphicRaycaster>();
            root = Rect("Safe layout",transform,640,360,1280,720); root.anchorMin = root.anchorMax = new Vector2(.5f,.5f); root.anchoredPosition = Vector2.zero;
            BuildTitle(); BuildHud(); BuildIntermission(); BuildResults(); BuildPause(); BuildCredits();
            Refresh();
        }
        RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name,typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent,false);
            rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(.5f,.5f); rt.anchoredPosition = new Vector2(x,-y); rt.sizeDelta = new Vector2(width,height); return rt;
        }
        GameObject Panel(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            var rt = Rect(name,parent,x,y,width,height); var image = rt.gameObject.AddComponent<Image>(); image.color = color; return rt.gameObject;
        }
        Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(value.Length > 25 ? value.Substring(0,25) : value,parent,x,y,width,height); var text = rt.gameObject.AddComponent<Text>();
            text.font = style == FontStyle.Bold ? strongFont : font; text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment;
            text.fontStyle = text.font == font ? style : FontStyle.Normal;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; return text;
        }
        Text Heading(Text text) { text.font = displayFont; text.fontStyle = FontStyle.Normal; return text; }
        Image Picture(Transform parent, Sprite sprite, float x, float y, float width, float height, Color color, bool preserve = true)
        {
            var rt = Rect("Imported art",parent,x,y,width,height); var image = rt.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.preserveAspect = preserve; image.raycastTarget = false; return image;
        }
        Button Button(Transform parent, string value, float x, float y, float width, float height, Action click, bool gold = false)
        {
            var panel = Panel(value,parent,x,y,width,height,gold ? HouseVisuals.Gold : Card);
            var b = panel.AddComponent<Button>(); var colors = b.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f,1.1f,.95f); colors.pressedColor = new Color(.7f,.7f,.7f); colors.disabledColor = new Color(.42f,.42f,.42f,.8f); b.colors = colors;
            b.onClick.AddListener(() => click());
            Label(panel.transform,value,width/2,height/2,width-22,height,gold ? 18 : 15,gold ? Ink : Cream,TextAnchor.MiddleCenter,FontStyle.Bold);
            return b;
        }
        void Rule(Transform parent, float x, float y, float width, Color color) { var p = Panel("Rule",parent,x,y,width,1,color); p.GetComponent<Image>().raycastTarget = false; }
        Image Bar(Transform parent, float x, float y, float width, Color color)
        {
            var bg = Panel("Meter",parent,x,y,width,4,new Color(.2f,.21f,.26f));
            var fill = Panel("Fill",bg.transform,width/2,2,width,4,color).GetComponent<Image>();
            fill.sprite = HouseVisuals.White; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.raycastTarget = false; return fill;
        }
        void Focus(Button b) { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(b.gameObject); }
        void HideAll()
        {
            title.SetActive(false); hud.SetActive(false); intermission.SetActive(false); results.SetActive(false); pause.SetActive(false); credits.SetActive(false);
        }
        void BuildTitle()
        {
            title = Panel("Title",root,640,360,1280,720,Ink);
            for (int i = 0; i < game.Config.titleLayers.Length; i++)
            {
                var image = Picture(title.transform,game.Config.titleLayers[i],640,360,1400,790,new Color(.6f,.62f,.8f,.6f),false);
                titleArt.Add(image.rectTransform);
            }
            Panel("Reading veil",title.transform,360,360,760,720,new Color(.015f,.021f,.04f,.9f));
            Panel("Lower shade",title.transform,640,650,1280,140,new Color(.015f,.021f,.04f,.9f));
            Label(title.transform,"A CURSED CASINO SURVIVAL",318,72,480,24,15,HouseVisuals.Gold);
            Rule(title.transform,176,105,196,new Color(.6f,.45f,.23f));
            Heading(Label(title.transform,"HOUSE",316,204,480,116,86,Cream,TextAnchor.MiddleLeft));
            Heading(Label(title.transform,"OF",129,307,102,90,38,Muted));
            Heading(Label(title.transform,"77",318,306,270,148,136,HouseVisuals.Gold,TextAnchor.MiddleLeft));
            Label(title.transform,"FIGHT TO RIG YOUR FATE.",294,414,432,35,24,Cream,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label(title.transform,"Four waves. One lucky number.\nSurvive the warriors. Break the house.",300,474,444,62,18,Muted);
            var play = Button(title.transform,"ENTER THE HOUSE    →",263,564,370,58,() => game.StartRun(),true);
            Button(title.transform,"CREDITS",166,624,176,36,() => { credits.SetActive(true); Focus(credits.GetComponentInChildren<Button>()); });
            var mute = Button(title.transform,"SOUND ON",359,624,176,36,() => { game.Audio.ToggleMute(); Refresh(); }); titleMute = mute.GetComponentInChildren<Text>();
            Picture(title.transform,game.Config.machine,938,325,442,354,Color.white);
            Label(title.transform,"THE HOUSE IS WAITING",937,120,420,26,15,HouseVisuals.Gold,TextAnchor.MiddleCenter);
            Heading(Label(title.transform,"77",951,345,220,75,70,Ink,TextAnchor.MiddleCenter));
            Label(title.transform,"LUCK CAN BE BOUGHT.\nSURVIVAL HAS TO BE EARNED.",949,553,430,62,18,Cream,TextAnchor.MiddleCenter);
            best = Label(title.transform,"BEST  000000",1009,657,380,26,15,Cream,TextAnchor.MiddleRight);
            Label(title.transform,"WASD  MOVE     /     MOUSE  AIM & FIRE     /     SPACE  DASH",400,686,650,24,14,Cream);
            play.name = "Start run";
        }
        void BuildHud()
        {
            hud = new GameObject("Combat HUD",typeof(RectTransform)); hud.transform.SetParent(root,false);
            var rt = (RectTransform)hud.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            Panel("Top band",hud.transform,640,37,1280,74,new Color(.015f,.02f,.035f,.94f));
            Heading(Label(hud.transform,"HOUSE / 77",126,25,204,32,20,HouseVisuals.Gold));
            health = Label(hud.transform,"",139,53,230,26,17,Cream);
            wave = Label(hud.transform,"WAVE 01 / 04",640,25,320,26,16,Cream,TextAnchor.MiddleCenter,FontStyle.Bold);
            time = Label(hud.transform,"01:15",640,53,190,28,22,HouseVisuals.Gold,TextAnchor.MiddleCenter);
            chips = Label(hud.transform,"7 CHIPS",1107,29,260,30,25,HouseVisuals.Gold,TextAnchor.MiddleRight,FontStyle.Bold);
            Label(hud.transform,"SPEND THEM BETWEEN WAVES",1092,55,290,20,11,Muted,TextAnchor.MiddleRight);
            waveFill = Bar(hud.transform,640,74,1280,HouseVisuals.Gold);
            var luck = Panel("Lucky number",hud.transform,904,37,192,59,new Color(.075f,.065f,.09f,.92f));
            Label(luck.transform,"LUCKY NUMBER",69,18,118,21,13,HouseVisuals.Gold);
            target = Label(luck.transform,"? ?",150,28,68,51,35,Cream,TextAnchor.MiddleCenter,FontStyle.Bold);
            pity = Label(luck.transform,"REVEALS IN 00:45",73,41,125,20,12,Muted);
            banner = Label(hud.transform,"",640,143,900,50,23,HouseVisuals.Gold,TextAnchor.MiddleCenter,FontStyle.Bold);
            jackpotPanel = Panel("Overdrive",hud.transform,640,615,430,56,new Color(.09f,.07f,.035f,.94f));
            jackpot = Label(jackpotPanel.transform,"JACKPOT OVERDRIVE",215,23,410,30,21,HouseVisuals.Gold,TextAnchor.MiddleCenter,FontStyle.Bold);
            jackpotFill = Bar(jackpotPanel.transform,215,50,410,HouseVisuals.Gold);
            bossPanel = Panel("Boss health",hud.transform,640,115,480,58,new Color(.04f,.02f,.03f,.9f));
            bossName = Label(bossPanel.transform,"THE HOUSE",240,20,460,26,16,HouseVisuals.Red,TextAnchor.MiddleCenter,FontStyle.Bold);
            bossFill = Bar(bossPanel.transform,240,46,448,HouseVisuals.Red);
            Panel("Bottom band",hud.transform,640,690,1280,60,new Color(.015f,.02f,.035f,.93f));
            footer = Label(hud.transform,"",315,683,570,25,13,Muted);
            streak = Label(hud.transform,"LUCKY SEVEN   0 / 7",751,679,242,25,15,HouseVisuals.Gold,TextAnchor.MiddleCenter,FontStyle.Bold);
            streakFill = Bar(hud.transform,751,701,214,HouseVisuals.Gold);
            Label(hud.transform,"SPACE / DASH",1038,679,150,25,12,Cream,TextAnchor.MiddleRight);
            dashFill = Bar(hud.transform,1029,701,150,HouseVisuals.Blue);
            Button(hud.transform,"PAUSE",1180,690,126,34,() => game.TogglePause());
        }
        void BuildIntermission()
        {
            intermission = Panel("Upgrade and spin",root,640,360,1280,720,new Color(.018f,.024f,.043f,.985f));
            Label(intermission.transform,"BETWEEN THE WAVES",284,46,440,24,13,HouseVisuals.Gold);
            Heading(Label(intermission.transform,"POWER OR PROBABILITY?",437,94,746,66,34,Cream));
            shopChips = Label(intermission.transform,"",1093,71,270,52,30,HouseVisuals.Gold,TextAnchor.MiddleRight,FontStyle.Bold);
            Rule(intermission.transform,640,141,1152,new Color(.35f,.28f,.17f));
            var machinePanel = Panel("Machine panel",intermission.transform,288,382,448,442,Card);
            Label(machinePanel.transform,"YOUR LUCKY NUMBER",224,26,410,28,13,Muted,TextAnchor.MiddleCenter);
            shopTarget = Label(machinePanel.transform,"77",224,75,410,70,58,HouseVisuals.Gold,TextAnchor.MiddleCenter,FontStyle.Bold);
            Picture(machinePanel.transform,game.Config.machine,224,244,330,258,Color.white);
            reelLeft = Label(machinePanel.transform,"?",144,236,56,70,40,Ink,TextAnchor.MiddleCenter,FontStyle.Bold);
            reelMiddle = Label(machinePanel.transform,"?",222,236,56,70,40,Ink,TextAnchor.MiddleCenter,FontStyle.Bold);
            reelRight = Label(machinePanel.transform,"?",302,236,56,70,40,Ink,TextAnchor.MiddleCenter,FontStyle.Bold);
            spinStatus = Label(machinePanel.transform,"",224,384,414,63,17,Cream,TextAnchor.MiddleCenter,FontStyle.Bold);
            Label(intermission.transform,"01 / BUILD YOUR ADVANTAGE",863,181,630,30,14,HouseVisuals.Gold);
            Label(intermission.transform,"One permanent upgrade. Seven chips. Your choice.",866,211,636,28,15,Muted);
            for (int i = 0; i < 3; i++)
            {
                int index = i; float x = 650+i*210;
                var card = Button(intermission.transform,"",x,330,196,192,() => Buy(index));
                var label = card.GetComponentInChildren<Text>(); label.rectTransform.anchoredPosition = new Vector2(98,-117); label.rectTransform.sizeDelta = new Vector2(178,130); label.fontSize = 15;
                upgradeButtons.Add(card); upgradeLabels.Add(label);
                upgradeIcons.Add(Picture(card.transform,game.Config.icons[i],98,37,36,36,Color.white));
                Rule(card.transform,98,67,158,new Color(.3f,.25f,.16f));
            }
            Label(intermission.transform,"02 / TIP THE ODDS",867,468,638,28,14,HouseVisuals.Gold);
            odds = Label(intermission.transform,"",790,510,484,42,26,Cream,TextAnchor.MiddleLeft,FontStyle.Bold);
            rigButton = Button(intermission.transform,"RIG + / 7 CHIPS",1110,512,210,48,() => { if (game.Rig()) RefreshShop(); });
            Label(intermission.transform,"15%  →  30%  →  50%  →  75%     /     TWO MISSES GUARANTEE THE NEXT MATCH",879,555,662,34,12,Muted);
            Rule(intermission.transform,640,622,1152,new Color(.35f,.28f,.17f));
            spinButton = Button(intermission.transform,"SPIN FOR YOUR FATE    →",995,668,418,56,BeginSpin,true);
            continueButton = Button(intermission.transform,"CONTINUE    →",995,668,418,56,() => { if (!presenting && game.Rules.Spun) game.BeginNextEncounter(); },true);
            Label(intermission.transform,"UPGRADES STAY. CHIPS CARRY. A MISS IS NEVER THE END.",358,665,590,36,12,Muted);
        }
        void Buy(int index)
        {
            if (presenting || index >= offers.Count) return;
            if (game.BuyUpgrade(offers[index])) RefreshShop();
        }
        public void ShowIntermission(List<UpgradeKind> choices)
        {
            HideAll(); intermission.SetActive(true); presenting = false; offers.Clear(); offers.AddRange(choices);
            reelLeft.text = reelMiddle.text = reelRight.text = "?";
            shopTarget.text = game.Rules.Target.ToString(); spinStatus.text = game.Wave == 4 ? "ONE LAST SPIN. THEN FACE THE HOUSE." : "A BETTER BUILD. A BETTER CHANCE.";
            RefreshShop(); Focus(spinButton);
        }
        void RefreshShop()
        {
            shopChips.text = game.Rules.Chips+" CHIPS";
            for (int i = 0; i < upgradeButtons.Count; i++)
            {
                bool exists = i < offers.Count; upgradeButtons[i].gameObject.SetActive(exists); if (!exists) continue;
                int id = (int)offers[i]; bool bought = game.Rules.Purchased;
                upgradeIcons[i].sprite = game.Config.icons[Mathf.Min(id,game.Config.icons.Length-1)];
                upgradeLabels[i].text = Names[id]+"\n\n"+Descriptions[id]+"\n\n"+(bought ? "PURCHASE CLOSED" : "BUY / 7 CHIPS");
                upgradeButtons[i].interactable = !presenting && game.Rules.CanBuy(offers[i]);
            }
            odds.text = game.Rules.Misses >= 2 ? "100% / HOUSE OWES YOU" : Mathf.RoundToInt((float)game.Rules.Chance*100)+"% MATCH CHANCE";
            rigButton.interactable = !presenting && game.Rules.CanRig;
            spinButton.gameObject.SetActive(!game.Rules.Spun); spinButton.interactable = !presenting;
            continueButton.gameObject.SetActive(game.Rules.Spun); continueButton.interactable = !presenting;
            continueButton.GetComponentInChildren<Text>().text = game.Wave == 4 ? "FACE THE HOUSE    →" : "ENTER WAVE 0"+(game.Wave+1)+"    →";
        }
        void BeginSpin()
        {
            if (presenting || !game.Spin()) return;
            presenting = true; reelTime = 1.9f; reelTick = 0; spinStatus.text = "THE HOUSE IS DECIDING..."; RefreshShop();
        }
        void BuildResults()
        {
            results = Panel("Results",root,640,360,1280,720,new Color(.018f,.024f,.043f,.96f));
            foreach (var layer in game.Config.titleLayers) Picture(results.transform,layer,640,360,1280,720,new Color(.4f,.4f,.6f,.18f),false);
            Label(results.transform,"HOUSE / 77",640,80,900,40,19,HouseVisuals.Gold,TextAnchor.MiddleCenter);
            resultTitle = Heading(Label(results.transform,"",640,203,1100,100,57,Cream,TextAnchor.MiddleCenter));
            Rule(results.transform,640,285,700,new Color(.45f,.33f,.17f));
            resultStats = Label(results.transform,"",640,393,950,175,23,Cream,TextAnchor.MiddleCenter);
            Button(results.transform,"ANOTHER HAND    →",640,565,380,58,() => game.StartRun(),true);
            Button(results.transform,"BACK TO THE HOUSE",640,630,380,40,() => game.ReturnToTitle());
        }
        void BuildPause()
        {
            pause = Panel("Pause",root,640,360,1280,720,new Color(.012f,.016f,.028f,.94f));
            Heading(Label(pause.transform,"TAKE A BREATH.",640,190,900,76,49,Cream,TextAnchor.MiddleCenter));
            Label(pause.transform,"YOUR LUCK CAN WAIT.",640,248,800,30,16,HouseVisuals.Gold,TextAnchor.MiddleCenter);
            Button(pause.transform,"BACK TO THE FIGHT",640,330,380,54,() => game.TogglePause(),true);
            var mute = Button(pause.transform,"SOUND ON",640,400,380,44,() => { game.Audio.ToggleMute(); Refresh(); }); muteLabel = mute.GetComponentInChildren<Text>();
            var shake = Button(pause.transform,"SCREEN SHAKE ON",640,458,380,44,() => { game.ToggleShake(); Refresh(); }); shakeLabel = shake.GetComponentInChildren<Text>();
            Button(pause.transform,"END RUN / RETURN TO TITLE",640,535,380,44,() => game.ReturnToTitle());
            Label(pause.transform,"WASD MOVE     •     MOUSE AIM     •     HOLD CLICK FIRE\nSPACE DASH     •     ESC PAUSE",640,625,860,68,16,Muted,TextAnchor.MiddleCenter);
        }
        public bool CreditsOpen => credits && credits.activeSelf;
        void CloseCredits()
        {
            if (credits) credits.SetActive(false);
            if (title && title.activeSelf)
            {
                var startBtn = title.transform.Find("Start run");
                if (startBtn) Focus(startBtn.GetComponent<Button>());
            }
        }
        void BuildCredits()
        {
            float cardW = 960, cardH = 540;
            credits = Panel("Credits Dimmer",root,640,360,1280,720,new Color(.01f,.013f,.025f,.92f));
            var card = Panel("Credits Card",credits.transform,640,360,cardW,cardH,Card);
            Panel("Top Accent",card.transform,cardW/2,2,cardW,4,HouseVisuals.Gold);
            Panel("Bottom Accent",card.transform,cardW/2,cardH-2,cardW,2,new Color(.38f,.30f,.18f));

            Heading(Label(card.transform,"THE PEOPLE BEHIND THE HOUSE",cardW/2,38,cardW-40,40,30,HouseVisuals.Gold,TextAnchor.MiddleCenter));
            Label(card.transform,"HOUSE OF 77  •  CREDITS & ATTRIBUTIONS",cardW/2,68,cardW-40,22,14,Cream,TextAnchor.MiddleCenter,FontStyle.Bold);
            Rule(card.transform,cardW/2,86,cardW-60,new Color(.38f,.30f,.18f));

            Panel("Column Divider",card.transform,cardW/2,252,1,310,new Color(.18f,.20f,.28f));

            // Left column (X = 245, W = 420)
            Label(card.transform,"DEVELOPERS",245,110,420,22,15,HouseVisuals.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label(card.transform,"A game by Saurabh & Prince\nGame design, C# gameplay architecture, and shaders.",245,140,420,40,14,Cream);
            Rule(card.transform,245,172,420,new Color(.16f,.18f,.25f));

            Label(card.transform,"VISUAL ASSETS",245,192,420,22,15,HouseVisuals.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label(card.transform,"• Bladekeeper: chierit (CC BY 4.0)\n• Human Warriors: Fat Cat Games\n• Pixel Fantasy Slot Machine: Caz Wolf\n• Survival Icons: maltyy\n• Parallax Worlds (Dungeon Gate): Nasha",245,260,420,110,14,Cream);
            Rule(card.transform,245,324,420,new Color(.16f,.18f,.25f));

            Label(card.transform,"TYPOGRAPHY",245,344,420,22,15,HouseVisuals.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label(card.transform,"• Cinzel Decorative: Natanael Gama (SIL OFL)\n• Barlow Semi Condensed: The Barlow Project (SIL OFL)",245,376,420,40,14,Cream);

            // Right column (X = 715, W = 420)
            Label(card.transform,"ORIGINAL AUDIO",715,110,420,22,15,HouseVisuals.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label(card.transform,"• \"Deadly Roulette\" & \"Darkling\"\n   Kevin MacLeod (incompetech.com) · CC BY 4.0\n• Sound Effects & Reward Jingles: Kenney · CC0",715,148,420,56,14,Cream);
            Rule(card.transform,715,186,420,new Color(.16f,.18f,.25f));

            Label(card.transform,"ETHICS & AI DISCLOSURE",715,206,420,22,15,HouseVisuals.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label(card.transform,"• AI assistance for C# code, shaders, and balance tuning.\n• NO generative artwork, music, or audio was used.\n  All media is from the credited human creators.",715,250,420,70,14,Cream);
            Rule(card.transform,715,294,420,new Color(.16f,.18f,.25f));

            Label(card.transform,"EVENT SUBMISSION",715,314,420,22,15,HouseVisuals.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label(card.transform,"Official Unity Game Jam 2026\nTheme: Lucky Number (House of 77)",715,346,420,40,14,Cream);

            // Footer
            Rule(card.transform,cardW/2,424,cardW-60,new Color(.38f,.30f,.18f));
            Button(card.transform,"RETURN TO MAIN MENU",cardW/2,474,320,46,() => CloseCredits(),true);
        }
        public void ShowTitle()
        {
            HideAll(); title.SetActive(true); Refresh(); Focus(title.transform.Find("Start run").GetComponent<Button>());
        }
        public void ShowCombat() { HideAll(); hud.SetActive(true); bossPanel.SetActive(game.Phase == RunPhase.Boss); jackpotPanel.SetActive(game.JackpotRemaining > 0); refreshTime = 0; Refresh(); }
        public void ShowPause(bool visible)
        {
            pause.SetActive(visible); hud.SetActive(!visible); Refresh();
            if (visible) Focus(pause.GetComponentInChildren<Button>());
            else if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        public void ShowResults()
        {
            HideAll(); results.SetActive(true);
            resultTitle.text = game.Won ? "THE HOUSE IS BROKEN." : "THE HOUSE COLLECTS.";
            resultTitle.color = game.Won ? HouseVisuals.Gold : Cream;
            resultStats.text = "TIME  "+FormatTime(game.CombatSeconds)+"     /     KILLS  "+game.Kills+"     /     JACKPOTS  "+game.Rules.Jackpots+"\n\nCHIPS SPENT  "+game.Rules.Spent+"     /     SCORE  "+game.Score.ToString("N0")+"\n\n"+(game.Won ? "YOU DIDN'T NEED LUCK. BUT IT HELPED." : "A DIFFERENT BUILD. A DIFFERENT NUMBER. ONE MORE TRY.");
            Focus(results.GetComponentInChildren<Button>());
        }
        public void Refresh()
        {
            if (health) health.text = "LIFE  "+game.Player.Health+" / "+game.Rules.MaxHealth;
            if (chips) chips.text = game.Rules.Chips+" CHIPS";
            if (best) best.text = "BEST  "+game.BestScore.ToString("D6");
            if (muteLabel) muteLabel.text = game.Audio.Muted ? "SOUND OFF" : "SOUND ON";
            if (titleMute) titleMute.text = game.Audio.Muted ? "SOUND OFF" : "SOUND ON";
            if (shakeLabel) shakeLabel.text = game.ScreenShake ? "SCREEN SHAKE ON" : "SCREEN SHAKE OFF";
        }
        public void Tick(float dt)
        {
            refreshTime -= dt;
            if (hud.activeSelf && refreshTime <= 0)
            {
                refreshTime = .08f;
                bool boss = game.Phase == RunPhase.Boss;
                wave.text = boss ? "FINAL ENCOUNTER" : "WAVE 0"+game.Wave+" / 04";
                time.text = boss ? FormatTime(game.CombatSeconds) : FormatTime(Mathf.Max(0,game.WaveRemaining));
                waveFill.fillAmount = boss ? 1 : game.WaveRemaining/game.Config.waveSeconds;
                target.text = game.Reveal ? game.Rules.Target.ToString() : "? ?";
                pity.text = boss ? "BREAK THE HOUSE" : !game.Reveal ? "REVEALS IN "+FormatTime(game.WaveRemaining-game.Config.revealSeconds) : game.Rules.Misses >= 2 ? "GUARANTEED SPIN" : "BAD LUCK  "+game.Rules.Misses+" / 2";
                banner.text = game.Banner;
                banner.rectTransform.anchoredPosition = new Vector2(640,boss ? -300 : -143);
                bossPanel.SetActive(boss); if (boss) { bossFill.fillAmount = game.Boss.HealthFraction; bossName.text = game.Boss.BeamWarning ? "THE HOUSE / BEAM INCOMING" : "THE HOUSE"; }
                jackpotPanel.SetActive(game.JackpotRemaining > 0); jackpotFill.fillAmount = game.JackpotRemaining/game.Config.jackpotSeconds;
                jackpot.text = "JACKPOT OVERDRIVE / "+Mathf.CeilToInt(game.JackpotRemaining)+"s";
                dashFill.fillAmount = game.Player.DashReady;
                streak.text = "LUCKY SEVEN   " + game.RushProgress + " / 7";
                streakFill.fillAmount = game.RushProgress / 7f;
                footer.text = game.CombatSeconds < 20 ? "WASD MOVE  /  MOUSE AIM  /  HOLD CLICK FIRE  /  SPACE DASH" : game.Reveal ? "COLLECT CHIPS. BUY POWER OR BETTER ODDS AFTER THE WAVE." : "SHARDCASTER  /  DMG "+Mathf.RoundToInt(game.Config.bladeDamage*game.Rules.DamageMultiplier)+"  /  PIERCE "+game.Rules.Upgrades[3]+"  /  SPLIT "+(game.Rules.Upgrades[2]>0 ? "ON" : "OFF");
            }
            if (title.activeSelf)
            {
                if (credits && credits.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    CloseCredits();
                for (int i = 0; i < titleArt.Count; i++) titleArt[i].anchoredPosition = new Vector2(640+Mathf.Sin(Time.unscaledTime*.15f)*(i+1)*2,-360);
            }
            if (!presenting) return;
            reelTime -= dt; reelTick -= dt;
            if (reelTick <= 0)
            {
                reelTick = reelTime < .6f ? .17f : .065f;
                reelLeft.text = UnityEngine.Random.Range(0,8).ToString(); reelMiddle.text = UnityEngine.Random.Range(0,8).ToString(); reelRight.text = UnityEngine.Random.Range(0,8).ToString();
                game.Audio.Play(game.Config.reelSound,.25f);
            }
            if (reelTime > 0) return;
            presenting = false;
            string value = game.LastSpin.ToString("D2"); reelLeft.text = value[0].ToString(); reelMiddle.text = value[1].ToString(); reelRight.text = game.LastMatch ? "+" : "—";
            spinStatus.text = game.LastMatch ? "JACKPOT / 20 SECONDS OF OVERDRIVE\n+2 HEALTH. MAKE IT COUNT." : "NO MATCH / +1 HEALTH\n"+(game.Rules.Misses >= 2 ? "NEXT SPIN IS GUARANTEED." : "THE HOUSE REMEMBERS YOUR BAD LUCK.");
            spinStatus.color = game.LastMatch ? HouseVisuals.Gold : Cream;
            game.Audio.Play(game.LastMatch ? game.Config.jackpotSound : game.Config.chipSound);
            RefreshShop(); Focus(continueButton);
        }
        public static string FormatTime(float seconds) { int total = Mathf.CeilToInt(Mathf.Max(0,seconds)); return (total/60).ToString("D2")+":"+(total%60).ToString("D2"); }
    }
}
