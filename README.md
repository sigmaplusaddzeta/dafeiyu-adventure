# 大肥鱼大冒险

一个使用 Unity 6 制作的 2D 横版平台跳跃原型。场景、贴图、粒子、音效和 UI 都由代码在运行时生成，不依赖外部美术资源。

## 运行方式

1. 使用 Unity `6000.0.51f1` 打开项目。
2. 打开 `Assets/Scenes/SampleScene.unity`。
3. 进入 Play Mode。

## 操作

| 操作 | 按键 |
| --- | --- |
| 移动 | `A/D` 或左右方向键 |
| 跳跃 | `Space`、`W` 或上方向键 |
| 加速跑 | `Shift` 或 `J` |
| 暂停 | `P` |
| 重新开始 | `R` |
| 静音 | `M` |

## 当前玩法

- 10 段连续关卡，包含地面、管道、砖块、坑洞、阶梯、米虫和旗杆终点。
- 支持土狼时间、跳跃缓冲、可变跳跃高度和踩敌反弹。
- 大米币可以作为分数和收集目标。
- 关卡中新增两个检查点，死亡后从最近激活的检查点复活。
- 记录历史最高分和单局最多大米，保存在 `PlayerPrefs`。
- 时间耗尽会直接进入最终结算，避免重生后反复超时。

## 代码结构

| 文件 | 作用 |
| --- | --- |
| `Bootstrap.cs` | 运行时组装游戏世界和常驻系统 |
| `GameManager.cs` | 游戏状态、分数、生命、计时、检查点和存档 |
| `FishController.cs` | 玩家移动、跳跃、动画和死亡流程 |
| `LevelBuilder.cs` | 将 ASCII 地图转换为 Tilemap、实体和装饰 |
| `LevelMaps.cs` | 10 段关卡数据 |
| `CheckpointZone.cs` | 检查点触发和视觉反馈 |
| `Entities.cs` | 大米币和米虫 |
| `HudUI.cs` | 标题、HUD、暂停、结算界面 |
| `GameAssets.cs` | 运行时像素贴图和 Tile 生成 |
| `SfxSynth.cs` | 运行时合成音效和 BGM |
| `Resources/Player/whale_maid.png` | 鲸鱼少女主角透明立绘 |

## Unity MCP

项目已接入 `com.coplaydev.unity-mcp`。在 `Window > MCP for Unity` 中启动 Server 并连接后，可以通过 Codex 直接修改场景、脚本和资源。
