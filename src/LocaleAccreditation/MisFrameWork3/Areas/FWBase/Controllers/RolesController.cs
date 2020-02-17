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
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Authorize;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MisFrameWork3.Classes.Membership;
using iTextSharp.text;
using iTextSharp.text.pdf;


namespace MisFrameWork3.Areas.FWBase.Controllers
{
    [Logined(OperateId = "SYS.ROLE")]
    public class RolesController : FWBaseController
    {
        // GET: FWBase/Roles
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
            data["ROLE_NAME"] = Request["ROLE_NAME"];
            data["ROLE_DESCRIPT"] = Request["ROLE_DESCRIPT"];
            data["SORT_CODE"] = Request["SORT_CODE"];
            
            string[] allOperate = new string[0];
            if (!string.IsNullOrEmpty(Request["OPERATE"]))
            {
                allOperate = Request["OPERATE"].Split(',');
            }

            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                
                session.BeginTransaction();
                DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "FW_S_ROLES", data);
                
                foreach(string op in allOperate)
                {
                    UnCaseSenseHashTable operate = new UnCaseSenseHashTable();
                    operate["ROLE_ID"] = data["ID"];
                    operate["OPERATE_ID"] = op;
                    DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "FW_S_ROLES_OPERATE", operate);                    
                }
                
                session.Commit();
                session.Close();
            }
            catch(Exception e)
            {
                session.Rollback();
                session.Close();
                var eResult = new { success = false, message = e.ToString() };
                return Json(eResult, JsonRequestBehavior.AllowGet); ;
            }
            return Json(new { success = true, message = "保存成功" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ActionEdit()
        {

            UnCaseSenseHashTable data = new UnCaseSenseHashTable();
            data["SORT_CODE"] = Request["SORT_CODE"];
            int sortCode = 0;
            try
            {
                sortCode = int.Parse(Request["SORT_CODE"]);
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = ee }, JsonRequestBehavior.AllowGet);
            }
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            if (RoleLevel <= sortCode || RoleLevel == 0 || RoleLevel == 9)
            {
                data["ID"] = Request["OBJECT_ID"];
                data["ROLE_NAME"] = Request["ROLE_NAME"];
                data["ROLE_DESCRIPT"] = Request["ROLE_DESCRIPT"];

                string[] allOperate = new string[0];
                if (!string.IsNullOrEmpty(Request["OPERATE"]))
                {
                    allOperate = Request["OPERATE"].Split(',');
                }

                Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
                try
                {
                    session.BeginTransaction();
                    DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "FW_S_ROLES", data, false);
                    DbUtilityManager.Instance.DefaultDbUtility.Execute("delete from FW_S_ROLES_OPERATE where ROLE_ID=" + int.Parse(data["ID"].ToString()));
                    foreach (string op in allOperate)
                    {
                        UnCaseSenseHashTable operate = new UnCaseSenseHashTable();
                        operate["ROLE_ID"] = data["ID"];
                        operate["OPERATE_ID"] = op;
                        DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "FW_S_ROLES_OPERATE", operate);
                    }

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
            else
            {
                var result = new { success = false, message = "权限不足无法修改！" };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
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
            catch(Exception ee)
            {
                return Json(new { success = false, message = "主键值有误【"+id+"】" }, JsonRequestBehavior.AllowGet);
            }

            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();
                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  FW_S_ROLES_OPERATE set DISABLED="+state+" where ROLE_ID="+id.ToString());
                DbUtilityManager.Instance.DefaultDbUtility.Execute("update  FW_S_ROLES set DISABLED=" + state + " where ID=" + id.ToString());
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

        public ActionResult ActionDelete()
        {
            return View();
        }

        public ActionResult JsonDataList()
        {
            int RoleLevel = Membership.CurrentUser.RoleLevel;
            Condition cdtIds = new Condition();
            if (!Membership.CurrentUser.HaveAuthority("SYS.USER.QUERY_ALL_USER")) 
            {
                cdtIds.AddSubCondition("AND", "SORT_CODE", ">", RoleLevel);
            }
            //#region 初始化基本查询参数 id,limit,offset,search,sort,order
            return QueryDataFromEasyUIDataGrid("FW_S_ROLES", "CREATE_ON", "ROLE_NAME,ROLE_DESCRIPT,CREATE_BY,UPDATE_BY", cdtIds, "*");
        }

        public ActionResult JsonMenuData()
        {
            string object_id = Request["OBJECT_ID"];
            List<UnCaseSenseHashTable> authority = new List<UnCaseSenseHashTable>();
            if (object_id!=null)
            {
                Condition cdt = new Condition();
                cdt.AddSubCondition("AND", "ROLE_ID", "=", object_id);
                authority = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_ROLES_OPERATE", cdt, "*", null);
            }
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
            CalNodes("",jsonObj, ref jsonResult, authority);
            return this.Content(jsonResult.ToString(), "application/json");
        }

        /// <summary>
        /// 判断是否具备oprate权限
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="oprate"></param>
        /// <returns></returns>
        private bool HaveAuthority(List<UnCaseSenseHashTable> authority,string oprate)
        {
            foreach(UnCaseSenseHashTable record in authority)
            {
                if (record.HasValue("OPERATE_ID") && record["OPERATE_ID"].Equals(oprate))
                    return true;
            }
            return false;
        }

        public void CalNodes(string parentId,JArray config, ref JArray r, List<UnCaseSenseHashTable> authority)
        {
            for (int i = 0;i<config.Count;i++)
            {
                JObject node =(JObject)config[i];
                JObject obj = new JObject();
                obj["id"] = node["id"];
                obj["text"] = node["text"];
                if (node["id"].ToString().StartsWith("GLOBAL")){
                    obj["iconCls"] = "fa-treenode-icon fa fa-cogs";
                    if(node["children"]==null)
                        obj["iconCls"] = "fa-treenode-icon fa fa-cog";
                }
                    

                if (node["children"]==null && HaveAuthority(authority, obj["id"].ToString()))
                    obj["checked"] = true;

                if (node["iconCls"]!=null)
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
                    CalNodes(obj["id"].ToString(),children, ref chilerenObj, authority);
                }
                r.Add(obj);
                if (node["option"] != null)
                {
                    //增加一个节点
                    JObject objOption = new JObject();
                    objOption["id"] = obj["id"].ToString()+".__OPTION__";
                    objOption["iconCls"] = "fa-treenode-icon fa fa-cogs";
                    objOption["text"] = obj["text"]+" 【可选项】";
                    JArray chilerenObjOption = new JArray();
                    objOption["children"] = chilerenObjOption;

                    string[] option = node["option"].ToString().Split(',');
                    foreach (string op in option)
                    {
                        string[] infor = op.Split(':');
                        JObject subOption = new JObject();
                        subOption["id"] = obj["id"].ToString()+"."+infor[0];
                        subOption["iconCls"] = "fa-treenode-icon fa fa-cog";
                        //subOption["text"] = "<span class=\"fa fa-cog\">" + infor[1];
                        subOption["text"] = infor[1];
                        if (HaveAuthority(authority, subOption["id"].ToString()))
                            subOption["checked"] = true;
                        chilerenObjOption.Add(subOption);
                    }
                    r.Add(objOption);
                }
                
            }
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
                cdtIds2.AddSubCondition("OR", "ROLE_NAME", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "ROLE_DESCRIPT", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "CREATE_BY", "like", "%" + search + "%");
                cdtIds2.AddSubCondition("OR", "UPDATE_BY", "like", "%" + search + "%");
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
            List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_ROLES", cdtIds, "*", null, null, -1, -1);

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
            PdfPTable table = new PdfPTable(8);
            table.SetWidths(new float[] { 2.5F, 8, 8, 12, 7, 6, 6, 6 });
            table.WidthPercentage = 100;
            AddBodyContentCell(table, "序号", cn);
            AddBodyContentCell(table, "角色名称", cn);
            AddBodyContentCell(table, "排序编号", cn);
            AddBodyContentCell(table, "创建用户", cn);
            AddBodyContentCell(table, "创建时间", cn);
            AddBodyContentCell(table, "最后编辑用户", cn);
            AddBodyContentCell(table, "更新时间", cn);
            AddBodyContentCell(table, "状态", cn);

            for (int i = 0; i < records.Count; i++)
            {
                UnCaseSenseHashTable record = records[i];
                AddBodyContentCell(table, Convert.ToString(i + 1), cn);
                if (!string.IsNullOrEmpty((string)record["ROLE_NAME"]))
                {
                    AddBodyContentCell(table, record["ROLE_NAME"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }

                if (!string.IsNullOrEmpty(record["SORT_CODE"].ToString()))
                {
                    AddBodyContentCell(table, record["SORT_CODE"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }
                if (!string.IsNullOrEmpty((string)record["CREATE_BY"]))
                {
                    AddBodyContentCell(table, record["CREATE_BY"].ToString(), cn);
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

                if (!string.IsNullOrEmpty((string)record["UPDATE_BY"]))
                {
                    AddBodyContentCell(table, record["UPDATE_BY"].ToString(), cn);
                }
                else
                {
                    AddBodyContentCell(table, "", cn);
                }
                
                if (record["UPDATE_ON"] != null)
                {
                    if (!string.IsNullOrEmpty(record["UPDATE_ON"].ToString()))
                    {
                        string s = record["UPDATE_ON"].ToString();
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