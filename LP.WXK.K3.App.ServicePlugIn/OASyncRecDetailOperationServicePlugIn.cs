using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using LP.WXK.K3.App.RecDetailSyncSchedule;
using System.Data;
using Kingdee.BOS;
using Kingdee.BOS.Util;

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
            // 读取全部的单据,for循环,转换成DynamicObject类型
            foreach (DynamicObject entity in e.DataEntitys)
            {
                if (entity != null)
                {
                    string billNo = Convert.ToString(entity["BillNo"]);
                    var tableName = "T_CN_BANKCASHFLOW";

                    // 检查是否已同步成功
                    if (IsAlreadySynced(this.Context, tableName, billNo))
                    {
                        throw new Exception("单据已成功同步OA，不需要再次同步");
                    }

                    bool isSync = recDetailSync.syncBill(this.Context, billNo);
                    // F_TWLG_OAStatus = 0（未反写）、1（已处理）
                    if (isSync)
                    {
                        string sqlStr = string.Format(@"update {0} set F_TWLG_OAStatus = 1 where FBILLNO = '{1}'", tableName, billNo);
                        DBUtils.Execute(this.Context, sqlStr);
                    }
                    else
                    {
                        string sqlStr = string.Format(@"update {0} set F_TWLG_OAStatus = 0 where FBILLNO = '{1}'", tableName, billNo);
                        DBUtils.Execute(this.Context, sqlStr);
                    }
                }
            }
        }

        /// <summary>
        /// 检查单据是否已同步成功
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="tableName">表名</param>
        /// <param name="billNo">单据编号</param>
        /// <returns>是否已同步成功</returns>
        private bool IsAlreadySynced(Context ctx, string tableName, string billNo)
        {
            try
            {
                string sql = string.Format("SELECT F_TWLG_OAStatus FROM {0} WHERE FBILLNO = '{1}'", tableName, billNo);
                using (IDataReader reader = DBUtils.ExecuteReader(ctx, sql))
                {
                    if (reader.Read())
                    {
                        object status = reader["F_TWLG_OAStatus"];
                        if (status != null && status != DBNull.Value)
                        {
                            return Convert.ToInt32(status) == 1;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
    }
}
