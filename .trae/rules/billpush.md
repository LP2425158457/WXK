// 获取源单与目标单直接的转换规则，如果规则未启用，则返回为空，注意容错\
// 假设：上游单据FormId为sourceFormId，下游单据FormId为targetFormId\
var rules = ConvertServiceHelper.GetConvertRules(this.View\.Context, sourceFormId, targetFormId);\
var rule = rules.FirstOrDefault(t => t.IsDefault);\
// 获取在列表上当前选择需下推的行\
ListSelectedRow\[] selectedRows = ((IListView)this.View).SelectedRowsInfo.ToArray();

// 如下代码为单据上获取当前当前选择行\
// string primaryKeyValue = ((IBillView)this.View).Model.GetPKValue().ToString();\
// ListSelectedRow row = new ListSelectedRow(primaryKeyValue, string.Empty, 0, this.View\.BillBusinessInfo.GetForm().Id);\
// ListSelectedRow\[] selectedRows = new ListSelectedRow\[] { row };\
\
// 调用下推服务，生成下游单据数据包\
ConvertOperationResult operationResult = null;\
Dictionary custParams = new Dictionary();\
try\
{\
PushArgs pushArgs = new PushArgs(rule, selectedRows)\
{\
TargetBillTypeId = "" , // 请设定目标单据单据类型。如无单据类型，可以空字符\
TargetOrgId = 0, // 请设定目标单据主业务组织。如无主业务组织，可以为0\
CustomParams = custParams , // 可以传递额外附加的参数给单据转换插件，如无此需求，可以忽略\
};\
//执行下推操作，并获取下推结果\
operationResult = ConvertServiceHelper.Push(this.View\.Context, pushArgs, OperateOption.Create());\
}\
catch (KDExceptionValidate ex)\
{\
this.View\.ShowErrMessage(ex.Message, ex.ValidateString);\
return false;\
}\
catch (KDException ex)\
{\
this.View\.ShowErrMessage(ex.Message);\
return false;\
}\
catch\
{\
throw;\
}\
// 获取生成的目标单据数据包\
DynamicObject\[] objs = (from p in operationResult.TargetDataEntities\
select p.DataEntity).ToArray();\
// 读取目标单据元数据\
var targetBillMeta = MetaDataServiceHelper.Load(this.View\.Context, targetFormId) as FormMetadata;\
OperateOption saveOption = OperateOption.Create();\
// 忽略全部需要交互性质的提示，直接保存；\
saveOption.SetIgnoreWarning(true); // 提交数据库保存，并获取保存结果\
var saveResult = BusinessDataServiceHelper.Save(this.View\.Context,targetBillMeta.BusinessInfo, objs, saveOption, "Save");\
// 根据保存结果，显示对应的提示\
// TODO: 略，请自行查看saveResult对象的结构，利用this.View\.ShowMessage之类函数，显示结果..

\[/code]
