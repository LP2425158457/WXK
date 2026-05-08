# 金蝶BOS OA同步功能开发规范

> 适用于 WXK 项目的金蝶BOS二次开发规范

## 一、项目基本信息

- **项目路径**: `d:\Code\WXK`
- **插件工程**: `LP.WXK.K3.App.ServicePlugIn`
- **开发商代码**: `WXK` (字段格式: `F_TWLG_xxx`)
- **推送仓库**: `https://github.com/LP2425158457/WXK.git`

## 二、代码提交规范

1. 提交前必须先拉取最新代码: `git pull --rebase`
2. 生成规范的提交记录 (含函数级注释说明)
3. 提交变动并推送: `git add` -> `git commit` -> `git push`

## 三、插件开发规范

### 3.1 插件类型选择

| 场景 | 继承基类 | 命名空间 |
|------|---------|---------|
| 操作服务插件 (下推、保存、同步等) | `AbstractOperationServicePlugIn` | `Kingdee.BOS.Core.DynamicForm.PlugIn` |
| 定时任务/执行计划 | `IScheduleService` | `Kingdee.BOS.Contracts` |
| 表单插件 (单据/列表界面交互) | `AbstractBillPlugIn` | `Kingdee.BOS.Core.Bill.PlugIn` |

**注意**: 列表场景使用 `AbstractBillPlugIn`，不是 `AbstractListPlugIn`

### 3.2 表单插件 AfterDoOperation 事件正确写法

```csharp
public override void AfterDoOperation(AfterDoOperationEventArgs e)
{
    base.AfterDoOperation(e);

    if (e.Operation.Name.Equals("操作名称", System.StringComparison.OrdinalIgnoreCase))
    {
        if (e.OperationResult.IsSuccess)
        {
            this.View.ShowMessage("提示消息");
            this.View.Refresh();
        }
        else
        {
            this.View.ShowErrMessage(e.OperationResult.GetString());
        }
    }
}
```

**常见错误**:
- ~~`e.OperationName.Equals(...)`~~ → 正确: `e.Operation.Name.Equals(...)`
- ~~`e.Result.IsSuccess`~~ → 正确: `e.OperationResult.IsSuccess`
- ~~`this.View.ShowError(e.Result.Msg)`~~ → 正确: `this.View.ShowErrMessage(e.OperationResult.GetString())`

### 3.3 日志记录正确写法

```csharp
Kingdee.BOS.Log.Logger.Info("OASync", logMessage);
```

**注意**: `Logger.Info` 只需两个参数 (分类, 消息)，不需要传 `this.Context`

## 四、OA同步相关字段定义

### 4.1 字段对照表

| 单据类型 | 表名 | OA流程ID字段 | 同步状态字段 | 银行状态字段 |
|---------|------|-------------|-------------|-------------|
| 付款单 | T_AP_PAYBILL | F_TWLG_OAPROCESSID | F_TWLG_OAStatus | FBANKSTATUS |
| 收款退款单 | T_AR_REFUNDBILL | F_TWLG_OAPROCESSID | F_TWLG_OAStatus | FBankStatus |
| 银行交易明细 | T_CN_BANKCASHFLOW | - | F_TWLG_OASyncStatus | - |

### 4.2 同步状态值

| 值 | 含义 |
|----|------|
| 0 | 未同步 |
| 1 | 已同步 |
| 2 | 同步失败 |
| 3 | 已排除 |

### 4.3 银行处理状态

| 值 | 含义 |
|----|------|
| F | 已付款确认 |

## 五、核心文件清单

| 文件名 | 用途 |
|--------|------|
| `OASyncService.cs` | OA同步核心服务 (HTTP请求、Token认证) |
| `OASyncOperationServicePlugIn.cs` | 付款单/收款退款单OA同步操作服务插件 |
| `OASyncOperationSchedule.cs` | OA同步定时任务 (付款单/收款退款单) |
| `PayBillLCBMSyncSchedule.cs` | 付款单OA流程ID同步定时任务 |
| `OASyncRecDetailOperationServicePlugIn.cs` | 银行交易明细OA同步操作服务插件 |
| `OASyncRecDetailSchedule.cs` | 银行交易明细OA同步定时任务 |
| `RecDetailService.cs` | 回款明细同步服务 |
| `RecClaimToReceiveBillOperationPlugIn.cs` | 收款认领单保存后自动提交审核、同批合并下推收款单 |
| `OAListPlugIn.cs` | 表单插件：OA同步操作完成提示+刷新列表 |

## 六、业务逻辑要点

### 6.1 收款退款单同步OA前置条件
必须先执行**已付款确认**动作后才能同步OA，否则抛出异常

### 6.2 收款认领单自动合并下推
- 保存后自动提交审核认领单
- 按 BatchKey 分组：流水总金额 + 付款单位 + 收款组织 + 银行账号 + 结算方式
- 同批次认款齐套 (金额一致) 时自动合并下推收款单
- 下推后自动保存 + 提交 + 审核

### 6.3 银行交易明细推送过滤
- 只推送贷方金额 > 0 的交易明细
- 已关联收款认领单的交易明细不重复推送
- 流水号唯一性校验

## 七、命名规范

1. **字段标识**: 必须以开发商代码加下划线开头 (`F_TWLG_xxx`)
2. **工程命名**: `{开发商标识}.{项目}.{工程归类}.XxxxxBusinessPlugIn`
3. **注释**: 添加函数级注释，说明方法用途和参数

## 八、安全规范

1. 禁止硬编码密钥、密码、域名等敏感信息
2. 敏感信息应加密存储到数据库
3. 禁止 Unsafe 代码
4. 禁止使用进程、Socket侦听等危险代码
