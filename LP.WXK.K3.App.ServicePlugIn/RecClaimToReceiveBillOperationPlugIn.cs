using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.Operation;
using System.Data;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS.ServiceHelper;

namespace LP.WXK.K3.App.ServicePlugIn
{
    [Description("【操作插件】收款认领单下推生成收款单：校验最后一笔认款，自动合并下推"), HotUpdate]
    public class RecClaimToReceiveBillOperationPlugIn : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 收款认领单表单ID
        /// </summary>
        private const string SOURCE_FORMID = "CN_RECCLAIMBIL";

        /// <summary>
        /// 收款单单据ID
        /// </summary>
        private const string TARGET_FORMID = "AR_RECEIVEBILL";

        /// <summary>
        /// 转换规则ID
        /// </summary>
        private const string CONVERT_RULE_ID = "CN_RecClaimBillToRecBill";

        /// <summary>
        /// 已确认认领状态
        /// </summary>
        private const string BILL_STATUS_CONFIRMED = "C";

        /// <summary>
        /// 金额比较容差（与标准币别小数位一致，避免浮点/舍入导致误判）
        /// </summary>
        private const decimal AmountTolerance = 0.01m;

        private List<long> _billIds = new List<long>();

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);

            if (e.DataEntitys == null || e.DataEntitys.Length == 0)
            {
                return;
            }

            _billIds.Clear();
            List<string> billNos = new List<string>();
            Dictionary<string, List<long>> bankSeqToBillIds = new Dictionary<string, List<long>>();

            foreach (DynamicObject billObj in e.DataEntitys)
            {
                long billId = Convert.ToInt64(billObj["Id"]);
                string billNo = Convert.ToString(billObj["BillNo"]);
                string bankSeqNo = GetBankSeqNo(billId);

                if (string.IsNullOrWhiteSpace(bankSeqNo))
                {
                    throw new Exception($"收款认领单 {billNo} 不存在交易流水号，不允许下推！");
                }

                billNos.Add(billNo);

                if (!bankSeqToBillIds.ContainsKey(bankSeqNo))
                {
                    bankSeqToBillIds[bankSeqNo] = new List<long>();
                }
                bankSeqToBillIds[bankSeqNo].Add(billId);
            }

            foreach (var kvp in bankSeqToBillIds)
            {
                string bankSeqNo = kvp.Key;
                List<long> currentBillIds = kvp.Value;

                ValidateLastClaimForBankSeq(bankSeqNo);

                List<long> allBillIds = GetUnpushedClaimBillsByBankSeq(bankSeqNo);
                foreach (long id in allBillIds)
                {
                    if (!_billIds.Contains(id))
                    {
                        _billIds.Add(id);
                    }
                }
            }

            foreach (long billId in _billIds)
            {
                if (!ValidateBillStatus(billId))
                {
                    throw new Exception($"收款认领单 {GetBillNo(billId)} 单据状态不是已审核，不允许下推！");
                }

                if (ValidateAlreadyPushed(billId))
                {
                    throw new Exception($"收款认领单 {GetBillNo(billId)} 已经生成过收款单，不允许重复下推！");
                }
            }

            ValidateClaimAmount(_billIds);
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
        }

        private string GetBillNo(long billId)
        {
            try
            {
                string sql = string.Format("SELECT FBILLNO FROM T_CN_RECCLAIMBILL WHERE FID = {0}", billId);
                using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sql))
                {
                    if (reader.Read())
                    {
                        return Convert.ToString(reader["FBILLNO"]);
                    }
                }
            }
            catch (Exception)
            {
            }
            return billId.ToString();
        }

        /// <summary>
        /// 校验该银企流水号是否已认款完毕（最后一笔）。
        /// 说明：部分环境 T_CN_BANKCASHFLOW 无 FRemainAmt 或该字段未随认领回写，仅用 FRemainAmt 会误判。
        /// 此处用「银行贷方收款金额 - 同流水号已确认认领单(按单汇总)的已认领金额之和」计算剩余认款。
        /// </summary>
        private void ValidateLastClaimForBankSeq(string bankSeqNo)
        {
            string esc = bankSeqNo.Replace("'", "''");
            string sql = string.Format(@"
                SELECT TOP 1
                    b.FCREDITAMOUNT,
                    ISNULL((
                        SELECT SUM(t.FCLAIMAMOUNT)
                        FROM (
                            SELECT DISTINCT h.FID, h.FCLAIMAMOUNT
                            FROM T_CN_RECCLAIMBILL h
                            INNER JOIN T_CN_RECCLAIMBILLENTRY e ON h.FID = e.FID
                            WHERE e.FBNKSEQNO = b.FSETTLENO AND h.FDOCUMENTSTATUS = N'C'
                        ) t
                    ), 0) AS ClaimedSum
                FROM T_CN_BANKCASHFLOW b
                WHERE b.FSETTLENO = N'{0}'", esc);

            using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sql))
            {
                if (!reader.Read())
                {
                    throw new Exception(
                        $"交易流水号 {bankSeqNo} 在银行交易明细中未找到（请核对认领单分录「交易流水号」是否与银行明细「银企流水号 FSETTLENO」完全一致，含空格）。");
                }

                decimal creditAmount = Convert.ToDecimal(reader["FCREDITAMOUNT"]);
                decimal claimedSum = Convert.ToDecimal(reader["ClaimedSum"]);
                decimal remain = creditAmount - claimedSum;

                if (Math.Abs(remain) > AmountTolerance)
                {
                    throw new Exception(
                        $"交易流水号 {bankSeqNo} 还有未认完的金额（银行收款 {creditAmount}，已确认认领合计 {claimedSum}，剩余约 {remain}），不是最后一笔认款，不允许下推！");
                }
            }
        }

        /// <summary>
        /// 根据交易流水号获取所有未下推的收款认领单ID
        /// </summary>
        /// <param name="bankSeqNo">交易流水号</param>
        /// <returns>未下推的认领单ID列表</returns>
        private List<long> GetUnpushedClaimBillsByBankSeq(string bankSeqNo)
        {
            List<long> billIds = new List<long>();
            try
            {
                string sql = string.Format(@"
                    SELECT DISTINCT h.FID
                    FROM T_CN_RECCLAIMBILL h
                    INNER JOIN T_CN_RECCLAIMBILLENTRY e ON h.FID = e.FID
                    WHERE e.FBNKSEQNO = '{0}'
                      AND h.FDocumentStatus = 'C'
                      AND NOT EXISTS (
                          SELECT 1 FROM T_AR_RECEIVEBILLSRCENTRY src
                          WHERE src.FSRCBILLID = h.FID
                      )",
                    bankSeqNo.Replace("'", "''"));

                using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sql))
                {
                    while (reader.Read())
                    {
                        billIds.Add(Convert.ToInt64(reader["FID"]));
                    }
                }
            }
            catch (Exception)
            {
            }
            return billIds;
        }

        /// <summary>
        /// 获取认领单的交易流水号
        /// </summary>
        private string GetBankSeqNo(long billId)
        {
            try
            {
                string sql = string.Format(@"
                    SELECT e.FBNKSEQNO
                    FROM T_CN_RECCLAIMBILL h
                    INNER JOIN T_CN_RECCLAIMBILLENTRY e ON h.FID = e.FID
                    WHERE h.FID = {0}", billId);

                using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sql))
                {
                    if (reader.Read())
                    {
                        return Convert.ToString(reader["FBNKSEQNO"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
            }
            return "";
        }

        /// <summary>
        /// 校验收款认领单单据状态是否为已确认认领
        /// </summary>
        private bool ValidateBillStatus(long billId)
        {
            try
            {
                string sql = $"SELECT FDocumentStatus FROM T_CN_RECCLAIMBILL WHERE FID = {billId}";
                using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sql))
                {
                    if (reader.Read())
                    {
                        string status = Convert.ToString(reader["FDocumentStatus"]);
                        return BILL_STATUS_CONFIRMED.Equals(status, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// 校验收款认领单是否已经生成过收款单
        /// </summary>
        private bool ValidateAlreadyPushed(long billId)
        {
            try
            {
                string sql = $"SELECT 1 FROM T_AR_RECEIVEBILLSRCENTRY WHERE FSRCBILLID = {billId}";
                using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sql))
                {
                    return reader.Read();
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// 校验所选收款认领单的认款金额是否等于流水总金额
        /// </summary>
        private void ValidateClaimAmount(List<long> billIds)
        {
            if (billIds.Count == 0)
            {
                return;
            }

            try
            {
                string idsStr = string.Join(",", billIds);

                string bankSeqSql = string.Format(@"
                    SELECT DISTINCT h.FID, h.FBILLNO, h.FRECAMOUNT, h.FCLAIMAMOUNT, e.FBNKSEQNO
                    FROM T_CN_RECCLAIMBILL h
                    INNER JOIN T_CN_RECCLAIMBILLENTRY e ON h.FID = e.FID
                    WHERE h.FID IN ({0})", idsStr);

                Dictionary<string, List<ClaimBillInfo>> batchDict = new Dictionary<string, List<ClaimBillInfo>>();

                using (IDataReader reader = DBUtils.ExecuteReader(this.Context, bankSeqSql))
                {
                    while (reader.Read())
                    {
                        string billNo = Convert.ToString(reader["FBILLNO"]);
                        string bankSeqNo = Convert.ToString(reader["FBNKSEQNO"]);
                        decimal recAmount = Convert.ToDecimal(reader["FRECAMOUNT"]);
                        decimal claimAmount = Convert.ToDecimal(reader["FCLAIMAMOUNT"]);

                        if (!batchDict.ContainsKey(bankSeqNo))
                        {
                            batchDict[bankSeqNo] = new List<ClaimBillInfo>();
                        }
                        batchDict[bankSeqNo].Add(new ClaimBillInfo
                        {
                            BillNo = billNo,
                            RecAmount = recAmount,
                            ClaimAmount = claimAmount
                        });
                    }
                }

                foreach (var batch in batchDict)
                {
                    string bankSeqNo = batch.Key;
                    List<ClaimBillInfo> bills = batch.Value;

                    decimal totalClaimAmount = bills.Sum(b => b.ClaimAmount);
                    decimal recAmount = bills.First().RecAmount;

                    if (bills.Any(b => b.RecAmount != recAmount))
                    {
                        throw new Exception($"交易流水号 {bankSeqNo} 对应的收款认领单流水总金额不一致，不允许下推！");
                    }

                    if (totalClaimAmount != recAmount)
                    {
                        string billNoList = string.Join(", ", bills.Select(b => b.BillNo));
                        throw new Exception($"交易流水号 {bankSeqNo} 对应的收款认领单 {billNoList} 已认领金额之和({totalClaimAmount})不等于流水总金额({recAmount})，不允许下推！");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("不允许下推"))
                {
                    throw;
                }
                throw new Exception($"校验认款金额失败：{ex.Message}");
            }
        }

        private class ClaimBillInfo
        {
            public string BillNo { get; set; }
            public decimal RecAmount { get; set; }
            public decimal ClaimAmount { get; set; }
        }

        /// <summary>
        /// 执行单据下推，生成收款单
        /// </summary>
        private void PushToReceiveBill(List<long> billIds)
        {
            try
            {
                var ruleMeta = ConvertServiceHelper.GetConvertRule(this.Context, CONVERT_RULE_ID);
                if (ruleMeta == null)
                {
                    throw new Exception("未找到收款认领单到收款单的转换规则，请检查转换规则是否启用！");
                }

                var rule = ruleMeta.Rule;

                List<ListSelectedRow> selectedRows = new List<ListSelectedRow>();
                foreach (long billId in billIds)
                {
                    ListSelectedRow row = new ListSelectedRow(Convert.ToString(billId), string.Empty, 0, SOURCE_FORMID);
                    selectedRows.Add(row);
                }

                PushArgs pushArgs = new PushArgs(rule, selectedRows.ToArray())
                {
                    TargetBillTypeId = "",
                    TargetOrgId = 0,
                    CustomParams = new Dictionary<string, object>()
                };

                ConvertOperationResult operationResult = ConvertServiceHelper.Push(this.Context, pushArgs, OperateOption.Create());

                if (!operationResult.IsSuccess)
                {
                    var errorMsg = operationResult.OperateResult.FirstOrDefault()?.Message ?? "下推失败";
                    throw new Exception($"下推失败：{errorMsg}");
                }

                DynamicObject[] objs = (from p in operationResult.TargetDataEntities
                                        select p.DataEntity).ToArray();

                if (objs == null || objs.Length == 0)
                {
                    throw new Exception("下推生成的目标单据为空！");
                }

                var targetBillMeta = MetaDataServiceHelper.Load(this.Context, TARGET_FORMID) as FormMetadata;
                OperateOption saveOption = OperateOption.Create();
                saveOption.SetIgnoreWarning(true);

                var saveResult = BusinessDataServiceHelper.Save(this.Context, targetBillMeta.BusinessInfo, objs, saveOption, "Audit");

                if (!saveResult.IsSuccess)
                {
                    var errorMsg = saveResult.OperateResult.FirstOrDefault()?.Message ?? "保存失败";
                    throw new Exception($"保存收款单失败：{errorMsg}");
                }

                object[] savedBillIds = new object[objs.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    savedBillIds[i] = objs[i][0];
                }

                var submitResult = BusinessDataServiceHelper.Submit(this.Context, targetBillMeta.BusinessInfo, savedBillIds, "Submit", saveOption);
                if (!submitResult.IsSuccess)
                {
                    var errorMsg = submitResult.OperateResult.FirstOrDefault()?.Message ?? "提交失败";
                    throw new Exception($"提交收款单失败：{errorMsg}");
                }

                var applyResult = BusinessDataServiceHelper.Audit(this.Context, targetBillMeta.BusinessInfo, savedBillIds, saveOption);
                if (!applyResult.IsSuccess)
                {
                    var errorMsg = applyResult.OperateResult.FirstOrDefault()?.Message ?? "审核失败";
                    throw new Exception($"审核收款单失败：{errorMsg}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"下推收款单操作失败：{ex.Message}");
            }
        }
    }
}