/* =============================================
   收款单信息视图
   功能：取收款单列表信息
   字段：单据编号、收款用途、业务日期、收款金额、付款单位
   创建日期：2026-03-17
   ============================================= */

CREATE OR ALTER VIEW V_RECEIVEBILL_INFO AS
SELECT 
    CONCAT(h.FID, p.FID) AS PKID,
    h.FBILLNO AS 单据编号,
    pl.FNAME AS 收款用途,
    h.FDATE AS 业务日期,
    h.FRECAMOUNTFOR AS 收款金额,
    CASE 
        WHEN h.FPAYUNITTYPE = 'BD_Supplier' THEN ISNULL(s.FNAME, '')
        WHEN h.FPAYUNITTYPE = 'BD_Customer' THEN ISNULL(c.FNAME, '')
        WHEN h.FPAYUNITTYPE = 'BD_Department' THEN ISNULL(d.FNAME, '')
        WHEN h.FPAYUNITTYPE = 'BD_Empinfo' THEN ISNULL(emp.FNAME, '')
        WHEN h.FPAYUNITTYPE = 'FIN_OTHERS' THEN ISNULL(o.FNAME, '')
        WHEN h.FPAYUNITTYPE = 'ORG_Organizations' THEN ISNULL(org.FNAME, '')
        WHEN h.FPAYUNITTYPE = 'BD_BANK' THEN ISNULL(b.FNAME, '')
        ELSE ''
    END AS 付款单位
FROM T_AR_RECEIVEBILL h
LEFT JOIN T_AR_RECEIVEBILLENTRY e ON h.FID = e.FID
LEFT JOIN T_CN_RECPAYPURPOSE p ON e.FPURPOSEID = p.FID
LEFT JOIN T_CN_RECPAYPURPOSE_L pl ON p.FID = pl.FID AND pl.FLOCALEID = 2052
LEFT JOIN T_BD_SUPPLIER_L s ON h.FPAYUNIT = s.FSUPPLIERID AND h.FPAYUNITTYPE = 'BD_Supplier' AND s.FLOCALEID = 2052
LEFT JOIN T_BD_CUSTOMER_L c ON h.FPAYUNIT = c.FCUSTID AND h.FPAYUNITTYPE = 'BD_Customer' AND c.FLOCALEID = 2052
LEFT JOIN T_BD_DEPARTMENT_L d ON h.FPAYUNIT = d.FDEPTID AND h.FPAYUNITTYPE = 'BD_Department' AND d.FLOCALEID = 2052
LEFT JOIN T_HR_EMPINFO_L emp ON h.FPAYUNIT = emp.FID AND h.FPAYUNITTYPE = 'BD_Empinfo'
LEFT JOIN T_FIN_OTHERS_L o ON h.FPAYUNIT = o.FID AND h.FPAYUNITTYPE = 'FIN_OTHERS' AND o.FLOCALEID = 2052
LEFT JOIN T_ORG_ORGANIZATIONS_L org ON h.FPAYUNIT = org.FORGID AND h.FPAYUNITTYPE = 'ORG_Organizations' AND org.FLOCALEID = 2052
LEFT JOIN T_BD_BANK_L b ON h.FPAYUNIT = b.FBANKID AND h.FPAYUNITTYPE = 'BD_BANK' AND b.FLOCALEID = 2052;

GO

/* 视图说明：
   1. PKID：主键ID，由单据头ID和收款用途ID拼接而成
   2. 单据编号：来自收款单单据头表 T_AR_RECEIVEBILL
   3. 收款用途：来自收款单明细表，关联收付款用途基础资料表
      - T_CN_RECPAYPURPOSE：收付款用途主表
      - T_CN_RECPAYPURPOSE_L：收付款用途多语言表
   4. 业务日期：来自收款单单据头表
   5. 收款金额：来自收款单单据头表（原币金额）
   6. 付款单位：根据付款单位类型动态关联不同的基础资料表
      - BD_Supplier：供应商 T_BD_SUPPLIER_L
      - BD_Customer：客户 T_BD_CUSTOMER_L
      - BD_Department：部门 T_BD_DEPARTMENT_L
      - BD_Empinfo：员工 T_HR_EMPINFO_L
      - FIN_OTHERS：其他往来单位 T_FIN_OTHERS_L
      - ORG_Organizations：组织机构 T_ORG_ORGANIZATIONS_L
      - BD_BANK：银行 T_BD_BANK_L
   7. 多语言表FLOCALEID = 2052 表示中文
*/
