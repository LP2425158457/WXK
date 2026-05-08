# ERP银行交易明细OA同步优化计划

## 需求分析

### 1. 过滤已生成/关联单据的交易明细
- **定时任务**：查询条件需增加"已生成/关联单据=否"的过滤
- **手工下推**：需在操作插件中校验并给出提示

### 2. 银企流水号唯一性校验
- 交易流水号(FSETTLENO)已推送过的不再重复推送
- 需要在ERP加重复提示

### 3. 只下推贷方金额大于0的交易明细
- 增加条件：FCREDITAMOUNT > 0

---

## 实施步骤

### Step 1: 修改 OASyncRecDetailSchedule.cs（定时任务）

**文件**: `d:\Code\WXK\LP.WXK.K3.App.ServicePlugIn\OASyncRecDetailSchedule.cs`

**修改内容**:
1. SQL查询条件增加：
   - `FCREDITAMOUNT > 0`（贷方/收款金额大于0）
   - `FSETTLENO NOT IN (SELECT DISTINCT jylsh FROM ... 已推送的流水号)`（排除已推送的）
   - 或通过关联表判断是否已生成收款认领单
2. 具体SQL示例：
   ```sql
   SELECT FID, FBILLNO, FEXPLANATION, FOppBankAcntName
   FROM T_CN_BANKCASHFLOW
   WHERE F_TWLG_OASyncStatus = 0
     AND FDOCUMENTSTATUS = 'C'
     AND FCREDITAMOUNT > 0
     AND NOT EXISTS (
         SELECT 1 FROM T_CN_RECCLAIMBILLENTRY
         WHERE FBNKSEQNO = T_CN_BANKCASHFLOW.FSETTLENO
     )
   ```

### Step 2: 修改 OASyncRecDetailOperationServicePlugIn.cs（手工操作插件）

**文件**: `d:\Code\WXK\LP.WXK.K3.App.ServicePlugIn\OASyncRecDetailOperationServicePlugIn.cs`

**修改内容**:
1. 在 AfterExecuteOperationTransaction 中增加校验：
   - 校验贷方金额是否大于0，如为0则抛出异常提示
   - 校验交易流水号是否已存在关联单据，如已存在则抛出异常提示
2. 增加方法 `ValidateBankSeqNoNotAssociated` 检查流水号是否可推送
3. 增加方法 `ValidateCreditAmount` 检查贷方金额

### Step 3: 修改 RecDetailService.cs（可选，如需在推送前校验）

**文件**: `d:\Code\WXK\LP.WXK.K3.App.ServicePlugIn\RecDetailService.cs`

**修改内容**:
- 在 getMainTableById 方法中获取 FSETTLENO（交易流水号）用于日志记录

---

## 字段说明

| 字段 | 说明 | 表名 |
|-----|------|------|
| FSETTLENO | 交易流水号 | T_CN_BANKCASHFLOW |
| FCREDITAMOUNT | 贷方发生额（收入） | T_CN_BANKCASHFLOW |
| FBNKSEQNO | 交易流水号（认领单分录） | T_CN_RECCLAIMBILLENTRY |
| F_TWLG_OASyncStatus | OA同步状态 | T_CN_BANKCASHFLOW |

---

## 状态码说明

| 状态码 | 说明 |
|-------|------|
| 0 | 未同步 |
| 1 | 已同步 |
| 2 | 同步失败 |
| 3 | 已排除 |

---

## 注意事项

1. 手工下推时需要给出明确的错误提示
2. 定时任务静默跳过不符合条件的记录
3. 确保SQL查询使用参数化避免注入风险