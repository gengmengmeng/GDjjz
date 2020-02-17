using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Reflection;

namespace MisFrameWork3.Areas.ZZJLCX.Controllers
{
    public class DWZZJLController : FWBaseController
    {
        // GET: ZZJLCX/SBZZJL
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult JsonConditionCombinationInfo()
        {
            return View();
        }
        #region __TIPS__:单位制证数量统计
        public ActionResult AcceptStat()
        {
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            string sql = "";
            if (Request["cdt_combination"] != null)
            {
                if (Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_ALL_ZZJL"))
                {
                    sql = "select FFDWDM,FFDWMC,count(*) as count from FX_LOG_ICK_T where 1=1 ";
                }
                else
                {
                    string comId2 = "";
                    string COMPANY_ID = Membership.CurrentUser.CompanyId.ToString();
                    char[] c = COMPANY_ID.ToCharArray();
                    string comId = "";
                    bool temp = false;
                    for (int i = c.Length - 1; i >= 0; i--)
                    {
                        string cc = c[i].ToString();
                        if (cc != "0" && !temp)
                        {
                            temp = true;
                        }
                        if (temp)
                        {
                            comId += c[i];
                        }
                    }
                    char[] charArray = comId.ToCharArray();
                    Array.Reverse(charArray);
                    comId2 = new String(charArray);
                    comId2 += "%";

                    sql = "select FFDWDM,FFDWMC,count(*) as count from FX_LOG_ICK_T where FFDWDM like '" + comId2 + "'";

                }
                string jsoncdtCombination = System.Text.ASCIIEncoding.UTF8.GetString(Convert.FromBase64String(Request["cdt_combination"]));
                Condition cdtCombination = Condition.LoadFromJson(jsoncdtCombination);
                cdtCombination.Relate = "AND";
                ReplaceCdtCombinationOpreate(cdtCombination);
                int count = cdtCombination.SubConditions.Count;
                if (count != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Condition c = cdtCombination.SubConditions[i];
                        if (c.Src == "CJSJ")
                        {
                            sql += " " + c.Relate + " " + c.Src + " " + c.Op + "  TO_DATE('" + c.Tag + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                        if (c.Src == "COMPANY_ID_1")
                        {
                            string comId2 = "";                     
                            char[] a = c.Tag.ToString().ToCharArray();
                            string comId = "";
                            bool temp = false;
                            for (int j = a.Length - 2; j >= 0; j--)
                            {
                                string cc = a[j].ToString();
                                if (cc != "0" && !temp)
                                {
                                    temp = true;
                                }
                                if (temp)
                                {
                                    comId += a[j];
                                }
                            }
                            char[] charArray = comId.ToCharArray();
                            Array.Reverse(charArray);
                            comId2 = new String(charArray);
                            comId2 += "%";
                            sql += " " + c.Relate + " FFDWDM like '" + comId2 + "'";
                        }
                        else
                        {
                            sql += " " + c.Relate + " " + c.Src + " " + c.Op + " '" + c.Tag + "'";
                        }
                    }
                }

                if (Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_OWN_ZZJL") && RoleLevel != 0)
                {
                    string userId = Membership.CurrentUser.UserId;
                    Condition cdtId = new Condition();
                    List<UnCaseSenseHashTable> record;
                    cdtId.AddSubCondition("AND", "SBFZR", "=", userId);
                    record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtId, "MACHINENO", null, null, -1, -1);                
                    string zzjbh = "(";
                    for (int i = 0; i < record.Count; i++)
                     {
                    zzjbh += "'" + record[i]["MACHINENO"] + "',";
                     }
                    zzjbh += "'0')";
                sql += " AND NO_MACHINE in " + zzjbh;

                }
                sql += "  group by FFDWDM,FFDWDMMC";
            }
            //连接数据库查询
            if (!string.IsNullOrEmpty(sql))
            {
                try
                {
                    string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                    OracleConnection conn = new OracleConnection(connString);
                    conn.Open();    
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.CommandType = CommandType.Text;
                    DataSet ds = new DataSet();
                    OracleDataAdapter da = new OracleDataAdapter();
                    da.SelectCommand = cmd;
                    da.Fill(ds);

                    DataTable dt = new DataTable();
                    if (ds != null && ds.Tables.Count > 0)
                        dt = ds.Tables[0];
                    int count = dt.Rows.Count;
                    for (int i = 0; i < count; i++)
                    {
                        string item = dt.Rows[i][0].ToString();
                    }
                    conn.Close();
                    return JsonDateObject(dt);
                }
                catch(Exception e){
                     return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                string query_sql;
                string search = Request["search"];

                if (string.IsNullOrEmpty(search))
                {
                    query_sql = "select FFDWDM,FFDWMC,count(*) as count from FX_LOG_ICK_T where 1=1 ";
                }
                else
                {
                    query_sql = "select FFDWDM,FFDWMC,count(*) as count from FX_LOG_ICK_T where 1=1  and ( FFDWDM like  '%" + search + "%' OR FFDWMC like  '%" + search + "%') ";
                }
                if (!Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_ALL_ZZJL"))
                {
                    string comId2 = "";
                    string COMPANY_ID = Membership.CurrentUser.CompanyId.ToString();
                    char[] c = COMPANY_ID.ToCharArray();
                    string comId = "";
                    bool temp = false;
                    for (int i = c.Length - 1; i >= 0; i--)
                    {
                        string cc = c[i].ToString();
                        if (cc != "0" && !temp)
                        {
                            temp = true;
                        }
                        if (temp)
                        {
                            comId += c[i];
                        }
                    }
                    char[] charArray = comId.ToCharArray();
                    Array.Reverse(charArray);
                    comId2 = new String(charArray);
                    comId2 += "%";

                    query_sql += " and FFDWDM like '" + comId2 + "'";
                }

                if (Request["date_range_type"] != null && (Request["start_date"] != null || Request["end_date"] != null))
                {
                    string fieldName = null;
                    int dataRangeTypeIndex = 0;
                    try
                    {
                        dataRangeTypeIndex = int.Parse(Request["date_range_type"]);
                    }
                    catch (Exception e)
                    {
                        dataRangeTypeIndex = 0;
                    }
                    string[] arrDataRangeFields = new string[] { "制证时间" };
                    if (dataRangeTypeIndex != 0)
                    {
                        fieldName = arrDataRangeFields[dataRangeTypeIndex - 1];
                        if (!String.IsNullOrEmpty(Request["start_date"]))
                        {
                            query_sql += " AND CJSJ >=  TO_DATE('" + Request["start_date"].ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                        if (!String.IsNullOrEmpty(Request["end_date"]))
                        {
                            DateTime dtEndDate = DateTime.Parse(Request["end_date"]);
                            dtEndDate = dtEndDate.AddDays(1);//加多一天

                            query_sql += " AND CJSJ <=  TO_DATE('" + dtEndDate.ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                    }
                }
                if (Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_OWN_ZZJL") && RoleLevel != 0)
                {
                    string userId = Membership.CurrentUser.UserId;
                    Condition cdtId = new Condition();
                    List<UnCaseSenseHashTable> record;
                    cdtId.AddSubCondition("AND", "SBFZR", "=", userId);
                    record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtId, "MACHINENO", null, null, -1, -1);
                    string zzjbh = "(";
                    for (int i = 0; i < record.Count; i++)
                    {
                        zzjbh += "'" + record[i]["MACHINENO"] + "',";
                    }
                    zzjbh += "'0')";
                    query_sql += " AND NO_MACHINE in " + zzjbh;
                }
                query_sql += " group by FFDWDM,FFDWMC";
                try
                {
                    string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                    OracleConnection conn = new OracleConnection(connString);
                    conn.Open();
                    OracleCommand cmd = new OracleCommand(query_sql, conn);
                    cmd.CommandType = CommandType.Text;
                    DataSet ds = new DataSet();
                    OracleDataAdapter da = new OracleDataAdapter();
                    da.SelectCommand = cmd;
                    da.Fill(ds);
                    DataTable dt = new DataTable();
                    if (ds != null && ds.Tables.Count > 0)
                        dt = ds.Tables[0];
                    int count = dt.Rows.Count;
                    for (int i = 0; i < count; i++)
                    {
                        string item = dt.Rows[i][0].ToString();
                    }
                    conn.Close();
                    return JsonDateObject(dt);
                }
                catch(Exception ee){
                    return Json(new { success = false, message = ee.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }
        #endregion
        #region __TIPS__:框架通用函数 ( 字典控件相关 )
        public ActionResult JsonDicShort()
        {
            //__TIPS__:这里可以先过滤一下业务允许使用什么字典
            List<UnCaseSenseHashTable> records = GetDicData(Request["dic"], null);
            return Json(records, JsonRequestBehavior.AllowGet);
        }

        public ActionResult JsonDicLarge()
        {
            //__TIPS__:这里可以先过滤一下业务允许使用什么字典
            return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC", null, "*");
        }

        public ActionResult ViewDicLargeUI()
        {
            /*
             * __TIPS__:有些特殊的字典可能需要显示更多的东西所以这里可以根据Request的值返回不同的视图
             *          以下演示根据字典内容，返回不同的视图。
             * */
            if ("V_D_FW_COMP".Equals(Request["dic"]))
            {
                return View("ViewDwzzjlType");
            }
            else
            {
                return View("~/Views/Shared/ViewCommonDicUI.cshtml");
            }
        }
        #endregion

        #region 打印数据
        public FileResult ActionPrint(string name, string oject_id)
        {
            //获取数据
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            string QuerySql = "";
            string sql = "";
            if (!string.IsNullOrEmpty(Request["cdt_combination"]))
            {
                sql = "select ZZXXZZDW,ZZXXZZDWMC,count(*) as count from C_JZZ_TMP where 1=1 ";
                
                string jsoncdtCombination = System.Text.ASCIIEncoding.UTF8.GetString(Convert.FromBase64String(Request["cdt_combination"]));
                Condition cdtCombination = Condition.LoadFromJson(jsoncdtCombination);
                cdtCombination.Relate = "AND";
                ReplaceCdtCombinationOpreate(cdtCombination);
                int count = cdtCombination.SubConditions.Count;
                if (count != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Condition c = cdtCombination.SubConditions[i];
                        if (c.Src == "ZZXXZZRQ")
                        {
                            sql += " " + c.Relate + " " + c.Src + " " + c.Op + "  TO_DATE('" + c.Tag + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                        else
                        {
                            sql += " " + c.Relate + " " + c.Src + " " + c.Op + " '" + c.Tag + "'";
                        }
                    }
                }
                if (!Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_ALL_ZZJL"))
                {
                    string COMPANY_ID = Membership.CurrentUser.CompanyId.ToString();
                    char[] c = COMPANY_ID.ToCharArray();
                    string comId = "";
                    bool temp = false;
                    for (int i = c.Length - 1; i >= 0; i--)
                    {
                        string cc = c[i].ToString();
                        if (cc != "0" && !temp)
                        {
                            temp = true;
                        }
                        if (temp)
                        {
                            comId += c[i];
                        }
                    }
                    char[] charArray = comId.ToCharArray();
                    Array.Reverse(charArray);
                    string comId3 = new String(charArray);
                    comId3 += "%";
                    sql += " AND ZZXXZZDW like '" + comId3 + "'";
                }
                if (Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_OWN_ZZJL") && RoleLevel != 0)
                {
                    string userId = Membership.CurrentUser.UserId;
                    sql += " AND ZZSBID in (select MACHINENO from B_MACHINE where SBFZR = '" + userId + "')";
                }
                sql += "  group by ZZXXZZDW,ZZXXZZDWMC";
            }
            if (!string.IsNullOrEmpty(sql))
            {
                QuerySql = sql;
            }
            else
            {
                string search = Request["Search"];
                string date_range_type = Request["date_range_type"];
                string start_date = Request["start_date"];
                string end_date = Request["end_date"];
                string query_sql;
                if (!string.IsNullOrEmpty(search))
                {
                    query_sql = "select ZZXXZZDW,ZZXXZZDWMC,count(*) as count from C_JZZ_TMP where 1=1 and (ZZXXZZDW like  '%" + search + "%' or ZZXXZZDWMC like  '%" + search + "%') ";
                }
                else
                {
                    query_sql = "select ZZXXZZDW,ZZXXZZDWMC,count(*) as count from C_JZZ_TMP where 1=1 ";
                }

                if (!string.IsNullOrEmpty(date_range_type) && date_range_type != "0" && (!string.IsNullOrEmpty(start_date) || !string.IsNullOrEmpty(end_date)))
                {
                    if (!String.IsNullOrEmpty(start_date))
                    {
                        query_sql += " AND ZZXXZZRQ >=  TO_DATE('" + start_date + "', 'YYYY-MM-DD HH24:MI:SS') ";
                    }
                    if (!String.IsNullOrEmpty(end_date))
                    {
                        DateTime dtEndDate = DateTime.Parse(end_date);
                        dtEndDate = dtEndDate.AddDays(1);//加多一天

                        query_sql += " AND ZZXXZZRQ <=  TO_DATE('" + dtEndDate.ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                    }
                }

                if (!Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_ALL_ZZJL"))
                {
                    string COMPANY_ID = Membership.CurrentUser.CompanyId.ToString();
                    char[] c = COMPANY_ID.ToCharArray();
                    string comId = "";
                    bool temp = false;
                    for (int i = c.Length - 1; i >= 0; i--)
                    {
                        string cc = c[i].ToString();
                        if (cc != "0" && !temp)
                        {
                            temp = true;
                        }
                        if (temp)
                        {
                            comId += c[i];
                        }
                    }
                    char[] charArray = comId.ToCharArray();
                    Array.Reverse(charArray);
                    string comId3 = new String(charArray);
                    comId3 += "%";
                    query_sql += " AND ZZXXZZDW like '" + comId3 + "'";
                }
                if (Membership.CurrentUser.HaveAuthority("ZZJL.DWZZJL.QUERY_OWN_ZZJL") && RoleLevel != 0)
                {
                    string userId = Membership.CurrentUser.UserId;
                    query_sql += " AND ZZSBID in (select MACHINENO from B_MACHINE where SBFZR = '" + userId + "')";
                }
                query_sql += " group by ZZXXZZDW,ZZXXZZDWMC";

                QuerySql = query_sql;
            }
            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
            OracleConnection conn = new OracleConnection(connString);
                conn.Open();   
                OracleCommand cmd = new OracleCommand(QuerySql, conn);
                cmd.CommandType = CommandType.Text;
                DataSet ds = new DataSet();
                OracleDataAdapter da = new OracleDataAdapter();
                da.SelectCommand = cmd;
                da.Fill(ds);

                DataTable dt = new DataTable();
                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
                int count_dt = dt.Rows.Count;
                for (int i = 0; i < count_dt; i++)
                {
                    string item = dt.Rows[i][0].ToString();
                    string item1 = dt.Rows[i][1].ToString();
                    string item2 = dt.Rows[i][2].ToString();
                }
                conn.Close();      
            //List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query(QuerySql, -1, -1);

            //设置打印图纸大小
            Document document = new Document(PageSize.A4);
            //设置页边距
            document.SetMargins(36, 36, 36, 60);
            //中文字体
            string chinese = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SIMSUN.TTC,1");
            BaseFont baseFont = BaseFont.CreateFont(chinese, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            //文字大小12，文字样式
            Font cn = new Font(baseFont, 14, Font.NORMAL);

            //PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(@"D:\temp.pdf", FileMode.Create));

            //这样写：是生成文件到内存中去
            var memoryStream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);//生成到内存中
            //writer.PageEvent = new PdfPageHelper();//页脚
            document.Open();//打开文件


            //Paragraph title = new Paragraph("国家工作人员登记备案表", new Font(baseFont, 23, Font.BOLD, BaseColor.BLACK));
            Paragraph title = new Paragraph("", new Font(baseFont, 23, Font.BOLD, BaseColor.BLACK));
            title.Alignment = Element.ALIGN_CENTER; //居中
            title.SpacingAfter = 20;
            document.Add(title);

            //数据表格
            PdfPTable table = new PdfPTable(4);           
            table.SetWidths(new float[] { 2.5F, 8, 8 ,8});
            table.WidthPercentage = 100;
            AddBodyContentCell(table, "序号", cn);
            AddBodyContentCell(table, "制证单位编号", cn);
            AddBodyContentCell(table, "制证单位名称", cn);
            AddBodyContentCell(table, "数量", cn);

            for (int i = 0; i < count_dt; i++)
            {
                //UnCaseSenseHashTable record =records[i];
                AddBodyContentCell(table, Convert.ToString(i + 1), cn);
                if (!string.IsNullOrEmpty(dt.Rows[i][0].ToString()))
                {
                    AddBodyContentCell(table, dt.Rows[i][0].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty(dt.Rows[i][1].ToString()))
                {
                    AddBodyContentCell(table, dt.Rows[i][1].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }
                if (dt.Rows[i][2].ToString()!= null)
                {
                    if (!string.IsNullOrEmpty(dt.Rows[i][2].ToString()))
                    {
                        AddBodyContentCell(table, dt.Rows[i][2].ToString(), cn);
                    }
                    else
                    {
                        AddBodyContentCell(table, "", cn);
                    }
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }
            }
            document.Add(table);

            document.Close();

            var bytes = memoryStream.ToArray();
            //result = Convert.ToBase64String(bytes);

            return File(bytes, "application/pdf");
        }

        private void AddBodyContentCell(PdfPTable bodyTable, String text, iTextSharp.text.Font font, int rowspan = 2, bool needRightBorder = false)
        {
            PdfPCell cell = new PdfPCell();
            //float defaultBorder = 0.5f;
            //cell.BorderWidthLeft = defaultBorder;
            //cell.BorderWidthTop = 0;
            //cell.BorderWidthRight = needRightBorder ? defaultBorder : 0;
            //cell.BorderWidthBottom = defaultBorder;
            cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
            cell.VerticalAlignment = iTextSharp.text.Element.ALIGN_BASELINE;
            //cell.Rowspan = rowspan;
            cell.PaddingBottom = 3;
            cell.Phrase = new Phrase(text, font);
            bodyTable.AddCell(cell);
        }      
        #endregion
    }
}