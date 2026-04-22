using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.App.Data;
using System;
using System.Data;

namespace LP.WXK.K3.App.ServicePlugIn
{
    /// <summary>
    /// 付款单OA流程ID更新执行计划
    /// 定期将付款单的单据编号同步到OA流程ID字段F_TWLG_OAPROCESSID
    /// </summary>
    public class PayBillLCBMSyncSchedule : IScheduleService
    {
        /// <summary>
        /// 执行计划执行入口
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="schedule">定时任务配置</param>
        public void Run(Context ctx, Schedule schedule)
        {
            SyncPayBillOAProcessId(ctx);
        }

        /// <summary>
        /// 同步付款单OA流程ID
        /// 将单据编号(FBILLNO)更新到OA流程ID字段(F_TWLG_OAPROCESSID)
        /// </summary>
        /// <param name="ctx">上下文</param>
        private void SyncPayBillOAProcessId(Context ctx)
        {
            try
            {
                string sql = @"
                    UPDATE T_AP_PAYBILL
                    SET F_TWLG_OAPROCESSID = FBILLNO
                    WHERE (F_TWLG_OAPROCESSID IS NULL OR F_TWLG_OAPROCESSID = '' OR F_TWLG_OAPROCESSID = ' ')
                      AND FDOCUMENTSTATUS = 'C'";

                DBUtils.Execute(ctx, sql);
            }
            catch (Exception)
            {
            }
        }
    }
}
