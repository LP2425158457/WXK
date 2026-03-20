using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System.ComponentModel;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Data;
using Kingdee.BOS;

namespace LP.WXK.K3.App.ServicePlugIn
{
    [Description("【服务插件】付款单、收款退款单的“已付款确认”增加插件，调用OA同步；并写入日志；"), HotUpdate]
    public class OASyncOperationServicePlugIn : AbstractOperationServicePlugIn
    {

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            OASyncService oASync = new OASyncService();
            // 读取全部的单据,for循环,转换成DynamicObject类型
            foreach (DynamicObject entity in e.DataEntitys)
            {
                // 如果不为空,开始循环
                if (entity != null)
                {
                    long payId = Convert.ToInt64(entity["Id"]);
                    var typeName = entity.DynamicObjectType.Name;
                    var tableName = "";
                    string lcbm = "";

                    if (typeName.Equals("PAYBILL"))
                    {   // 付款单
                        tableName = "T_AP_PAYBILL";
                        lcbm = GetLCBM(this.Context, tableName, payId);
                    }
                    else if (typeName.Equals("REFUNDBILL"))
                    {   // 收款退款单
                        tableName = "T_AR_REFUNDBILL";
                        lcbm = GetLCBM(this.Context, tableName, payId);
                    }

                    // 检查是否已同步成功
                    if (IsAlreadySynced(this.Context, tableName, payId))
                    {
                        throw new Exception("单据已成功同步OA，不需要再次同步");
                    }

                    bool isSync = oASync.skipCurrentCodeAsync(this.Context, lcbm);
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
                }
            }
        }

        /// <summary>
        /// 检查单据是否已同步成功
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="tableName">表名</param>
        /// <param name="billId">单据ID</param>
        /// <returns>是否已同步成功</returns>
        private bool IsAlreadySynced(Context ctx, string tableName, long billId)
        {
            try
            {
                string sql = string.Format("SELECT F_TWLG_OAStatus FROM {0} WHERE FID = {1}", tableName, billId);
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

        private string GetLCBM(Context ctx, string tableName, long billId)
        {
            string lcbm = "";
            try
            {
                string sql = string.Format("SELECT F_TWLG_LCBM FROM {0} WHERE FID = {1}", tableName, billId);
                using (IDataReader reader = DBUtils.ExecuteReader(ctx, sql))
                {
                    if (reader.Read())
                    {
                        lcbm = Convert.ToString(reader["F_TWLG_LCBM"]) ?? "";
                    }
                }
            }
            catch (Exception)
            {
            }
            return lcbm;
        }
    }
}
