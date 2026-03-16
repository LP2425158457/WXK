using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using LP.WXK.K3.App.RecDetailSyncSchedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.WXK.K3.App.ServicePlugIn
{
    class OASyncRecDetailSchedule : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            RecDetailService recDetailSync = new RecDetailService();
            //读取全部的单据,for循环,转换成DynamicObject类型
            foreach (DynamicObject entity in e.DataEntitys)
            {
                //如果不为空,开始循环
                if (entity != null)
                {
                    long payId = Convert.ToInt64(entity["Id"]);
                    var typeName = entity.DynamicObjectType.Name;
                    // bool isSync = recDetailSync.syncBill(this.Context, payId);
                    bool isSync = recDetailSync.syncBill(this.Context, 57609281599132159);
                }
            }
            throw new NotImplementedException();
        }
    }
}
