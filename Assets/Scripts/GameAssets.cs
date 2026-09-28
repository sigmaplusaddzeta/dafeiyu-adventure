using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Fishy
{
    /// 程序化资产生成：所有像素贴图 / 图块 / 字体都在运行时用代码生成
    /// （像素数据与网页版完全一致，无需任何导入的美术文件）
    public static class GameAssets
    {
        public const float PPU = 16f; // 每单位 16 像素（1 图块 = 1 单位）

        // ================= 调色板 =================
        static readonly Color32 C_o = new Color32(22, 41, 79, 255);     // 深蓝轮廓
        static readonly Color32 C_b = new Color32(63, 143, 224, 255);   // 鱼体蓝
        static readonly Color32 C_l = new Color32(126, 201, 244, 255);  // 高光浅蓝
        static readonly Color32 C_w = new Color32(238, 246, 251, 255);  // 白肚皮
        static readonly Color32 C_K = new Color32(13, 18, 32, 255);     // 黑眼睛
        static readonly Color32 C_Wh = new Color32(255, 255, 255, 255); // 眼白
        static readonly Color32 C_p = new Color32(255, 157, 177, 255);  // 腮红粉
        static readonly Color32 C_y = new Color32(255, 233, 168, 255);  // 米粒暖光
        static readonly Color32 C_bgO = new Color32(90, 74, 58, 255);   // 米虫轮廓
        static readonly Color32 C_bgW = new Color32(245, 240, 230, 255);// 米虫体
        static readonly Color32 C_bgK = new Color32(26, 20, 16, 255);   // 米虫眼

        static readonly Dictionary<char, Color32> PalFish = new Dictionary<char, Color32>();
        static readonly Dictionary<char, Color32> PalBug = new Dictionary<char, Color32>();
        static readonly Dictionary<char, Color32> PalBush = new Dictionary<char, Color32>();

        static GameAssets()
        {
            PalFish['o'] = C_o; PalFish['b'] = C_b; PalFish['l'] = C_l; PalFish['w'] = C_w;
            PalFish['K'] = C_K; PalFish['W'] = C_Wh; PalFish['p'] = C_p; PalFish['y'] = C_y;
            PalBug['o'] = C_bgO; PalBug['w'] = C_bgW; PalBug['k'] = C_bgK;
            PalBush['o'] = new Color32(29, 107, 38, 255);
            PalBush['W'] = new Color32(74, 168, 50, 255);
            PalBush['w'] = PalBush['W']; PalBush['y'] = PalBush['W'];
        }

        // ================= 像素画数据（与网页版一致） =================
        static readonly string[] ArtFish = {
            "....oooooo......",
            "..obbllllbboo...",
            ".obbllllllbbbo..",
            ".obbKKKllllbbo..",
            "obbbKWKlllbbbo..",
            "obbbKKKllpppbbo.",
            "owwbbbllppppbbo.",
            "owwwwbbbllllllbo",
            "owwwwwwwwwwwwbo.",
            ".owwwwwwwwwbbbo.",
            "..oowwwwwwoo....",
            "....oooooo......"
        };
        // 眨眼帧：眼区换成体色
        static readonly string[] ArtFishBlink = {
            "....oooooo......",
            "..obbllllbboo...",
            ".obbllllllbbbo..",
            ".obbbbbllllbbo..",
            "obbbbbbblllbbbo.",
            "obbbKKKllpppbbo.",
            "owwbbbllppppbbo.",
            "owwwwbbbllllllbo",
            "owwwwwwwwwwwwbo.",
            ".owwwwwwwwwbbbo.",
            "..oowwwwwwoo....",
            "....oooooo......"
        };
        static readonly string[] ArtBug = {
            "...oooooo...",
            ".oowwwwwwoo.",
            ".owwwwkkwo..",
            "owwwwwwwwwwo",
            "owwwwwwwwwwo",
            ".owwwwwwwwo.",
            "..oo.oo.oo.."
        };
        static readonly string[] ArtBugSq = {
            "..o.oo.oo...",
            ".oooooooooo.",
            "oowwwwwwwwoo",
            ".oooooooooo."
        };
        static readonly string[] ArtRice = {
            "...oo...",
            "..oWWo..",
            ".oWWWWo.",
            ".oWWyWo.",
            "oWWWyWWo",
            "oWWyyWWo",
            "oWWyyWWo",
            "oWWWyWWo",
            ".oWWWWo.",
            ".oWWWWo.",
            "..oWWo..",
            "...oo..."
        };
        static readonly string[] ArtCloud = {
            "..........oooooo........",
            "......ooooWWWWoooo......",
            "....ooWWWWWWWWWWWWoo....",
            "...oWWWWWWWWWWWWWWWWo...",
            "..oWWWWWWWWWWWWWWWWWWo..",
            ".oWWWWWWWWWWWWWWWWWWWWo.",
            "oWWWWWWWWWWWWWWWWWWWWWWo",
            "oWWWWWWWWWWWWWWWWWWWWWWo",
            ".oWWWWWWWWWWWWWWWWWWWWo.",
            "...oooooooooooooooooooo."
        };

        // ================= 贴图/精灵 =================
        static Texture2D Tex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Texture2D FromArt(string[] art, Dictionary<char, Color32> pal)
        {
            int w = art[0].Length, h = art.Length;
            var t = Tex(w, h);
            var px = new Color32[w * h];
            for (int r = 0; r < h; r++)
            {
                string row = art[r];
                for (int c = 0; c < w; c++)
                {
                    char ch = row[c];
                    if (ch == '.') continue;
                    Color32 col;
                    px[(h - 1 - r) * w + c] = pal.TryGetValue(ch, out col) ? col : new Color32(255, 0, 255, 255);
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static Sprite Spr(Texture2D t)
        {
            t.Apply(); // 关键：像素写入后必须上传 GPU，否则渲染为灰色
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                new Vector2(0.5f, 0.5f), PPU);
        }

        // 画布坐标辅助：y 从顶部算（与网页版一致），自动翻转到底部原点
        static void R(Texture2D t, int x, int yTop, int w, int h, Color32 c)
        {
            int H = t.height;
            for (int j = 0; j < h; j++)
            {
                int ty = H - 1 - (yTop + j);
                if (ty < 0) continue;
                for (int i = x; i < x + w; i++)
                    if (i >= 0 && i < t.width) t.SetPixel(i, ty, c);
            }
        }
        static void Edge(Texture2D t, Color32 c)
        {
            R(t, 0, 0, t.width, 1, c);
            R(t, 0, t.height - 1, t.width, 1, c);
            R(t, 0, 0, 1, t.height, c);
            R(t, t.width - 1, 0, 1, t.height, c);
        }
        static void FillAll(Texture2D t, Color32 c)
        {
            var px = new Color32[t.width * t.height];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            t.SetPixels32(px);
        }

        // ---- 16x16 图块贴图 ----
        static Texture2D GroundTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(217, 123, 63, 255);
            var dark = new Color32(138, 74, 30, 255);
            var light = new Color32(240, 176, 112, 255);
            FillAll(t, baseC);
            R(t, 0, 14, 16, 2, dark);
            R(t, 14, 0, 2, 16, dark);
            R(t, 0, 0, 14, 2, light);
            R(t, 6, 4, 4, 2, dark);
            R(t, 2, 8, 4, 2, dark);
            return t;
        }
        static Texture2D NetBrickTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(74, 163, 232, 255);
            var light = new Color32(142, 205, 245, 255);
            var border = new Color32(22, 50, 92, 255);
            FillAll(t, baseC);
            for (int i = 0; i < 16; i += 4)
            {
                R(t, i, 0, 1, 16, C_Wh);
                R(t, 0, i, 16, 1, C_Wh);
            }
            R(t, 1, 1, 2, 2, light);
            Edge(t, border);
            return t;
        }
        static Texture2D QuestionTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(247, 184, 56, 255);
            var border = new Color32(138, 74, 16, 255);
            var q = new Color32(255, 243, 200, 255);
            FillAll(t, baseC);
            Edge(t, border);
            R(t, 5, 3, 6, 2, q);
            R(t, 4, 4, 2, 3, q);
            R(t, 10, 4, 2, 3, q);
            R(t, 9, 6, 2, 2, q);
            R(t, 7, 8, 2, 2, q);
            R(t, 7, 12, 2, 2, q);
            return t;
        }
        static Texture2D UsedTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(122, 106, 74, 255);
            var border = new Color32(74, 62, 40, 255);
            var light = new Color32(154, 138, 104, 255);
            FillAll(t, baseC);
            Edge(t, border);
            R(t, 2, 2, 12, 1, light);
            return t;
        }
        static Texture2D StoneTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(200, 216, 232, 255);
            var dark = new Color32(90, 122, 154, 255);
            var dot = new Color32(138, 168, 200, 255);
            FillAll(t, baseC);
            R(t, 1, 1, 14, 2, C_Wh);
            R(t, 1, 1, 2, 14, C_Wh);
            R(t, 0, 14, 16, 2, dark);
            R(t, 14, 0, 2, 16, dark);
            R(t, 13, 12, 2, 2, dot);
            return t;
        }
        static Texture2D PipeTopLTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(63, 162, 74, 255);
            var light = new Color32(127, 208, 138, 255);
            var dark = new Color32(29, 107, 38, 255);
            FillAll(t, baseC);
            R(t, 1, 0, 3, 16, light);
            R(t, 13, 0, 3, 16, dark);
            R(t, 0, 0, 1, 16, dark);
            R(t, 0, 0, 16, 1, dark);
            R(t, 0, 14, 16, 2, dark);
            return t;
        }
        static Texture2D PipeTopRTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(63, 162, 74, 255);
            var light = new Color32(127, 208, 138, 255);
            var dark = new Color32(29, 107, 38, 255);
            FillAll(t, baseC);
            R(t, 1, 0, 1, 16, light);
            R(t, 12, 0, 3, 16, dark);
            R(t, 15, 0, 1, 16, dark);
            R(t, 0, 0, 16, 1, dark);
            R(t, 0, 14, 16, 2, dark);
            return t;
        }
        static Texture2D PipeBodyLTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(63, 162, 74, 255);
            var light = new Color32(127, 208, 138, 255);
            var dark = new Color32(29, 107, 38, 255);
            FillAll(t, baseC);
            R(t, 0, 0, 2, 16, new Color32(0, 0, 0, 0));
            R(t, 2, 0, 14, 16, baseC);
            R(t, 4, 0, 3, 16, light);
            R(t, 12, 0, 4, 16, dark);
            return t;
        }
        static Texture2D PipeBodyRTex()
        {
            var t = Tex(16, 16);
            var baseC = new Color32(63, 162, 74, 255);
            var dark = new Color32(29, 107, 38, 255);
            FillAll(t, baseC);
            R(t, 14, 0, 2, 16, new Color32(0, 0, 0, 0));
            R(t, 0, 0, 14, 16, baseC);
            R(t, 11, 0, 3, 16, dark);
            R(t, 15, 0, 1, 16, dark);
            return t;
        }

        // ---- 城堡（80x76）----
        static Texture2D CastleTex()
        {
            var t = Tex(80, 76);
            var brick = new Color32(206, 96, 58, 255);
            var dark = new Color32(128, 48, 32, 255);
            var light = new Color32(240, 152, 96, 255);
            var black = new Color32(34, 26, 30, 255);
            var white = new Color32(235, 228, 216, 255);

            // ---- 中央塔（x26..53，顶垛口 + 拱窗）----
            R(t, 26, 4, 28, 44, brick);
            for (int i = 0; i < 3; i++) R(t, 26 + i * 10, 0, 6, 4, brick);
            R(t, 26, 3, 28, 1, light);
            for (int y = 10; y < 48; y += 6) R(t, 26, y, 28, 1, dark);
            R(t, 36, 12, 9, 8, black);       // 拱窗
            R(t, 38, 10, 5, 2, black);
            R(t, 36, 12, 9, 1, light);       // 窗楣
            R(t, 33, 32, 14, 2, white);      // 石纹

            // ---- 两侧副塔（带垛口 + 窗）----
            R(t, 2, 20, 16, 28, brick);
            R(t, 62, 20, 16, 28, brick);
            R(t, 2, 16, 5, 4, brick);
            R(t, 11, 16, 5, 4, brick);
            R(t, 62, 16, 5, 4, brick);
            R(t, 71, 16, 5, 4, brick);
            R(t, 2, 19, 16, 1, light);
            R(t, 62, 19, 16, 1, light);
            for (int y = 26; y < 48; y += 6) { R(t, 2, y, 16, 1, dark); R(t, 62, y, 16, 1, dark); }
            R(t, 7, 28, 6, 7, black);
            R(t, 67, 28, 6, 7, black);

            // ---- 主体（全宽 + 垛口 + 错位砖缝）----
            R(t, 0, 48, 80, 28, brick);
            for (int x = 2; x < 78; x += 14) R(t, x, 44, 8, 4, brick);
            R(t, 0, 47, 80, 1, light);
            for (int y = 54; y < 76; y += 6) R(t, 0, y, 80, 1, dark);
            for (int y = 48; y < 76; y += 6)
            {
                int off = ((y / 6) % 2) * 4;
                for (int x = off; x < 80; x += 8) R(t, x, y, 1, 6, dark);
            }

            // ---- 拱门 ----
            R(t, 30, 60, 20, 16, black);
            for (int yy = 52; yy <= 60; yy++)
                for (int xx = 30; xx <= 49; xx++)
                {
                    float dx = xx - 39.5f, dy = yy - 60f;
                    if (dx * dx + dy * dy * 2.2f <= 100f) t.SetPixel(xx, t.height - 1 - yy, black);
                }
            R(t, 30, 60, 20, 1, dark);

            // ---- 点缀与描边 ----
            R(t, 8, 52, 10, 2, white);
            R(t, 62, 52, 10, 2, white);
            R(t, 0, 48, 1, 28, dark);
            R(t, 79, 48, 1, 28, dark);
            return t;
        }

        // ---- 旗杆部件 ----
        static Texture2D PoleTex()
        {
            var t = Tex(4, 64);
            var a = new Color32(226, 232, 240, 255);
            var b = new Color32(150, 160, 178, 255);
            var c = new Color32(96, 106, 124, 255);
            for (int y = 0; y < 64; y++)
            {
                t.SetPixel(0, y, a); t.SetPixel(1, y, a);
                t.SetPixel(2, y, b); t.SetPixel(3, y, c);
            }
            for (int y = 8; y < 64; y += 14) R(t, 0, 64 - y - 1, 4, 1, b); // 环纹
            return t;
        }
        static Texture2D BallTex()
        {
            var t = Tex(8, 8);
            var gold = new Color32(247, 196, 84, 255);
            var hi = new Color32(255, 236, 170, 255);
            var dk = new Color32(186, 138, 40, 255);
            FillAll(t, gold);
            R(t, 1, 1, 3, 2, hi);
            R(t, 0, 7, 8, 1, dk);
            R(t, 7, 0, 1, 7, dk);
            var clear = new Color32(0, 0, 0, 0);
            t.SetPixel(0, 0, clear); t.SetPixel(7, 0, clear);
            t.SetPixel(0, 7, clear); t.SetPixel(7, 7, clear);
            return t;
        }
        static Texture2D FlagTex() // 白底金边旗 + 鱼图案（朝左，16x12）
        {
            var t = Tex(16, 12);
            var w = new Color32(244, 246, 250, 255);
            var gold = new Color32(247, 196, 84, 255);
            var fish = new Color32(63, 143, 224, 255);
            FillAll(t, w);
            R(t, 0, 0, 16, 1, gold);
            R(t, 0, 11, 16, 1, gold);
            R(t, 0, 0, 1, 12, gold);
            for (int x = 1; x < 16; x++) if (x % 4 < 2) t.SetPixel(x, 10, gold); // 飘动纹
            R(t, 4, 5, 6, 2, fish);      // 鱼身
            t.SetPixel(10, 5, fish);     // 尾
            t.SetPixel(11, 6, fish);
            R(t, 3, 5, 1, 2, fish);
            t.SetPixel(5, 6, w);         // 眼
            return t;
        }
        static Texture2D CastleFlagTex() // 城堡胜利旗（波浪白旗金边 + 鱼图案，20x14）
        {
            var t = Tex(20, 14);
            var w = new Color32(244, 246, 250, 255);
            var gold = new Color32(247, 196, 84, 255);
            var fish = new Color32(63, 143, 224, 255);
            for (int y = 0; y < 14; y++)
            {
                int wob = (y / 3) % 2;              // 右缘波动
                int len = 19 - wob;
                for (int x = 0; x < len; x++)
                {
                    var c = w;
                    if (y == 0 || y == 13 || x == len - 1 || x == 0) c = gold;
                    t.SetPixel(x, 13 - y, c);
                }
            }
            R(t, 6, 6, 6, 3, fish);      // 鱼身
            R(t, 12, 6, 2, 1, fish);     // 尾
            t.SetPixel(7, 7, w);         // 眼
            return t;
        }

        // ---- 粒子 ----
        static Sprite _white2;
        public static Sprite White2
        {
            get
            {
                if (_white2 == null)
                {
                    var t = Tex(2, 2);
                    FillAll(t, C_Wh);
                    t.Apply();
                    _white2 = Spr(t);
                }
                return _white2;
            }
        }

        // ---- 字体 ----
        static Font _uiFont;
        public static Font UiFont
        {
            get
            {
                if (_uiFont == null)
                    _uiFont = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "SimHei", "PingFang SC", "Arial" }, 12);
                return _uiFont;
            }
        }

        // ---- 惰性精灵属性 ----
        static Sprite _fishOpen, _fishBlink, _bug, _bugSq, _rice, _cloud, _bush;
        static Sprite _ground, _net, _question, _used, _stone, _ptl, _ptr, _pbl, _pbr;
        static Sprite _castle, _pole, _ball, _flag, _castleFlag;

        public static Sprite FishOpen { get { if (_fishOpen == null) _fishOpen = Spr(FromArt(ArtFish, PalFish)); return _fishOpen; } }
        public static Sprite FishBlink { get { if (_fishBlink == null) _fishBlink = Spr(FromArt(ArtFishBlink, PalFish)); return _fishBlink; } }
        public static Sprite Bug { get { if (_bug == null) _bug = Spr(FromArt(ArtBug, PalBug)); return _bug; } }
        public static Sprite BugSq { get { if (_bugSq == null) _bugSq = Spr(FromArt(ArtBugSq, PalBug)); return _bugSq; } }
        public static Sprite Rice { get { if (_rice == null) _rice = Spr(FromArt(ArtRice, PalFish)); return _rice; } }
        public static Sprite Cloud { get { if (_cloud == null) _cloud = Spr(FromArt(ArtCloud, PalFish)); return _cloud; } }
        public static Sprite Bush { get { if (_bush == null) _bush = Spr(FromArt(ArtCloud, PalBush)); return _bush; } }
        public static Sprite Ground { get { if (_ground == null) _ground = Spr(GroundTex()); return _ground; } }
        public static Sprite NetBrick { get { if (_net == null) _net = Spr(NetBrickTex()); return _net; } }
        public static Sprite Question { get { if (_question == null) _question = Spr(QuestionTex()); return _question; } }
        public static Sprite Used { get { if (_used == null) _used = Spr(UsedTex()); return _used; } }
        public static Sprite Stone { get { if (_stone == null) _stone = Spr(StoneTex()); return _stone; } }
        public static Sprite PipeTopL { get { if (_ptl == null) _ptl = Spr(PipeTopLTex()); return _ptl; } }
        public static Sprite PipeTopR { get { if (_ptr == null) _ptr = Spr(PipeTopRTex()); return _ptr; } }
        public static Sprite PipeBodyL { get { if (_pbl == null) _pbl = Spr(PipeBodyLTex()); return _pbl; } }
        public static Sprite PipeBodyR { get { if (_pbr == null) _pbr = Spr(PipeBodyRTex()); return _pbr; } }
        public static Sprite Castle { get { if (_castle == null) _castle = Spr(CastleTex()); return _castle; } }
        public static Sprite Pole { get { if (_pole == null) _pole = Spr(PoleTex()); return _pole; } }
        public static Sprite Ball { get { if (_ball == null) _ball = Spr(BallTex()); return _ball; } }
        public static Sprite FlagTri { get { if (_flag == null) _flag = Spr(FlagTex()); return _flag; } }
        public static Sprite CastleFlag { get { if (_castleFlag == null) _castleFlag = Spr(CastleFlagTex()); return _castleFlag; } }

        // ---- Tile 资产 ----
        static Tile MakeTile(Sprite s)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.colliderType = Tile.ColliderType.Grid; // 整格方块碰撞，不依赖贴图轮廓
            return t;
        }
        static Tile _tGround, _tNet, _tQuestion, _tUsed, _tStone, _tPtl, _tPtr, _tPbl, _tPbr;
        public static Tile TGround { get { if (_tGround == null) _tGround = MakeTile(Ground); return _tGround; } }
        public static Tile TNet { get { if (_tNet == null) _tNet = MakeTile(NetBrick); return _tNet; } }
        public static Tile TQuestion { get { if (_tQuestion == null) _tQuestion = MakeTile(Question); return _tQuestion; } }
        public static Tile TUsed { get { if (_tUsed == null) _tUsed = MakeTile(Used); return _tUsed; } }
        public static Tile TStone { get { if (_tStone == null) _tStone = MakeTile(Stone); return _tStone; } }
        public static Tile TPipeTL { get { if (_tPtl == null) _tPtl = MakeTile(PipeTopL); return _tPtl; } }
        public static Tile TPipeTR { get { if (_tPtr == null) _tPtr = MakeTile(PipeTopR); return _tPtr; } }
        public static Tile TPipeBL { get { if (_tPbl == null) _tPbl = MakeTile(PipeBodyL); return _tPbl; } }
        public static Tile TPipeBR { get { if (_tPbr == null) _tPbr = MakeTile(PipeBodyR); return _tPbr; } }
    }
}
