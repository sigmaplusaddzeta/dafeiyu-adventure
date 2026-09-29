using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Fishy
{
    /// 关卡构建：ASCII 地图 → Tilemap + 实体 + 装饰 + 旗杆城堡
    public static class LevelBuilder
    {
        public static float FlagX, FlagBaseY, FlagTopY, DoorX;
        public static Transform FlagTriT, CastleFlagT;
        public static Transform SRRoot; // 逐格 SpriteRenderer 的父节点

        public static void Build(Transform world)
        {
            // 重置静态关卡状态（重生重建时防止残留）
            FlagX = 0f; FlagBaseY = 0f; FlagTopY = 0f; DoorX = 0f;
            FlagTriT = null; CastleFlagT = null;
            GameManager.Kinds.Clear();

            // ---- Grid + Tilemap ----
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(world, false);
            gridGo.AddComponent<Grid>();
            var tm = gridGo.AddComponent<Tilemap>();
            var tr = gridGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = 0;
            // URP 下运行时创建的 Tilemap 需要显式指定 sprite 材质，否则渲染为纯色
            tr.material = new Material(Shader.Find("Sprites/Default"));
            var gridBody = gridGo.AddComponent<Rigidbody2D>();
            gridBody.bodyType = RigidbodyType2D.Static;
            var tilemapCollider = gridGo.AddComponent<TilemapCollider2D>(); // 地面碰撞（关键！）
            var composite = gridGo.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            composite.generationType = CompositeCollider2D.GenerationType.Manual;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            GameManager.TileMap = tm;

            // ---- 解析地图 ----
            BuildMaps();
            for (int r = 0; r < GameConfig.Rows; r++)
            {
                for (int c = 0; c < GameConfig.SegCount * GameConfig.SegCols; c++)
                {
                    char t = CharAt(r, c);
                    int y = GameConfig.Rows - 1 - r; // 行 → 世界 y（y 向上）
                    var cell = new Vector3Int(c, y, 0);
                    switch (t)
                    {
                        case '#': tm.SetTile(cell, GameAssets.TGround); GameManager.Kinds[cell] = '#'; break;
                        case 'B': tm.SetTile(cell, GameAssets.TNet); GameManager.Kinds[cell] = 'B'; break;
                        case '?': tm.SetTile(cell, GameAssets.TQuestion); GameManager.Kinds[cell] = '?'; break;
                        case 'x': tm.SetTile(cell, GameAssets.TUsed); GameManager.Kinds[cell] = 'x'; break;
                        case 'S': case 'F': tm.SetTile(cell, GameAssets.TStone); GameManager.Kinds[cell] = 'S'; break;
                        case 'T': tm.SetTile(cell, GameAssets.TPipeTL); GameManager.Kinds[cell] = 'T'; break;
                        case 't': tm.SetTile(cell, GameAssets.TPipeBL); GameManager.Kinds[cell] = 't'; break;
                        case 'Y': tm.SetTile(cell, GameAssets.TPipeTR); GameManager.Kinds[cell] = 'Y'; break;
                        case 'y': tm.SetTile(cell, GameAssets.TPipeBR); GameManager.Kinds[cell] = 'y'; break;
                        case 'C': MakeCoin(world, c + 0.5f, 16.5f - r); break;
                        case 'E': MakeBug(world, c + 0.5f, 16f - r); break;
                        case 'P':
                            GameManager.SpawnPos = new Vector2(
                                c + 0.5f,
                                16f - r + GameConfig.PlayerHalfH + 0.04f);
                            break;
                        case 'K':
                            MakeCheckpoint(
                                world,
                                c + 0.5f,
                                16f - r + GameConfig.PlayerHalfH + 0.04f);
                            break;
                        case '|':
                            FlagX = c + 0.5f;
                            FlagTopY = Mathf.Max(FlagTopY, y + 1f); // 杆顶（最高格上缘）
                            FlagBaseY = FlagBaseY == 0f ? y : Mathf.Min(FlagBaseY, y); // 最下格底
                            break;
                        case 'H': DoorX = c + 0.5f; break;
                    }
                }
            }

            // 地图完全生成后一次性合并碰撞，之后动态破坏砖块仍会自动更新。
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            composite.GenerateGeometry();

            // 逐格 SpriteRenderer 显示（TilemapRenderer 在 URP 下渲染异常，仅保留其碰撞）
            SRRoot = new GameObject("CellSRs").transform;
            SRRoot.SetParent(world, false);
            var bnd = tm.cellBounds;
            for (int xx = bnd.xMin; xx < bnd.xMax; xx++)
                for (int yy = bnd.yMin; yy < bnd.yMax; yy++)
                {
                    var cell = new Vector3Int(xx, yy, 0);
                    var tl = tm.GetTile<Tile>(cell);
                    if (tl == null || tl.sprite == null) continue;
                    var sgo = new GameObject("cell_" + cell.x + "_" + cell.y);
                    sgo.transform.SetParent(SRRoot, false);
                    sgo.transform.position = tm.GetCellCenterWorld(cell);
                    var sr = sgo.AddComponent<SpriteRenderer>();
                    sr.sprite = tl.sprite;
                    sr.sortingOrder = 0;
                }
            tr.enabled = false; // 关闭 TilemapRenderer 显示

            BuildFlag(world);
            BuildCastle(world);
            BuildDecor(world);
            SpawnPlayer(world);
        }

        public static void SetCellSprite(Vector3Int cell, Sprite sp)
        {
            var t = SRRoot != null ? SRRoot.Find("cell_" + cell.x + "_" + cell.y) : null;
            if (t != null) t.GetComponent<SpriteRenderer>().sprite = sp;
        }

        public static void HideCell(Vector3Int cell)
        {
            var t = SRRoot != null ? SRRoot.Find("cell_" + cell.x + "_" + cell.y) : null;
            if (t != null) t.gameObject.SetActive(false);
        }

        static char[][] _maps; // 处理后的关卡网格

        // 关卡后处理：地面行的大坑压缩为最多 2 格宽，降低掉坑难度
        static void BuildMaps()
        {
            int cols = GameConfig.SegCount * GameConfig.SegCols;
            var g = new char[GameConfig.Rows][];
            for (int r = 0; r < GameConfig.Rows; r++)
            {
                var row = new char[cols];
                for (int c = 0; c < cols; c++) row[c] = CharAt0(r, c);
                g[r] = row;
            }
            for (int r = GameConfig.Rows - 2; r < GameConfig.Rows; r++)
            {
                int c = 0;
                while (c < cols)
                {
                    if (g[r][c] == '.')
                    {
                        int s = c;
                        while (c < cols && g[r][c] == '.') c++;
                        if (c - s > 2)
                            for (int k = s + 2; k < c; k++) g[r][k] = '#';
                    }
                    else c++;
                }
            }
            _maps = g;
        }

        static char CharAt0(int r, int c)
        {
            int seg = c / GameConfig.SegCols;
            int col = c % GameConfig.SegCols;
            var rows = LevelMaps.Segs[seg];
            if (r >= rows.Length) return '.';
            string row = rows[r];
            if (col >= row.Length || row[col] == '\0') return '.';
            return row[col] == ' ' ? '.' : row[col];
        }

        static char CharAt(int r, int c)
        {
            return _maps[r][c];
        }

        // ---- 大米币 ----
        static void MakeCoin(Transform world, float x, float y)
        {
            var go = new GameObject("Rice");
            go.transform.SetParent(world, false);
            go.transform.position = new Vector3(x, y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.Rice;
            sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.38f;
            go.AddComponent<RiceCoin>();
        }

        // ---- 米虫 ----
        static void MakeBug(Transform world, float x, float feetY)
        {
            var go = new GameObject("Bug");
            go.transform.SetParent(world, false);
            go.transform.position = new Vector3(x, feetY + 0.22f, 0);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.72f, 0.42f);
            col.sharedMaterial = new PhysicsMaterial2D("bugSlip") { friction = 0f, bounciness = 0f };
            go.AddComponent<RiceBug>();
        }

        // ---- 中途检查点 ----
        static void MakeCheckpoint(Transform world, float x, float y)
        {
            var go = new GameObject("Checkpoint");
            go.transform.SetParent(world, false);
            go.transform.position = new Vector3(x, y, 0);

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.Rice;
            sr.sortingOrder = 2;
            visualGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var haloGo = new GameObject("Halo");
            haloGo.transform.SetParent(go.transform, false);
            var halo = haloGo.AddComponent<SpriteRenderer>();
            halo.sprite = GameAssets.White2;
            halo.color = new Color32(255, 233, 168, 45);
            halo.sortingOrder = 1;
            haloGo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.9f;
            go.AddComponent<CheckpointZone>().Init(new Vector2(x, y), sr, halo);
        }

        // ---- 旗杆 ----
        static void BuildFlag(Transform world)
        {
            float cx = FlagX;
            float baseY = FlagBaseY > 0f ? FlagBaseY : 3f;
            float height = FlagTopY - baseY;

            var pole = new GameObject("Pole");
            pole.transform.SetParent(world, false);
            var psr = pole.AddComponent<SpriteRenderer>();
            psr.sprite = GameAssets.Pole;
            psr.sortingOrder = -1;
            pole.transform.position = new Vector3(cx, baseY + height * 0.5f, 0);
            pole.transform.localScale = new Vector3(1f, height / 4f, 1f); // 贴图 64px = 4 单位

            var ball = new GameObject("Ball");
            ball.transform.SetParent(world, false);
            var bsr = ball.AddComponent<SpriteRenderer>();
            bsr.sprite = GameAssets.Ball;
            bsr.sortingOrder = -1;
            ball.transform.position = new Vector3(cx, FlagTopY + 0.19f, 0);

            var tri = new GameObject("FlagTri");
            tri.transform.SetParent(world, false);
            var tsr = tri.AddComponent<SpriteRenderer>();
            tsr.sprite = GameAssets.FlagTri;
            tsr.sortingOrder = 1;
            // 旗三角贴图枢轴在中心，向左伸出
            tri.transform.position = new Vector3(cx - 0.55f, FlagTopY - 0.7f, 0);
            FlagTriT = tri.transform;

            // 触发区
            var zone = new GameObject("FlagZone");
            zone.transform.SetParent(world, false);
            zone.transform.position = new Vector3(cx, baseY + height * 0.5f, 0);
            var zc = zone.AddComponent<BoxCollider2D>();
            zc.isTrigger = true;
            zc.size = new Vector2(1.6f, height + 1f);
            var fz = zone.AddComponent<FlagZone>();
            fz.Init(cx, baseY, FlagTopY, DoorX, tri.transform);
        }

        // ---- 城堡 ----
        static void BuildCastle(Transform world)
        {
            var go = new GameObject("Castle");
            go.transform.SetParent(world, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.Castle;
            sr.sortingOrder = -2;
            // 贴图 80x76 px = 5 x 4.75 单位；门中心对齐 DoorX，底贴地面 y=2
            go.transform.position = new Vector3(DoorX, 2f + 4.75f * 0.5f, 0);

            var cf = new GameObject("CastleFlag");
            cf.transform.SetParent(world, false);
            var csr = cf.AddComponent<SpriteRenderer>();
            csr.sprite = GameAssets.CastleFlag;
            csr.sortingOrder = 2;
            csr.enabled = false;
            // 塔顶世界 y ≈ 6.5，旗初始藏在塔内
            cf.transform.position = new Vector3(DoorX - 0.15f, 5.4f, 0);
            CastleFlagT = cf.transform;
        }

        // ---- 云与灌木（视差） ----
        static void BuildDecor(Transform world)
        {
            var par = new GameObject("Parallax");
            par.transform.SetParent(world, false);
            var layer = par.AddComponent<ParallaxLayer>();

            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < 12; i++)
            {
                float bx = i * 21.25f + (i * 97 % 140) * 0.0625f;
                float by = 5.2f + (i * 53) % 90 * 0.0625f;
                layer.Add(MakeDeco(par.transform, GameAssets.Cloud, bx, by, -4,
                    new Color32(255, 255, 255, 255)));
            }
            for (int i = 0; i < 9; i++)
            {
                float bx = i * 26.9f + (i * 137 % 220) * 0.0625f + 5.6f;
                layer.Add(MakeDeco(par.transform, GameAssets.Bush, bx, 2.28f, -3, white));
            }
        }

        static SpriteRenderer MakeDeco(Transform parent, Sprite s, float x, float y, int order, Color32 c)
        {
            var go = new GameObject("deco");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = c;
            return sr;
        }

        // ---- 玩家 ----
        static void SpawnPlayer(Transform world)
        {
            var old = GameManager.Player;
            if (old != null) Object.Destroy(old.gameObject);

            var go = new GameObject("FishPlayer");
            go.transform.SetParent(world, false);
            var fish = go.AddComponent<FishController>();
            fish.Init(GameManager.GetRespawnPos());
            GameManager.Player = fish;
        }
    }

    /// 云层视差：x 随相机以 0.6 倍速移动
    public class ParallaxLayer : MonoBehaviour
    {
        struct Item { public Transform T; public float BaseX; }
        readonly List<Item> _items = new List<Item>();

        public void Add(SpriteRenderer sr)
        {
            _items.Add(new Item { T = sr.transform, BaseX = sr.transform.position.x });
        }

        void LateUpdate()
        {
            var cam = CameraFollow.Cam;
            if (cam == null) return;
            float camX = cam.transform.position.x - GameConfig.CamHalfW;
            float dx = camX * 0.6f;
            foreach (var it in _items)
                it.T.position = new Vector3(it.BaseX + dx, it.T.position.y, it.T.position.z);
        }
    }
}
