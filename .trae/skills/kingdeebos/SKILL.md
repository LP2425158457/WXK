---
name: "kingdeebos"
description: "金蝶BOS开发综合规范。Invoke when user develops Kingdee BOS plugins, works with DynamicObject, bill push/pull, scheduled tasks, SQL standards, or WXK project OA sync features."
---

# 金蝶BOS 开发综合规范

> 适用于 WXK 项目的金蝶BOS二次开发综合规范

## 项目基本信息

- **项目路径**: `d:\Code\WXK`
- **插件工程**: `LP.WXK.K3.App.ServicePlugIn`
- **开发商代码**: `WXK` (字段格式: `F_TWLG_xxx`)
- **推送仓库**: `https://github.com/LP2425158457/WXK.git`

---

## 一、命名规范

### 工程命名

- 【强制】工程名称必须以下述标准进行命名，{开发商标识}.{项目}.{工程归类}（如：PPAB.K3Cloud.PrintingSystem.cscproj）
- 【推荐】如果存在细分模块，可以使用四级命名空间
- 【强制】命名空间必须与工程名、生成的程序集名称一致
- 【强制】工程命名遵守大驼峰法则，除了用以分隔的'.'外，仅允许使用26个大小写英文字母或辅助数字命名
- 【推荐】表单插件工程命名规则：{开发商标识}.{项目}.{工程归类}[.{模块名}].XxxxxBusinessPlugIn
- 【推荐】服务操作插件工程命名规则：{开发商标识}.{项目}.{工程归类}[.{模块名}].XxxxxServicePlugIn
- 【推荐】目录和文件名尽量不要使用中文

### 业务对象命名

- 【推荐】扩展或新增业务对象时(含转换规则、报表等动态领域模型)，其业务对象Key可以选择系统生成的GUID
- 【强制】如果需要自定义业务对象Key，必须以开发商代码加下划线排头（如：PAAB_xxxxx），且Key长度不允许超过30字符，只能使用字母、下划线
- 【强制】扩展的字段标识和属性必须以开发商代码加下划线排头（如：PAAB_xxxxx），字段名必须包含开发商代码（如：F_PAAB_xxxxx）
- 【强制】扩展的任何元素，例如标签控件、页签控件、面板控件等，标识必须以开发商代码加下划线排头（如：PAAB_LableXXX，PAAB_TabXXX等）

### WXK项目字段规范

1. **字段标识**: 必须以开发商代码加下划线开头 (`F_TWLG_xxx`)
2. **注释**: 添加函数级注释，说明方法用途和参数

---

## 二、插件开发规范

### 插件类型选择

| 场景 | 继承基类 | 命名空间 |
|------|---------|---------|
| 操作服务插件 (下推、保存、同步等) | `AbstractOperationServicePlugIn` | `Kingdee.BOS.Core.DynamicForm.PlugIn` |
| 定时任务/执行计划 | `IScheduleService` | `Kingdee.BOS.Contracts` |
| 表单插件 (单据/列表界面交互) | `AbstractListPlugIn` | `Kingdee.BOS.Core.List.PlugIn` |

### 表单插件 AfterDoOperation 事件正确写法

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

### 日志记录正确写法

```csharp
Kingdee.BOS.Log.Logger.Info("OASync", logMessage);
```

**注意**: `Logger.Info` 只需两个参数 (分类, 消息)，不需要传 `this.Context`

---

## 三、执行计划（定时任务）插件

### 基本要求

1. 必须继承 `Kingdee.BOS.Contracts.IScheduleService` 接口
2. 实现里面的 `void Run(Context ctx, Schedule schedule)` 方法
3. 编译成组件放到bin目录下

### 代码框架

```csharp
using System;
using System.ComponentModel;
using Kingdee.BOS.Core;
using Kingdee.BOS.Contracts;

namespace LP.WXK.K3.App.ServicePlugIn
{
    /// <summary>
    /// 执行计划：自定义执行计划
    /// </summary>
    [Description("自定义执行计划")]
    public class CustomerSchedule : IScheduleService
    {
        /// <summary>
        /// 自动计划，执行入口
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="schedule">定时任务配置</param>
        public void Run(Context ctx, Schedule schedule)
        {
            // 实现业务代码
        }
    }
}
```

### 注意事项

1. 定时任务运行在后台，无法与用户交互
2. 数据库操作必须带过滤条件
3. 批量操作要控制每次处理的数据量
4. 必须处理异常，避免整个定时任务中断
5. 日志记录要清晰，便于排查问题

---

## 四、DynamicObject 数据结构操作

### 结构说明

单据数据包DynamicObject，相当于一个**有层次结构的数据字典**：
- 第一层包含全部的单据头字段以及单据体行集合
- 单据体数据行集合、基础资料字段，则需要通过第二层的DynamicObject来展示

### 基本特征

1. 包含了全部单据头字段值
2. 包含了单据体行集合对象
3. 字段通过Key + Value，形成一个键值对，占据DynamicObject的一个节点
4. 字段在数据包中的Key，使用的是字段的属性名
5. 基础资料字段的值，也是一个DynamicObject对象，其中嵌套包含了各个引用属性的值

### 操作示例（推荐：通过元数据操作）

```csharp
// 假设billObj是单据的数据包
DynamicObject billObj = this.Model.DataObject;

// 首先获取各种元素的元数据
Field fldBillNo = this.View.BillBusinessInfo.GetField("FBillNo");
Field fldDate = this.View.BillBusinessInfo.GetField("F_JD_Date");
BaseDataField fldSupplier = this.View.BillBusinessInfo.GetField("F_JD_Supplier") as BaseDataField;
BaseDataField fldMaterial = this.View.BillBusinessInfo.GetField("F_JD_FMaterialId") as BaseDataField;
Field fldQty = this.View.BillBusinessInfo.GetField("F_JD_Qty");
Entity entity = this.ListView.BillBusinessInfo.GetEntity("FEntity");

// 读取单据内码
long billId = Convert.ToInt64(billObj[0]);

// 单据编号
string billNo = Convert.ToString(fldBillNo.DynamicProperty.GetValue(billObj));
fldBillNo.DynamicProperty.SetValue(billObj, billNo); 

// 日期
DateTime fldDateValue = Convert.ToDateTime(fldDate.DynamicProperty.GetValue(billObj));
fldDate.DynamicProperty.SetValue(billObj, fldDateValue);

// 供应商：基础资料字段
DynamicObject fldSupplierValue = fldSupplier.DynamicProperty.GetValue(billObj) as DynamicObject;

// 设置供应商基础字段值
DynamicObject[] supplierObjs = Kingdee.BOS.ServiceHelper.BusinessDataServiceHelper.LoadFromCache(
    this.Context, 
    new object[] { fldSupplierValue[0] }, 
    fldSupplier.RefFormDynamicObjectType);

fldSupplier.RefIDDynamicProperty.SetValue(billObj, supplierObjs[0][0]);
fldSupplier.DynamicProperty.SetValue(billObj, supplierObjs[0]);

// 基础资料属性值
if (fldSupplierValue != null)
{
    long supplierId = Convert.ToInt64(fldSupplierValue[0]);
    string supplierNumber = fldSupplier.GetRefPropertyValue(fldSupplierValue, "FNumber").ToString();
    string supplierName = fldSupplier.GetRefPropertyValue(fldSupplierValue, "FName").ToString();
}

// 单据体的字段
DynamicObjectCollection entityRows = entity.DynamicProperty.GetValue(billObj) as DynamicObjectCollection;
foreach (var entityRow in entityRows)
{
    // 内码
    long entityId = Convert.ToInt64(entityRow[0]);
    // 物料：基础资料字段
    DynamicObject fldMaterialValue = fldMaterial.DynamicProperty.GetValue(entityRow) as DynamicObject;
    // 数量
    decimal fldQtyValue = Convert.ToDecimal(fldQty.DynamicProperty.GetValue(entityRow));
    fldQty.DynamicProperty.SetValue(entityRow, fldQtyValue);
}

// 给单据体添加新行
DynamicObject newRow = new DynamicObject(entity.DynamicObjectType);
entityRows.Add(newRow);
```

### 注意事项

1. 推荐使用**通过元数据操作**的方式，更加安全和规范
2. 基础资料字段需要特殊处理，包含RefID和DynamicObject两个层面的设置
3. 单据体行集合是只读的，只能通过Add方法添加新行
4. 所有字段值需要使用Convert进行类型转换

---

## 五、单据下推操作

### 标准流程

#### 1. 获取转换规则

```csharp
// 获取源单与目标单直接的转换规则，如果规则未启用，则返回为空，注意容错
// 假设：上游单据FormId为sourceFormId，下游单据FormId为targetFormId
var rules = ConvertServiceHelper.GetConvertRules(this.View.Context, sourceFormId, targetFormId);
var rule = rules.FirstOrDefault(t => t.IsDefault);
```

#### 2. 获取选择行

**从列表获取选择行：**
```csharp
ListSelectedRow[] selectedRows = ((IListView)this.View).SelectedRowsInfo.ToArray();
```

**从单据获取当前行：**
```csharp
string primaryKeyValue = ((IBillView)this.View).Model.GetPKValue().ToString();
ListSelectedRow row = new ListSelectedRow(primaryKeyValue, string.Empty, 0, this.View.BillBusinessInfo.GetForm().Id);
ListSelectedRow[] selectedRows = new ListSelectedRow[] { row };
```

#### 3. 调用下推服务

```csharp
ConvertOperationResult operationResult = null;
Dictionary custParams = new Dictionary();
try
{
    PushArgs pushArgs = new PushArgs(rule, selectedRows)
    {
        TargetBillTypeId = "",     // 请设定目标单据单据类型。如无单据类型，可以空字符
        TargetOrgId = 0,           // 请设定目标单据主业务组织。如无主业务组织，可以为0
        CustomParams = custParams, // 可以传递额外附加的参数给单据转换插件，如无此需求，可以忽略
    };
    // 执行下推操作，并获取下推结果
    operationResult = ConvertServiceHelper.Push(this.View.Context, pushArgs, OperateOption.Create());
}
catch (KDExceptionValidate ex)
{
    this.View.ShowErrMessage(ex.Message, ex.ValidateString);
    return false;
}
catch (KDException ex)
{
    this.View.ShowErrMessage(ex.Message);
    return false;
}
catch
{
    throw;
}
```

#### 4. 保存目标单据

```csharp
// 获取生成的目标单据数据包
DynamicObject[] objs = (from p in operationResult.TargetDataEntities
                        select p.DataEntity).ToArray();

// 读取目标单据元数据
var targetBillMeta = MetaDataServiceHelper.Load(this.View.Context, targetFormId) as FormMetadata;

OperateOption saveOption = OperateOption.Create();
// 忽略全部需要交互性质的提示，直接保存
saveOption.SetIgnoreWarning(true);

// 提交数据库保存，并获取保存结果
var saveResult = BusinessDataServiceHelper.Save(this.View.Context, targetBillMeta.BusinessInfo, objs, saveOption, "Save");
```

---

## 六、SQL脚本规范

### DDL规范

- 【强制】表名视图名必须以开发商标识加下划线排头[ISV标识符_T_名称](如：ABC_T_USER)
- 【强制】字段名必须以F下划线加开发商标识加下划线排头[F_ISV标识符_名称]（如： F_ABC_USERNAME）
- 【强制】表必须有物理主键，建立聚集索引
- 【推荐】主键原则上使用整型（不允许用自增长主键）
- 【强制】禁止修改标准产品视图
- 【强制】禁止删除物理表（临时表除外）
- 【强制】禁止使用触发器
- 【推荐】不建议使用存储过程
- 【推荐】大字段单独放一个表
- 【强制】主键和索引字段的值不允许为null
- 【强制】GUID字段不允许建立为聚集索引
- 【强制】一般数量、金额类型：必须使用精确数值类型，如：Decimal，禁止为空，指定默认值为0
- 【强制】Not null属性的字段，必须设置缺省值
- 【强制】临时表必须显式地创建，使用完毕后必须显式地删除

### DML规范

- 【强制】任何查询、更新、删除语句都要带过滤条件
- 【强制】查询条件必须使用参数化查询
- 【强制】必须使用join语句进行表间连接，禁止使用where条件进行表连接
- 【推荐】Join表数量控制在10个以内
- 【强制】预插数据insert前必须按主键匹配删除再插入
- 【强制】一次批量提交SQL的语句不应该超过500条，如果超过，需要分批提交
- 【推荐】可以关联物理表实现的SQL语句，不要关联视图
- 【强制】严禁直接删库
- 【强制】禁止在循环中执行低性能或是高数据量SQL，会导致严重性能问题

---

## 七、代码安全规范

- 【强制】禁止所有一切可能威害服务器运行安全的代码
- 【强制】禁止任何使用进程的代码：（如：Process、ProcessStartInfo、System.Diagnostics…）
- 【强制】禁止任何Socket侦听的代码
- 【强制】禁止调用管理中心或其他BOS禁用调用接口
- 【强制】禁止UnSafe代码使用
- 【强制】禁止将密钥、密码、连接信息、域名等敏感信息硬编码在代码中
- 【强制】二开代码写入日志时，禁止写入C盘系统盘。每天日志文件大小不要超过100MB，且最多保留15天日志
- 【强制】使用参数化查询方式来进行数据查询及操作，禁止将完整sql代码直接作为参数

---

## 八、敏感业务开发规范

敏感业务是指业务操作过程中出现问题会带来无法挽救的损失，如资金支付类业务的多付或少付、大批量数据或文件删除等。

- 【强制】大批量删除或修改数据等敏感高危业务场景，必须设计保护措施，如权限校验和多级确认等
- 【强制】接口设计必须针对候选键字段（唯一性字段）设置唯一索引防重机制，以及日志预警功能
- 【强制】对于交易成功的单据，不允许反审核操作
- 【强制】保证密码的安全性，加密存放至数据库或者配置中心
- 【强制】与资金类核心系统对接要严格控制权限，各环节处理需要增加日志记录

---

## 九、WXK项目 OA同步功能

### OA同步相关字段定义

#### 字段对照表

| 单据类型 | 表名 | OA流程ID字段 | 同步状态字段 | 银行状态字段 |
|---------|------|-------------|-------------|-------------|
| 付款单 | T_AP_PAYBILL | F_TWLG_OAPROCESSID | F_TWLG_OAStatus | FBANKSTATUS |
| 收款退款单 | T_AR_REFUNDBILL | F_TWLG_OAPROCESSID | F_TWLG_OAStatus | FBankStatus |
| 银行交易明细 | T_CN_BANKCASHFLOW | - | F_TWLG_OASyncStatus | - |

#### 同步状态值

| 值 | 含义 |
|----|------|
| 0 | 未同步 |
| 1 | 已同步 |
| 2 | 同步失败 |
| 3 | 已排除 |

#### 银行处理状态

| 值 | 含义 |
|----|------|
| F | 已付款确认 |

### 核心文件清单

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

### 业务逻辑要点

#### 收款退款单同步OA前置条件

必须先执行**已付款确认**动作后才能同步OA，否则抛出异常

#### 收款认领单自动合并下推

- 保存后自动提交审核认领单
- 按 BatchKey 分组：流水总金额 + 付款单位 + 收款组织 + 银行账号 + 结算方式
- 同批次认款齐套 (金额一致) 时自动合并下推收款单
- 下推后自动保存 + 提交 + 审核

#### 银行交易明细推送过滤

- 只推送贷方金额 > 0 的交易明细
- 已关联收款认领单的交易明细不重复推送
- 流水号唯一性校验

---

## 十、代码提交规范

1. 提交前必须先拉取最新代码: `git pull --rebase`
2. 生成规范的提交记录 (含函数级注释说明)
3. 提交变动并推送: `git add` -> `git commit` -> `git push`

---

## 十一、设计规范补充

- 【强制】不允许对一个业务对象或是单据转换，进行多个平级扩展
- 【推荐】一个单据的数据库表的字段总数原则上不要超过50个字段（含隐藏字段）
- 【强制】网控必须配置，防止并发修改数据导致数据错误
- 【推荐】基础资料表单主键设计建议使用整型，否则不支持单据转换
- 【强制】所有配置的方案，修改前一定要做好备份
