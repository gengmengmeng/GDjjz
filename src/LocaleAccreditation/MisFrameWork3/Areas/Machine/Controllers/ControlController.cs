using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
using Oracle.ManagedDataAccess.Client;

namespace MisFrameWork3.Areas.Machine.Controllers
{
    public class ControlController : FWBaseController
    {
        //private 
        public ActionResult Index()
        {
            //if (Membership.CurrentUser.HaveAuthority("MACHINE.MACHINEMGR.CHANGE_MACHINE"))
            //{
            //    ViewBag.HideCommonButtons = true;
            //}
            //else
            //{
            //    ViewBag.HideCommonButtons = false;
            //}
            return View();
        }
        public ActionResult JsonConditionCombinationInfo()
        {
            return View();
        }
        public ActionResult ViewSBYXZT()
        {
            return View();
        }
        #region __TIPS__:框架通用函数( 增 删 改)
        public ActionResult JsonDataList()//业务主界面数据查询函数
        {
            //Condition cdtIds = new Condition();
            //int RoleLevel = Membership.CurrentUser.RoleLevel;
            //if (!Membership.CurrentUser.HaveAuthority("MACHINE.MACHINEMGR.QUERY_ALL_MACHINE"))
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
            //    cdtIds.AddSubCondition("AND", "SSDW", "like", comId3);
            //    cdtIds.AddSubCondition("AND", "DELETED_MARK", "=", "0");
            //}
            return QueryDataFromEasyUIDataGrid("B_FAULT", "CREATE_ON", "MACHINE_CODE,COMPANY_CODE,COMPANY_CODE_V_D_FW_COMP__MC,GZCODE,GZCODE_D_FAULT__MC", null, "*");
        }
        public ActionResult ViewFormAdd()
        {
            return View();
        }
        public ActionResult ViewFormEdit()
        {
            if (Membership.CurrentUser.HaveAuthority("MACHINE.MACHINEMGR.QUERY_ALL_MACHINE"))
            {
                ViewBag.ShowSBFZR = true;
            }
            else
            {
                ViewBag.ShowSBFZR = false;
            }
            return View();
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
                ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("B_MACHINE");
                data.LoadFromNameValueCollection(Request.Unvalidated.Form, ti, true);//使用Request.Unvalidated.Form可以POST HTML标签数据。
                session.BeginTransaction();
                int r = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_MACHINE", data);
                string MC = "";
                string V2 = "";
                if (!string.IsNullOrEmpty(data["SBFZR"].ToString()))
                {
                    Condition cdtId3 = new Condition();
                    cdtId3.AddSubCondition("AND", "ZZY_ID", "=", data["SBFZR"].ToString());
                    cdtId3.AddSubCondition("AND", "MACHINENO", "=", data["MACHINENO"].ToString());
                    List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "B_MACHINE_ZZY", cdtId3, "*", null, null, -1, -1);
                    if (record.Count == 0)
                    {
                        UnCaseSenseHashTable data2 = new UnCaseSenseHashTable();
                        data2["MACHINENO"] = data["MACHINENO"].ToString();
                        data2["ZZY_ID"] = data["SBFZR"].ToString();
                        int re = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_MACHINE_ZZY", data2);
                    }
                }
                if (!string.IsNullOrEmpty(data["SSDW"].ToString()))
                {
                    Condition cdtId3 = new Condition();
                    cdtId3.AddSubCondition("AND", "DM", "=", data["SSDW"].ToString());
                    List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "V_D_FW_COMP", cdtId3, "*", null, null, -1, -1);
                    if (record.Count != 0)
                    {
                        MC = record[0]["MC"].ToString();
                    }
                }
                if (!string.IsNullOrEmpty(data["MAKECARD_ID"].ToString()))
                {
                    if (data["MAKECARD_ID"].ToString().Contains(","))
                    {
                        string makeID = data["MAKECARD_ID"].ToString();
                        string[] sArray = Regex.Split(makeID, ",", RegexOptions.IgnoreCase);
                        
                        try
                        {
                            foreach (string i in sArray)
                            {
                                UnCaseSenseHashTable data_makecare1 = new UnCaseSenseHashTable();
                                data_makecare1["MACHINE_ID"] = data["MACHINENO"].ToString();
                                data_makecare1["COMPANY_ID"] = i.ToString();
                                int r1 = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_SBZZ", data_makecare1);
                                string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                                String sql = "insert into S_DICT(LX,DM,MC,V1,V2,V5,EDT_TIME)values(:LX,:DM,:MC,:V1,:V2,:V5,:EDT_TIME)";
                                OracleConnection conn = new OracleConnection(connString);
                                conn.Open();
                                OracleCommand cmd = new OracleCommand(sql, conn);
                                string LX = "Z451";
                                cmd.Parameters.Add(":LX", OracleDbType.NVarchar2, 600).Value = LX;
                                cmd.Parameters.Add(":DM", OracleDbType.NVarchar2, 600).Value = data["MACHINENO"].ToString();
                                cmd.Parameters.Add(":MC", OracleDbType.NVarchar2, 600).Value = MC;
                                cmd.Parameters.Add(":V1", OracleDbType.NVarchar2, 600).Value = i.ToString();
                                Condition cdtId3 = new Condition();
                                cdtId3.AddSubCondition("AND", "DM", "=", i.ToString());
                                List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "V_D_FW_COMP", cdtId3, "*", null, null, -1, -1);
                                if (record.Count!=0) {
                                    V2 = record[0]["MC"].ToString();
                                }                              
                                cmd.Parameters.Add(":V2", OracleDbType.NVarchar2, 600).Value = V2;
                                cmd.Parameters.Add(":V5", OracleDbType.NVarchar2, 600).Value = "1";
                                cmd.Parameters.Add(":EDT_TIME", OracleDbType.Date, 10000000).Value = DateTime.Now.Date;
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    conn.Close();
                                    session.Rollback();
                                    session.Close();
                                    return Json(new { success = false, message = "保存信息时出错！" }, JsonRequestBehavior.AllowGet);                                   
                                }                         
                            }
                        }
                        catch (Exception e)
                        {
                            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                            String sql = "rollback";
                            OracleConnection conn = new OracleConnection(connString);
                            conn.Open();
                            OracleCommand cmd = new OracleCommand(sql, conn);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            conn.Close();
                            session.Rollback();
                            session.Close();
                            return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        try
                        {
                            UnCaseSenseHashTable data_makecare2 = new UnCaseSenseHashTable();
                            data_makecare2["MACHINE_ID"] = data["MACHINENO"].ToString();
                            data_makecare2["COMPANY_ID"] = data["MAKECARD_ID"].ToString();
                            int r2 = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_SBZZ", data_makecare2);
                            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                            String sql = "insert into S_DICT(LX,DM,MC,V1,V2,V5,EDT_TIME)values(:LX,:DM,:MC,:V1,:V2,:V5,:EDT_TIME)";
                            OracleConnection conn = new OracleConnection(connString);
                            conn.Open();
                            OracleCommand cmd = new OracleCommand(sql, conn);
                            string LX = "Z451";
                            cmd.Parameters.Add(":LX", OracleDbType.NVarchar2, 600).Value = LX;
                            cmd.Parameters.Add(":DM", OracleDbType.NVarchar2, 600).Value = data["MACHINENO"].ToString();
                            cmd.Parameters.Add(":MC", OracleDbType.NVarchar2, 600).Value = MC;
                            cmd.Parameters.Add(":V1", OracleDbType.NVarchar2, 600).Value = data["MAKECARD_ID"].ToString();
                            Condition cdtId3 = new Condition();
                            cdtId3.AddSubCondition("AND", "DM", "=", data["MAKECARD_ID"].ToString());
                            List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "V_D_FW_COMP", cdtId3, "*", null, null, -1, -1);
                            if (record.Count != 0)
                            {
                                V2 = record[0]["MC"].ToString();
                            }
                            cmd.Parameters.Add(":V2", OracleDbType.NVarchar2, 600).Value = V2;
                            cmd.Parameters.Add(":V5", OracleDbType.NVarchar2, 600).Value = "1";
                            cmd.Parameters.Add(":EDT_TIME", OracleDbType.Date, 10000000).Value = DateTime.Now.Date;
                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected == 0)
                            {
                                conn.Close();
                                session.Rollback();
                                session.Close();
                                return Json(new { success = false, message = "保存信息时出错！" }, JsonRequestBehavior.AllowGet);
                            }                           
                        }
                        catch(Exception e)
                        {
                            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                            String sql = "rollback";
                            OracleConnection conn = new OracleConnection(connString);
                            conn.Open();
                            OracleCommand cmd = new OracleCommand(sql, conn);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            conn.Close();
                            session.Rollback();
                            session.Close();
                            return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
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
        public ActionResult ActionEdit()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                //__TIPS__*:这里修改表名，参考ActionAdd 
                ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("B_MACHINE");
                data.LoadFromNameValueCollection(Request.Unvalidated.Form, ti, true);//使用Request.Unvalidated.Form可以POST HTML标签数据。
                data["ID"] = Request["OBJECT_ID"];//这ID字段是加载不进来的。  
                session.BeginTransaction();
                int r = DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "B_MACHINE", data, false);
                string V2 = "";
                string MC = "";
                if (!string.IsNullOrEmpty(data["SBFZR"].ToString()))
                {
                    Condition cdtId3 = new Condition();
                    cdtId3.AddSubCondition("AND", "ZZY_ID", "=", data["SBFZR"].ToString());
                    cdtId3.AddSubCondition("AND", "MACHINENO", "=", data["MACHINENO"].ToString());
                    List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "B_MACHINE_ZZY", cdtId3, "*", null, null, -1, -1);
                    if (record.Count == 0)
                    {
                        UnCaseSenseHashTable data2 = new UnCaseSenseHashTable();
                        data2["MACHINENO"] = data["MACHINENO"].ToString();
                        data2["ZZY_ID"] = data["SBFZR"].ToString();
                        int re = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_MACHINE_ZZY", data2);
                    }
                }
                if (!string.IsNullOrEmpty(data["SSDW"].ToString()))
                {
                    Condition cdtId3 = new Condition();
                    cdtId3.AddSubCondition("AND", "DM", "=", data["SSDW"].ToString());
                    List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "V_D_FW_COMP", cdtId3, "*", null, null, -1, -1);
                    if (record.Count != 0)
                    {
                        MC = record[0]["MC"].ToString();
                    }
                }
                if (!string.IsNullOrEmpty(data["MACHINENO"].ToString())) {
                    string MACHINE_ID = data["MACHINENO"].ToString();
                    Session session_delete = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
                    try
                    {
                        DbUtilityManager.Instance.DefaultDbUtility.Execute("delete from B_SBZZ  where MACHINE_ID='" + MACHINE_ID+"'");
                        string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                        String sql = "delete from S_DICT where DM='"+ data["MACHINENO"].ToString()+"'";
                        OracleConnection conn = new OracleConnection(connString);
                        conn.Open();
                        OracleCommand cmd = new OracleCommand(sql, conn);
                        int rowsAffected = cmd.ExecuteNonQuery();                       
                        if (rowsAffected == 0)
                        {                        
                            conn.Close();
                            session.Rollback();
                            session.Close();
                            return Json(new { success = false, message = "删除原信息失败！" }, JsonRequestBehavior.AllowGet);
                        }
                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        session_delete.Rollback();
                        session_delete.Close();
                        var eResult = new { success = false, message = e.ToString() };                      
                        string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                        String sql = "rollback";
                        OracleConnection conn = new OracleConnection(connString);
                        conn.Open();
                        session.Rollback();
                        session.Close();
                        OracleCommand cmd = new OracleCommand(sql, conn);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                        return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                    }
                    if (data["MAKECARD_ID"].ToString().Contains(","))
                    {
                        string makeID = data["MAKECARD_ID"].ToString();
                        string[] sArray = Regex.Split(makeID, ",", RegexOptions.IgnoreCase);

                        try
                        {
                            foreach (string i in sArray)
                            {
                                UnCaseSenseHashTable data_makecare1 = new UnCaseSenseHashTable();
                                data_makecare1["MACHINE_ID"] = data["MACHINENO"].ToString();
                                data_makecare1["COMPANY_ID"] = i.ToString();
                                int r1 = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_SBZZ", data_makecare1);
                                string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                                String sql = "insert into S_DICT(LX,DM,MC,V1,V2,V5,EDT_TIME)values(:LX,:DM,:MC,:V1,:V2,:V5,:EDT_TIME)";
                                OracleConnection conn = new OracleConnection(connString);
                                conn.Open();
                                OracleCommand cmd = new OracleCommand(sql, conn);
                                string LX = "Z451";
                                cmd.Parameters.Add(":LX", OracleDbType.NVarchar2, 600).Value = LX;
                                cmd.Parameters.Add(":DM", OracleDbType.NVarchar2, 600).Value = data["MACHINENO"].ToString();
                                cmd.Parameters.Add(":MC", OracleDbType.NVarchar2, 600).Value = MC;
                                cmd.Parameters.Add(":V1", OracleDbType.NVarchar2, 600).Value = i.ToString();                             
                                Condition cdtId3 = new Condition();
                                cdtId3.AddSubCondition("AND", "DM", "=", i.ToString());
                                List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "V_D_FW_COMP", cdtId3, "*", null, null, -1, -1);
                                if (record.Count != 0)
                                {
                                    V2 = record[0]["MC"].ToString();
                                }
                                cmd.Parameters.Add(":V2", OracleDbType.NVarchar2, 600).Value = V2;
                                cmd.Parameters.Add(":V5", OracleDbType.NVarchar2, 600).Value = "1";
                                cmd.Parameters.Add(":EDT_TIME", OracleDbType.Date, 10000000).Value = DateTime.Now.Date;
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    conn.Close();
                                    session.Rollback();
                                    session.Close();
                                    return Json(new { success = false, message = "保存信息时出错！" }, JsonRequestBehavior.AllowGet);
                                }
                                conn.Close();
                            }
                        }
                        catch (Exception e)
                        {
                            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                            String sql = "rollback";
                            OracleConnection conn = new OracleConnection(connString);
                            conn.Open();
                            OracleCommand cmd = new OracleCommand(sql, conn);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            conn.Close();
                            session.Rollback();
                            session.Close();
                            return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        try
                        {
                            UnCaseSenseHashTable data_makecare2 = new UnCaseSenseHashTable();
                            data_makecare2["MACHINE_ID"] = data["MACHINENO"].ToString();
                            data_makecare2["COMPANY_ID"] = data["MAKECARD_ID"].ToString();
                            int r2 = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_SBZZ", data_makecare2);
                            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                            String sql = "insert into S_DICT(LX,DM,MC,V1,V2,V5,EDT_TIME)values(:LX,:DM,:MC,:V1,:V2,:V5,:EDT_TIME)";
                            OracleConnection conn = new OracleConnection(connString);
                            conn.Open();
                            OracleCommand cmd = new OracleCommand(sql, conn);
                            string LX = "Z451";
                            cmd.Parameters.Add(":LX", OracleDbType.NVarchar2, 600).Value = LX;
                            cmd.Parameters.Add(":DM", OracleDbType.NVarchar2, 600).Value = data["MACHINENO"].ToString();
                            cmd.Parameters.Add(":MC", OracleDbType.NVarchar2, 600).Value = MC;
                            cmd.Parameters.Add(":V1", OracleDbType.NVarchar2, 600).Value = data["MAKECARD_ID"].ToString();
                            Condition cdtId3 = new Condition();
                            cdtId3.AddSubCondition("AND", "DM", "=", data["MAKECARD_ID"].ToString());
                            List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "V_D_FW_COMP", cdtId3, "*", null, null, -1, -1);
                            if (record.Count != 0)
                            {
                                V2 = record[0]["MC"].ToString();
                            }
                            cmd.Parameters.Add(":V2", OracleDbType.NVarchar2, 600).Value = V2;
                            cmd.Parameters.Add(":V5", OracleDbType.NVarchar2, 600).Value = "1";
                            cmd.Parameters.Add(":EDT_TIME", OracleDbType.Date, 10000000).Value = DateTime.Now.Date;
                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected == 0)
                            {
                                conn.Close();
                                session.Rollback();
                                session.Close();
                                return Json(new { success = false, message = "保存信息时出错！" }, JsonRequestBehavior.AllowGet);
                            }                           
                        }
                        catch (Exception e)
                        {
                            string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                            String sql = "rollback";
                            OracleConnection conn = new OracleConnection(connString);
                            conn.Open();
                            session.Rollback();
                            session.Close();
                            OracleCommand cmd = new OracleCommand(sql, conn);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            conn.Close();
                            return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    session_delete.Commit();
                    session_delete.Close();
                }
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
        public ActionResult ActionDelete()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();
            int id = 0;
            string make_id ="";
            try
            {
                id = int.Parse(Request["OBJECT_ID"]);
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = "主键值有误【" + id + "】" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                make_id = Request["MACHINENO"].ToString();
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = "设备编号有误【" + make_id + "】" }, JsonRequestBehavior.AllowGet);
            }
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();
                //__TIPS__*:这里修改表名，参考ActionAdd
                DbUtilityManager.Instance.DefaultDbUtility.Execute("delete from B_MACHINE where ID=" + id.ToString());
                DbUtilityManager.Instance.DefaultDbUtility.Execute("delete from B_SBZZ  where MACHINE_ID='" + make_id+"'");
                string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                String sql = "delete from S_DICT where DM='"+ make_id+"'";
                OracleConnection conn = new OracleConnection(connString);
                conn.Open();
                OracleCommand cmd = new OracleCommand(sql, conn);
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    conn.Close();
                    session.Rollback();
                    session.Close();
                    return Json(new { success = false, message = "删除信息时出错！" }, JsonRequestBehavior.AllowGet);
                }
                conn.Close();
                session.Commit();
                session.Close();
            }
            catch (Exception e)
            {
                session.Rollback();
                session.Close();
                var eResult = new { success = false, message = e.ToString() };
                return Json(eResult, JsonRequestBehavior.AllowGet); ;
            }

            var result = new { success = true, message = "删除成功" };
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ActionChangeState()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();
            int id = 0;
            int state = 0;
            try
            {
                id = int.Parse(Request["OBJECT_ID"]);
                state = int.Parse(Request["state"]);
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = "主键值有误【" + id + "】" }, JsonRequestBehavior.AllowGet);
            }

            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();
                //__TIPS__*:这里修改表名，参考ActionAdd
                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  B_MACHINE set DISABLED=" + state + " where ID=" + id.ToString());
                session.Commit();
                session.Close();
            }
            catch (Exception e)
            {
                session.Rollback();
                session.Close();
                var eResult = new { success = false, message = e.ToString() };
                return Json(eResult, JsonRequestBehavior.AllowGet); ;
            }
            return Json(new { success = true, message = "操作成功" }, JsonRequestBehavior.AllowGet);
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
            if ("V_D_FW_S_USERS".Equals(Request["dic"]))
            {
                int RoleLevel = Membership.CurrentUser.RoleLevel;
                Condition cdtId2 = new Condition("AND", "ROLES_ID", "=", 2);
                if (!Membership.CurrentUser.HaveAuthority("SYS.USER.SELECT_OTHOR_COMPANY"))
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
                    cdtId2.AddSubCondition("AND", "COMPANY_ID", "like", comId3);
                    cdtId2.AddSubCondition("AND", "DISABLED", "=", 0);
                    return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC,DW", cdtId2, "*");
                }
                else
                {

                    return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC,DW", cdtId2, "*");
                }
            }
            else if ("V_D_FW_COMP".Equals(Request["dic"]))
            {
                Condition cdtId2 = new Condition();
                int RoleLevel = Membership.CurrentUser.RoleLevel;
                if (!Membership.CurrentUser.HaveAuthority("SYS.USER.SELECT_OTHOR_COMPANY"))
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
                    cdtId2.AddSubCondition("AND", "DM", "like", comId3);
                    cdtId2.AddSubCondition("AND", "DISABLED", "=", 0);
                    return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC", cdtId2, "*");
                }
                else
                {

                    return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC", cdtId2, "*");
                }
            }
            else
            {
                return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC", null, "*");
            }

        }

        public ActionResult ViewDicLargeUI()
        {
            /*
             * __TIPS__:有些特殊的字典可能需要显示更多的东西所以这里可以根据Request的值返回不同的视图
             *          以下演示根据字典内容，返回不同的视图。
             * */
            
            if ("V_D_FW_COMP".Equals(Request["dic"]))
                return View("ViewSSDW");
            else if ("V_D_FW_S_USERS".Equals(Request["dic"]))
                return View("ViewSBFZR");
            else if ("D_SBZTMS".Equals(Request["dic"]))
                return View("ViewSBYXZT");
            else
                return View("~/Views/Shared/ViewCommonDicUI.cshtml");
        }
        #endregion


        #region 打印数据
        public FileResult ActionPrint()
        {
            //获取数据
            Condition cdtIds = new Condition();
            string search = Request["Search"];
            string date_range_type = Request["date_range_type"];
            string start_date = Request["start_date"];
            string end_date = Request["end_date"];
            
            Condition cdtIds2 = new Condition();
            if (!string.IsNullOrEmpty(search))
            {
                cdtIds2.AddSubCondition("OR", "MACHINENO", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "SSDW", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "SSDW_V_D_FW_COMP__MC", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "SBFZR", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "SBFZR_V_D_FW_S_USERS__MC", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "PHONE", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "ADDRESS", "like", "%" + search + "%");
            }
            if ( !string.IsNullOrEmpty(date_range_type) && date_range_type != "0" && (!string.IsNullOrEmpty(start_date) || !string.IsNullOrEmpty(end_date)))
            {
                if (!string.IsNullOrEmpty(start_date))
                {
                    cdtIds.AddSubCondition("AND", "CREATE_ON", ">=", DateTime.Parse(start_date));
                }
                if (!string.IsNullOrEmpty(end_date))
                {
                    DateTime dtEndDate = DateTime.Parse(end_date);
                    dtEndDate = dtEndDate.AddDays(1);//加多一天
                    cdtIds.AddSubCondition("AND", "CREATE_ON", "<=", dtEndDate);
                }
            }
            if (Request["cdt_combination"] != null)
            {
                string jsoncdtCombination = System.Text.ASCIIEncoding.UTF8.GetString(Convert.FromBase64String(Request["cdt_combination"]));
                Condition cdtCombination = Condition.LoadFromJson(jsoncdtCombination);
                cdtCombination.Relate = "AND";
                ReplaceCdtCombinationOpreate(cdtCombination);
                cdtIds.AddSubCondition(cdtCombination);
            }

            cdtIds2.Relate = "AND";
            cdtIds.AddSubCondition(cdtIds2);
            if (!Membership.CurrentUser.HaveAuthority("MACHINE.MACHINEMGR.CHANGE_MACHINE"))
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
                cdtIds.AddSubCondition("AND", "SSDW", "like", comId3);
                cdtIds.AddSubCondition("AND", "DELETED_MARK", "=", "0");
            }
            List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtIds, "*", null, null, -1, -1);

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
            table.SetWidths(new float[] { 2.5F, 8, 8, 12, 7, 6 , 6, 6, 6, 6});
            table.WidthPercentage = 100;
            AddBodyContentCell(table, "序号", cn);
            AddBodyContentCell(table, "设备编号", cn);
            AddBodyContentCell(table, "所属单位编号", cn);
            AddBodyContentCell(table, "所属单位名称", cn);
            AddBodyContentCell(table, "录入时间", cn);
            AddBodyContentCell(table, "状态", cn);
            AddBodyContentCell(table, "负责人账户", cn);
            AddBodyContentCell(table, "负责人名称", cn);
            AddBodyContentCell(table, "负责人联系电话", cn);
            AddBodyContentCell(table, "负责人联系地址", cn);

            for (int i = 0; i < records.Count; i++)
            {
                UnCaseSenseHashTable record = records[i];
                AddBodyContentCell(table, Convert.ToString(i + 1), cn);
                if (!string.IsNullOrEmpty((string)record["MACHINENO"]))
                {
                    AddBodyContentCell(table, record["MACHINENO"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "",  cn);
                }

                if (!string.IsNullOrEmpty((string)record["SSDW"]))
                {
                    AddBodyContentCell(table, record["SSDW"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }
                if (!string.IsNullOrEmpty((string)record["SSDW_V_D_FW_COMP__MC"]))
                {
                    AddBodyContentCell(table, record["SSDW_V_D_FW_COMP__MC"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty(record["CREATE_ON"].ToString()))
                {
                    string s = record["CREATE_ON"].ToString();
                    string date = s.Substring(0, 8);
                    AddBodyContentCell(table, date, cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty(record["DISABLED"].ToString()))
                {
                    if (record["DISABLED"].ToString() == "0")
                        AddBodyContentCell(table, "启用", cn);
                    else
                        AddBodyContentCell(table, "禁用", cn);
                }
                else
                {
                    AddBodyContentCell(table, "未知", cn);
                }

                if (!string.IsNullOrEmpty((string)record["SBFZR"]))
                {
                    AddBodyContentCell(table, record["SBFZR"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["SBFZR_V_D_FW_S_USERS__MC"]))
                {
                    AddBodyContentCell(table, record["SBFZR_V_D_FW_S_USERS__MC"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["PHONE"]))
                {
                    AddBodyContentCell(table, record["PHONE"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["ADDRESS"]))
                {
                    AddBodyContentCell(table, record["ADDRESS"].ToString(), cn);
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