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
    [Description("【操作插件】收款认领单下推生成收款单：校验单据状态和认款金额，执行下推"), HotUpdate]
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
        private const string CONVERT_RULE_ID = "CN_MultiRecClaimBillToRecBill";

        /// <summary>
        /// 已确认认领状态
        /// </summary>
        private const string BILL_STATUS_CONFIRMED = "C";

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            if (e.DataEntitys == null || e.DataEntitys.Length == 0)
            {
                return;
            }

            List<long> billIds = new List<long>();
            List<string> billNos = new List<string>();

            foreach (DynamicObject billObj in e.DataEntitys)
            {
                long billId = Convert.ToInt64(billObj["Id"]);
                string billNo = Convert.ToString(billObj["BillNo"]);
                if (!ValidateBillStatus(billId, billNo))
                {
                    throw new Exception($"收款认领单 {billNo} 单据状态不是已审核，不允许下推！");
                }

                if (ValidateAlreadyPushed(billId, billNo))
                {
                    throw new Exception($"收款认领单 {billNo} 已经生成过收款单，不允许重复下推！");
                }
                billIds.Add(billId);
                billNos.Add(billNo);
            }

            if (!ValidateClaimAmount(billIds, billNos))
            {
                string billNoList = string.Join(", ", billNos);
                throw new Exception($"所选收款认领单 {billNoList} 的已认领金额之和不等于流水总金额，不允许下推！");
            }

            PushToReceiveBill(billIds);
        }

        /// <summary>
        /// 校验收款认领单单据状态是否为已确认认领
        /// </summary>
        /// <param name="billId">单据ID</param>
        /// <param name="billNo">单据编号</param>
        /// <returns>是否通过校验</returns>
        private bool ValidateBillStatus(long billId, string billNo)
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
            catch (Exception ex)
            {
                throw new Exception($"校验单据状态失败：{ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 校验收款认领单是否已经生成过收款单
        /// </summary>
        /// <param name="billId">单据ID</param>
        /// <param name="billNo">单据编号</param>
        /// <returns>是否已生成过收款单</returns>
        private bool ValidateAlreadyPushed(long billId, string billNo)
        {
            try
            {
                string sql = $"SELECT 1 FROM T_AR_RECEIVEBILLSRCENTRY WHERE FSRCBILLID = {billId} ";
                using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sql))
                {
                    if (reader.Read())
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"校验是否已下推失败：{ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 校验所选收款认领单的认款金额是否等于流水总金额
        /// 根据交易流水号分批判断，同一批次的单据：已认领金额之和 = 流水总金额
        /// </summary>
        /// <param name="billIds">单据ID列表</param>
        /// <param name="billNos">单据编号列表</param>
        /// <returns>是否通过校验</returns>
        private bool ValidateClaimAmount(List<long> billIds, List<string> billNos)
        {
            try
            {
                string idsStr = string.Join(",", billIds);

                // 先查询所选单据中所有交易流水号
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

                        // 按交易流水号分组
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

                // 对每批次进行校验
                foreach (var batch in batchDict)
                {
                    string bankSeqNo = batch.Key;
                    List<ClaimBillInfo> bills = batch.Value;

                    decimal totalClaimAmount = bills.Sum(b => b.ClaimAmount);
                    decimal recAmount = bills.First().RecAmount;

                    // 检查同一批次内所有单据的流水总金额是否一致
                    if (bills.Any(b => b.RecAmount != recAmount))
                    {
                        throw new Exception($"交易流水号 {bankSeqNo} 对应的收款认领单流水总金额不一致，不允许下推！");
                    }

                    // 检查已认领金额之和是否等于流水总金额
                    if (totalClaimAmount != recAmount)
                    {
                        string billNoList = string.Join(", ", bills.Select(b => b.BillNo));
                        throw new Exception($"交易流水号 {bankSeqNo} 对应的收款认领单 {billNoList} 已认领金额之和({totalClaimAmount})不等于流水总金额({recAmount})，不允许下推！");
                    }
                }

                return true;
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

        /// <summary>
        /// 收款认领单信息
        /// </summary>
        private class ClaimBillInfo
        {
            public string BillNo { get; set; }
            public decimal RecAmount { get; set; }
            public decimal ClaimAmount { get; set; }
        }

        /// <summary>
        /// 执行单据下推，生成收款单
        /// </summary>
        /// <param name="billIds">单据ID列表</param>
        private void PushToReceiveBill(List<long> billIds)
        {
            try
            {
                // var rules = ConvertServiceHelper.GetConvertRules(this.Context, SOURCE_FORMID, TARGET_FORMID);
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

                var saveResult = BusinessDataServiceHelper.Save(this.Context, targetBillMeta.BusinessInfo, objs, saveOption, "Save");

                if (!saveResult.IsSuccess)
                {
                    var errorMsg = saveResult.OperateResult.FirstOrDefault()?.Message ?? "保存失败";
                    throw new Exception($"保存收款单失败：{errorMsg}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"下推收款单操作失败：{ex.Message}");
            }
        }
    }
}
