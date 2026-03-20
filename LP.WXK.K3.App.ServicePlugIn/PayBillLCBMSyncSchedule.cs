using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.App.Data;
using System;
using System.Data;

namespace LP.WXK.K3.App.ServicePlugIn
{
    /// <summary>
    /// 付款单流程编码更新执行计划
    /// 定期将付款单的单据编号同步到流程编码字段F_TWLG_LCBM
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
            SyncPayBillLCBM(ctx);
        }

        /// <summary>
        /// 同步付款单流程编码
        /// 将单据编号(FBILLNO)更新到流程编码字段(F_TWLG_LCBM)
        /// </summary>
        /// <param name="ctx">上下文</param>
        private void SyncPayBillLCBM(Context ctx)
        {
            try
            {
                string sql = @"
                    UPDATE T_AP_PAYBILL
                    SET F_TWLG_LCBM = FBILLNO
                    WHERE (F_TWLG_LCBM IS NULL OR F_TWLG_LCBM = '' OR F_TWLG_LCBM = ' ')
                      AND FDOCUMENTSTATUS = 'C'";

                DBUtils.Execute(ctx, sql);
            }
            catch (Exception)
            {
            }
        }
    }
}
