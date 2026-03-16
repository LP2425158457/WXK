using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using LP.WXK.K3.App.RecDetailSyncSchedule;

namespace LP.WXK.K3.App.ServicePlugIn
{
    [Description("【服务插件】测试:ERP银行交易明细传输至OA回款明细（需设置过滤条件或人工选择标记）"), HotUpdate]
    public class OASyncRecDetailOperationServicePlugIn : AbstractOperationServicePlugIn
    {

        // 单据保存成功后,同步OA
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
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
        }
    }
}
