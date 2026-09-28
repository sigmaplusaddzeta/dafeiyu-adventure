using UnityEngine;

namespace Fishy
{
    /// 大米币：旋转 + 上下浮动，碰到玩家收集
    public class RiceCoin : MonoBehaviour
    {
        float _t;
        Vector3 _basePos;
        SpriteRenderer _sr;
        bool _got;

        void Awake() { _basePos = transform.position; _t = Random.value * 6.28f; _sr = GetComponent<SpriteRenderer>(); }

        void Update()
        {
            if (_got) return;
            _t += Time.deltaTime;
            transform.position = _basePos + Vector3.up * (Mathf.Sin(_t * 4.8f) * 0.09f);
            float sp = Mathf.Abs(Mathf.Cos(_t * 4.2f));
            _sr.transform.localScale = new Vector3(Mathf.Max(0.08f, sp), 1f, 1f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_got || GameManager.Player == null) return;
            if (other.attachedRigidbody != GameManager.Player.Body) return;
            if (GameManager.State != GameState.Play) return;

            _got = true;
            GameManager.AddRice(1);
            GameManager.AddScore(100);
            SfxSynth.Coin();
            for (int i = 0; i < 5; i++)
                Particles.Spawn(transform.position.x, transform.position.y,
                    Random.Range(-1.9f, 1.9f), Random.Range(-1f, 3.4f),
                    new Color32(255, 233, 168, 255), 0.3f, 6f);
            Destroy(gameObject);
        }
    }

    /// 顶问号块弹出的展示大米（纯视觉，向上弹出后消失）
    public class PopRice : MonoBehaviour
    {
        float _vy = 12.75f;
        float _life;

        public static void Spawn(Vector3 pos)
        {
            var go = new GameObject("PopRice");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.Rice;
            sr.sortingOrder = 4;
            go.AddComponent<PopRice>();
        }

        void Update()
        {
            _life += Time.deltaTime;
            _vy -= 56.25f * Time.deltaTime;
            transform.position += Vector3.up * (_vy * Time.deltaTime);
            if (_life > 0.5f) Destroy(gameObject);
        }
    }

    /// 米虫：来回巡逻，悬崖折返，可踩扁，触碰致死
    public class RiceBug : MonoBehaviour
    {
        Rigidbody2D _rb;
        SpriteRenderer _sr;
        BoxCollider2D _col;

        float _vx = -GameConfig.BugSpeed;
        float _vy;
        bool _active, _squashed;
        float _sqT, _anim;

        public void Init()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = GameAssets.Bug;
            _sr.sortingOrder = 2;
            _col = GetComponent<BoxCollider2D>();
        }

        void Awake() { Init(); }

        void SetVel(Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = v;
#else
            _rb.velocity = v;
#endif
        }

        void FixedUpdate()
        {
            if (GameManager.State != GameState.Play || _squashed)
            {
                if (_rb != null) SetVel(Vector2.zero);
                return;
            }

            // 相机视野左缘前方 33 单位内才激活（与网页版一致）
            var cam = CameraFollow.Cam;
            if (!_active)
            {
                if (cam != null && transform.position.x <
                    cam.transform.position.x - GameConfig.CamHalfW + GameConfig.BugActivateDist)
                    _active = true;
                else return;
            }

            float dt = Time.fixedDeltaTime;
            _vy = Mathf.Max(_vy - GameConfig.Gravity * dt, -GameConfig.MaxFall);

            // 悬崖折返
            float dir = Mathf.Sign(_vx);
            var front = (Vector2)transform.position + Vector2.right * (dir * 0.42f);
            var hit = Physics2D.Raycast(front, Vector2.down, 0.5f, Physics2D.DefaultRaycastLayers);
            if (hit.collider == null) _vx = -_vx;

            SetVel(new Vector2(_vx, _vy));

            // 动画
            _anim += dt;
            _sr.flipX = _vx > 0f;
            _sr.transform.localPosition = new Vector3(0, Mathf.Sin(_anim * 12f) * 0.03f, 0);
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (_squashed) return;

            // 撞墙折返
            foreach (var cp in c.contacts)
            {
                if (Mathf.Abs(cp.normal.x) > 0.5f) { _vx = -_vx; break; }
            }

            // 与玩家交互
            var p = GameManager.Player;
            if (p == null || GameManager.State != GameState.Play) return;
            if (c.gameObject != p.gameObject) return;

            bool falling = p.GetVel().y < -1.9f;
            bool above = p.transform.position.y > transform.position.y + 0.08f;

            if (falling && above)
            {
                Squash();
                p.Bounce(InputProvider.JumpHeld());
            }
            else
            {
                GameManager.KillPlayer(false);
            }
        }

        void Squash()
        {
            _squashed = true;
            _sqT = 0f;
            SfxSynth.Stomp();
            GameManager.AddScore(200);
            _sr.sprite = GameAssets.BugSq;
            _sr.transform.localPosition = new Vector3(0, -0.14f, 0);
            _col.enabled = false;
            SetVel(Vector2.zero);
        }

        void Update()
        {
            if (_squashed)
            {
                _sqT += Time.deltaTime;
                if (_sqT > 0.6f) Destroy(gameObject);
            }
        }
    }
}
