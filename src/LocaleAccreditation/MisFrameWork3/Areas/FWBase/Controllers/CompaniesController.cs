using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Authorize;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
namespace MisFrameWork3.Areas.FWBase.Controllers
{
    [Logined(OperateId = "SYS.COMPANIES")]
    public class CompaniesController : FWBaseController
    {
        // GET: FWBase/Companies
        #region 增删改
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
            ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("FW_S_COMAPANIES");
            data.LoadFromNameValueCollection(Request.Form, ti, true);

            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();
                int r = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "FW_S_COMAPANIES", data);                
                session.Commit();
                session.Close();
                if (r!=1)
                    return Json(new { success = false, message = "保存记录失败！" }, JsonRequestBehavior.AllowGet);
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
            
            ITableInfo ti = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo("FW_S_COMAPANIES");
            data["ID"] = Request["OBJECT_ID"];//这一字字段是加载不进来的。
            data.LoadFromNameValueCollection(Request.Form, ti, true);

            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();

                DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "FW_S_COMAPANIES", data, false);
                
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
            var result = new { success = true, message = "保存成功" };
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
                if (state==1)//如果是禁用的，要同时把用户也禁用了，如果是启用的，就要手动去逐个启用用户
                    DbUtilityManager.Instance.DefaultDbUtility.Execute("update  FW_S_USERS set DISABLED=" + state + " where COMPANY_ID=" + id.ToString());
                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  FW_S_COMAPANIES set DISABLED=" + state + " where ID=" + id.ToString());
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
            return QueryDataFromEasyUIDataGrid("FW_S_COMAPANIES", "CREATE_ON", "NAME,COMPANY_CODE,FAX,TEL1,ADDR_WORKING", null, "*");
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
                cdtIds2.AddSubCondition("OR", "NAME", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "COMPANY_CODE", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "FAX", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "TEL1", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "TEL2", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "ADDR_WORKING", "like", "%" + search + "%");
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
            List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_COMAPANIES", cdtIds, "*", null, null, -1, -1);

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
            PdfPTable table = new PdfPTable(9);
            table.SetWidths(new float[] { 2.5F, 8, 8, 12, 7, 6, 6, 6, 6 });
            table.WidthPercentage = 100;
            AddBodyContentCell(table, "序号", cn);
            AddBodyContentCell(table, "单位编号", cn);
            AddBodyContentCell(table, "单位名称", cn);
            AddBodyContentCell(table, "录入时间", cn);
            AddBodyContentCell(table, "状态", cn);
            AddBodyContentCell(table, "联系电话1", cn);
            AddBodyContentCell(table, "联系电话2", cn);
            AddBodyContentCell(table, "传真号码", cn);
            AddBodyContentCell(table, "办公地址", cn);

            for (int i = 0; i < records.Count; i++)
            {
                UnCaseSenseHashTable record = records[i];
                AddBodyContentCell(table, Convert.ToString(i + 1), cn);
                if (!string.IsNullOrEmpty((string)record["COMPANY_CODE"]))
                {
                    AddBodyContentCell(table, record["COMPANY_CODE"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["NAME"]))
                {
                    AddBodyContentCell(table, record["NAME"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (record["CREATE_ON"] != null)
                {
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

                if (!string.IsNullOrEmpty((string)record["TEL1"]))
                {
                    AddBodyContentCell(table, record["TEL1"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["TEL2"]))
                {
                    AddBodyContentCell(table, record["TEL2"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["FAX"]))
                {
                    AddBodyContentCell(table, record["FAX"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty((string)record["ADDR_WORKING"]))
                {
                    AddBodyContentCell(table, record["ADDR_WORKING"].ToString(), cn);
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