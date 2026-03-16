# WXK - 金蝶 K3 Cloud BOS 插件项目

## 项目简介

本项目是金蝶 K3 Cloud BOS 平台的插件开发解决方案，包含多个业务模块的服务插件和同步调度任务。

## 项目结构

```
WXK/
├── LP.WXK.K3.App.OASyncSchedule/          # OA 同步调度任务
├── LP.WXK.K3.App.RecDetailSyncSchedule/   # 收款明细同步调度
├── LP.WXK.K3.App.ServicePlugIn/           # OA 同步服务插件
├── LP.WXK.TEST/                           # 测试项目
├── RecDetailSyncSchedule/                 # VB 版收款明细同步
├── ZK.WXK.App.ServicePlugIn/              # 付款申请单服务插件
└── WXK.sln                                # 解决方案文件
```

## 模块说明

### 1. LP.WXK.K3.App.OASyncSchedule
OA 系统同步调度任务模块，用于定时执行 OA 数据同步操作。

### 2. LP.WXK.K3.App.RecDetailSyncSchedule
收款明细同步调度模块，负责收款明细数据的同步处理。

### 3. LP.WXK.K3.App.ServicePlugIn
OA 同步服务插件核心模块，包含：
- OA 同步操作服务插件
- 收款明细操作服务插件
- RSA 加密转换工具

### 4. LP.WXK.TEST
测试项目模块，包含 HelloWorld 单据插件示例。

### 5. RecDetailSyncSchedule
VB 语言编写的收款明细同步调度模块。

### 6. ZK.WXK.App.ServicePlugIn
付款申请单相关服务插件，包含：
- 付款申请单服务插件
- 采购订单提交服务插件

## 技术栈

- **平台**: 金蝶 K3 Cloud BOS
- **语言**: C# / VB.NET
- **框架**: .NET Framework 4.5 / 4.8
- **依赖组件**: 
  - Kingdee.BOS.* 系列组件
  - NPOI (Excel 操作)
  - Newtonsoft.Json (JSON 处理)
  - log4net (日志记录)

## 开发环境

- Visual Studio 2019/2022
- .NET Framework 4.5 或更高版本
- 金蝶 K3 Cloud 开发环境

## 构建说明

1. 使用 Visual Studio 打开 `WXK.sln` 解决方案文件
2. 还原 NuGet 包依赖
3. 选择 Debug 或 Release 配置进行构建

## 部署说明

将编译生成的 DLL 文件部署到金蝶 K3 Cloud 服务器的相应插件目录中。

## 作者

- **作者**: Paul_li
- **邮箱**: 2425158457@qq.com

## 许可证

本项目为私有项目，未经授权不得使用或分发。
