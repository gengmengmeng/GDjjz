using System;
using System.Collections.Generic;
using System.Web.Mvc;

using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
using MisFrameWork3.Classes.Authorize;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace MisFrameWork3.Areas.FWBase.Controllers
{
    [Logined(OperateId = "SYS.USER")]
    public class UsersController : FWBaseController
    {
        // GET: FWBase/Users
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult JsonConditionCombinationInfo()
        {
            return View();
        }

        public ActionResult ViewFormAdd()
        {
            return View();
        }

        public ActionResult ViewFormEdit()
        {
            return View();
        }

        public ActionResult ViewFormQueryCompany()
        {
            return View();
        }
        public ActionResult ViewFormQueryMachine()
        {
            return View();
        }

        public ActionResult ActionAdd()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();
            /*
            __TIPS__: 
                1、加载窗体提交的数据可以使用LoadFromNameValueCollection 结合正则表达式过滤掉没有用的数据
                    比如：data.LoadFromNameValueCollection(Request.Form, "NAME|TYPE|COMPANY_CODE",true);
                    这样只加载NAME、TYPE、COMPANY_CODE 这几项，其它项不处理
                2、获取表信息，然后加只加载与表字段同名的内容，这个方法最常用，比如这样：
                    ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("FW_S_COMAPANIES");
                    data.LoadFromNameValueCollection(Request.Form, ti, true);
            */
            //data.LoadFromNameValueCollection(Request.Form, "NAME|TYPE|COMPANY_CODE",true);
            ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("FW_S_USERS");
            data.LoadFromNameValueCollection(Request.Form, ti, true);
            //查询出所属单位
            if (data["SB_PASSWD"].ToString()!= "") {
                System.Text.RegularExpressions.Regex rex =  new System.Text.RegularExpressions.Regex(@"^\d+$");                
                if (!rex.IsMatch(data["SB_PASSWD"].ToString())) {
                    return Json(new { success = false, message = "设备密码只能为纯数字"}, JsonRequestBehavior.AllowGet);
                }
            }
            Condition cdtCompany = new Condition();
            cdtCompany.AddSubCondition("AND", "DM", "=", data["COMPANY_ID"]);
            List<UnCaseSenseHashTable> recordCompaies = DbUtilityManager.Instance.DefaultDbUtility.Query("V_D_FW_COMP", cdtCompany, "*", null);
            if (recordCompaies.Count != 1)
                return Json(new { success = false, message = "所属单位信息无效：" + data["COMPANY_ID"] }, JsonRequestBehavior.AllowGet);
            Condition cdtuser_id = new Condition();
            cdtuser_id.AddSubCondition("AND", "USER_ID", "=", data["USER_ID"]);
            List<UnCaseSenseHashTable> recorduser_id = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_USERS", cdtuser_id, "*", null);
            if (recorduser_id.Count != 0)
                return Json(new { success = false, message = "账户名【" + data["USER_ID"] + "】已存在"}, JsonRequestBehavior.AllowGet);
            data["DISABLED"] = recordCompaies[0]["DISABLED"];
            //处理密码部份
            data["USER_PASSWD"] = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(data["USER_PASSWD"].ToString(), "MD5");
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {

                session.BeginTransaction();
                DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "FW_S_USERS", data);
                if (!String.IsNullOrEmpty(Request["ROLES_ID"]))
                {
                    string[] roleIds = Request["ROLES_ID"].Split(',');
                    foreach (string roleId in roleIds)
                    {
                        UnCaseSenseHashTable record = new UnCaseSenseHashTable();
                        record["USER_ID"] = data["USER_ID"];
                        record["ROLE_ID"] = roleId;
                        DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "FW_S_USERS_M_ROLES", record);
                    }
                }
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


        public ActionResult ActionEdit()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();

            ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("FW_S_USERS");
            data.LoadFromNameValueCollection(Request.Form, ti, true);
            data["USER_ID"] = Request["OBJECT_ID"];//这一字字段是加载不进来的。  
            //这里要判断一下用户的角色级别
            if (data["SB_PASSWD"].ToString() != "")
            {
                System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(@"^\d+$");
                if (!rex.IsMatch(data["SB_PASSWD"].ToString()))
                {
                    return Json(new { success = false, message = "设备密码只能为纯数字" }, JsonRequestBehavior.AllowGet);
                }
            }
            FWUserInfo userTarget = new FWUserInfo();          
            if (!userTarget.LoadData(Request["OBJECT_ID"]))
                return Json(new { success = false, message = "该用户不存在" }, JsonRequestBehavior.AllowGet);
            if (userTarget.RoleLevel <= Membership.CurrentUser.RoleLevel)
            {
                if (userTarget.RoleLevel <= Membership.CurrentUser.RoleLevel && Membership.CurrentUser.RoleLevel != 0 && userTarget.RoleLevel != 0)
                    return Json(new { success = false, message = "当前登陆用户的角色级别过低，不允许操作" }, JsonRequestBehavior.AllowGet);
            }
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
                //TODO 这里一定要改成参数形式的SQL语句防止SQL攻击
                DbUtilityManager.Instance.DefaultDbUtility.Execute(session, "DELETE FROM FW_S_USERS_M_ROLES WHERE USER_ID='" + Request["OBJECT_ID"] + "'", null);
                DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "FW_S_USERS", data, false);
                if (!String.IsNullOrEmpty(Request["ROLES_ID"]))
                {
                    string[] roleIds = Request["ROLES_ID"].Split(',');
                    foreach (string roleId in roleIds)
                    {
                        UnCaseSenseHashTable record = new UnCaseSenseHashTable();
                        record["USER_ID"] = Request["OBJECT_ID"];
                        record["ROLE_ID"] = roleId;
                        DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "FW_S_USERS_M_ROLES", record);
                    }
                }
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

        public ActionResult ActionChangeState()
        {
            UnCaseSenseHashTable data = new UnCaseSenseHashTable();
            string id = "";
            int state = 0;
            try
            {
                id = Request["OBJECT_ID"].Replace("\'", "").Replace("\"", "");
                state = int.Parse(Request["state"]);
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = "主键值有误【" + id + "】" }, JsonRequestBehavior.AllowGet);
            }

            //这里要判断一下用户的角色级别
            FWUserInfo userTarget = new FWUserInfo();
            if (!userTarget.LoadData(Request["OBJECT_ID"]))
                return Json(new { success = false, message = "该用户不存在" }, JsonRequestBehavior.AllowGet);
            if (userTarget.RoleLevel <= Membership.CurrentUser.RoleLevel)
                return Json(new { success = false, message = "当前登陆用户的角色级别过低，不允许操作" }, JsonRequestBehavior.AllowGet);

            if (state == 0)//启用的话，要检查一下单位信息，看单位有没被停用
            {
                try
                {
                    UnCaseSenseHashTable recordUser = DbUtilityManager.Instance.DefaultDbUtility.GetOneRecord("FW_S_USERS", id, "USER_ID,COMPANY_ID,(SELECT c.DISABLED as C_DISABLED FROM FW_S_COMAPANIES as c where c.ID=COMPANY_ID) as C_DISABLED");
                    if (recordUser == null)
                        return Json(new { success = false, message = "用有此用户：" + id }, JsonRequestBehavior.AllowGet);
                    if (recordUser.GetDecimalValue("C_DISABLED") != 0)
                        return Json(new { success = false, message = "因为所属单位被停用，所以不能启用本用户" }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                    return Json(new { success = false, message = e.ToString() }, JsonRequestBehavior.AllowGet);
                }
            }
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();
                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  FW_S_USERS set DISABLED=" + state + " where USER_ID=\'" + id + "\'");
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
            var result = new { success = true, message = "操作成功" };
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public ActionResult JsonDataList()
        {
            //#region 初始化基本查询参数 id,limit,offset,search,sort,order
            Condition cdtMyCompany = new Condition();
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            if (!Membership.CurrentUser.HaveAuthority("SYS.USER.QUERY_ALL_USER"))
            {
                string COMPANYID= Membership.CurrentUser.CompanyId.ToString();
                char[] c = COMPANYID.ToCharArray();
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
                string comId2 = new String(charArray);
                comId2 += "%";
                cdtMyCompany.AddSubCondition("AND", "COMPANY_ID", "like", comId2);
            }
            return QueryDataFromEasyUIDataGrid("FW_S_USERS", "CREATE_ON", "USER_ID,SB_ID,USER_NAME,ROLES_ID,ROLES_ID_V_D_FW_S_ROLES__MC,SB_PASSWD,COMPANY_ID,COMPANY_ID_V_D_FW_COMP__MC,SB_ID_V_D_MACHINE__MC", cdtMyCompany, "USER_ID,USER_NAME,ROLES_ID,ROLES_ID_V_D_FW_S_ROLES__MC,COMPANY_ID,COMPANY_ID_V_D_FW_COMP__MC,DISABLED,CREATE_BY,CREATE_ON,SB_ID_V_D_MACHINE__MC,SB_PASSWD,UPDATE_BY,UPDATE_ON,TEL1,TEL2,SB_ID,EMAIL,ADDR");
        }

        public ActionResult JsonRoles(string user_id)
        {
            Condition cdtMain = new Condition();
            //如果当前不是超级管理员，只查询自己有的角色。
            if (Membership.CurrentUser.RoleLevel > 9)
            {
                Condition cdtIsMe = new Condition();
                foreach (UnCaseSenseHashTable record in Membership.CurrentUser.GetRolesId(true))
                {
                    cdtIsMe.AddSubCondition("OR", "SORT_CODE", ">=", record["SORT_CODE"]);
                }
                foreach (UnCaseSenseHashTable record in Membership.CurrentUser.GetRolesId(false))
                {
                    cdtIsMe.AddSubCondition("OR", "SORT_CODE", ">=", record["SORT_CODE"]);
                }
                cdtMain.AddSubCondition(cdtIsMe);
            }
            List<UnCaseSenseHashTable> rows = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_ROLES", cdtMain, "*", "SORT_CODE,ID", null, -1, -1);
            int total = DbUtilityManager.Instance.DefaultDbUtility.RecordCount("FW_S_ROLES", cdtMain);

            Condition cdt_uid_c_rid = new Condition();
            cdt_uid_c_rid.AddSubCondition("AND", "USER_ID", "=", user_id);
            Condition cdt_rid = new Condition("AND", "ROLE_ID", "=", "后面循环会改这个TAG,所以这里随便设");
            cdt_uid_c_rid.AddSubCondition(cdt_rid);
            foreach (UnCaseSenseHashTable r in rows)
            {
                if (String.IsNullOrEmpty(user_id))
                    r["CHECKED"] = false;
                else
                {
                    cdt_rid.Tag = r["ID"];
                    r["CHECKED"] = DbUtilityManager.Instance.DefaultDbUtility.RecordCount("FW_S_USERS_M_ROLES", cdt_uid_c_rid) > 0;
                }
            }
            return Json(new { total = total, rows = rows }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult JsonCompanies()
        {
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            Condition cdtIds = new Condition();
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
                cdtIds.AddSubCondition("AND", "DM", "like", comId3);
                cdtIds.AddSubCondition("AND", "DISABLED", "=", 0);
            }
            return this.QueryDataFromEasyUIDataGrid("V_D_FW_COMP", null, "DM,MC", cdtIds, "DM,MC");
        }


        public ActionResult JsonMachine()
        {
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            Condition cdtIds = new Condition();
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
                cdtIds.AddSubCondition("AND", "COMPANY", "like", comId3);
            }
            return this.QueryDataFromEasyUIDataGrid("V_D_MACHINE", null, "DM,MC,COMPANY", cdtIds, "DM,MC,COMPANY");
        }

        #region 打印数据
        public FileResult ActionPrint(string name, string oject_id)
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
                cdtIds2.AddSubCondition("OR", "USER_ID", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "USER_NAME", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "ROLES_ID", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "ROLES_ID_V_D_FW_S_ROLES__MC", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "COMPANY_ID", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "COMPANY_ID_V_D_FW_COMP__MC", "like", "%" + search + "%");
            }
            cdtIds2.Relate = "AND";
            cdtIds.AddSubCondition(cdtIds2);
            if (!string.IsNullOrEmpty(date_range_type) && date_range_type != "0" && (!string.IsNullOrEmpty(start_date) || !string.IsNullOrEmpty(end_date)))
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
            if (!Membership.CurrentUser.HaveAuthority("SYS.USER.QUERY_ALL_USER"))
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
                cdtIds.AddSubCondition("AND", "COMPANY_ID", "like", comId3);
                cdtIds.AddSubCondition("AND", "DELETED_MARK", "=", "0");
            }
            List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_USERS", cdtIds, "*", null, null, -1, -1);

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
            table.SetWidths(new float[] { 2.5F, 8, 8, 12, 12, 7, 6, 6, 6, 6 });
            table.WidthPercentage = 100;
            AddBodyContentCell(table, "序号", cn);
            AddBodyContentCell(table, "登陆账号", cn);
            AddBodyContentCell(table, "用户名称", cn);
            AddBodyContentCell(table, "所属单位编号", cn);
            AddBodyContentCell(table, "所属单位名称", cn);
            AddBodyContentCell(table, "系统角色", cn);
            AddBodyContentCell(table, "联系电话", cn);
            AddBodyContentCell(table, "电子邮箱", cn);
            AddBodyContentCell(table, "联系地址", cn);
            AddBodyContentCell(table, "是否启用", cn);

            for (int i = 0; i < records.Count; i++)
            {
                UnCaseSenseHashTable record = records[i];
                AddBodyContentCell(table, Convert.ToString(i + 1), cn);
                if (!string.IsNullOrEmpty((string)record["USER_ID"]))
                {
                    AddBodyContentCell(table, record["USER_ID"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["USER_NAME"]))
                {
                    AddBodyContentCell(table, record["USER_NAME"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }
                if (!string.IsNullOrEmpty((string)record["COMPANY_ID"]))
                {
                    AddBodyContentCell(table, record["COMPANY_ID"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["COMPANY_ID_V_D_FW_COMP__MC"]))
                {
                    AddBodyContentCell(table, record["COMPANY_ID_V_D_FW_COMP__MC"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["ROLES_ID_V_D_FW_S_ROLES__MC"]))
                {
                    AddBodyContentCell(table, record["ROLES_ID_V_D_FW_S_ROLES__MC"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }


                if (!string.IsNullOrEmpty((string)record["TEL1"]))
                {
                    AddBodyContentCell(table, record["TEL1"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["EMAIL"]))
                {
                    AddBodyContentCell(table, record["EMAIL"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["ADDR"]))
                {
                    AddBodyContentCell(table, record["ADDR"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (record["DISABLED"] != null)
                {
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