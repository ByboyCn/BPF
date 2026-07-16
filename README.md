# bpf

> 一个从零开始、**NativeAOT 友好**的 .NET UI 框架。

`bpf` 是一个学习/实验性质的 .NET 桌面 UI 框架,目标是在 **不依赖运行时反射** 的前提下,实现一套完整的控件树、属性系统、布局、渲染与输入管线,并能以**单文件原生 AOT** 的方式发布运行。

设计灵感来自 Avalonia / WPF 的 XAML 体系(StyledProperty、逻辑树、路由事件、布局 pass),但刻意避开了 Avalonia 的 `AvaloniaPropertyRegistry` 反射注册机制 —— 每一个属性/路由事件都是独立的泛型静态字段,让 NativeAOT 为每个具体 owner 生成独立代码并保留,从而做到零反射。

## 设计目标

- **AOT 友好**:全程零反射,可用 `dotnet publish -r win-x64 /p:PublishAot=true` 编译成单 exe。UI 标记(.bpfaml)由源生成器在构建期编译,运行时零解析。
- **平台可移植**:核心库 `Bpf` 面向 `netstandard2.1`,平台后端以插件形式接入。M5 起渲染统一走 SkiaSharp,Windows 后端已就绪,Linux/macOS 待实现(核心库零改动)。
- **类 WPF/Avalonia 架构**:属性系统(StyledProperty)、逻辑/视觉树、布局系统、路由事件、数据绑定、Style,.bpfaml 标记语言贴近 Avalonia .axaml。

## 架构一览

```
samples/Bpf.Samples.HelloWorld     示例:Hello World + .bpfaml 声明式 UI
│
├── src/Bpf                        核心库(平台无关,netstandard2.1)
│   ├── Application/               应用入口、生命周期、平台后端注入
│   ├── Controls/                  控件树:Visual / Layoutable / Control /
│   │   ├── Routing/               路由事件系统(RoutedEvent / EventRoute)
│   │   ├── Window / Button / TextBlock / Grid / StackPanel / ScrollViewer / ...
│   ├── Data/                      数据绑定:Binding / CompiledBinding(M6 编译式)/ INPC 助手
│   ├── Input/                     键盘输入:Key / KeyEventArgs / TextEventArgs / ...
│   ├── Layout/                    LayoutManager(Measure / Arrange 两遍布局)+ GridLength
│   ├── Media/                     Color / Brush / SolidColorBrush / Stretch
│   ├── Platform/                  平台抽象接口(IPlatformBackend / IPlatformWindow /
│   │                              IPlatformRenderInterface / IRenderTarget / ...)
│   ├── PropertySystem/            StyledProperty / AttachedProperty(AOT 友好,无反射注册)
│   ├── Styling/                   Style / Setter(按类型匹配,祖先链查找)
│   ├── Threading/                 Dispatcher 主循环
│   └── Utilities/                 PropertyValueStore
│
├── src/Bpf.Windows                Windows 平台后端(netstandard2.1)
│   ├── Interop/                   P/Invoke:User32 / Gdi32
│   ├── Platform/                  Skia* 适配器:SkiaDrawingContext / SkiaRenderTarget /
│   │                              SkiaTextFormat(CJK 回退)/ SkiaBitmap / SvgRasterizer
│   └── Threading/                 Win32 消息循环 Dispatcher
│
└── src/Bpf.Markup.Generator       .bpfaml 源生成器(netstandard2.0,Roslyn IIncrementalGenerator)
    ├── Parser/                    XmlReader 流式解析 .bpfaml → BpfamlDocument
    ├── Emitter/                   BpfamlDocument → C# 源(partial class + Build())
    └── Model/                     类型表 / 属性类型表 / 事件表 / 资源表
```

**渲染管线**(M5 起):控件 → IDrawingContext → SkiaDrawingContext → SKCanvas → libSkiaSharp.dll。
全部 16 个控件、文字(CJK 回退)、图片(PNG/JPEG)、矢量图(自研 SVG 光栅化器)统一走 SkiaSharp,核心库零改动即可移植到 Linux/macOS。

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

## .bpfaml 标记语言(M6)

除了用 C# 代码构建 UI,bpf 还支持 **.bpfaml** —— 一种 XAML 风格的声明式标记语言,由 **Roslyn 增量源生成器**在**构建期**编译成 C# 代码。零运行时反射、NativeAOT 完全兼容。

### MainForm.bpfaml(声明式 UI)

```xml
<StackPanel x:Class="App.MainForm"
            xmlns="bpf" xmlns:x="bpf:x"
            x:DataType="App.MainViewModel">

  <StackPanel.Resources>
    <SolidColorBrush x:Key="AccentBrush" Color="#2D7FF9"/>
  </StackPanel.Resources>

  <StackPanel.Styles>
    <Style TargetType="TextBlock">
      <Setter Property="FontSize" Value="13"/>
    </Style>
  </StackPanel.Styles>

  <Label Text="标题" FontSize="16" Foreground="{StaticResource AccentBrush}"/>
  <TextBox Text="{Binding Name, Mode=TwoWay}"/>
  <Button Content="确定" Click="OnConfirmClick"/>
</StackPanel>
```

### MainForm.cs(code-behind)

```csharp
public partial class MainForm
{
    // Click="OnConfirmClick" 对应的事件处理(静态,签名 (object, RoutedEventArgs))
    private static void OnConfirmClick(object sender, RoutedEventArgs e) { /* ... */ }
}
```

### Program.cs(入口)

```csharp
var app = Bpf.Windows.WindowsAppExtensions.UseWindows();
var window = app.CreateWindow(480, 360);

var form = MainForm.Build();        // 源生成器生成,返回控件树根
form.DataContext = new MainViewModel(); // 数据绑定的源
window.SetContent(form);
app.Run();
```

### 支持的语法

| 特性 | 语法 | 说明 |
|------|------|------|
| 控件树 | `<Button/>` | 构造期 `new Button()` |
| 属性 | `FontSize="14"` `Background="Red"` | 类型转换器(double/Color/Brush/枚举) |
| 附加属性 | `Grid.Column="1"` | → `Grid.SetColumn(child, 1)` |
| x:Name | `x:Name="myBtn"` | 生成静态字段,code-behind 可访问 |
| 事件 | `Click="OnClick"` | → `btn.Click += MainForm.OnClick;` |
| {Binding} | `Text="{Binding Name}"` | 编译式 lambda,需 `x:DataType` |
| {StaticResource} | `Foreground="{StaticResource Key}"` | 编译期资源查找 |
| Style | `<Style TargetType="..."><Setter .../>` | 按类型批量应用属性 |
| {x:Static} | `Value="{x:Static Stretch.Uniform}"` | 静态成员引用 |

消费端 csproj 引用生成器即可启用:

```xml
<ProjectReference Include="..\..\src\Bpf.Markup.Generator\Bpf.Markup.Generator.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
<AdditionalFiles Include="**\*.bpfaml" />
```

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

### [x] M3 — 交互控件 / 样式

- [x] **文本输入**:`TextBox`(光标、选区、IME)。
- [x] **选择控件**:`CheckBox` / `RadioButton` / `ToggleButton`。
- [x] **样式系统**:Style / Setter,属性可批量应用到控件树。
- [ ] **焦点导航增强**:`TabIndex`、方向键(Arrow)导航、焦点可视化框(FocusVisual)。
- [ ] **真正的 Tunnel 路由**:实现 Preview 前缀事件(目前留接口未实装)。

### [x] M4 — 布局面板扩展

- [x] **Grid**:行列定义、`*` / `Auto` 尺寸、跨列跨行。
- [x] **Canvas**:绝对定位。
- [x] **DockPanel** / **WrapPanel**。
- [x] **ScrollViewer**:内容滚动 + 鼠标滚轮。
- [x] **数据绑定**:BindingExpression + INotifyPropertyChanged + ListBox/ComboBox。
- [ ] 布局性能:增量布局、布局裁剪。

### [x] M5 — 跨平台渲染后端

- [x] **SkiaSharp 渲染后端**:删除全部 D2D1/DWrite/WIC COM 互操作,改用 SkiaSharp(libSkiaSharp.dll)统一渲染。所有控件、文字、图片、矢量图(SVG 自研光栅化器)走 SKCanvas,跨平台统一。
- [x] **CJK 字体回退**:主字体 + CJK 回退字体分段绘制,解决中文方块问题。
- [ ] **Linux**:基于 libwayland + Skia 的后端(核心库已平台无关,待实现)。
- [ ] **macOS**:基于 Metal / CoreGraphics 的后端。

### [x] M6 — .bpfaml 标记语言 + 源生成器

- [x] **.bpfaml 标记语言**:XAML 风格的声明式 UI 描述,语法贴近 Avalonia .axaml。
- [x] **Roslyn 增量源生成器**:构建期把 .bpfaml 编译成 C# 代码,零运行时反射、NativeAOT 完全兼容。
- [x] **控件树 + 属性 + 附加属性**:对象实例化、属性类型转换器(Color/Brush/枚举/数字)、Grid.Row/Column 附加属性。
- [x] **事件挂接 + code-behind**:`Click="OnClick"` → `btn.Click += MainForm.OnClick;`(纯 C# 绑定)。
- [x] **编译式数据绑定**:`{Binding Path}` 由源生成器生成强类型 lambda(`vm => vm.Path`),配合 DataContext 树继承,AOT 安全。
- [x] **资源字典 + Style + markup extension**:`{StaticResource Key}` 编译期解析、`<Style>` 批量应用、`{x:Static}`/`{x:Null}`。
- [ ] **动画系统**。
- [ ] **控件主题 / 默认样式库**。

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
