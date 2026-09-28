using UnityEngine;

namespace Fishy
{
    /// 大米的自动存档点：触碰后本次生命从该位置复活。
    public class CheckpointZone : MonoBehaviour
    {
        Vector2 _spawnPos;
        SpriteRenderer _visual;
        SpriteRenderer _halo;
        bool _active;
        float _t;

        public void Init(Vector2 spawnPos, SpriteRenderer visual, SpriteRenderer halo)
        {
            _spawnPos = spawnPos;
            _visual = visual;
            _halo = halo;
            _active = GameManager.HasCheckpoint
                && Vector2.SqrMagnitude(GameManager.CheckpointPos - spawnPos) < 0.01f;
            RefreshVisual();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_active || GameManager.State != GameState.Play) return;
            var player = GameManager.Player;
            if (player == null || other.attachedRigidbody != player.Body) return;

            _active = true;
            GameManager.SetCheckpoint(_spawnPos);
            SfxSynth.Coin();

            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                Particles.Spawn(
                    transform.position.x,
                    transform.position.y,
                    Mathf.Cos(angle) * 2.2f,
                    Mathf.Sin(angle) * 2.2f,
                    new Color32(255, 233, 168, 255),
                    0.45f,
                    1.5f);
            }
            RefreshVisual();
        }

        void Update()
        {
            if (_visual == null || _halo == null) return;

            _t += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(_t * 4.5f) * (_active ? 0.08f : 0.04f);
            _visual.transform.localScale = new Vector3(1.2f, 1.2f, 1f) * pulse;
            _halo.transform.localScale = new Vector3(1.8f, 1.8f, 1f) * (1f + (pulse - 1f) * 0.5f);
        }

        void RefreshVisual()
        {
            if (_visual != null)
                _visual.color = _active
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(188, 230, 255, 170);
            if (_halo != null)
                _halo.color = _active
                    ? new Color32(255, 214, 120, 90)
                    : new Color32(120, 190, 255, 28);
        }
    }
}
