using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace MisFrameWork3.Areas.ZZJLCX.Controllers
{
    public class TCZZJLCXController : FWBaseController
    {
        // GET: ZZJLCX/ZZJLCX
        public ActionResult Index()
        {
            ViewBag.HideCommonButtons = false;
            return View();
        }
        public ActionResult ViewFormEdit()
        {
            return View();
        }
        public ActionResult ViewErrorType()
        {
            return View();
        }
        public ActionResult ViewStateType()
        {
            return View();
        }
        public ActionResult JsonConditionCombinationInfo()
        {
            return View();
        }
        public ActionResult ViewFormTCPhoto()
        {
            return View();
        }


        public ActionResult GetPhoto()//业务主界面数据查询函数
        {
            //接收的基本查询参数有： id,limit,offset,search,sort,order            
            //__TIPS__*:根据表结构，修改以下函数的参数
            string sql = "";
            string SLBH = Request["SLBH"].ToString();
            sql = "select ZP from C_JZZ_ZP_TC  where SLBH = '" + SLBH + "'";
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
                Hashtable hash = new Hashtable();
                for (int i = 0; i < count; i++)
                {
                    byte[] imgByte = (byte[])dt.Rows[i][0];
                    string url = Server.MapPath("/Content/images/");
                    string savepath = url + SLBH + ".png";
                    if (System.IO.File.Exists(savepath))
                    {
                        hash["photoUrl"] = "/Content/images/" + SLBH + ".png";
                        conn.Close();
                        return JsonDateObject(hash);
                    }
                    FileStream fs = new FileStream(savepath, FileMode.CreateNew);
                    BinaryWriter bw = new BinaryWriter(fs);
                    bw.Write(imgByte, 0, imgByte.Length);
                    bw.Close();
                    fs.Close();
                    hash["photoUrl"] = "/Content/images/" + SLBH + ".png";
                }
                conn.Close();
                return JsonDateObject(hash);
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = ee.Message }, JsonRequestBehavior.AllowGet);
            }
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
                if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_ZZJL"))
                {
                    sql = "select ROWNUM AS rowno,SLBH,XM,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG,SLBH as PHOTO from (select * from C_JZZ_TMP_TC  order by ZZXXZZRQ desc nulls last) where ZZSBID in  " + GetMachineNo();
                    sql_number = "select count(*) from C_JZZ_TMP_TC where ZZSBID in  " + GetMachineNo();
                }
                else
                {
                    sql = "select ROWNUM AS rowno,SLBH,XM,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG,SLBH as PHOTO from (select * from C_JZZ_TMP_TC  order by ZZXXZZRQ desc nulls last) where 1=1 ";
                    sql_number = "select count(*) from C_JZZ_TMP_TC where 1=1 ";
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
                        if (c.Src == "ZZXXZZRQ")
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
                            sql += " " + c.Relate + " ZZXXZZDW like '" + comId2 + "'";
                            sql_number += " " + c.Relate + " ZZXXZZDW like '" + comId2 + "'";
                        }
                        else
                        {
                            sql += " " + c.Relate + " " + c.Src + " " + c.Op + " '" + c.Tag + "'";
                            sql_number += " " + c.Relate + " " + c.Src + " " + c.Op + " '" + c.Tag + "'";
                        }
                    }
                }
                if (Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_OWN_ZZJL") && RoleLevel != 0)
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
                    sql += " AND ZZSBID in " + zzjbh;
                    sql_number += " AND ZZSBID in " + zzjbh;
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
                    for (int i = 0; i < count; i++)
                    {
                        string item = dt.Rows[i][0].ToString();
                    }
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
                    for (int i = 0; i < count1; i++)
                    {
                        string item = dt_number.Rows[i][0].ToString();
                    }
                    conn_number.Close();
                    return JsonDateObject(new { total = count1, rows = dt });
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
                    if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_ZZJL"))
                    {
                        query_sql = "select ROWNUM AS rowno,SLBH,XM,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG,SLBH as PHOTO from (select * from C_JZZ_TMP_TC  order by ZZXXZZRQ desc nulls last) where ZZSBID in  " + GetMachineNo();
                        number_sql = "select count(*) from C_JZZ_TMP_TC where ZZSBID in  " + GetMachineNo();
                    }
                    else
                    {
                        query_sql = "select ROWNUM AS rowno,SLBH,XM,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG,SLBH as PHOTO from (select * from C_JZZ_TMP_TC  order by ZZXXZZRQ desc nulls last) where 1=1";
                        number_sql = "select count(*) from C_JZZ_TMP_TC where 1=1";
                    }
                }
                else
                {
                    if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_ZZJL"))
                    {
                        query_sql = "select ROWNUM AS rowno,SLBH,XM,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG,SLBH as PHOTO from (select * from C_JZZ_TMP_TC  order by ZZXXZZRQ desc nulls last) where ZZSBID in  " + GetMachineNo() + " and ( ZZSBID like  '%" + search + "%' OR ZZXXZZDW like  '%" + search + "%' OR ZZXXZZDWMC like  '%" + search + "%'OR SLBH like  '%" + search + "%' OR GMSFHM like  '%" + search + "%') ";
                        number_sql = "select count(*) from C_JZZ_TMP_TC where ZZSBID in  " + GetMachineNo() + " and ( ZZSBID like  '%" + search + "%' OR ZZXXZZDW like  '%" + search + "%' OR ZZXXZZDWMC like  '%" + search + "%'OR SLBH like  '%" + search + "%' OR GMSFHM like  '%" + search + "%') ";

                    }
                    else
                    {
                        query_sql = "select ROWNUM AS rowno,SLBH,XM,ZZZXPH,GMSFHM,ZZXXZZDW,ZZSBID,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG,SLBH as PHOTO from (select * from C_JZZ_TMP_TC  order by ZZXXZZRQ desc nulls last)  where 1=1 and ( ZZSBID like  '%" + search + "%' OR ZZXXZZDW like  '%" + search + "%' OR ZZXXZZDWMC like  '%" + search + "%'OR SLBH like  '%" + search + "%'OR GMSFHM like  '%" + search + "%') ";
                        number_sql = "select count(*) from C_JZZ_TMP_TC where 1=1 and ( ZZSBID like  '%" + search + "%' OR ZZXXZZDW like  '%" + search + "%' OR ZZXXZZDWMC like  '%" + search + "%'OR SLBH like  '%" + search + "%'OR GMSFHM like  '%" + search + "%') ";

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
                    string[] arrDataRangeFields = new string[] { "制证时间" };
                    if (dataRangeTypeIndex != 0)
                    {
                        fieldName = arrDataRangeFields[dataRangeTypeIndex - 1];
                        if (!String.IsNullOrEmpty(Request["start_date"]))
                        {
                            query_sql += " AND ZZXXZZRQ >=  TO_DATE('" + Request["start_date"].ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                            number_sql += " AND ZZXXZZRQ >=  TO_DATE('" + Request["start_date"].ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                        if (!String.IsNullOrEmpty(Request["end_date"]))
                        {
                            DateTime dtEndDate = DateTime.Parse(Request["end_date"]);
                            dtEndDate = dtEndDate.AddDays(1);//加多一天

                            query_sql += " AND ZZXXZZRQ <=  TO_DATE('" + dtEndDate.ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                            number_sql += " AND ZZXXZZRQ <=  TO_DATE('" + dtEndDate.ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                    }
                }

                if (Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_OWN_ZZJL") && RoleLevel != 0)
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
                    query_sql += " AND ZZSBID in " + zzjbh;
                    number_sql += " AND ZZSBID in " + zzjbh;
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

        public ActionResult JsonDataList1()//业务主界面数据查询函数
        {
            //接收的基本查询参数有： id,limit,offset,search,sort,order            
            //__TIPS__*:根据表结构，修改以下函数的参数
            string sql = "";
            string SLBH = Request["SLBH"].ToString();
            sql = "select XM,XB,MZMC,CSRQ,GMSFHM,RESERVATION01,RESERVATION02,FFDWMC,RESERVATION36,SLBH,ZZXXZZSFCG,JZZYXQQSRQ,JZZYXQJZRQ,FFDWMC,ZZXXZZDWMC,RESERVATION37 from C_JZZ_TMP_TC  where SLBH = '" + SLBH + "'";
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
                UnCaseSenseHashTable data = new UnCaseSenseHashTable();
                for (int i = 0; i < count; i++)
                {

                    data["XM"] = dt.Rows[i][0].ToString();
                    if (dt.Rows[i][1].ToString() == "1")
                    {
                        data["XB"] = "男";
                    }
                    if (dt.Rows[i][1].ToString() == "2")
                    {
                        data["XB"] = "女";
                    }
                    if (dt.Rows[i][3].ToString().Length >= 11 && dt.Rows[i][11].ToString().Length >= 11 && dt.Rows[i][12].ToString().Length >= 11 && dt.Rows[i][15].ToString().Length >= 11)
                    {
                        data["CSRQ"] = dt.Rows[i][3].ToString().Substring(0, 10);
                        data["JZZYXQQSRQ"] = dt.Rows[i][11].ToString().Substring(0, 10);
                        data["JZZYXQJZRQ"] = dt.Rows[i][12].ToString().Substring(0, 10);
                        data["RESERVATION37"] = dt.Rows[i][15].ToString().Substring(0, 10);
                    }
                    else
                    {
                        data["CSRQ"] = dt.Rows[i][3].ToString();
                        data["JZZYXQQSRQ"] = dt.Rows[i][11].ToString();
                        data["JZZYXQJZRQ"] = dt.Rows[i][12].ToString();
                    }
                    data["MZMC"] = dt.Rows[i][2].ToString();
                    data["GMSFHM"] = dt.Rows[i][4].ToString();
                    data["RESERVATION01"] = dt.Rows[i][5].ToString();
                    data["RESERVATION02"] = dt.Rows[i][6].ToString();
                    data["FFDWMC"] = dt.Rows[i][7].ToString();
                    if (dt.Rows[i][8].ToString().Length >= 11)
                    {
                        data["RESERVATION36"] = dt.Rows[i][8].ToString().Substring(0, 10);
                    }
                    else
                    {
                        data["RESERVATION36"] = dt.Rows[i][8].ToString();
                    }
                    data["SLBH"] = dt.Rows[i][9].ToString();
                    if (dt.Rows[i][10].ToString() == "1")
                    {
                        data["ZZXXZZSFCG"] = "成功";
                    }
                    if (dt.Rows[i][10].ToString() == "0")
                    {
                        data["ZZXXZZSFCG"] = "失败";
                    }
                    if (dt.Rows[i][10].ToString() != "1" && dt.Rows[i][10].ToString() != "0")
                    {
                        data["ZZXXZZSFCG"] = "未制证";
                    }
                    data["FFDWMC"] = dt.Rows[i][13].ToString();
                    data["ZZXXZZDWMC"] = dt.Rows[i][14].ToString();
                }
                conn.Close();
                return JsonDateObject(data);
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = ee.Message }, JsonRequestBehavior.AllowGet);
            }
        }

       
        //public ActionResult ActionEdit()
        //{          
        //    try
        //    {
        //        Condition cdtId = new Condition();
        //        string ZZXXZZSFCG = Request["ZZXXZZSFCG"].ToString();
        //        string ZKSBYY = Request["ZKSBYY"].ToString();               
        //        string UserId = Membership.CurrentUser.UserId;                
        //        string sql = "";
        //        string SLBH = Request["SLBH"].ToString();
        //        string GMSFHM = Request["GMSFHM"].ToString();             
        //        if (ZZXXZZSFCG == "2")
        //        {
        //            cdtId.AddSubCondition("AND", "DM", "=", ZKSBYY);
        //            List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query("D_ZKSBYY", cdtId, "MC", null, null, -1, -1);
        //            if (record.Count ==0) {
        //                var result = new { success = false, message = "需选择错误类型" };
        //                return Json(result, JsonRequestBehavior.AllowGet);
        //            }
        //            string ZKSBYYMC = record[0]["MC"].ToString();
        //            sql = "select FT_ZuoFeiChongZhi('" + SLBH + "','" + GMSFHM + "','" + ZKSBYY + "','" + ZKSBYYMC + "','" + UserId + "') from dual";
        //            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
        //            OracleConnection conn = new OracleConnection(connString);
        //            conn.Open();
        //            OracleCommand cmd = new OracleCommand(sql, conn);
        //            cmd.CommandType = CommandType.Text;
        //            DataSet ds = new DataSet();
        //            OracleDataAdapter da = new OracleDataAdapter();
        //            da.SelectCommand = cmd;
        //            da.Fill(ds);
        //            DataTable dt = new DataTable();
        //            if (ds != null && ds.Tables.Count > 0)
        //                dt = ds.Tables[0];
        //            string number = dt.Rows[0][0].ToString();
        //            conn.Close();
        //            if (number == "1")
        //            {
        //                var result = new { success = true, message = "修改成功" };
        //                return Json(result, JsonRequestBehavior.AllowGet);
        //            }
        //            else
        //            {
        //                var result = new { success = false, message = "修改失败:"+number};
        //                return Json(result, JsonRequestBehavior.AllowGet);
        //            }                    
        //        }
        //        if (ZZXXZZSFCG == "1" || ZZXXZZSFCG == "0")
        //        {
        //            sql = "update C_JZZ_TMP_TC set ZZXXZZSFCG = '" + ZZXXZZSFCG + "' where SLBH = '" + SLBH + "'";
        //            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
        //            OracleConnection conn = new OracleConnection(connString);
        //            conn.Open();
        //            OracleCommand cmd = new OracleCommand(sql, conn);
        //            conn.Close();
        //            var result = new { success = true, message = "修改成功" };
        //            return Json(result, JsonRequestBehavior.AllowGet);
        //        }
        //        else {
        //            var result = new { success = false, message = "该记录未进行制证" };
        //            return Json(result, JsonRequestBehavior.AllowGet);
        //        }
        //    }
        //    catch (Exception e)
        //    {
              
        //        return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
        //    }                     
        //}

        //状态更改
        //public ActionResult ActionChangeState()
        //{
        //    UnCaseSenseHashTable data = new UnCaseSenseHashTable();
        //    string SLBH = null;
        //    string ZZXXZZSFCG = null;
        //    string cg = "1";
        //    string sb = "0";
        //    try
        //    {
        //        SLBH = Request["SLBH"];
        //        ZZXXZZSFCG = Request["ZZXXZZSFCG"];
        //    }
        //    catch (Exception ee)
        //    {
        //        return Json(new { success = false, message = "受理编号有误【" + SLBH + "】" }, JsonRequestBehavior.AllowGet);
        //    }

        //    Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
        //    try
        //    {
        //        if (ZZXXZZSFCG == "1" || ZZXXZZSFCG == "0")
        //        {
        //            if (ZZXXZZSFCG == "1")
        //            {
        //                session.BeginTransaction();
        //                //__TIPS__*:这里修改表名，参考ActionAdd
        //                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  C_JZZ_TMP_TC set ZZXXZZSFCG=" + sb + " where ID=" + SLBH);
        //                session.Commit();
        //                session.Close();
        //            }
        //            else {
        //                session.BeginTransaction();
        //                //__TIPS__*:这里修改表名，参考ActionAdd
        //                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  C_JZZ_TMP_TC set ZZXXZZSFCG=" + cg + " where ID=" + SLBH);
        //                session.Commit();
        //                session.Close();
        //            }
                    
        //        }
        //        else {
        //            return Json(new { success = false, message = "状态代码错误" }, JsonRequestBehavior.AllowGet);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        session.Rollback();
        //        session.Close();
        //        var eResult = new { success = false, message = e.ToString() };
        //        return Json(eResult, JsonRequestBehavior.AllowGet); ;
        //    }
        //    return Json(new { success = true, message = "操作成功" }, JsonRequestBehavior.AllowGet);
        //}


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
                return View("ViewZzjlcxType");
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
            //int RoleLevel = Membership.CurrentUser.RoleLevel;
            //Condition cdtIds = new Condition();
            //string search = Request["Search"];
            //string date_range_type = Request["date_range_type"];
            //string start_date = Request["start_date"];
            //string end_date = Request["end_date"];
            //Condition cdtIds2 = new Condition();
            //if (!string.IsNullOrEmpty(search))
            //{
            //    cdtIds2.AddSubCondition("OR", "SLBH", "like", "%" + search + "%");
            //    cdtIds2.AddSubCondition("OR", "ZZZXPH", "like", "%" + search + "%");
            //    cdtIds2.AddSubCondition("OR", "GMSFHM", "like", "%" + search + "%");
            //    cdtIds2.AddSubCondition("OR", "ZZSBID", "like", "%" + search + "%");
            //    cdtIds2.AddSubCondition("OR", "ZZXXZZDW", "like", "%" + search + "%");
            //    cdtIds2.AddSubCondition("OR", "ZZXXZZDWMC", "like", "%" + search + "%");
            //}

            //cdtIds2.Relate = "AND";
            //cdtIds.AddSubCondition(cdtIds2);
            //if (!string.IsNullOrEmpty(date_range_type) && date_range_type != "0" && (!string.IsNullOrEmpty(start_date) || !string.IsNullOrEmpty(end_date)))
            //{
            //    if (!string.IsNullOrEmpty(start_date))
            //    {
            //        cdtIds.AddSubCondition("AND", "ZZXXZZRQ", ">=", DateTime.Parse(start_date));
            //    }
            //    if (!string.IsNullOrEmpty(end_date))
            //    {
            //        DateTime dtEndDate = DateTime.Parse(end_date);
            //        dtEndDate = dtEndDate.AddDays(1);//加多一天
            //        cdtIds.AddSubCondition("AND", "ZZXXZZRQ", "<=", dtEndDate);
            //    }
            //}

            //if (Request["cdt_combination"] != null)
            //{
            //    string cdt = Request["cdt_combination"].ToString();
            //    string jsoncdtCombination = System.Text.ASCIIEncoding.UTF8.GetString(Convert.FromBase64String(cdt));
            //    Condition cdtCombination = Condition.LoadFromJson(jsoncdtCombination);
            //    cdtCombination.Relate = "AND";
            //    ReplaceCdtCombinationOpreate(cdtCombination);
            //    cdtIds.AddSubCondition(cdtCombination);
            //}

            //if (!Membership.CurrentUser.HaveAuthority("SYS.USER.QUERY_ALL_USER"))
            //{
            //    string COMPANY_ID = Membership.CurrentUser.CompanyId.ToString();
            //    char[] c = COMPANY_ID.ToCharArray();
            //    string comId = "";
            //    bool temp = false;
            //    for (int i = c.Length - 1; i >= 0; i--)
            //    {
            //        string cc = c[i].ToString();
            //        if (cc != "0" && !temp)
            //        {
            //            temp = true;
            //        }
            //        if (temp)
            //        {
            //            comId += c[i];
            //        }
            //    }
            //    char[] charArray = comId.ToCharArray();
            //    Array.Reverse(charArray);
            //    string comId3 = new String(charArray);
            //    comId3 += "%";
            //    cdtIds.AddSubCondition("AND", "ZZXXZZDW", "like", comId3);
            //}
            //if (Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_OWN_ZZJL") && RoleLevel != 0)
            //{
            //    string userId = Membership.CurrentUser.UserId;

            //    Condition cdtId = new Condition();
            //    cdtId.AddSubCondition("AND", "SBFZR", "=", userId);
            //    List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtId, "MACHINENO", null, null, -1, -1);

            //    string zzjbh = "(";
            //    for (int i = 0; i < record.Count; i++)
            //    {
            //        zzjbh += "'" + record[i]["MACHINENO"] + "',";
            //    }
            //    zzjbh += "'0')";
            //    cdtIds.AddSubCondition("AND", "ZZSBID", "in", "EXPR:" + zzjbh);
            //}
            //List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("C_JZZ_TMP", cdtIds, "*", null, null, -1, -1);

            int RoleLevel = Membership.CurrentUser.RoleLevel;
            string sql = "";
            string QuerySql = "";
            if (Request["cdt_combination"] != null)
            {
                if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_ZZJL"))
                {
                    sql = "select SLBH,XM,ZZZXPH,GMSFHM,ZZSBID,ZZXXZZDW,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP_TC where ZZSBID in  " + GetMachineNo();
                }
                else
                {
                    sql = "select SLBH,XM,ZZZXPH,GMSFHM,ZZSBID,ZZXXZZDW,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP_TC where 1=1 ";
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
                if (Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_OWN_ZZJL") && RoleLevel != 0)
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
                    sql += " AND ZZSBID in " + zzjbh;
                }
            }
            if (!string.IsNullOrEmpty(sql))
            {
                QuerySql = sql;
            }
            else
            {
                string query_sql;
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
                    if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_ZZJL"))
                    {
                        query_sql = "select SLBH,XM,ZZZXPH,GMSFHM,ZZSBID,ZZXXZZDW,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP_TC where rownum<10000 and ZZSBID in  " + GetMachineNo();
                    }
                    else
                    {
                        query_sql = "select SLBH,XM,ZZZXPH,GMSFHM,ZZSBID,ZZXXZZDW,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP_TC where rownum<10000 ";
                    }
                }
                else
                {
                    if (!Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_ALL_ZZJL"))
                    {
                        query_sql = "select SLBH,XM,ZZZXPH,GMSFHM,ZZSBID,ZZXXZZDW,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP_TC where ZZSBID in  " + GetMachineNo() + " and ( ZZSBID like  '%" + search + "%' OR ZZXXZZDW like  '%" + search + "%' OR ZZXXZZDWMC like  '%" + search + "%') ";

                    }
                    else
                    {
                        query_sql = "select SLBH,XM,ZZZXPH,GMSFHM,ZZSBID,ZZXXZZDW,ZZXXZZDWMC,ZZXXZZRQ,ZZXXZZSFCG from C_JZZ_TMP_TC where 1=1 and ( ZZSBID like  '%" + search + "%' OR ZZXXZZDW like  '%" + search + "%' OR ZZXXZZDWMC like  '%" + search + "%') ";
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
                    string[] arrDataRangeFields = new string[] { "制证时间" };
                    if (dataRangeTypeIndex != 0)
                    {
                        fieldName = arrDataRangeFields[dataRangeTypeIndex - 1];
                        if (!String.IsNullOrEmpty(Request["start_date"]))
                        {
                            query_sql += " AND ZZXXZZRQ >=  TO_DATE('" + Request["start_date"].ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                        if (!String.IsNullOrEmpty(Request["end_date"]))
                        {
                            DateTime dtEndDate = DateTime.Parse(Request["end_date"]);
                            dtEndDate = dtEndDate.AddDays(1);//加多一天

                            query_sql += " AND ZZXXZZRQ <=  TO_DATE('" + dtEndDate.ToString() + "', 'YYYY-MM-DD HH24:MI:SS') ";
                        }
                    }
                }

                if (Membership.CurrentUser.HaveAuthority("ZZJL.ZZJLCX.QUERY_OWN_ZZJL") && RoleLevel != 0)
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
                    query_sql += " AND ZZSBID in " + zzjbh;
                }
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
                string item3 = dt.Rows[i][3].ToString();
                string item4 = dt.Rows[i][4].ToString();
                string item5 = dt.Rows[i][5].ToString();
                string item6 = dt.Rows[i][6].ToString();
                string item7 = dt.Rows[i][7].ToString();               
            }
            conn.Close();


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
            PdfPTable table = new PdfPTable(10);
            table.SetWidths(new float[] { 2.5F, 8, 8, 12, 12, 7, 6, 6, 6,9 });
            table.WidthPercentage = 100;
            AddBodyContentCell(table, "序号", cn);
            AddBodyContentCell(table, "受理编号", cn);
            AddBodyContentCell(table, "姓名", cn);
            AddBodyContentCell(table, "制证芯片号", cn);
            AddBodyContentCell(table, "身份号码", cn);
            AddBodyContentCell(table, "制证设备编号", cn);
            AddBodyContentCell(table, "制证单位代码", cn);
            AddBodyContentCell(table, "制证单位名称", cn);
            AddBodyContentCell(table, "制证时间", cn);
            AddBodyContentCell(table, "状态", cn);

            for (int i = 0; i < count_dt; i++)
            {
                //UnCaseSenseHashTable record = records[i];
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
                if (!string.IsNullOrEmpty(dt.Rows[i][2].ToString()))
                {
                    AddBodyContentCell(table, dt.Rows[i][2].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty(dt.Rows[i][3].ToString()))
                {
                    AddBodyContentCell(table, dt.Rows[i][3].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty(dt.Rows[i][4].ToString()))
                {
                    AddBodyContentCell(table, dt.Rows[i][4].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }


                if (!string.IsNullOrEmpty(dt.Rows[i][5].ToString()))
                {
                    AddBodyContentCell(table, dt.Rows[i][5].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }
                if (!string.IsNullOrEmpty(dt.Rows[i][6].ToString()))
                {
                    AddBodyContentCell(table, dt.Rows[i][6].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (dt.Rows[i][7].ToString()!= null)
                {
                    if (!string.IsNullOrEmpty(dt.Rows[i][7].ToString()))
                    {
                        string s = dt.Rows[i][7].ToString();
                        string date = s.Substring(0, 8);
                        AddBodyContentCell(table, date, cn);
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
                

                if (!string.IsNullOrEmpty(dt.Rows[i][8].ToString()))
                {
                    if (dt.Rows[i][8].ToString() == "0")
                        AddBodyContentCell(table, "成功", cn);
                    else
                        AddBodyContentCell(table, "失败", cn);
                }
                else
                {
                    AddBodyContentCell(table, "未知", cn);
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