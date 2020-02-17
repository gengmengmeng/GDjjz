using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MisFrameWork3.Classes.Membership;
using MisFrameWork3.Classes.Authorize;

namespace MisFrameWork3.Controllers
{    
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Logined]
        public ActionResult ViewMainForm()
        {
            ViewBag.UserName = Membership.CurrentUser.UserName;
            ViewBag.CompanyName = Membership.CurrentUser.CompanyName;
            ViewBag.Theme = "default";
            if (!String.IsNullOrEmpty(Request["theme"]))
            {
                HttpCookie hc1 = new HttpCookie("theme", Request["theme"]);
                hc1.Expires = DateTime.Now.AddDays(60);
                Response.Cookies.Add(hc1);
                ViewBag.Theme = Request["theme"];
            }
            else
            {
                HttpCookie hc2 = Request.Cookies["theme"];
                if (hc2 != null)
                {
                    hc2.Expires = DateTime.Now.AddDays(60);
                    Response.Cookies.Add(hc2);
                    if (String.IsNullOrEmpty(hc2.Value))
                        ViewBag.Theme = hc2.Value;
                }
                else
                {
                    hc2 = new HttpCookie("theme", "default");
                    hc2.Expires = DateTime.Now.AddDays(60);
                    Response.Cookies.Add(hc2);
                }
            }
            return View();
        }

        [Logined]
        public ActionResult ViewHomePage()
        {
            ViewBag.UserName = Membership.CurrentUser.UserName;
            ViewBag.CompanyName = Membership.CurrentUser.CompanyName;
            return View();
        }

        [Logined]
        public ActionResult ViewHeader()
        {
            ViewBag.UserName = Membership.CurrentUser.UserName;
            ViewBag.CompanyName = Membership.CurrentUser.CompanyName;
            return View();
        }



        [Logined]
        public ActionResult ViewEditUserInfo()
        {
            return View();
        }
        

        public ActionResult ActionLogin(string uid,string pwd_md5)
        {
            try
            {
                Membership.LoginResult result = Membership.Login(uid, pwd_md5);
                return Json(new { success = result.Result, message = result.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = e.ToString() }, JsonRequestBehavior.AllowGet);
            }            
        }

        public ActionResult ActionLogout(string uid, string pwd_md5)
        {
            try
            {
                Membership.Logout();
                
            }
            catch (Exception e)
            {
                
            }
            return Json(new { success = true, message = "退出成功" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ActionUpdateSession()
        {
            if (Membership.CurrentUser!=null)
                return Json(new { success = true, message = Membership.CurrentUser.UserId+" 成功！" }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { success = false, message = "更新失败成功！" }, JsonRequestBehavior.AllowGet);
        }

        [Logined]
        public ActionResult ActionEditUserInfo()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();

            ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("FW_S_USERS");
            data.LoadFromNameValueCollection(Request.Form, ti, true);
            data["USER_ID"] = Membership.CurrentUser.UserId; 
            if (!data.HasValue("USER_PASSWD"))//没有值就是不设置密码
            {
                data.Remove("USER_PASSWD");
            }
            else
                data["USER_PASSWD"] = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(data["USER_PASSWD"].ToString(), "MD5");

            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();
                DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "FW_S_USERS", data, false);                
                session.Commit();
                session.Close();
            }
            catch (Exception e)
            {
                session.Rollback();
                session.Close();
                return Json(new { success = false, message = e.ToString() }, JsonRequestBehavior.AllowGet);
            }
            var result = new { success = true, message = "保存成功" };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult JsonUserInfo()
        {
            string uid = Membership.CurrentUser.UserId;
            Condition cdtId = new Condition("AND","USER_ID","=",uid);
            List<UnCaseSenseHashTable> rows = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_USERS", cdtId, "USER_ID,USER_NAME,ROLES_ID,ROLES_ID_V_D_FW_S_ROLES__MC,COMPANY_ID,COMPANY_ID_V_D_FW_COMP__MC,DISABLED,CREATE_BY,CREATE_ON,UPDATE_BY,UPDATE_ON,TEL1,TEL2,EMAIL,ADDR", null, null, -1, -1);
            int total = DbUtilityManager.Instance.DefaultDbUtility.RecordCount("FW_S_USERS", cdtId);
            return Json(new { total = total, rows = rows }, JsonRequestBehavior.AllowGet);        
        }

        [Logined]
        public ActionResult JsonMenuData()
        {
            string filename = System.Web.HttpRuntime.AppDomainAppPath + "Configs\\menu.config";
            String strJson = "";
            StreamReader sr = new StreamReader(filename, System.Text.Encoding.Default);
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                strJson += line.ToString();
            }
            sr.Close();
            JArray jsonObj = JArray.Parse(strJson);
            JArray jsonResult = new JArray();
            CalNodes(jsonObj, ref jsonResult);
            return this.Content(jsonResult.ToString(), "application/json");
        }

        public void CalNodes(JArray config, ref JArray r)
        {
            for (int i = 0; i < config.Count; i++)
            {
                JObject node = (JObject)config[i];
                JObject obj = new JObject();
                if (node["id"].ToString().StartsWith("GLOBAL"))
                    continue;
                obj["id"] = node["id"];
                obj["text"] = node["text"];
                
                if (node["iconCls"] != null)
                    obj["iconCls"] = node["iconCls"];
                if (node["url"] != null)
                {
                    obj["attributes"] = new JObject();
                    obj["attributes"]["url"] = node["url"];
                }
                if (node["children"] != null)
                {
                    JArray children = (JArray)node["children"];
                    JArray chilerenObj = new JArray();
                    obj["children"] = chilerenObj;
                    CalNodes(children, ref chilerenObj);
                }
                //有URL地址和有权限才显示
                if (node["url"]!=null && Membership.CurrentUser.HaveAuthority(obj["id"].ToString()))
                    r.Add(obj);
                else if (obj["children"]!=null && obj["children"].Count()>0)
                    r.Add(obj);

            }
        }
    }
}