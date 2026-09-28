using System.Collections;
using UnityEngine;

namespace Fishy
{
    /// 旗杆通关流程：碰旗 → 滑杆 → 走向城堡 → 庆祝（烟花 + 城堡旗升起）
    public class FlagZone : MonoBehaviour
    {
        float _poleX, _baseY, _topY, _doorX;
        Transform _tri;
        bool _running;

        public void Init(float poleX, float baseY, float topY, float doorX, Transform tri)
        {
            _poleX = poleX; _baseY = baseY; _topY = topY; _doorX = doorX; _tri = tri;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_running || GameManager.State != GameState.Play) return;
            var p = GameManager.Player;
            if (p == null || other.attachedRigidbody != p.Body) return;

            _running = true;
            GameManager.Dr.StartCoroutine(Run(p));
        }

        IEnumerator Run(FishController p)
        {
            GameManager.State = GameState.Flag;
            SfxSynth.StopBGM();
            SfxSynth.Flag();

            p.FreezePhysics();
            var pos = p.transform.position;

            // ---- 阶段1：滑杆（从碰旗高度向下）----
            pos.x = _poleX - 0.45f;
            float feetTarget = _baseY > 0f ? _baseY : 3f;   // 基座顶
            float targetY = feetTarget + 0.34f;
            float flagTarget = feetTarget + 0.6f;
            while (pos.y > targetY + 0.01f || (_tri != null && _tri.position.y > flagTarget + 0.01f))
            {
                float d = GameConfig.FlagSlide * Time.deltaTime;
                pos.y = Mathf.Max(pos.y - d, targetY);
                if (_tri != null)
                {
                    var fp = _tri.position;
                    fp.y = Mathf.Max(fp.y - d, flagTarget);
                    _tri.position = fp;
                }
                p.transform.position = pos;
                yield return null;
            }
            yield return new WaitForSeconds(0.35f);

            // ---- 阶段2：走向城堡 ----
            while (p.transform.position.x < _doorX - 0.05f)
            {
                p.SnapToGround(Time.deltaTime, GameConfig.CastleWalk);
                yield return null;
            }

            // ---- 阶段3：庆祝 ----
            GameManager.ClearBonus = GameManager.TimeLeft * 10;
            GameManager.AddScore(GameManager.ClearBonus);
            GameManager.SaveProgress();
            p.HideVisual();
            GameManager.State = GameState.ClearWin;
            SfxSynth.Clear();
            if (GameManager.Dr != null)
            {
                GameManager.Dr.StartCoroutine(RiseCastleFlag());
                GameManager.Dr.StartCoroutine(Fireworks());
            }
        }

        IEnumerator RiseCastleFlag()
        {
            Transform cf = LevelBuilder.CastleFlagT;
            if (cf == null) yield break;
            var sr = cf.GetComponent<SpriteRenderer>();
            sr.enabled = true;
            Vector3 from = new Vector3(_doorX - 0.15f, 5.4f, 0);
            Vector3 to = new Vector3(_doorX - 0.15f, 7.1f, 0);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime;
                cf.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        IEnumerator Fireworks()
        {
            Color32[] cols = {
                new Color32(255,255,255,255), new Color32(126,201,244,255),
                new Color32(255,157,177,255), new Color32(255,233,168,255)
            };
            for (int burst = 0; burst < 6; burst++)
            {
                var cam = CameraFollow.Cam;
                float fx = cam != null ? cam.transform.position.x + Random.Range(-5f, 5f) : _doorX;
                float fy = Random.Range(5f, 12f);
                for (int i = 0; i < 26; i++)
                {
                    float a = Random.value * 6.283f;
                    float sp = Random.Range(2f, 8.5f);
                    Particles.Spawn(fx, fy,
                        Mathf.Cos(a) * sp, Mathf.Sin(a) * sp,
                        cols[i % 4], 0.75f, 1.8f);
                }
                SfxSynth.Stomp(); // 近似爆裂声
                yield return new WaitForSeconds(1.4f);
            }
        }
    }
}
