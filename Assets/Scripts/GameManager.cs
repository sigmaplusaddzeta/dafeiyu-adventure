using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Fishy
{
    public enum GameState { Title, Play, Pause, Dying, Flag, ClearWin, GameOver }

    /// 全局游戏状态（静态类，跨重建保持分数等数据）
    public static class GameManager
    {
        public static GameState State = GameState.Title;
        public static int Score, Rice, Lives = 3, TimeLeft = 300;
        public static int ClearBonus;
        public static int BestScore, BestRice;

        public static FishController Player;
        public static Tilemap TileMap;
        public static readonly Dictionary<Vector3Int, char> Kinds = new Dictionary<Vector3Int, char>();
        public static Vector2 SpawnPos = new Vector2(
            2.5f,
            2f + GameConfig.PlayerHalfH + 0.04f);
        public static Vector2 CheckpointPos;
        public static bool HasCheckpoint;
        public static string NoticeText;
        public static float NoticeTime;

        static float _timeAcc;
        const string BestScoreKey = "Fishy.BestScore";
        const string BestRiceKey = "Fishy.BestRice";

        public static Driver Dr; // 协程宿主（常驻对象）

        // ================= 流程 =================
        public static void LoadProgress()
        {
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            BestRice = PlayerPrefs.GetInt(BestRiceKey, 0);
        }

        public static void SaveProgress()
        {
            bool changed = false;
            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(BestScoreKey, BestScore);
                changed = true;
            }
            if (Rice > BestRice)
            {
                BestRice = Rice;
                PlayerPrefs.SetInt(BestRiceKey, BestRice);
                changed = true;
            }
            if (changed) PlayerPrefs.Save();
        }

        public static void StartGame(bool freshRun = false)
        {
            if (freshRun)
            {
                TimeLeft = 300;
                _timeAcc = 0f;
            }
            State = GameState.Play;
            SfxSynth.StartBGM();
        }

        public static void FullRestart()
        {
            Score = 0; Rice = 0; Lives = 3; ClearBonus = 0;
            HasCheckpoint = false;
            CheckpointPos = Vector2.zero;
            NoticeTime = 0f;
            Bootstrap.SpawnWorld();
            StartGame(true);
        }

        public static void SetCheckpoint(Vector2 pos)
        {
            CheckpointPos = pos;
            HasCheckpoint = true;
            ShowNotice("检查点已激活");
        }

        public static Vector2 GetRespawnPos()
        {
            return HasCheckpoint ? CheckpointPos : SpawnPos;
        }

        public static void ShowNotice(string text)
        {
            NoticeText = text;
            NoticeTime = 1.6f;
        }

        public static void TickPresentation(float dt)
        {
            if (NoticeTime > 0f) NoticeTime = Mathf.Max(0f, NoticeTime - dt);
        }

        /// 每帧调用（仅 Play 状态）：计时
        public static void TickPlay(float dt)
        {
            _timeAcc += dt;
            if (_timeAcc >= 1f)
            {
                _timeAcc -= 1f;
                TimeLeft--;
                if (TimeLeft <= 0)
                {
                    TimeLeft = 0;
                    Lives = 1; // 时间耗尽按最终一命结算，避免重生后立即再次超时
                    KillPlayer(false);
                }
            }
        }

        public static void AddScore(int n) { Score += n; }
        public static void AddRice(int n) { Rice += n; }

        public static void KillPlayer(bool pit)
        {
            if (State != GameState.Play) return;
            State = GameState.Dying;
            SfxSynth.StopBGM();
            SfxSynth.Die();
            if (Player != null) Player.BeginDeath(pit);
        }

        /// 由玩家死亡动画结束回调
        public static void ResolveDeath()
        {
            Lives--;
            if (Lives > 0)
            {
                Bootstrap.SpawnWorld(); // 重建关卡（保留分数与大米）
                StartGame();
            }
            else
            {
                SaveProgress();
                State = GameState.GameOver;
            }
        }

        // ================= 顶砖 =================
        public static void BumpTile(Vector3Int cell)
        {
            if (TileMap == null || !Kinds.ContainsKey(cell)) return;
            char kind = Kinds[cell];
            switch (kind)
            {
                case '?':
                    TileMap.SetTile(cell, GameAssets.TUsed);
                    LevelBuilder.SetCellSprite(cell, GameAssets.Used);
                    Kinds[cell] = 'x';
                    SpawnPopRice(cell);
                    Rice++; AddScore(200);
                    SfxSynth.Coin();
                    BumpAnim(cell);
                    break;
                case 'B':
                    TileMap.SetTile(cell, null);
                    LevelBuilder.HideCell(cell);
                    Kinds.Remove(cell);
                    AddScore(50);
                    SfxSynth.Break();
                    Debris(cell);
                    break;
                default:
                    SfxSynth.Bump();
                    BumpAnim(cell);
                    break;
            }
        }

        static void SpawnPopRice(Vector3Int cell)
        {
            Vector3 world = TileMap.GetCellCenterWorld(cell) + Vector3.up * 0.55f;
            PopRice.Spawn(world);
        }

        static void Debris(Vector3Int cell)
        {
            Vector3 c = TileMap.GetCellCenterWorld(cell);
            Color32[] cols = {
                new Color32(255,255,255,255), new Color32(74,163,232,255)
            };
            for (int i = 0; i < 8; i++)
            {
                Particles.Spawn(
                    c.x, c.y,
                    Random.Range(-1.75f, 1.75f), Random.Range(4f, 10f),
                    cols[i % 2], 0.65f, 15f);
            }
        }

        static void BumpAnim(Vector3Int cell)
        {
            if (Dr != null) Dr.StartCoroutine(BumpCo(cell));
        }

        static IEnumerator BumpCo(Vector3Int cell)
        {
            var m = Matrix4x4.Translate(new Vector3(0, 0.3f, 0));
            TileMap.SetTransformMatrix(cell, m);
            yield return new WaitForSeconds(0.085f);
            TileMap.SetTransformMatrix(cell, Matrix4x4.identity);
        }
    }
}
