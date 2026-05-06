# TunnelPlatform.QtClient

C++ / Qt 6 桌面浏览端，用于连接现有 `TunnelPlatform.Api`，浏览工程实例、站点区间、概览统计、灰度图和病害记录。

## 构建

### 用 Visual Studio 打开

推荐方式一：在 Visual Studio 里选择：

```text
File -> Open -> Folder
```

然后打开：

```text
D:\展示平台数据\TunnelPlatform\TunnelPlatform.QtClient
```

项目已经配置了 `CMakePresets.json`，默认使用：

```text
C:\Qt\Qt6.10\6.8.3\msvc2022_64
```

推荐方式二：直接运行脚本生成并打开 VS 工程：

```powershell
.\TunnelPlatform.QtClient\OpenInVisualStudio.ps1
```

脚本会在 `TunnelPlatform.QtClient\build` 下生成 Visual Studio 工程文件。当前本机生成的是：

```text
TunnelPlatform.QtClient\build\TunnelPlatformQtClient.slnx
```

### 命令行构建

```powershell
cmake -S .\TunnelPlatform.QtClient -B .\TunnelPlatform.QtClient\build -DCMAKE_PREFIX_PATH="C:\Qt\Qt6.10\6.8.3\msvc2022_64"
cmake --build .\TunnelPlatform.QtClient\build --config Release
```

如果本机 Qt 安装目录不同，把 `CMAKE_PREFIX_PATH` 改成实际 Qt 目录。

运行：

```powershell
.\TunnelPlatform.QtClient\build\Release\TunnelPlatformQtClient.exe
```

如果要拷贝到没有 Qt 环境的电脑运行，先打包 Qt 依赖：

```powershell
& "C:\Qt\Qt6.10\6.8.3\msvc2022_64\bin\windeployqt.exe" .\TunnelPlatform.QtClient\build\Release\TunnelPlatformQtClient.exe
```

## 运行前提

先启动 API：

```powershell
dotnet run --project .\TunnelPlatform.Api\TunnelPlatform.Api.csproj
```

默认 API 地址：

```text
http://localhost:5140
```

服务器部署后也可以改成：

```text
http://43.106.8.3
```

## 功能范围

- 工程实例列表
- 当前工程概览
- 站点/区间列表
- 灰度图浏览
- 病害记录表
- 病害类型统计

## 代码结构

```text
src/ApiClient.h
src/ApiClient.cpp
```

封装 Web/API 访问，包括 API 地址管理、URL 拼接、JSON 请求、图片字节下载和业务接口方法。

```text
src/MainWindow.h
src/MainWindow.cpp
```

只负责桌面 UI、用户事件、表格填充、图片显示和状态提示，不直接处理 `QNetworkReply`。

## 常见问题

### VS 编译时包含到 Qt5.8 头文件

如果看到类似错误：

```text
C:\Qt\Qt5.8.0\5.8\msvc2015_64\include\QtCore\qlist.h
错误 C2065 “stdext”: 未声明的标识符
```

说明 VS 当前构建继承了旧的 Qt5.8 include 路径。项目要求使用：

```text
C:\Qt\Qt6.10\6.8.3\msvc2022_64
```

处理方式：

```powershell
cmake --preset qt-6.8.3-msvc
cmake --build --preset release
```

然后在 VS 里重新加载 CMake 项目，或直接打开：

```text
TunnelPlatform.QtClient\build\TunnelPlatformQtClient.slnx
```

如果 VS 仍然使用 Qt5.8，检查 VS 的 Qt VS Tools 配置，删除或禁用旧的 `C:\Qt\Qt5.8.0\5.8\msvc2015_64` Kit。
