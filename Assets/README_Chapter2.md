# 第二章 - 晋祠水镜台

## 流程

1. **Chapter2Intro** → 背景图、虚线箭头闪烁、「向前进入晋祠」、走到门口按 E 进入水镜台
2. **Chapter2Shuijing** → 水镜台背景、3 个对话框依次显示，点继续切换
3. **Chapter2Scroll** → 卷轴飞来视频 → 脸上有卷轴 +「点击以解开卷轴」→ 点击播放解卷轴动画 → 图纸四 → 点继续
4. **Chapter2Platformer** → 2D 平台游戏：AD 移动、收集 3 木构件、避开尖刺、到出口按 S 通关
5. **Chapter2VideoEnd** → 水镜台灰变彩视频，播放结束后停在最后一帧，右下角「继续」按钮直接进入第三章

## 创建场景

菜单：**Tools → 创建第二章场景** → 选择对应子项或「全部」

**第一章→第二章过渡**：第一章完成后直接进入 **Chapter2Intro**。

## 素材设置

| 场景 | 需插入素材 |
|------|------------|
| 开场 | 背景图、虚线箭头图 |
| 水镜台 | 背景图、3 个对话框原图像（可单独设置）、3 段对话文字 |
| 卷轴 | 卷轴飞来视频（最后一帧即脸上卷轴）、解卷轴动画视频、图纸四图 |
| 平台游戏 | 背景图（含平台）、乔乔行走帧、3 个木构件（带发光特效）、尖刺、出口 |
| 通关视频 | 灰变彩视频（或 StreamingAssets/chapter2_end.mp4）|

## 平台游戏说明

### 最简单上手（推荐）

1. 打开 Chapter2Platformer 场景
2. 菜单 **Tools → 创建第二章场景 → 添加简单平台（一整块）**
3. 把人物拖到平台上方，运行即可

### 其他说明

- **出生点**：人物在场景中的位置就是出生点，拖到哪里就在哪里重生
- **操作**：AD 左右移动，W/空格/上方向键 跳跃
- **碰撞**：平台顶部可站立，头顶碰到平台会下落，侧面碰到需跳上去
- **构件**：碰到木构件自动收集，收集满 3 个后才能通关
- **尖刺**：碰到尖刺区域游戏失败，点重新开始
- **出口**：收集满后到出口区域，按 S 通关
- **平台**：Player 和平台都需在 GameRoot 下。Platforms 为空时会自动查找 Floor、Platform1、Platform2 等
- **站稳容差**：Inspector 中 Grounded Tolerance 调大（如 15）更容易站稳，调小更严格
- **自动踏上**：Step Up Height 设为 60–80 时，碰到平台边缘会自动踏上（高度差≤该值），设为 0 禁用
- **人物形象**：Idle Sprite（站立图）、Walk Frames（行走帧），走时播放行走动画，静止时显示站立图

## 把背景地面做成平台

**快捷方式**：菜单 **Tools → 创建第二章场景 → 添加平台（覆盖背景地面）**，自动在 GameRoot 下创建平台。

**手动步骤**：
1. 在 **GameRoot** 下新建空物体，添加 **Rect Transform** 和 **Image**
2. 拖到地面位置，**RectTransform 的顶部边缘**要对齐地面表面（角色站在这里）
3. 调整宽高覆盖地面，可设 Image 的 Alpha=0 做成隐形平台
4. 选中 **Canvas** → Chapter2PlatformerController → 在 **Platforms** 数组中拖入这些 RectTransform

## 平台拼接（契合背景）

### 方法一：用菜单生成重叠链

1. 菜单 **Tools → 创建第二章场景 → 拼接平台链（3段重叠）**
2. 自动生成 3 段平台，每段**重叠 40 像素**，已加入 Platforms
3. 在 Scene 里拖拽各段，让顶部对齐背景地面
4. 需要更多段：复制 `Platform_链1`，改名为 `Platform_链4`，拖到下一段位置，**确保与上一段重叠 30–50px**，再拖入 Platforms 数组

### 方法二：手动拼接

| 步骤 | 说明 |
|------|------|
| 1 | GameRoot 下新建空物体，加 RectTransform + Image |
| 2 | 第一段：拖到起点，顶部对齐地面，设 Alpha=0 |
| 3 | 第二段：复制第一段，往右拖，**与第一段重叠 30–50px**（不要留缝） |
| 4 | 斜坡 | 单独建一块，旋转 RectTransform，交界处与相邻段重叠 |
| 5 | 全部拖入 Chapter2PlatformerController → Platforms |

### 要点

- **重叠**：相邻段必须重叠 30–50 像素，否则交界处会掉
- **顶部对齐**：每段 RectTransform 的**顶边**要对齐可站立的地面
- **隐形**：Image Alpha=0，不挡背景

## 用平台原图做平台

你有平台原图（如 zigzag 形状）时，可以这样用：

### 步骤

1. **导入图片**：把平台图拖进 `Assets`，在 Inspector 里设为 **Sprite (2D and UI)**，Texture Type 选 Sprite。

2. **放原图当显示**：在 GameRoot 下新建空物体，加 **Rect Transform** + **Image**，把平台 Sprite 拖进 Image 的 Source Image。拖到合适位置，作为**纯显示**（不参与碰撞）。

3. **做碰撞平台**：在原图上方叠一层**隐形平台**，用多段 RectTransform 贴合原图的可站立表面：
   - 菜单 **Tools → 创建第二章场景 → 添加平台（覆盖背景地面）** 或 **拼接平台链**
   - 把生成的平台拖到原图上方，让每段的**顶边**对齐原图的可站立边缘
   - 每段 Image 的 **Color → Alpha 设为 0**（隐形）
   - 斜坡段：在 Inspector 里调 **Rect Transform → Rotation** 的 Z，让顶边和斜坡一致

4. **加入 Platforms**：把所有这些隐形平台拖进 Canvas → Chapter2PlatformerController → **Platforms** 数组。

### 简化做法（一整块）

若原图大致水平，可只做一块长条平台：
- 菜单 **Tools → 创建第二章场景 → 添加简单平台（一整块）**
- 把平台拖到原图上方，顶边对齐
- 平台 Image Alpha=0，原图单独作为背景显示

### 原图直接当碰撞（推荐）

平台 Image 有 Sprite 时，会按**图片透明度**自动生成碰撞，无需再叠隐形平台。

1. 导入平台图，设为 Sprite，勾选 **Read/Write Enabled**
2. 菜单 **添加平台（显示原图）**（先在 Project 选中该 Sprite 再执行）
3. 把平台拖入 **Platforms** 数组

**若碰撞仍是矩形**（如 Tuanjie 等引擎不支持运行时读纹理）→ 用**烘焙高度图**：

1. 选中平台物体 → **Add Component** → 添加 **Platform Height Map**
2. 在 Inspector 中点击 **「烘焙高度图（从 Sprite）」**
3. **务必**把平台拖入 Canvas → Chapter2PlatformerController → **Platforms** 数组（否则不会参与碰撞）
4. 运行游戏，碰撞会按烘焙的轮廓生效，**物体是啥样空气墙就啥样**

烘焙会生成：顶面、底面（头顶碰撞）、**左右边缘**（消除侧面空气墙）。若仍有空气墙，请**重新烘焙**（旧场景需再点一次「烘焙高度图」以生成左右边缘数据）。

### 踏不上去 / 空气墙不阻挡

- **平台顶边对齐**：平台 RectTransform 的**顶边**必须与桥面/地面可站立表面对齐，否则角色会穿过去或踏不上
- **加入 Platforms**：所有作为平台/空气墙的 RectTransform 必须拖入 **Platforms** 数组，否则不会参与碰撞
- **Step Up Height**：若踏不上边缘，在 Inspector 中把 Step Up Height 调大（如 80）
