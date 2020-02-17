        using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.IO;
using System.Globalization;
using System.IO.Compression;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
using MisFrameWork3.Classes.Authorize;
using System.Web.Script.Serialization;
//using AutoUpdateWeb.Controllers;
using System.Web.Configuration;
using AutoUpdateWeb.Class;

namespace MisFrameWork3.Areas.WhiteCard.Controllers
{
    public class StockController : FWBaseController
    {
        public ActionResult Index()
        {
            if (Membership.CurrentUser.HaveAuthority("SYS.USER.QUERY_ALL_USER"))
            {
                ViewBag.DisableBTN_Add = false;
                ViewBag.DisableBTN_Edit = false;
            }
            else
            {
                ViewBag.DisableBTN_Add = true;
                ViewBag.DisableBTN_Edit = true;
            }
            return View();
        }

        public ActionResult JsonConditionStock()
        {
            return View();
        }
        public ActionResult GetSelect()
        {
            List<UnCaseSenseHashTable> planType = DbUtilityManager.Instance.DefaultDbUtility.Query("V_D_FW_COMP", null, "DM,MC", null, null, -1, -1);
            return JsonDateObject(new {planType = planType });
        }
        #region __TIPS__:框架通用函数( 增 删 改)
        public ActionResult JsonDataList()//业务主界面数据查询函数
        {
            Condition cdtId = new Condition();
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
                cdtId .AddSubCondition("AND", "COMPANY_ID", "like", comId3);
                string company = Request["COMPANY_ID_1"];
                if (!string.IsNullOrEmpty(company)) {
                    company = "da";
                }
               
            }           
            return QueryDataFromEasyUIDataGrid("B_CARD_STOCK", "INPUT_TIME,STOCK_WHOLE", "COMPANY_ID,STOCK_WHOLE,STOCK_OVERPLUS,INPUT_TIME,COMPANY_ID_V_D_FW_COMP__MC,STOCK_SCRAP,CLEAN_CARD,CARD_SD", cdtId, "*");

        }
        public ActionResult JsonDataList1()//业务主界面数据查询函数
        {
            try
            {
                UnCaseSenseHashTable data = new UnCaseSenseHashTable();
                Condition cdtId = new Condition();
                cdtId = new Condition("AND", "COMPANY_ID", "=", Request["id"]);
                List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_CARD_STOCK", cdtId, "*", null, null, -1, -1);
                if (record.Count == 1)
                {
                    data["STOCK_WHOLE"] = record[0]["STOCK_WHOLE"];
                    data["STOCK_OVERPLUS"] = record[0]["STOCK_OVERPLUS"];
                    data["STOCK_SCRAP"] = record[0]["STOCK_SCRAP"];
                    int CARD_SD = Convert.ToInt32(record[0]["CARD_SD"]);
                    //int a = CARD_SD / 500;
                    data["CARD_SD"] = CARD_SD / 500;
                }
                return JsonDateObject(data);
            }
            catch (Exception e) {
                return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult ViewFormAdd()
        {
            return View();
        }

        public ActionResult ViewFormEdit()
        {
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
                ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("B_CARD_STOCK");
                data.LoadFromNameValueCollection(Request.Unvalidated.Form, ti, true);//使用Request.Unvalidated.Form可以POST HTML标签数据。
                Condition cdtId = new Condition("AND", "COMPANY_ID", "=", data["COMPANY_ID"]);
                List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_CARD_STOCK", cdtId, "*", null, null, -1, -1);
                if (record.Count == 1)
                {
                    return Json(new { success = false, message = "已存在该单位！" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    data["STOCK_OVERPLUS"] = "0";
                    data["STOCK_WHOLE"] = "0";
                    data["STOCK_SCRAP"] = "0";
                    session.BeginTransaction();
                    int r = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_CARD_STOCK", data);
                    session.Commit();
                    session.Close();
                    if (0 == r)
                    {
                        return Json(new { success = false, message = "保存信息时出错！" }, JsonRequestBehavior.AllowGet);
                    }
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
            string RoleLevel = Membership.CurrentUser.RoleLevel.ToString();
            try
            {
                //__TIPS__*:这里修改表名，参考ActionAdd 
                string COMPANY_ID_1 = "440000000000";
                int number=0;
                string COMPANY_ID_2 = Membership.CurrentUser.CompanyId.ToString();
                Int64 COMPANY_ID_1_1 = Convert.ToInt64(COMPANY_ID_1);
                Int64 COMPANY_ID_2_1 = Convert.ToInt64(COMPANY_ID_2);
                if (COMPANY_ID_2_1 == COMPANY_ID_1_1|| RoleLevel=="0") {
                    Condition cdtId_add = new Condition("AND", "COMPANY_ID", "=", COMPANY_ID_1);
                    List<UnCaseSenseHashTable> record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_CARD_STOCK", cdtId_add, "*", null, null, -1, -1);
                    data.LoadFromNameValueCollection(Request.Unvalidated.Form, "STOCK_WHOLE|STOCK_OVERPLUS|STOCK_SCRAP|CARD_SD", true);
                    string nowtime = DateTime.Now.ToString("yyyy-MM-dd");
                    data["ID"] = record[0]["ID"];
                    data["COMPANY_ID"] = record[0]["COMPANY_ID"];
                    data["COMPANY_ID_V_D_FW_COMP__MC"] = record[0]["COMPANY_ID_V_D_FW_COMP__MC"];
                    data["INPUT_TIME"] = nowtime;
                    if (!string.IsNullOrEmpty(data["CARD_SD"].ToString()))
                    {
                        number = Convert.ToInt32(data["CARD_SD"].ToString())*500;
                    }
                    data["CARD_SD"] = number;
                    session.BeginTransaction();
                    int r = DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "B_CARD_STOCK", data, false);
                    session.Commit();
                    session.Close();
                    if (0 == r)
                    {
                        return Json(new { success = false, message = "保存信息时出错！" }, JsonRequestBehavior.AllowGet);
                    }
                }
                else {
                    return Json(new { success = false, message = "无该操作权限" }, JsonRequestBehavior.AllowGet);
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
                DbUtilityManager.Instance.DefaultDbUtility.Execute("delete from B_PLAN  where ID=" + id.ToString());
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
                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  B_PLAN set DISABLED=" + state + " where ID=" + id.ToString());
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
            if ("V_D_FW_COMP".Equals(Request["dic"]))
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
                return QueryDataFromEasyUIDataGrid(Request["dic"], null, "DM,MC,PY", null, "*");
            }
        }

        public ActionResult ViewDicLargeUI()
        {
            /*
             * __TIPS__:有些特殊的字典可能需要显示更多的东西所以这里可以根据Request的值返回不同的视图
             *          以下演示根据字典内容，返回不同的视图。
             * */
            if ("V_D_FW_COMP".Equals(Request["dic"])) {
                return View("ViewStockType");
            }
            else
            {
                return View("~/Views/Shared/ViewCommonDicUI.cshtml");
            }
        }
        #endregion


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
                cdtIds2.AddSubCondition("OR", "STOCK_WHOLE", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "STOCK_OVERPLUS", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "INPUT_TIME", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "COMPANY_ID", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "COMPANY_ID_V_D_FW_COMP__MC", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "STOCK_SCRAP", "like", "%" + search + "%");                
            }
            if (!string.IsNullOrEmpty(date_range_type) && date_range_type != "0" && (!string.IsNullOrEmpty(start_date) || !string.IsNullOrEmpty(end_date)))
            {
                if (!string.IsNullOrEmpty(start_date))
                {
                    cdtIds.AddSubCondition("AND", "INPUT_TIME", "=", DateTime.Parse(start_date));
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
                cdtIds.AddSubCondition("AND", "COMPANY_ID", "like", comId3);         
            }
            List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("B_CARD_STOCK", cdtIds, "*", null, null, -1, -1);

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
            PdfPTable table = new PdfPTable(7);
            table.SetWidths(new float[] { 2.5F, 8, 8, 8, 8, 8,8 });
            table.WidthPercentage = 100;
            AddBodyContentCell(table, "序号", cn);
            AddBodyContentCell(table, "单位编号", cn);
            AddBodyContentCell(table, "单位名称", cn);
            AddBodyContentCell(table, "报废库存", cn);
            AddBodyContentCell(table, "剩余库存", cn);
            AddBodyContentCell(table, "全部库存", cn);
            AddBodyContentCell(table, "录入时间", cn); 

            for (int i = 0; i < records.Count; i++)
            {
                UnCaseSenseHashTable record = records[i];
                AddBodyContentCell(table, Convert.ToString(i + 1), cn);
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
                if (!string.IsNullOrEmpty(record["STOCK_SCRAP"].ToString()))
                {
                    AddBodyContentCell(table, record["STOCK_SCRAP"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                

                if (!string.IsNullOrEmpty(record["STOCK_OVERPLUS"].ToString()))
                {
                    AddBodyContentCell(table, record["STOCK_OVERPLUS"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty(record["STOCK_WHOLE"].ToString()))
                {
                    AddBodyContentCell(table, record["STOCK_WHOLE"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                } 
                if (!string.IsNullOrEmpty(record["INPUT_TIME"].ToString()))
                {
                    string s = record["INPUT_TIME"].ToString();
                    string date = s.Substring(0, 8);
                    AddBodyContentCell(table, date, cn);
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

    }
}