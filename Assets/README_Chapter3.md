# 第三章 - 卢沟桥（热胀冷缩）

## 流程

1. **桥上行走**：乔乔在桥上行走，平台可贴合桥面轮廓（使用 Platform Height Map 烘焙）
2. **到达桥中**：弹出对话框两次，点击继续切换
3. **人物切换**：乔乔变为手拿罗盘形象
4. **按 E 进入**：下方显示「按 E 进入游戏」
5. **玩法介绍**：按 E 后弹出玩法介绍图，点击继续
6. **罗盘游戏**：60 秒倒计时，点击指针随机转动
   - **春/秋**：弹出说明，继续返回罗盘
   - **夏**：危险！按 ↑ 上键将指针转到春区修复
   - **冬**：危险！按 ↓ 下键将指针转到秋区修复
   - **修复成功**：弹出成功提示，继续
   - **通关**：60 秒内成功修复 3 次
7. **知识补充**：通关后显示 4 张知识图，右侧箭头切换下一张
8. **结尾视频**：知识补充完成后进入 GameEnding 场景，播放结尾视频，点继续返回主菜单

## 创建场景

菜单：**Tools → 创建第三章场景**
- **卢沟桥场景**：第三章主场景
- **游戏结尾视频**：结尾视频场景，播放 ending.mp4 后返回主菜单

## 素材设置

| 项目 | 说明 |
|------|------|
| 桥背景 | 拖到 Background 的 Source Image |
| 桥面平台 | Platform_桥面：拖入桥面 Sprite，添加 Platform Height Map 并烘焙 |
| 对话背景 | Dialog Box Images：拖入 2 个 Image，分别设置各自的 Sprite 作为两段对话的背景 |
| 人物形象 | Idle Sprite（站立图）、Walk Frames（行走帧），走时播放行走动画，静止时显示站立图 |
| 罗盘形象 | Chapter3BridgeController → Compass Sprite（对话后切换） |
| 玩法介绍图 | Intro Image → Source Image |
| 四季图 | Season Sprites（春 0、夏 1、秋 2、冬 3） |
| 四季视频 | Season Videos：4 个 VideoClip（春 0、夏 1、秋 2、冬 3），指针指向该季节时在右侧播放。使用单一 VideoPlayer + RawImage（SeasonVideoOverlay），保证四季逻辑完全一致。旧场景会回退使用 Season0/VideoDisplay |
| 知识补充 | Knowledge Sprites（4 张） |
| 结尾视频 | ending.mp4 放入 StreamingAssets，或拖到 GameEndingController.videoClip |

## 罗盘区域说明

- 指针顺时针：0=春（上）、1=夏（右）、2=秋（下）、3=冬（左）
- **Season Range**：可自定义春夏秋冬角度范围 (X=起始 Y=结束)，默认 春0-90 夏270-360 秋90-180 冬180-270
- **Pointer Rotation Duration**：指针旋转动画时长(秒)，默认 2.5（转得更久）
- **Pointer Extra Rotations**：停下前至少多转的圈数，默认 1（至少转一圈再停）
- 夏季需按 ↑ 转到春；冬季需按 ↓ 转到秋

## 踏不上去 / 空气墙不阻挡

- **平台顶边对齐**：Platform_桥面（或各段平台）的 RectTransform **顶边**要对齐桥面可站立表面，不要悬空
- **加入 Platforms**：桥面平台必须拖入 Chapter3BridgeController → **Platforms** 数组
- **Step Up Height**：若踏不上边缘，在 Inspector 中把 Step Up Height 调大（如 80）
