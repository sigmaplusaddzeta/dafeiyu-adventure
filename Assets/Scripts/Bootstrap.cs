using UnityEngine;

namespace Fishy
{
    /// 启动器：运行时用代码组装全部场景（相机 / 关卡 / 玩家 / UI / 音频）
    /// 用法：2D 项目导入本文件夹后直接按 Play，无需任何手动配置
    public static class Bootstrap
    {
        static GameObject _world;
        static GameObject _persistent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            EnsurePersistent();
            SpawnWorld();
        }

        /// 常驻系统：相机 / 音频 / 粒子 / UI（重建关卡时不销毁）
        static void EnsurePersistent()
        {
            if (_persistent != null) return;

            var go = new GameObject("FishySystems");
            Object.DontDestroyOnLoad(go);
            _persistent = go;
            GameManager.LoadProgress();

            Time.fixedDeltaTime = 1f / 60f; // 物理 60Hz：与渲染同步，输入跟手不顿挫

            // 相机（复用场景已有主相机或新建）
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("FishyCamera").AddComponent<Camera>();
            }
            cam.transform.SetParent(go.transform, true);
            cam.gameObject.AddComponent<CameraFollow>();
            if (Object.FindAnyObjectByType<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();

            // 物理
            Physics2D.queriesHitTriggers = false;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            Time.fixedDeltaTime = 1f / 60f;
            QualitySettings.vSyncCount = 0; // 由目标帧率统一控帧，避免高刷屏下 vsync 节拍不均
            Application.targetFrameRate = 60;

            // 音频
            var sfxSrc = go.AddComponent<AudioSource>();
            sfxSrc.playOnAwake = false;
            sfxSrc.volume = 0.55f;
            var bgmSrc = go.AddComponent<AudioSource>();
            bgmSrc.playOnAwake = false;
            bgmSrc.volume = 0.4f;
            SfxSynth.Ensure(sfxSrc, bgmSrc);

            // 粒子与 UI
            Particles.Bootstrap(go.transform);
            HudUI.Bootstrap(go.transform);

            GameManager.Dr = go.AddComponent<Driver>();
        }

        /// 重建游戏世界（关卡 / 玩家 / 敌人）
        public static void SpawnWorld()
        {
            if (_world != null) Object.Destroy(_world);
            _world = new GameObject("FishyWorld");
            LevelBuilder.Build(_world.transform);
        }
    }

    /// 驱动器：全局按键 / 状态推进 / 协程宿主
    public class Driver : MonoBehaviour
    {
        void Update()
        {
            GameManager.TickPresentation(Time.deltaTime);
            var st = GameManager.State;

            if (InputProvider.MutePressed()) SfxSynth.ToggleMute();

            switch (st)
            {
                case GameState.Title:
                    if (InputProvider.AnyKeyDown()
                        || Mathf.Abs(InputProvider.Horizontal()) > 0.01f
                        || InputProvider.JumpHeld())
                        GameManager.StartGame();
                    break;

                case GameState.Play:
                    GameManager.TickPlay(Time.deltaTime);
                    if (InputProvider.PausePressed()) GameManager.State = GameState.Pause;
                    if (InputProvider.RestartPressed()) GameManager.FullRestart();
                    break;

                case GameState.Pause:
                    if (InputProvider.PausePressed()) GameManager.State = GameState.Play;
                    if (InputProvider.RestartPressed()) GameManager.FullRestart();
                    break;

                case GameState.ClearWin:
                case GameState.GameOver:
                    if (InputProvider.RestartPressed()) GameManager.FullRestart();
                    break;
            }
        }
    }
}
