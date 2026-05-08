**2. 结构说明：**\
单据数据包DynamicObject，相当于一个**有层次结构的数据字典**：\
第一层包含全部的单据头字段以及单据体行集合；\
单据体数据行集合、基础资料字段，则需要通过第二层的DynamicObject来展示。

基本特征：\
1\. 包含了全部单据头字段值\
2\. 包含了单据体行集合对象\
3\. 字段通过Key + Value，形成一个键值对，占据DynamicObject的一个节点\
4\. 字段在数据包中的Key，使用的是字段的属性名\
5\. 基础资料字段的值，也是一个DynamicObject对象，其中嵌套包含了各个引用属性的值

<br />

**4. 操作示例：**

//\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\
**方法一：直接操作DynamicObject**

// 假设billObj是单据的数据包\
DynamicObject billObj = this.Model.DataObject;\
\
\
// 读取单据内码\
long billId = Convert.ToInt64(billObj\[0]);\
// 普通文本字段（读取 + 设置）\
string fldBillNoValue = Convert.ToString(billObj\["BillNo"]);\
billObj\["BillNo"] = fldBillNoValue ;\
// 日期字段（读取 + 设置）\
DateTime fldDateValue = Convert.ToDateTime(billObj\["F\_JD\_Date"]);\
billObj\["F\_JD\_Date"] = fldDateValue;\
// 基础资料字段(读取 + 设置)\
DynamicObject fldSupplierValue = billObj\["F\_JD\_Supplier"] as DynamicObject;\
billObj\["F\_JD\_Supplier"] = fldSupplierValue ;\
if (fldSupplierValue != null)\
{ billObj\["F\_JD\_Supplier\_Id"] = Convert.ToInt64(fldSupplierValue\[0]);\
long supplierId = Convert.ToInt64(fldSupplierValue\[0]);\
string supplierNumber = fldSupplierValue\["Number"].ToString();\
string supplierName = fldSupplierValue\["Name"].ToString();\
}\
\
\
// 单据体（单据体行集合属性本身只读，可以通过单据体集合提供的方法，添加行、删除行）\
DynamicObjectCollection entityRows = billObj\["FEntity"] as DynamicObjectCollection;\
foreach (var entityRow in entity1Rows)\
{\
&#x20;// 内码\
long entityId = Convert.ToInt64(entityRow\[0]);\
&#x20;// 数量 （读取 + 设置）\
decimal fldQtyValue = Convert.ToDecimal(entityRow\["F\_JD\_Qty"]);\
&#x20;entityRow\["F\_JD\_Qty"] = fldQtyValue;\
}

//\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\
**方法二：通过字段的元数据，操作DynamicObject（推荐）**\
\
\
// 假设billObj是单据的数据包\
DynamicObject billObj = this.Model.DataObject;\
\
\
// 首先获取各种元素的元数据\
Field fldBillNo = this.View\.BillBusinessInfo.GetField("FBillNo");\
Field fldDate = this.View\.BillBusinessInfo.GetField("F\_JD\_Date");\
BaseDataField fldSupplier = this.View\.BillBusinessInfo.GetField("F\_JD\_Supplier") as BaseDataField;\
BaseDataField fldMaterial = this.View\.BillBusinessInfo.GetField("F\_JD\_FMaterialId") as BaseDataField;\
Field fldQty = this.View\.BillBusinessInfo.GetField("F\_JD\_Qty");\
Entity entity = this.ListView\.BillBusinessInfo.GetEntity("FEntity");

// 读取单据内码\
long billId = Convert.ToInt64(billObj\[0]);\
//单据编号\
string billNo = Convert.ToDateTime(fldBillNo.DynamicProperty.GetValue(billObj));\
fldBillNo.DynamicProperty.SetValue(billObj, billNo)); \
// 日期\
DateTime fldDateValue = Convert.ToDateTime(fldDate.DynamicProperty.GetValue(billObj));\
fldDate.DynamicProperty.SetValue(billObj, fldDateValue)\
// 供应商：基础资料字段\
DynamicObject fldSupplierValue = fldSupplier.DynamicProperty.GetValue(billObj) as DynamicObject;

// 设置供应商基础字段值\
DynamicObject\[] supplierObjs = Kingdee.BOS.ServiceHelper.BusinessDataServiceHelper.LoadFromCache(\
this.Context, \
new object\[] { fldSupplierValue\[0] }, \
fldSupplier.RefFormDynamicObjectType);

fldSupplier.RefIDDynamicProperty.SetValue(billObj, supplierObjs\[0]\[0]);\
fldSupplier.DynamicProperty.SetValue(billObj, supplierObjs\[0]);

// 基础资料属性值\
if (fldSupplierValue != null)\
{\
long supplierId = Convert.ToInt64(fldSupplierValue\[0]);\
string supplierNumber = fldSupplier.GetRefPropertyValue(fldSupplierValue, "FNumber").ToString();\
string supplierName = fldSupplier.GetRefPropertyValue(fldSupplierValue, "FName").ToString();\
}

// 单据体的字段\
DynamicObjectCollection entityRows = entity.DynamicProperty.GetValue(billObj) as DynamicObjectCollection;\
foreach (var entityRow in entityRows)\
{\
// 内码\
long entityId = Convert.ToInt64(entity1Row\[0]);\
// 物料：基础资料字段\
DynamicObject fldMaterialValue = fldMaterial.DynamicProperty.GetValue(entityRow) as DynamicObject;\
if (fldMaterialValue != null)\
{\
long materialId = Convert.ToInt64(fldMaterialValue\[0]);\
string materialNumber = fldMaterial.GetRefPropertyValue(fldMaterialValue, "FNumber").ToString();\
string materialName = fldMaterial.GetRefPropertyValue(fldMaterialValue, "FName").ToString();\
}\
// 数量\
decimal fldQtyValue = Convert.ToDecimal(fldQty.DynamicProperty.GetValue(entityRow));\
fldQty.DynamicProperty.SetValue(entityRow, fldQtyValue);\
}

// 给单据体添加新行\
DynamicObject newRow = new DynamicObject(entity.DynamicObjectType);\
entityRows.Add(newRow);
