/* =============================================
   ERP客户信息视图
   功能：取客户名称、客户编码、使用组织、使用组织编码
   创建日期：2026-03-18
   ============================================= */

CREATE OR ALTER VIEW V_ERP_CUSTOMER_INFO AS
SELECT 
    c.FCUSTID AS 客户内码,
    cl.FNAME AS 客户名称,
    c.FNUMBER AS 客户编码,
    ol.FNAME AS 使用组织,
    o.FNUMBER AS 使用组织编码
FROM T_BD_CUSTOMER c
LEFT JOIN T_BD_CUSTOMER_L cl ON c.FCUSTID = cl.FCUSTID AND cl.FLOCALEID = 2052
LEFT JOIN T_ORG_ORGANIZATIONS o ON c.FUSEORGID = o.FORGID
LEFT JOIN T_ORG_ORGANIZATIONS_L ol ON o.FORGID = ol.FORGID AND ol.FLOCALEID = 2052
WHERE c.FDOCUMENTSTATUS = 'C'
  AND c.FFORBIDSTATUS = 'A'


GO

/* 视图说明：
   1. 客户内码：来自客户主表 T_BD_CUSTOMER.FCUSTID
   2. 客户名称：来自客户多语言表 T_BD_CUSTOMER_L.FNAME
   3. 客户编码：来自客户主表 T_BD_CUSTOMER.FNUMBER
   4. 使用组织：来自组织多语言表 T_ORG_ORGANIZATIONS_L.FNAME
   5. 使用组织编码：来自组织主表 T_ORG_ORGANIZATIONS.FNUMBER
   6. 过滤条件：
      - FDOCUMENTSTATUS = 'C'：已审核状态
      - FFORBIDSTATUS = 'A'：未禁用状态
   7. 多语言表FLOCALEID = 2052 表示中文
*/
