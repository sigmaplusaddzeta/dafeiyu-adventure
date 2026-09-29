using UnityEngine;
using UnityEngine.UI;

namespace Fishy
{
    /// HUD 与全部界面（标题 / 暂停 / 结算 / 游戏结束），运行时用代码搭建
    public class HudUI : MonoBehaviour
    {
        Text _score, _rice, _world, _time, _lives, _muteHint;
        GameObject _title, _pause, _over, _clear;
        Text _overScore, _clearStats, _titleBest, _overBest, _clearBest, _notice;
        Image _titleFish;
        float _t;
        int _lastScore = int.MinValue, _lastRice = int.MinValue, _lastTime = int.MinValue;
        int _lastLives = int.MinValue, _lastBestScore = int.MinValue, _lastBestRice = int.MinValue;
        bool _lastMuted;
        GameState _lastState = (GameState)(-1);

        public static void Bootstrap(Transform parent)
        {
            var go = new GameObject("Hud");
            go.transform.SetParent(parent, false);
            go.AddComponent<HudUI>();
        }

        void Awake()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 272);

            // ---- 顶部 HUD ----
            _score = Text(canvasGo.transform, "分数 000000", 10, 6, 130, TextAnchor.UpperLeft);
            _rice = Text(canvasGo.transform, "米 x00", 150, 6, 70, TextAnchor.UpperLeft);
            _world = Text(canvasGo.transform, "世界 1-1", 240, 6, 80, TextAnchor.UpperLeft);
            _time = Text(canvasGo.transform, "时间 300", 330, 6, 80, TextAnchor.UpperLeft);
            _lives = Text(canvasGo.transform, "生命 x3", 420, 6, 55, TextAnchor.UpperRight);
            _muteHint = Text(canvasGo.transform, "静音 M", 10, 22, 80, TextAnchor.UpperLeft);
            _muteHint.gameObject.SetActive(false);

            // ---- 标题画面 ----
            _title = Panel(canvasGo.transform, GameConfig.Sky);
            BigText(_title.transform, "大肥鱼大冒险", 34, new Color32(0, 0, 0, 120), 0f, 2f);
            BigText(_title.transform, "大肥鱼大冒险", 34, new Color32(255, 255, 255, 255), 3f, 0f);
            Text(_title.transform, "FISHY ADVENTURE · 世界 1-1", 0, -66, 480, TextAnchor.MiddleCenter, 12);
            _titleBest = Text(_title.transform, "", 0, -94, 480, TextAnchor.MiddleCenter, 10);
            _titleBest.color = new Color32(255, 233, 168, 255);
            _blink = Text(_title.transform, "按任意键开始", 0, -130, 480, TextAnchor.MiddleCenter, 14);
            var help = Text(_title.transform, "←→/AD 移动   空格/↑ 跳跃   Shift 加速跑   M 静音", 0, -176, 480, TextAnchor.MiddleCenter, 10);
            help.color = new Color32(232, 240, 255, 255);

            var fishGo = new GameObject("TitleFish");
            fishGo.transform.SetParent(_title.transform, false);
            _titleFish = fishGo.AddComponent<Image>();
            _titleFish.sprite = GameAssets.PlayerSprite;
            _titleFish.preserveAspect = true;
            _titleFish.rectTransform.sizeDelta = new Vector2(36, 54);
            _titleFish.rectTransform.anchoredPosition = new Vector2(0, 12);

            // ---- 暂停 ----
            _pause = Panel(canvasGo.transform, new Color32(0, 0, 0, 100));
            BigText(_pause.transform, "暂停", 24, Color.white, 0f, 0f);
            Text(_pause.transform, "按 P 继续    按 R 重新开始", 0, -40, 480, TextAnchor.MiddleCenter, 12);

            // ---- 游戏结束 ----
            _over = Panel(canvasGo.transform, new Color32(0, 0, 0, 160));
            BigText(_over.transform, "游戏结束", 30, Color.white, 0f, 20f);
            _overScore = Text(_over.transform, "最终分数 0", 0, -28, 480, TextAnchor.MiddleCenter, 14);
            _overBest = Text(_over.transform, "", 0, -52, 480, TextAnchor.MiddleCenter, 10);
            _overBest.color = new Color32(255, 233, 168, 255);
            Text(_over.transform, "按 R 重新开始", 0, -78, 480, TextAnchor.MiddleCenter, 12);

            // ---- 通关 ----
            _clear = Panel(canvasGo.transform, new Color32(0, 0, 0, 90));
            var ct = BigText(_clear.transform, "通关！", 26, new Color32(255, 233, 168, 255), 0f, 26f);
            ct.rectTransform.anchoredPosition = new Vector2(0, 20);
            _clearStats = Text(_clear.transform, "", 0, -24, 480, TextAnchor.MiddleCenter, 13);
            _clearStats.rectTransform.sizeDelta = new Vector2(480, 60);
            _clearBest = Text(_clear.transform, "", 0, -54, 480, TextAnchor.MiddleCenter, 10);
            _clearBest.color = new Color32(255, 233, 168, 255);
            _clearHint = Text(_clear.transform, "按 R 再来一局", 0, -80, 480, TextAnchor.MiddleCenter, 12);

            _notice = Text(canvasGo.transform, "", 0, 34, 480, TextAnchor.MiddleCenter, 13);
            _notice.color = new Color32(255, 233, 168, 255);
            _notice.gameObject.SetActive(false);
        }

        Text _blink, _clearHint;

        static Text Text(Transform parent, string s, float x, float y, float w,
            TextAnchor align, int size = 10)
        {
            var go = new GameObject("t");
            go.transform.SetParent(parent, false);
            var tr = go.AddComponent<RectTransform>();
            tr.sizeDelta = new Vector2(w, 24);
            if (parent.name == "Canvas")
            {
                tr.anchorMin = new Vector2(0, 1);
                tr.anchorMax = new Vector2(0, 1);
                tr.pivot = new Vector2(0, 1);
                tr.anchoredPosition = new Vector2(x, -y);
                tr.localEulerAngles = Vector3.zero;
                // 右对齐的项以右边定位
                if (align == TextAnchor.UpperRight)
                {
                    tr.pivot = new Vector2(1, 1);
                    tr.anchoredPosition = new Vector2(x + w, -y);
                }
            }
            else
            {
                tr.anchorMin = new Vector2(0.5f, 0.5f);
                tr.anchorMax = new Vector2(0.5f, 0.5f);
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.anchoredPosition = new Vector2(x, y);
            }
            var txt = go.AddComponent<Text>();
            txt.font = GameAssets.UiFont;
            txt.fontSize = size;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = align;
            txt.color = Color.white;
            txt.text = s;
            txt.raycastTarget = false;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color32(0, 0, 0, 120);
            sh.effectDistance = new Vector2(1, -1);
            return txt;
        }

        static Text BigText(Transform parent, string s, int size, Color c, float x, float y)
        {
            var txt = Text(parent, s, x, y, 480, TextAnchor.MiddleCenter, size);
            txt.color = c;
            return txt;
        }

        static GameObject Panel(Transform parent, Color32 c)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            var tr = img.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            return go;
        }

        void RefreshBestLabels()
        {
            string score = GameManager.BestScore.ToString("D6");
            string rice = GameManager.BestRice.ToString("D2");
            _titleBest.text = "最佳分数 " + score + "   最多大米 " + rice;
            _overBest.text = "历史最佳 " + GameManager.BestScore + "   最多大米 " + GameManager.BestRice;
            _clearBest.text = "历史最佳 " + GameManager.BestScore + "   最多大米 " + GameManager.BestRice;
            _lastBestScore = GameManager.BestScore;
            _lastBestRice = GameManager.BestRice;
        }

        void Update()
        {
            _t += Time.deltaTime;
            var st = GameManager.State;

            if (st != _lastState)
            {
                _lastState = st;
                _title.SetActive(st == GameState.Title);
                _pause.SetActive(st == GameState.Pause);
                _over.SetActive(st == GameState.GameOver);
                _clear.SetActive(st == GameState.ClearWin);

                if (st == GameState.GameOver)
                    _overScore.text = "最终分数 " + GameManager.Score;
                if (st == GameState.ClearWin)
                {
                    _clearStats.text = "最终分数 " + GameManager.Score +
                        "   收集大米 " + GameManager.Rice +
                        "\n时间奖励 +" + GameManager.ClearBonus;
                }
            }

            if (GameManager.Score != _lastScore)
            {
                _lastScore = GameManager.Score;
                _score.text = "分数 " + _lastScore.ToString("D6");
            }
            if (GameManager.Rice != _lastRice)
            {
                _lastRice = GameManager.Rice;
                _rice.text = "米 x" + _lastRice.ToString("D2");
            }
            if (GameManager.TimeLeft != _lastTime)
            {
                _lastTime = GameManager.TimeLeft;
                _time.text = "时间 " + Mathf.Max(0, _lastTime).ToString("D3");
            }
            if (GameManager.Lives != _lastLives)
            {
                _lastLives = GameManager.Lives;
                _lives.text = "生命 x" + _lastLives;
            }
            if (SfxSynth.Muted != _lastMuted)
            {
                _lastMuted = SfxSynth.Muted;
                _muteHint.gameObject.SetActive(_lastMuted);
            }

            if (GameManager.BestScore != _lastBestScore || GameManager.BestRice != _lastBestRice)
                RefreshBestLabels();

            if (_notice != null)
            {
                bool show = st == GameState.Play && GameManager.NoticeTime > 0f;
                _notice.gameObject.SetActive(show);
                if (show)
                {
                    if (_notice.text != GameManager.NoticeText)
                    {
                        _notice.text = GameManager.NoticeText;
                    }
                    var nc = _notice.color;
                    nc.a = Mathf.Clamp01(GameManager.NoticeTime / 0.45f);
                    _notice.color = nc;
                }
            }

            if (_blink != null && _blink.gameObject.activeInHierarchy)
            {
                var c = Color.white;
                c.a = (Mathf.FloorToInt(_t * 2f) % 2 == 0) ? 1f : 0f;
                _blink.color = c;
            }
            if (_titleFish != null && _titleFish.gameObject.activeInHierarchy)
            {
                var p = _titleFish.rectTransform.anchoredPosition;
                p.y = 12f + Mathf.Abs(Mathf.Sin(_t * 3.3f)) * 6f;
                _titleFish.rectTransform.anchoredPosition = p;
            }

            if (st == GameState.ClearWin)
            {
                var c = Color.white;
                c.a = (Mathf.FloorToInt(_t * 2f) % 2 == 0) ? 1f : 0f;
                _clearHint.color = c;
            }
        }
    }
}
