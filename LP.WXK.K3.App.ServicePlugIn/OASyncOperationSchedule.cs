using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.WXK.K3.App.ServicePlugIn
{
    class OASyncOperationSchedule : IScheduleService
    {
        // 通过定时任务，查询位同步定期调用泛微OA接口，同步OA节点
        public void Run(Context ctx, Schedule schedule)
        {

            long payId = Convert.ToInt64(entity["Id"]);
            var typeName = entity.DynamicObjectType.Name;
            var tableName = "";
            if (typeName.Equals("PAYBILL"))
            {// 付款单
                tableName = "T_AP_PAYBILL";
            }
            else if (typeName.Equals("REFUNDBILL"))
            {// 收款退款单
                tableName = "T_AR_REFUNDBILL";
            }
            bool isSync = oASync.skipCurrentCodeAsync(this.Context, Convert.ToString(payId));
            // F_TWLG_OAStatus = 0（未反写）、1（已处理）
            if (isSync)
            {
                string sqlStr = string.Format(@"update {0} set F_TWLG_OAStatus = 1 where FID = {1}", tableName, payId);
                DBUtils.Execute(this.Context, sqlStr);
            }
            else
            {
                string sqlStr = string.Format(@"update {0} set F_TWLG_OAStatus = 0 where FID = {1}", tableName, payId);
                DBUtils.Execute(this.Context, sqlStr);
            }
            throw new NotImplementedException();
        }
    }
}
