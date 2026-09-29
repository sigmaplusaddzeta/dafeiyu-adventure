using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fishy
{
    /// 大肥鱼角色控制器：手感与网页版完全一致
    /// （加速度移动 / 土狼时间 / 跳跃缓冲 / 可变跳跃高度）
    public class FishController : MonoBehaviour
    {
        public Vector2 Velocity;
        public bool Grounded;
        public int Face = 1;

        Rigidbody2D _rb;
        BoxCollider2D _col;
        Transform _visual;
        SpriteRenderer _sr;

        float _coyote, _jumpBuf, _bob, _sprayT, _dustT, _turnT, _animTime;
        bool _jumpHeld, _dying, _walkMode;
        float _hAxis; bool _walkHeld; // Update 采样的输入缓存（FixedUpdate 使用，更跟手）
        readonly List<Collider2D> _groundHits = new List<Collider2D>(8);
        ContactFilter2D _groundFilter;
        Sprite[] _idleFrames, _walkRightFrames, _walkLeftFrames;
        Sprite[] _runRightFrames, _runLeftFrames, _jumpFrames;

        const float HalfW = GameConfig.PlayerHalfW;
        const float HalfH = GameConfig.PlayerHalfH;
        const float PlayerVisualScale = 0.82f * 3f;
        const float IdleSpeedThreshold = 0.12f;

        public Rigidbody2D Body { get { return _rb; } }

        public void Init(Vector2 pos)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            _col = gameObject.AddComponent<BoxCollider2D>();
            _col.size = new Vector2(HalfW * 2f, HalfH * 2f);
            _col.edgeRadius = 0.08f;
            _col.sharedMaterial = new PhysicsMaterial2D("fishSlip") { friction = 0f, bounciness = 0f }; // 无摩擦，防卡墙

            var vgo = new GameObject("Visual");
            vgo.transform.SetParent(transform, false);
            _visual = vgo.transform;
            _sr = vgo.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 3;
            _sr.flipX = false;
            _visual.localPosition = new Vector3(0f, -HalfH, 0f);
            _visual.localScale = new Vector3(PlayerVisualScale, PlayerVisualScale, 1f);

            _idleFrames = GameAssets.LoadPlayerFrames("stand", 2);
            _walkRightFrames = GameAssets.LoadPlayerFrames("walk_right", 4);
            _walkLeftFrames = GameAssets.LoadPlayerFrames("walk_left", 4);
            _runRightFrames = GameAssets.LoadPlayerFrames("run_right", 4);
            _runLeftFrames = GameAssets.LoadPlayerFrames("run_left", 4);
            _jumpFrames = GameAssets.LoadPlayerFrames("jump", 4);
            _sr.sprite = _idleFrames.Length > 0 && _idleFrames[0] != null
                ? _idleFrames[0]
                : GameAssets.PlayerSprite;

            transform.position = pos;
            Velocity = Vector2.zero;
            Face = 1; Grounded = false; _dying = false;
            _walkMode = false; _animTime = 0f;
            _sprayT = 1f; _dustT = 0f;
            _groundFilter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = Physics2D.DefaultRaycastLayers
            };
            gameObject.SetActive(true);
        }

        void Update()
        {
            // 先采样输入：标题页面开始游戏的同一帧，后续 FixedUpdate 也能立即移动。
            _hAxis = InputProvider.Horizontal();
            _jumpHeld = InputProvider.JumpHeld();
            _walkHeld = InputProvider.WalkModifierHeld();

            if (GameManager.State != GameState.Play || _dying) return;

            if (InputProvider.JumpPressed()) _jumpBuf = GameConfig.JumpBuffer;
            if (_jumpBuf > 0f) _jumpBuf -= Time.deltaTime;

            // 转向即时响应 + 转身挤压动画
            if (_hAxis > 0.01f) { if (Face != 1) { Face = 1; _turnT = 0.14f; } }
            else if (_hAxis < -0.01f) { if (Face != -1) { Face = -1; _turnT = 0.14f; } }

            if (transform.position.y < GameConfig.PitY)
            {
                GameManager.KillPlayer(true);
                return;
            }

            Animate(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (GameManager.State != GameState.Play || _dying)
            {
                if (_rb != null) SetVel(Vector2.zero);
                return;
            }

            float dt = Time.fixedDeltaTime;
            float h = _hAxis;
            bool walk = _walkHeld;
            _walkMode = walk;
            if (_rb.IsSleeping()) _rb.WakeUp();
            float max = walk ? GameConfig.WalkSpeed : GameConfig.RunSpeed;
            float acc = Grounded ? GameConfig.GroundAccel : GameConfig.AirAccel;

            if (h > 0f) Velocity.x += acc * dt;
            else if (h < 0f) Velocity.x -= acc * dt;
            else
            {
                float fr = Mathf.Pow(Grounded ? GameConfig.GroundFriction : GameConfig.AirFriction, dt * 60f);
                Velocity.x *= fr;
                if (Mathf.Abs(Velocity.x) < 0.05f) Velocity.x = 0f;
            }
            Velocity.x = Mathf.Clamp(Velocity.x, -max, max);

            // 跳跃（缓冲 + 土狼时间）
            if (_coyote > 0f) _coyote -= dt;
            if (_jumpBuf > 0f && _coyote > 0f)
            {
                Velocity.y = GameConfig.JumpVel;
                _coyote = 0f; _jumpBuf = 0f;
                SfxSynth.Jump();
                for (int i = 0; i < 4; i++)
                    Particles.Spawn(transform.position.x, transform.position.y - HalfH,
                        Random.Range(-1f, 1f), Random.Range(0f, 1.5f),
                        new Color32(207, 232, 255, 255), 0.25f, 0f);
            }

            // 可变跳跃：松键削弱上升
            if (!_jumpHeld && Velocity.y > GameConfig.JumpCutThreshold)
                Velocity.y *= Mathf.Pow(GameConfig.JumpCut, dt * 60f);

            // 重力
            Velocity.y = Mathf.Max(Velocity.y - GameConfig.Gravity * dt, -GameConfig.MaxFall);

            // 落地检测
            Grounded = false;
            int hitCount = Physics2D.OverlapBox(
                _rb.position + Vector2.down * (HalfH + 0.04f),
                new Vector2(HalfW * 1.8f, 0.08f), 0f, _groundFilter, _groundHits);
            for (int i = 0; i < hitCount; i++)
            {
                var hz = _groundHits[i];
                if (hz.attachedRigidbody != _rb) { Grounded = true; break; }
            }
            if (Grounded)
            {
                _coyote = GameConfig.Coyote;
                _dustT += dt;
                if (_dustT > 0.15f && Mathf.Abs(Velocity.y) < 0.01f && _wasFalling)
                {
                    _dustT = 0f; _wasFalling = false;
                    for (int i = 0; i < 3; i++)
                        Particles.Spawn(
                            transform.position.x + Random.Range(-0.4f, 0.4f),
                            transform.position.y - HalfH,
                            Random.Range(-2.2f, 2.2f), Random.Range(-1f, 0f),
                            new Color32(223, 233, 245, 255), 0.2f, 0f);
                }
            }
            if (Velocity.y < -6f) _wasFalling = true;

            SetVel(Velocity);
        }

        bool _wasFalling;

        void SetVel(Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = v;
#else
            _rb.velocity = v;
#endif
        }
        public Vector2 GetVel()
        {
#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity;
#else
            return _rb.velocity;
#endif
        }

        // ---- 头顶撞块 ----
        void OnCollisionEnter2D(Collision2D c)
        {
            if (GameManager.State != GameState.Play || _dying) return;
            if (Velocity.y <= 1f) return; // 用期望速度判断（物理引擎速度在回调时已被碰撞清零）
            if (GameManager.TileMap == null) return;

            Vector3Int best = Vector3Int.zero;
            float bd = 99f;
            bool found = false;
            foreach (var cp in c.contacts)
            {
                if (cp.normal.y > -0.5f) continue; // 只处理天花板法线
                var cell = GameManager.TileMap.WorldToCell(
                    new Vector2(cp.point.x, cp.point.y + 0.12f));
                Vector3 center = GameManager.TileMap.GetCellCenterWorld(cell);
                float d = Mathf.Abs(center.x - transform.position.x);
                if (d < bd) { bd = d; best = cell; found = true; }
            }
            if (found)
                GameManager.BumpTile(best);
        }

        // ---- 动画：起伏 / 眨眼 / 喷水 ----
        void Animate(float dt)
        {
            float scaleX = 1f;
            float scaleY = 1f;

            // 转身挤压回弹（先压扁再弹回，马里奥式急转手感）
            if (_turnT > 0f)
            {
                _turnT -= dt;
                float k = Mathf.Clamp01(_turnT / 0.14f);
                scaleX = 0.6f + 0.4f * (1f - k);
                scaleY = 1.16f - 0.16f * (1f - k);
            }
            _visual.localScale = new Vector3(
                PlayerVisualScale * scaleX,
                PlayerVisualScale * scaleY,
                1f);

            _bob += dt * (2f + Mathf.Abs(Velocity.x) * 0.6f);
            float yOff = Grounded
                ? (Mathf.Abs(Velocity.x) > 0.3f ? Mathf.Sin(_bob * 2f) * 0.075f : 0f)
                : -0.0625f;
            _visual.localPosition = new Vector3(0, -HalfH + yOff, 0);
            _sr.flipX = false;
            UpdateFrame(dt);

            _sprayT += dt;
            if (Grounded && _sprayT > 1.83f)
            {
                _sprayT = 0f;
                for (int i = 0; i < 4; i++)
                    Particles.Spawn(
                        transform.position.x + Face * 0.55f,
                        transform.position.y + 0.3f,
                        Face * Random.Range(0.75f, 2.25f),
                        Random.Range(-1f, -3f),
                        new Color32(191, 228, 255, 255), 0.37f, -0.6f);
            }
        }

        void UpdateFrame(float dt)
        {
            Sprite[] frames;
            int index;

            if (!Grounded)
            {
                frames = _jumpFrames;
                float vy = Velocity.y;
                index = vy > 8f ? 1 : vy > 2f ? 0 : vy > -3f ? 2 : 3;
            }
            else if (Mathf.Abs(Velocity.x) < IdleSpeedThreshold)
            {
                frames = _idleFrames;
                _animTime += dt * 2f;
                index = (int)_animTime;
            }
            else if (_walkMode)
            {
                float speed = Mathf.Abs(Velocity.x);
                float t = Mathf.InverseLerp(0f, GameConfig.WalkSpeed, speed);
                _animTime += dt * Mathf.Lerp(3.5f, 6f, t);
                frames = Face < 0 ? _walkLeftFrames : _walkRightFrames;
                index = (int)_animTime;
            }
            else
            {
                float speed = Mathf.Abs(Velocity.x);
                float t = Mathf.InverseLerp(0f, GameConfig.RunSpeed, speed);
                _animTime += dt * Mathf.Lerp(7f, 12f, t);
                frames = Face < 0 ? _runLeftFrames : _runRightFrames;
                index = (int)_animTime;
            }

            if (frames == null || frames.Length == 0)
            {
                _sr.sprite = GameAssets.PlayerSprite;
                return;
            }

            index %= frames.Length;
            if (frames[index] != null) _sr.sprite = frames[index];
        }

        // ---- 死亡动画（悬停后坠落） ----
        public void BeginDeath(bool pit)
        {
            if (_dying) return;
            _dying = true;
            _rb.simulated = false;
            GameManager.Dr.StartCoroutine(DeathCo(pit));
        }

        IEnumerator DeathCo(bool pit)
        {
            Velocity = Vector2.zero;
            float hover = 0.42f;
            while (hover > 0f) { hover -= Time.deltaTime; yield return null; }

            float vy = pit ? 0f : GameConfig.JumpVel * 0.9f;
            var pos = transform.position;
            while (pos.y > GameConfig.PitY - 3f)
            {
                vy = Mathf.Max(vy - GameConfig.Gravity * Time.deltaTime, -GameConfig.MaxFall);
                pos.y += vy * Time.deltaTime;
                transform.position = pos;
                if (_visual != null) _visual.localPosition = new Vector3(0f, -HalfH, 0f);
                if (_sr != null) _sr.flipY = false;
                yield return null;
            }
            GameManager.ResolveDeath();
        }

        // ---- 踩敌反弹 ----
        public void Bounce(bool held)
        {
            Velocity.y = held ? GameConfig.StompBounceHeld : GameConfig.StompBounce;
            SetVel(Velocity);
        }

        // ---- 旗杆流程支持 ----
        public void FreezePhysics()
        {
            _rb.simulated = false;
        }
        public void UnfreezePhysics()
        {
            _rb.simulated = true;
        }

        public void SnapToGround(float dt, float speed)
        {
            var pos = transform.position;
            pos.x += speed * dt;
            var hit = Physics2D.Raycast(
                (Vector2)pos, Vector2.down, HalfH + 1.2f, Physics2D.DefaultRaycastLayers);
            if (hit.collider != null)
                pos.y = hit.point.y + HalfH;
            transform.position = pos;
            _visual.localScale = new Vector3(
                PlayerVisualScale,
                PlayerVisualScale,
                1f);
            _sr.flipX = false;
            _bob += dt * 8f;
            _visual.localPosition = new Vector3(
                0f,
                -HalfH + Mathf.Sin(_bob * 2f) * 0.075f,
                0f);
        }

        public void HideVisual() { if (_visual != null) _visual.gameObject.SetActive(false); }
    }
}
