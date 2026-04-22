# 乔家大院游戏 - 素材与场景说明

## 游戏流程

### 主菜单
0. **游戏菜单** → 背景 + 「游戏开始」「退出游戏」按钮 → 点游戏开始 → 进入开场视频

### 第一章
1. **开场分镜** → 黑屏渐显 → 多页开场图文（字幕+配音，含视频/特效步骤）→ 渐隐 → 进入第一章前置对话
2. **第一章前置（Chapter1Intro）** → 黑屏渐显一张背景图 → 约 0.5s 后在背景上渐显第一张对话框图（点继续）→ 渐隐后渐显第二张对话框图（点继续）→ 黑屏渐隐 → 进入户外并渐显可走动画面
3. **户外** → 黑屏渐显 → 到门前按 E →（可选：黑屏后背景图 + 第一张对话框渐显 + 继续；再在同背景上渐显第二张对话框 + 继续）→ 进入规则
4. **规则视频** → 黑屏渐显 → 播放规则 → 点开始游戏 → 渐隐 → 进入游戏
5. **榫卯游戏** → 黑屏渐显 → 配对游戏 → 三个榫卯全部完成后 → **Chapter1Outro**：短视频（播完停在最后一帧）→ 两段对白文字（UI Text）自动渐显并可选配音（无需点击）→ 黑屏渐显 → **Chapter2Intro**（第二章）

### 第二章
6. **Chapter2Intro** → … → **Chapter2VideoEnd**：通关视频播完停在最后一帧 → **自动**渐显一段**纯文字字幕**（可配 `subtitleVoiceClip`）→ **黑屏渐显** → **小知识插图**（`knowledgeSprite`）+ 图上 **「继续」** → 进入第三章（默认 `Chapter3Bridge`）
  - 场景结构更新后：在 Unity 菜单 **Tools → 创建第二章场景 → 5.通关视频** 重新生成 `Chapter2VideoEnd.scene`，或在场景里手动对齐 `Chapter2VideoEndController` 的新字段。

### 第三章
7. **Chapter3Bridge** → 卢沟桥场景 → 知识补充完成后 → 进入结尾视频

### 游戏结尾
8. **GameEnding** → 多段结尾演出（首图+字幕→点击→视频1两段字幕→视频2一段字幕→插图→终图+字幕→点击回主菜单），见下节

---

## 素材放置

将以下素材放入 `Assets/素材` 或对应位置：

| 用途 | 建议文件名 | 说明 |
|-----|-----------|------|
| 菜单背景 | 菜单背景.png/jpg | 游戏菜单背景图，拖到 GameMenu 的 Background |
| 户外场景图 | 乔家大院外部.png/jpg | 乔家大院外部背景 |
| 乔乔形象 | idle.png（站立图）、walk_1.png, walk_2.png...（行走帧） | 见下方「乔乔行走帧设置」 |
| 开场视频 | opening.mp4 | 放入 StreamingAssets，或拖到 OpeningVideoController |
| 规则视频 | rules.mp4 | 放入 StreamingAssets，或拖到 RulesVideoController |
| 结尾视频 1/2 | `ending_part1.mp4`、`ending_part2.mp4` | 放入 StreamingAssets，或在 **GameEndingController** 拖 `videoPart1Clip` / `videoPart2Clip` |
| 游戏背景 | 游戏背景.png | 榫卯游戏背景图 |
| 榫卯-槽位空 | - | 上方三个槽的默认/空状态 |
| 榫卯-微微铆合 | - | 匹配成功后的图，可调 DropZone 的 offset 调节位置大小 |
| 榫卯-完全铆合 | - | 按空格完成后的图 |
| 榫卯介绍图 | - | 完全铆合后渐显展示，每个 DropZone 可设不同介绍图 |
| 可拖榫卯 | - | 下方三个可拖物品的图 |
| 测量仪 | - | 见下方「测量仪素材设置」 |

---

## 乔乔行走帧设置（重要）

1. 把行走帧图片（如 walk_1.png, walk_2.png...）和站立图放入 `Assets/素材` 文件夹
2. 打开**户外场景**：双击 `Assets/Scenes/Outdoor`
3. 找 **Player** 对象（任选一种方式）：
   - **方式 A**：在**层级**窗口顶部的搜索框输入 `Player` 回车
   - **方式 B**：展开 `Canvas` → 展开其子物体（可能叫 `户外区域` 或 `Area_Outside`）→ 点选 **Player**
4. 选中 Player 后，在右侧 **Inspector** 找到 **Qiao Qiao Player Controller** 组件
5. **Idle Sprite**：拖入站立图（静止时的模样）
6. **Walk Frames**：把 **Size** 改成帧数（如 4），再把每张行走帧图拖到 **Element 0**、**Element 1**...（移动时播放）

---

## 创建场景

在 Unity 菜单：**Tools → 创建乔家大院游戏场景**

- 可单独创建每个场景，或选「全部场景」一键创建
- 创建后按顺序：GameMenu → OpeningVideo → Outdoor → RulesVideo → **5.第一章结束过渡（Chapter1Outro）** → **6.榫卯游戏**（或先 6 再 5，两场景独立保存）
- **首次使用 Chapter1Outro**：在 Unity 菜单任选其一即可生成 `Assets/Scenes/Chapter1Outro.scene` 并写入 Build Settings：  
  - **Tools → 创建乔家大院游戏场景 → 5.第一章结束过渡(视频+自动对话)**  
  - 若子菜单里看不到：**Tools → 创建 Chapter1Outro 场景**（顶层备用入口）  
  - 若仍没有：打开 **Console** 看是否有红色编译错误（脚本报错时整个 Editor 菜单都不会更新）

### Chapter1Outro（第一章结束过渡）
- **Chapter1PostMiniGameController**：`Dialog1` / `Dialog2` 子物体上的 **Text** 为两段对白（直接改 `Text` 内容即可，非富文本）；可选 `dialogVoice1` / `dialogVoice2`；视频为 `videoClip` 或 `StreamingAssets/chapter1_outro.mp4`

---

## 可调节参数

### 游戏结尾
- **GameEndingController**：黑场渐显 **首图+字幕**（可配音）→ **点击任意处** → **视频1**（上屏）+ 下方 **两段**字幕（可配音，与视频并行至视频结束）→ 黑场渐隐渐显 → **视频2** + **一段**字幕 → **中间插图**（点击继续）→ **最后插图** + 可选字幕（可配音，点击回 **GameMenu**）。结构更新后请菜单 **Tools → 创建第三章场景 → 游戏结尾视频** 覆盖生成 `GameEnding.scene`

### 游戏菜单
- **GameMenuController**：nextSceneName 为点击「游戏开始」后加载的场景，默认 OpeningVideo
- **Background**：可拖入背景图替换默认深色

### 开场视频/情景
- **OpeningVideoController**：
  - `openingSteps`：配置步骤序列，支持 `Image / FrameAnimation / Video / BlackTransition`（旧序列里若仍有 `TravelEffect` 会跳过且无画面，请删掉该步）
  - 图片步可勾 `waitForClick` 实现“点一下进入下一帧”
  - 每步支持字幕 `text`、配音 `voice`、`minDuration`、`extraDelay`
  - `FrameAnimation`：填 `animationFrames` + `frameInterval`
  - `Video`：填 `video`，可同时配字幕/独立配音，且可按步骤设置 `videoFadeInDuration`
  - **全部步骤结束后**：黑场（`fadeOutDuration`）→ **衔接视频**（`bridgeTransitionClip` 或 StreamingAssets 下 `bridgeTransitionStreamingName`，如 `intro_bridge.mp4`）从黑渐显（`bridgeVideoFadeInDuration`）→ 播完再黑场（`bridgeToChapterFadeDuration`）→ `nextSceneName`（默认 `Chapter1Intro`）。未配置衔接视频时，仅最后黑场后进第一章。
  - 分镜之间用 `slideFadeOutDuration / slideFadeInDuration` 做渐隐渐显

### 第一章前置（Chapter1Intro）
- **Chapter1IntroDialogController**：
  - `backgroundImage` + `backgroundSprite`：底层全屏背景图
  - `dialogImage` + `firstDialogSprite` / `secondDialogSprite`：叠在背景上的两张对话框图（图里可含字）
  - `dialogAppearDelay`：背景出现后延迟再渐显第一张对话框（默认 0.5）
  - `dialogFadeDuration`：对话框渐显/渐隐时长
  - `continueButton`：第一张对话框渐显后出现；第一次点击后渐隐再渐显第二张；第二次点击后全屏黑场并进入 `nextSceneName`（默认 `Outdoor`，户外场景会再黑屏渐显）
  - `firstDialogVoice` / `secondDialogVoice`：可选配音

### 户外场景
- **QiaoQiaoPlayerController**：idleSprite（站立图）、walkFrames（行走帧）、moveSpeed, minX, maxX, frameInterval
- **户外场景**：QiaoQiaoPlayerController.moveHint 为「按AD进行移动」提示，移动后自动隐藏
- **OutdoorSceneController**：doorZone 范围、nextSceneName；进门前弹层见 `preEnter*`：第一张用 `preEnterDialogImage` + `preEnterDialogSprite`，第二张用**独立** `preEnterSecondDialogImage` + `preEnterSecondDialogSprite`（不配第二张 Sprite 则第一次继续后直接进下一场景），`preEnterDialogFadeDuration` 控制渐显/渐隐

### 测量仪
- **指针图像**：层级 `Canvas` → `MeasurementBar` → `Bar` → **Needle**，选中 Needle，在 **Image** 组件的 **Source Image** 里拖入你的指针图
- **测量仪背景**：选中 `Bar`，在 **Image** 的 **Source Image** 里拖入背景图
- **MeasurementBarController**：
  - `confirmKey`：确认键，默认 Enter（若用空格导致游戏退出，多为编辑器快捷键冲突，可改回 Space 或换其他键）
  - `minValue` / `maxValue`：指针移动范围（0~1，底部到顶部）
  - `zeroValue`：零度位置，默认 0.5（中间）
  - `zeroTolerance`：零度判定容差

### 榫卯
- **DropZone**：勾选 `useCustomRect`，调节 `anchorMin/Max`、`offsetMin/Max` 可自由调节微微铆合图的大小和位置；**Intro Sprite** 为完全铆合后展示的榫卯介绍图

### 场景切换渐变
- 各场景切换已加渐隐渐显：离开时渐隐黑屏，进入时黑屏渐显
- 可调 `fadeInDuration`、`fadeOutDuration` 控制时长
- 榫卯游戏需挂 **SceneFadeIn** 组件（新建场景会自动添加）

### 若出现「Coroutine couldn't be started because the game object is inactive」
- 说明 TenonMortiseIntroDisplay 挂在未激活的 Panel 上。解决：新建空物体 `TenonMortiseIntroController`（放 Canvas 下），挂 TenonMortiseIntroDisplay，把 panelRoot、introImage、fadeOverlay、continueButton 指向 TenonMortiseIntroPanel 内对应子物体，然后从 Panel 上移除该组件，MatchToMeasurementBridge 的 introDisplay 改为指向新物体
