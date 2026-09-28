using UnityEngine;

namespace Fishy
{
    /// 相机跟随：水平平滑追踪，垂直固定；全关卡范围钳制
    public class CameraFollow : MonoBehaviour
    {
        public static Camera Cam;

        void Awake()
        {
            EnsureCamera();
        }

        // 域重载会清空静态引用，LateUpdate 也要能够按需恢复相机。
        void EnsureCamera()
        {
            if (Cam != null && Cam.gameObject != gameObject) Cam = null;
            if (Cam != null) return;
            Cam = GetComponent<Camera>();
            if (Cam == null) Cam = gameObject.AddComponent<Camera>();
            Cam.orthographic = true;
            Cam.orthographicSize = GameConfig.CamHalfH;
            Cam.backgroundColor = GameConfig.Sky;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.tag = "MainCamera";
            Cam.transform.position = new Vector3(GameConfig.CamHalfW, GameConfig.CamHalfH, -10f);
        }

        void LateUpdate()
        {
            EnsureCamera();

            // 窄窗口自适应：保证水平方向始终能看到 CamHalfW 宽度，防止角色出画
            if (Cam.aspect > 0.01f)
            {
                float need = GameConfig.CamHalfW / Cam.aspect;
                if (Cam.orthographicSize < need) Cam.orthographicSize = need;
            }

            var p = GameManager.Player;
            if (p == null) return;

            float target = Mathf.Clamp(
                p.transform.position.x,
                GameConfig.CamHalfW,
                GameConfig.LevelW - GameConfig.CamHalfW);

            float k = 1f - Mathf.Pow(1f - GameConfig.CamLerp, Time.deltaTime * 60f);
            float nx = Mathf.Lerp(transform.position.x, target, k);
            if (Mathf.Abs(target - nx) < 0.03f) nx = target;

            var pos = transform.position;
            pos.x = nx;
            pos.y = Cam.orthographicSize; // 底边贴地
            pos.z = -10f;
            transform.position = pos;
        }
    }
}
