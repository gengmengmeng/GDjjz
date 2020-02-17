using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Controller;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using MisFrameWork3.Classes.Membership;
using System.Collections;
using System.IO;
using System.Linq;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace MisFrameWork3.Areas.Card_QZ.Controllers
{
    public class Card_QZController : FWBaseController
    {
        // GET: Card_Init/Card_Init
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ViewFormAdd()
        {
            return View();
        }
        public ActionResult ViewCardDW()
        {
            return View();
        }
        public ActionResult ViewCardZT()
        {
            return View();
        }
        public ActionResult JsonConditionCombinationInfo()
        {
            return View();
        }

        public ActionResult JsonDataList()
        {
            int rows = 30;
            int page = 1;
            try
            {
                rows = int.Parse(Request["rows"]);
                page = int.Parse(Request["page"]);
            }
            catch (Exception ee)
            {
                rows = 30;
                page = 1;
                return Json(new { success = false, message = ee.Message }, JsonRequestBehavior.AllowGet);
            }
            string bignumber = (rows * page).ToString();
            string smallnumber = ((page - 1) * rows + 1).ToString();
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            string sql = "";
            string sql_number = "";
            if (Request["cdt_combination"] != null)
            {
                if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_FFJL"))
                {
                    sql = "select ROWNUM AS rowno,NO_ICK,NO_MACHINE,CXRQ,XM,SFZHM,MAKE_UNIT,MAKE_TIME,CXDWMC from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc nulls last) where NO_MACHINE in  " + GetMachineNo();
                    sql_number = "select count(*) from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc) where NO_MACHINE in  " + GetMachineNo();
                }
                else
                {
                    sql = "select ROWNUM AS rowno,NO_ICK,NO_MACHINE,CXRQ,XM,SFZHM,MAKE_UNIT,MAKE_TIME,CXDWMC from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc nulls last) where 1=1 ";
                    sql_number = "select count(*) from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc) where 1=1 ";
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
                        if (c.Src == "CXRQ")
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
                            sql += " " + c.Relate + " MAKE_TIME like '" + comId2 + "'";
                            sql_number += " " + c.Relate + " MAKE_TIME like '" + comId2 + "'";
                        }
                        else
                        {
                            sql += " " + c.Relate + " " + c.Src + " " + c.Op + " '" + c.Tag + "'";
                            sql_number += " " + c.Relate + " " + c.Src + " " + c.Op + " '" + c.Tag + "'";
                        }
                    }
                }
                if (Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_OWN_FFJL") && RoleLevel != 0)
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
                    sql_number += " AND NO_MACHINE in " + zzjbh;
                }
            }
            if (!string.IsNullOrEmpty(sql))
            {
                try
                {
                    //sql = "(" + sql + " and rownum<=" + bignumber + ") minus (" + sql + " and rownum<" + smallnumber + ")";
                    sql = "select * from (" + sql + " AND ROWNUM <=" + bignumber + ") p where p.rowno>=" + smallnumber;
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
                    //for (int i = 0; i < count; i++)
                    //{
                    //    string item = dt.Rows[i][0].ToString();
                    //}
                    conn.Close();
                    //数据获取
                    OracleConnection conn_number = new OracleConnection(connString);
                    conn_number.Open();
                    OracleCommand cmd_number = new OracleCommand(sql_number, conn_number);
                    cmd_number.CommandType = CommandType.Text;
                    DataSet ds_number = new DataSet();
                    OracleDataAdapter da_number = new OracleDataAdapter();
                    da_number.SelectCommand = cmd_number;
                    da_number.Fill(ds_number);
                    DataTable dt_number = new DataTable();
                    if (ds_number != null && ds_number.Tables.Count > 0)
                        dt_number = ds_number.Tables[0];
                    int count1 = dt_number.Rows.Count;
                    int item = 0;
                    if (count1 == 1)
                    {
                        item = Convert.ToInt32(dt_number.Rows[0][0].ToString());
                    }
                    conn_number.Close();
                    return JsonDateObject(new { total = item, rows = dt });
                }
                catch (Exception e)
                {
                    return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                string query_sql;
                string number_sql = "";
                string search = Request["search"];
                if (string.IsNullOrEmpty(search))
                {
                    //if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_ZZJL"))
                    //{
                    //    query_sql = "select SLBH,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP where  and ZZSBID in  " + GetMachineNo();
                    //}
                    //else
                    //{
                    //    query_sql = "select SLBH,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP where 1=1 ";
                    //}
                    if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_FFJL"))
                    {
                        query_sql = "select ROWNUM AS rowno,NO_ICK,NO_MACHINE,CXRQ,XM,SFZHM,MAKE_UNIT,MAKE_TIME,CXDWMC from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc nulls last) where NO_MACHINE in  " + GetMachineNo();
                        number_sql = "select count(*) from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc) where NO_MACHINE in  " + GetMachineNo();
                    }
                    else
                    {
                        query_sql = "select ROWNUM AS rowno,NO_ICK,NO_MACHINE,CXRQ,XM,SFZHM,MAKE_UNIT,MAKE_TIME,CXDWMC from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc nulls last) where 1=1";
                        number_sql = "select count(*) from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc) where 1=1";
                    }
                }
                else
                {
                    if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_FFJL"))
                    {
                        query_sql = "select ROWNUM AS rowno,NO_ICK,NO_MACHINE,CXRQ,XM,SFZHM,MAKE_UNIT,MAKE_TIME,CXDWMC from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc nulls last) where NO_MACHINE in  " + GetMachineNo() + " and ( NO_MACHINE like  '%" + search + "%' OR XM like  '%" + search + "%' OR CXDWMC like  '%" + search + "%' OR SFZHM like  '%" + search + "%'OR MAKE_UNIT like  '%" + search + "%' OR NO_ICK like  '%" + search + "%') ";
                        number_sql = "select count(*) from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc) where ZZSBID in  " + GetMachineNo() + " and ( NO_MACHINE like  '%" + search + "%' OR XM like  '%" + search + "%' OR CXDWMC like  '%" + search + "%' OR SFZHM like  '%" + search + "%'OR MAKE_UNIT like  '%" + search + "%' OR NO_ICK like  '%" + search + "%') ";

                    }
                    else
                    {
                        query_sql = "select ROWNUM AS rowno,NO_ICK,NO_MACHINE,CXRQ,XM,SFZHM,MAKE_UNIT,MAKE_TIME,CXDWMC from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc nulls last) where 1=1 and ( NO_MACHINE like  '%" + search + "%' OR XM like  '%" + search + "%' OR CXDWMC like  '%" + search + "%' OR SFZHM like  '%" + search + "%'OR MAKE_UNIT like  '%" + search + "%' OR NO_ICK like  '%" + search + "%') ";
                        number_sql = "select count(*) from (select * from  FX_LOG_QZ_T  order by MAKE_TIME desc) where 1=1 and ( NO_MACHINE like  '%" + search + "%' OR XM like  '%" + search + "%' OR CXDWMC like  '%" + search + "%' OR SFZHM like  '%" + search + "%'OR MAKE_UNIT like  '%" + search + "%' OR NO_ICK like  '%" + search + "%') ";

                    }

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
                    string[] arrDataRangeFields = new string[] { "擦写时间" };
                    if (dataRangeTypeIndex != 0)
                    {
                        fieldName = arrDataRangeFields[dataRangeTypeIndex - 1];
                        if (!String.IsNullOrEmpty(Request["start_date"]))
                        {
                            query_sql += " AND MAKE_TIME >=  TO_DATE('" + Request["start_date"].ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                            number_sql += " AND MAKE_TIME >=  TO_DATE('" + Request["start_date"].ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                        if (!String.IsNullOrEmpty(Request["end_date"]))
                        {
                            DateTime dtEndDate = DateTime.Parse(Request["end_date"]);
                            dtEndDate = dtEndDate.AddDays(1);//加多一天

                            query_sql += " AND MAKE_TIME <=  TO_DATE('" + dtEndDate.ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                            number_sql += " AND MAKE_TIME <=  TO_DATE('" + dtEndDate.ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                    }
                }

                if (Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_OWN_FFJL") && RoleLevel != 0)
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
                    number_sql += " AND NO_MACHINE in " + zzjbh;
                }
                try
                {
                    //query_sql = "(" + query_sql + " and rownum<=" + bignumber + ") minus (" + query_sql + " and rownum<" + smallnumber + ")";
                    query_sql = "select * from (" + query_sql + " AND ROWNUM <=" + bignumber + ") p where p.rowno>=" + smallnumber;
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
                    //for (int i = 0; i < count; i++)
                    //{
                    //    string item = dt.Rows[i][0].ToString();
                    //}
                    conn.Close();
                    //数据统计
                    OracleConnection conn_number = new OracleConnection(connString);
                    conn_number.Open();
                    OracleCommand cmd_number = new OracleCommand(number_sql, conn_number);
                    cmd_number.CommandType = CommandType.Text;
                    DataSet ds_number = new DataSet();
                    OracleDataAdapter da_number = new OracleDataAdapter();
                    da_number.SelectCommand = cmd_number;
                    da_number.Fill(ds_number);
                    DataTable dt_number = new DataTable();
                    if (ds_number != null && ds_number.Tables.Count > 0)
                        dt_number = ds_number.Tables[0];
                    int count1 = dt_number.Rows.Count;
                    int item = 0;
                    if (count1 == 1)
                    {
                        item = Convert.ToInt32(dt_number.Rows[0][0].ToString());
                    }
                    conn_number.Close();
                    return JsonDateObject(new { total = item, rows = dt });
                }
                catch (Exception ee)
                {
                    return Json(new { success = false, message = ee.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public ActionResult ActionAdd()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                /*
                __TIPS__*: 区取表相关的字段信息的方式有两种：
                    1、加载窗体提交的数据可以使用LoadFromNameValueCollection 结合正则表达式过滤掉没有用的数据
                        比如：data.LoadFromNameValueCollection(Request.Form, "NAME|TYPE|COMPANY_CODE",true);
                        这样只加载NAME、TYPE、COMPANY_CODE 这几项，其它项不处理
                    2、获取表信息，然后加只加载与表字段同名的内容，这个方法最常用，比如这样：
                        ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("FW_S_COMAPANIES");
                        data.LoadFromNameValueCollection(Request.Form, ti, true);
                    通过以上方式，data 里可以保留业务所需的数据。
                    因止，下面的内容只需要修改表名即可完成数据库操作。
                */
                ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("B_CARD_MAKE");
                data.LoadFromNameValueCollection(Request.Unvalidated.Form, ti, true);//使用Request.Unvalidated.Form可以POST HTML标签数据。
                session.BeginTransaction();
                int r = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_CARD_MAKE", data);               
                session.Commit();
                session.Close();
                if (0 == r)
                {
                    return Json(new { success = false, message = "保存信息时出错！" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                session.Rollback();
                session.Close();
                return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }
            var result = new { success = true, message = "保存成功" };
            return Json(result, JsonRequestBehavior.AllowGet);
        }






        public ActionResult JsonDicShort()
        {
            //__TIPS__:这里可以先过滤一下业务允许使用什么字典
            List<UnCaseSenseHashTable> records = GetDicData(Request["dic"], Request["filter"], null);
            return Json(records, JsonRequestBehavior.AllowGet);
        }

        public virtual ActionResult JsonDicLarge()
        {
            //__TIPS__:这里可以先过滤一下业务允许使用什么字典
            //return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC,PY", null, "*");
            if ("V_D_FW_COMP".Equals(Request["dic"]))
            {               
                Condition cdtId2 = new Condition("AND", "DISABLED", "=", 0);
                return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC,DW", cdtId2, "*");
            }
            else
           {
                return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC,PY", null, "*");
           }
            
        }

        public ActionResult ViewDicLargeUI()
        {
            /*
             * __TIPS__:有些特殊的字典可能需要显示更多的东西所以这里可以根据Request的值返回不同的视图
             *          以下演示根据字典内容，返回不同的视图。
             * */
            if ("V_D_FW_COMP".Equals(Request["dic"]))
                return View("ViewCardDW");
            else if ("D_CARDZT".Equals(Request["dic"]))
                return View("ViewCardZT");
            else
                return View("~/Views/Shared/ViewCommonDicUI.cshtml");
        }
    }
}