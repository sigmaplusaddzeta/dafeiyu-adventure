using UnityEngine;

namespace Fishy
{
    /// 全局配置：所有数值由网页版物理参数换算而来
    /// 换算规则（1 格图块 = 16px = 1 Unity 单位，60fps）：
    ///   速度 v px/帧 → v/16*60 = v*3.75 单位/秒
    ///   加速度 a px/帧² → a/16*3600 = a*225 单位/秒²
    public static class GameConfig
    {
        // ---- 玩家移动手感 ----
        public const float WalkSpeed = 9.75f;      // 按住 Shift/J 时的行走速度
        public const float RunSpeed = 15f;         // 默认奔跑速度
        public const float GroundAccel = 49.5f;    // 0.22 px/f² 地面加速
        public const float AirAccel = 33.75f;      // 0.15 px/f² 空中加速
        public const float GroundFriction = 0.8f;  // 地面摩擦（每帧乘算）
        public const float AirFriction = 0.95f;    // 空中摩擦

        // ---- 跳跃 ----
        public const float Gravity = 94.5f;        // 0.42 px/f²
        public const float MaxFall = 26.25f;       // 7.0 px/f 最大下落
        public const float JumpVel = 29.25f;       // 7.8 px/f 起跳
        public const float JumpCut = 0.86f;        // 松开跳跃键每帧速度保留率
        public const float JumpCutThreshold = 4f;  // 上升速度高于此值才削弱
        public const float Coyote = 0.1f;          // 土狼时间 6 帧
        public const float JumpBuffer = 0.1f;      // 跳跃缓冲 6 帧
        public const float StompBounce = 18.75f;   // 5.0 px/f 踩敌反弹
        public const float StompBounceHeld = 28.125f; // 7.5 px/f 按住跳踩敌反弹

        // ---- 流程 ----
        public const float FlagSlide = 12f;        // 3.2 px/f 旗杆下滑
        public const float CastleWalk = 4.875f;    // 1.3 px/f 走向城堡
        public const float BugSpeed = 1.875f;      // 0.5 px/f 米虫速度
        public const float BugActivateDist = 33f;  // 米虫激活距离（相对相机左缘）

        // ---- 关卡结构 ----
        public const int Rows = 17;
        public const int SegCols = 20;
        public const int SegCount = 10;
        public const float LevelW = 200f;          // 200 格 = 200 单位
        public const float PitY = -2.5f;           // 掉出此高度判死

        // ---- 相机 ----
        public const float CamHalfW = 15f;         // 视野半宽 480px/2/16
        public const float CamHalfH = 8.5f;        // 视野半高 272px/2/16
        public const float CamLerp = 0.18f;        // 每帧跟随插值

        public static readonly Color32 Sky = new Color32(92, 148, 252, 255);
    }
}
