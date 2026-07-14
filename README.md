# bpf

> 一个从零开始、**NativeAOT 友好**的 .NET UI 框架。

`bpf` 是一个学习/实验性质的 .NET 桌面 UI 框架,目标是在 **不依赖运行时反射** 的前提下,实现一套完整的控件树、属性系统、布局、渲染与输入管线,并能以**单文件原生 AOT** 的方式发布运行。

设计灵感来自 Avalonia / WPF 的 XAML 体系(StyledProperty、逻辑树、路由事件、布局 pass),但刻意避开了 Avalonia 的 `AvaloniaPropertyRegistry` 反射注册机制 —— 每一个属性/路由事件都是独立的泛型静态字段,让 NativeAOT 为每个具体 owner 生成独立代码并保留,从而做到零反射。

## 设计目标

- **AOT 友好**:全程零反射,可用 `dotnet publish -r win-x64 /p:PublishAot=true` 编译成单 exe。
- **平台可移植**:核心库 `Bpf` 面向 `netstandard2.1`,平台后端以插件形式接入(目前提供 Windows/D2D1 实现)。
- **类 WPF/Avalonia 架构**:属性系统(StyledProperty)、逻辑/视觉树、布局系统、路由事件,熟悉这套体系的人能直接上手。

## 架构一览

```
samples/Bpf.Samples.HelloWorld     示例:Hello World 窗口 + 可点击按钮
│
├── src/Bpf                        核心库(平台无关)
│   ├── Application/               应用入口、生命周期、平台后端注入
│   ├── Controls/                  控件树:Visual / Layoutable / Control /
│   │   ├── Routing/               路由事件系统(RoutedEvent / EventRoute)
│   │   ├── Window / Button / TextBlock / StackPanel / ...
│   ├── Input/                     键盘输入:Key / KeyEventArgs / TextEventArgs / ...
│   ├── Layout/                    LayoutManager(Measure / Arrange 两遍布局)
│   ├── Media/                     Color / Brush / SolidColorBrush
│   ├── Platform/                  平台抽象接口(IPlatformBackend / IPlatformWindow /
│   │                              IPlatformRenderInterface / IRenderTarget / ...)
│   ├── PropertySystem/            StyledProperty 属性系统(AOT 友好,无反射注册)
│   ├── Threading/                 Dispatcher 主循环
│   └── Utilities/                 PropertyValueStore
│
└── src/Bpf.Windows                Windows 平台后端
    ├── Interop/                   P/Invoke:D2D1 / D3D11 / DWrite / User32 / Com
    ├── Platform/                  Win32 窗口、渲染目标、绘制上下文
    ├── Render/                    D2D1 / DWrite 工厂封装
    └── Threading/                 Win32 消息循环 Dispatcher
```

## 用法

```csharp
using Bpf;
using Bpf.Controls;
using Bpf.Media;

// 1. 初始化 Windows 后端
var app = Bpf.Windows.WindowsAppExtensions.UseWindows();

// 2. 创建窗口
var window = app.CreateWindow(480, 320);
window.Title = "bpf Hello World";

// 3. 放几个控件
var title = new TextBlock { Text = "你好,bpf!", FontSize = 24 };
window.AddChild(title);

var button = new Button { Content = "点我" };
button.Click += (s, e) => title.Text = "被点了!";
window.AddChild(button);

// 4. 跑起来
app.Run();
```

完整示例见 [`samples/Bpf.Samples.HelloWorld`](samples/Bpf.Samples.HelloWorld/Program.cs)。

## 里程碑路线图

项目按里程碑迭代。打勾 = 已完成,空框 = 未完成。

### [x] M1 — 基础骨架

- [x] **平台抽象层**:`IPlatformBackend` / `IPlatformWindow` / `IPlatformRenderInterface` / `IRenderTarget` / `IDrawingContext`,核心库不耦合具体平台。
- [x] **Windows 后端**:基于 Win32 + Direct2D / DirectWrite,通过 P/Invoke 调原生 API,窗口创建、消息循环、硬件加速绘制跑通。
- [x] **属性系统**:AOT 友好的 `StyledProperty<TValue>`,`GetValue` / `SetValue` 读写,属性变更自动触发 Measure / Arrange / Render 失效,无反射注册。
- [x] **控件树**:`Visual → Layoutable → Control` 继承体系,逻辑树挂载(`AttachToHost` 递归注入窗口引用)。
- [x] **布局系统**:`LayoutManager` 两遍布局(Measure / Arrange)。
- [x] **基础控件**:`Window`、`TextBlock`、`Button`(按下态 + 点击)、`StackPanel`(横向 / 纵向)。
- [x] **渲染管线**:Dispatcher 每帧回调 → 布局 → `BeginDraw` → 绘制 → `Present`。
- [x] **鼠标输入**:窗口层命中测试 + 事件派发。
- [x] **NativeAOT 发布**:可 `publish /p:PublishAot=true` 编译成单 exe。

### [x] M2 — 键盘 / 焦点 / 路由事件

- [x] **路由事件系统**:`RoutedEvent<TArgs>` 泛型静态注册(与 `StyledProperty` 同构,零反射),支持 `Bubble` / `Tunnel` / `Direct` 策略;`Control` 上提供 `AddHandler` / `RemoveHandler` / `RaiseEvent`。
- [x] **全局事件 Id**:计数器放在非泛型基类,避免不同 `TArgs` 的事件 Id 撞车导致 `InvalidCastException`。
- [x] **键盘输入**:`Key` / `KeyModifiers` / `KeyEventArgs` / `TextEventArgs`;Windows 后端映射 `WM_KEYDOWN` / `WM_KEYUP` / `WM_CHAR`。
- [x] **焦点管理**:`IsFocusable` / `IsFocused` / `Focus()`,全局单焦点;Tab / Shift+Tab 在可聚焦控件间切换。
- [x] **键盘触发 Click**:焦点 `Button` 按 Enter / Space 触发 `Click`。
- [x] **递归命中测试**:修掉 M1 只遍历一层 children 的 bug,冒泡路由让祖先控件也能收到事件。

### [ ] M3 — 交互控件 / 样式

- [ ] **文本输入**:`TextBox`(光标、选区、IME)。
- [ ] **选择控件**:`CheckBox` / `RadioButton` / `ToggleButton`。
- [ ] **样式系统**:Style / Setter,属性可批量应用到控件树。
- [ ] **焦点导航增强**:`TabIndex`、方向键(Arrow)导航、焦点可视化框(FocusVisual)。
- [ ] **真正的 Tunnel 路由**:实现 Preview 前缀事件(目前留接口未实装)。

### [ ] M4 — 布局面板扩展

- [ ] **Grid**:行列定义、`*` / `Auto` 尺寸、跨列跨行。
- [ ] **Canvas**:绝对定位。
- [ ] **DockPanel** / **WrapPanel**。
- [ ] 布局性能:增量布局、布局裁剪。

### [ ] M5 — 跨平台后端

- [ ] **Linux**:尝试基于 libwayland + Skia 或 GTK 的后端。
- [ ] **macOS**:基于 Metal / CoreGraphics 的后端。
- [ ] 平台抽象层打磨:`IPlatformRenderInterface` 接口稳定化。

### [ ] M6 — 生态(远期)

- [ ] XAML / 标记语言描述 UI(需在 AOT 下用源生成器而非反射)。
- [ ] 数据绑定(`{Binding}`)。
- [ ] 动画系统。
- [ ] 控件主题 / 默认样式库。

## 构建

```bash
# 构建
dotnet build

# 运行示例
dotnet run --project samples/Bpf.Samples.HelloWorld

# AOT 发布(Windows)
dotnet publish samples/Bpf.Samples.HelloWorld \
  -r win-x64 -c Release /p:PublishAot=true
```

## 环境要求

- .NET 10 SDK(示例与 Windows 后端),核心库面向 `netstandard2.1`
- Windows 10+ 目前已实现后端

## 许可证

[MIT](LICENSE.txt)
