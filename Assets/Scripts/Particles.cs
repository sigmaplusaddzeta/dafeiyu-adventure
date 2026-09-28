using System.Collections.Generic;
using UnityEngine;

namespace Fishy
{
    /// 轻量粒子系统：小方块粒子（尘土/碎屑/烟花/水花）
    public class Particles : MonoBehaviour
    {
        class P
        {
            public Transform T;
            public SpriteRenderer SR;
            public Vector2 V;
            public float Life, G;
        }

        static Particles _inst;
        readonly List<P> _list = new List<P>();

        public static void Bootstrap(Transform parent)
        {
            var go = new GameObject("Particles");
            go.transform.SetParent(parent, false);
            _inst = go.AddComponent<Particles>();
        }

        public static void Spawn(float x, float y, float vx, float vy, Color32 c, float life, float g)
        {
            if (_inst == null) return;
            var go = new GameObject("p");
            go.transform.SetParent(_inst.transform, false);
            go.transform.position = new Vector3(x, y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.White2;
            sr.color = c;
            sr.sortingOrder = 5;
            _inst._list.Add(new P { T = go.transform, SR = sr, V = new Vector2(vx, vy), Life = life, G = g });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var p = _list[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    Destroy(p.T.gameObject);
                    _list.RemoveAt(i);
                    continue;
                }
                p.V.y -= p.G * dt;
                p.T.position += new Vector3(p.V.x, p.V.y, 0) * dt;
            }
        }
    }
}
