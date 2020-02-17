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
using Newtonsoft.Json.Converters;
using MisFrameWork3.Classes.Membership;

namespace MisFrameWork3.Classes.Controller
{
    public class FWBaseController : System.Web.Mvc.Controller
    {
        protected ActionResult JsonDateObject(object data)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
            return Content(JsonConvert.SerializeObject(data, Formatting.Indented, timeConverter));
        }
        /// <summary>
        /// 基础框架的数据查询函数
        /// </summary>
        /// <param name="tableName">要查询的表名</param>
        /// <param name="dataRangeFields">时间段查询条件支持的字段</param>
        /// <param name="conditionFields">参与查询的字段名</param>
        /// <param name="externalCondition">附加的条件，比如有些查询要过滤只查本人信息的，可以设置这个参数</param>
        /// <param name="returnFields">查询结果返回的字段列表</param>
        /// <returns></returns>
        protected ActionResult QueryDataFromEasyUIDataGrid(string tableName, string dataRangeFields, string conditionFields,Condition externalCondition,string returnFields)
        {
            int limit = 30;
            int offset = 0;
            try
            {
                limit = int.Parse(Request["rows"]);
                offset = (int.Parse(Request["page"]) - 1) * limit;
            }
            catch (Exception ee)
            {
                limit = 30;
                offset = 0;
            }
            string id = Request["id"];
            string search = Request["search"];
            string sort = Request["sort"];
            string order = Request["order"];
            Condition cdtMain = new Condition();
            if (Request["cdt_combination"]!=null)
            {
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
                        if (c.Src == "COMPANY_ID_1")
                        {
                            char[] company = c.Tag.ToString().ToCharArray();
                            string comId = "";
                            bool temp = false;
                            for (int j = company.Length - 2; j >= 0; j--)
                            {
                                string cc = company[j].ToString();
                                if (cc != "0" && !temp)
                                {
                                    temp = true;
                                }
                                if (temp)
                                {
                                    comId += company[j];
                                }
                            }
                            char[] charArray = comId.ToCharArray();
                            Array.Reverse(charArray);
                            string comId3 = new String(charArray);
                            c.Src = "COMPANY_ID";
                            c.Tag = comId3+"%";
                        }                       
                    }
                }
                cdtMain.AddSubCondition(cdtCombination);
            }
            
            if (!string.IsNullOrEmpty(id))//输入了ID的话，只查询一条记录
            {
                string idFieldName = DbUtilityManager.Instance.DefaultDbUtility.CreateTableInfo(tableName).PrimaryFields.Keys.FirstOrDefault();
                if (idFieldName==null)
                {
                    if (tableName.StartsWith("V_D_") || tableName.StartsWith("D_"))
                        idFieldName = "DM";
                    else
                        idFieldName = "ID";
                }
                Condition cdtId = new Condition();
                string[] ids = id.Split(',');
                foreach (string v in ids)
                    cdtId.AddSubCondition("OR", idFieldName, "=", v); 
                cdtMain.AddSubCondition(cdtId);
            }
            else
            {
                if ((!string.IsNullOrEmpty(search))&&(!string.IsNullOrEmpty(conditionFields)))
                {
                    string[] cdtFields = conditionFields.Split(',');
                    string[] values = search.Replace("，",",").Split(',');
                    bool commaIsOrRelation = !String.IsNullOrEmpty(Request["comma_is_or"]);//查询内容的“,”号是用于OR关系
                    foreach (string v in values)
                    {
                        Condition cdtSearch = new Condition();
                        foreach (string cdeField in cdtFields)
                        {
                            cdtSearch.AddSubCondition("OR", cdeField, "like", "%" + v + "%");
                        }
                        if (commaIsOrRelation)
                            cdtSearch.Relate = "OR";
                        else
                            cdtSearch.Relate = "AND";
                        cdtMain.AddSubCondition(cdtSearch);
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
                    catch(Exception e)
                    {
                        dataRangeTypeIndex = 0;
                    }
                    string[] arrDataRangeFields = dataRangeFields.Split(',');
                    //(arrDataRangeFields.Length <= dataRangeTypeIndex)&&
                    if ( dataRangeTypeIndex!=0)
                    {
                        fieldName = arrDataRangeFields[dataRangeTypeIndex-1];
                        if (!String.IsNullOrEmpty(Request["start_date"]))
                        {
                            cdtMain.AddSubCondition("AND", fieldName, ">=", DateTime.Parse(Request["start_date"]));
                        }
                        if (!String.IsNullOrEmpty(Request["end_date"]))
                        {
                            DateTime dtEndDate = DateTime.Parse(Request["end_date"]);
                            dtEndDate = dtEndDate.AddDays(1);//加多一天
                            cdtMain.AddSubCondition("AND", fieldName, "<", dtEndDate);
                        }
                    }
                }
            }
            string sortInfor = null;
            if (sort != null)
            {
                sortInfor = sort;
                if (order != null)
                    sortInfor += " " + order;
            }
            if (externalCondition != null)
                cdtMain.AddSubCondition(externalCondition);
            
            //if (String.IsNullOrEmpty(Request["id"]))
            //{
            //    if (!string.IsNullOrEmpty(Request["filter"]))
            //    {
            //        Condition cdtFilter = new Condition();
            //        string[] fs = Request["filter"].Split(',');
            //        foreach(string f in fs)
            //            cdtFilter.AddSubCondition("OR","DM","LIKE",f+"%");
            //        cdtMain.AddSubCondition(cdtFilter);
            //    }
                    
            //}
            List<UnCaseSenseHashTable> rows = DbUtilityManager.Instance.DefaultDbUtility.Query(tableName, cdtMain, returnFields, sortInfor, null, offset, limit);
            int total = DbUtilityManager.Instance.DefaultDbUtility.RecordCount(tableName, cdtMain);
            return JsonDateObject(new { total = total, rows = rows });
        }

        protected void ReplaceCdtCombinationOpreate(Condition rootCdt)//处理客户端传来的条件操作符
        {
            if (!String.IsNullOrEmpty(rootCdt.Op))
            {
                switch(rootCdt.Op)
                {
                    case "start"  :rootCdt.Op="like";rootCdt.Tag=rootCdt.Tag+"%";break;
                    case "end"    :rootCdt.Op="like";rootCdt.Tag="%"+rootCdt.Tag;break;
                    case "contain":rootCdt.Op="like";rootCdt.Tag="%"+rootCdt.Tag+"%";break;
                    case "not_contain":rootCdt.Not = true;rootCdt.Op="like";rootCdt.Tag="%"+rootCdt.Tag+"%";break;
                    case "eq"     :rootCdt.Op="=";break;
                    case "ne"     :rootCdt.Op="<>";break;
                    case "gt"     :rootCdt.Op=">";break;
                    case "lt"     :rootCdt.Op="<";break;
                    case "ge"     :rootCdt.Op=">=";break;
                    case "le"     :rootCdt.Op="<=";break;
                }
            }
            if (rootCdt.SubConditions!=null && rootCdt.SubConditions.Count>0)
            {
                foreach (Condition u in rootCdt.SubConditions)
                    ReplaceCdtCombinationOpreate(u);
            }
        }

        public ActionResult ExportExcel()
        {
            string data = Request.Unvalidated.Form["data"];
            if (Request.UserAgent.ToLower().IndexOf("msie", System.StringComparison.Ordinal) > -1)//IE浏览器
            {
                Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(DateTime.Now.ToString("yyyyMMddHHmm")+".xls"));
            }
 
            else //其他浏览器
            {
                Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlDecode(DateTime.Now.ToString("yyyyMMddHHmm")+".xls", System.Text.Encoding.UTF8));
            }
            byte[] fileBs = ASCIIEncoding.UTF8.GetBytes(data);
            Response.AddHeader("Content-Length", fileBs.Length.ToString());
            Response.AddHeader("Content-Transfer-Encoding", "binary");
            Response.ContentType = "application/octet-stream;charset=utf8";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.BinaryWrite(fileBs);
            Response.Flush();
            return new EmptyResult();
        }

        protected List<UnCaseSenseHashTable> GetDicData(string dicName,Condition extCDT )
        {
            return GetDicData(dicName,null,extCDT);
        }

        protected List<UnCaseSenseHashTable> GetDicData(string dicName,string filter,Condition extCDT )
        {
            Condition cdtMain = new Condition();
            Condition cdtQ = new Condition();   
            if (!String.IsNullOrEmpty(Request["id"]))
            {
                string id = Request["id"].Replace("，", ",");
                if (id.LastIndexOf(',') > 0)
                {
                    string[] arrQuery = id.Split(',');
                    foreach (string aq in arrQuery)
                    {
                        if (!String.IsNullOrEmpty(aq))
                            cdtQ.AddSubCondition("OR", "ID", "=", aq);
                    }
                }
                cdtMain.AddSubCondition(cdtQ);
            }
            else if (!String.IsNullOrEmpty(Request["q"]))
            {
                string query = Request["q"].Replace("，",",");                
                if (query.LastIndexOf(',') > 0)
                {
                    string[] arrQuery = query.Split(',');
                    foreach(string aq in arrQuery)
                    {
                        if (!String.IsNullOrEmpty(aq))
                            cdtQ.AddSubCondition("OR", "MC", "=", aq);
                    }
                    query = query.Substring(query.LastIndexOf(',') + 1);
                }
                cdtQ.AddSubCondition("OR", "DM", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "MC", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "WB", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "PY", "like", "%" + query + "%");
                cdtMain.AddSubCondition(cdtQ);
            }
            if (String.IsNullOrEmpty(Request["id"]))
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    Condition cdtFilter = new Condition();
                    string[] fs = filter.Split(',');
                    foreach(string f in fs)
                        cdtFilter.AddSubCondition("OR","DM","LIKE",f+"%");
                    cdtMain.AddSubCondition(cdtFilter);
                }
                    
            }
            if (extCDT != null)
                cdtMain.AddSubCondition(extCDT);
            return DbUtilityManager.Instance.DefaultDbUtility.Query(dicName, cdtMain, "*", "DM");            
        }




        protected List<UnCaseSenseHashTable> GetDicCity(string dicName, string filter, Condition extCDT)
        {
            Condition cdtMain = new Condition();
            Condition cdtQ = new Condition();
            if (!String.IsNullOrEmpty(Request["id"]))
            {
                string id = Request["id"].Replace("，", ",");
                if (id.LastIndexOf(',') > 0)
                {
                    string[] arrQuery = id.Split(',');
                    foreach (string aq in arrQuery)
                    {
                        if (!String.IsNullOrEmpty(aq))
                            cdtQ.AddSubCondition("OR", "ID", "=", aq);
                    }
                }
                cdtMain.AddSubCondition(cdtQ);
            }
            else if (!String.IsNullOrEmpty(Request["q"]))
            {
                string query = Request["q"].Replace("，", ",");
                if (query.LastIndexOf(',') > 0)
                {
                    string[] arrQuery = query.Split(',');
                    foreach (string aq in arrQuery)
                    {
                        if (!String.IsNullOrEmpty(aq))
                            cdtQ.AddSubCondition("OR", "MC", "=", aq);
                    }
                    query = query.Substring(query.LastIndexOf(',') + 1);
                }
                cdtQ.AddSubCondition("OR", "DM", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "MC", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "WB", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "PY", "like", "%" + query + "%");
                cdtMain.AddSubCondition(cdtQ);
            }
            if (String.IsNullOrEmpty(Request["id"]))
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    Condition cdtFilter = new Condition();
                    string Code = filter.Substring(0, 4) + "__000000";
                    //string[] fs = filter.Split(',');
                    //foreach (string f in fs)
                    cdtFilter.AddSubCondition("AND", "DM", "LIKE", Code);
                    cdtMain.AddSubCondition(cdtFilter);
                }
            }
            if (extCDT != null)
                cdtMain.AddSubCondition(extCDT);
            return DbUtilityManager.Instance.DefaultDbUtility.Query(dicName, cdtMain, "*", "DM");
        }



        protected List<UnCaseSenseHashTable> GetDicArea(string dicName, string filter, Condition extCDT)
        {
            Condition cdtMain = new Condition();
            Condition cdtQ = new Condition();
            if (!String.IsNullOrEmpty(Request["id"]))
            {
                string id = Request["id"].Replace("，", ",");
                if (id.LastIndexOf(',') > 0)
                {
                    string[] arrQuery = id.Split(',');
                    foreach (string aq in arrQuery)
                    {
                        if (!String.IsNullOrEmpty(aq))
                            cdtQ.AddSubCondition("OR", "ID", "=", aq);
                    }
                }
                cdtMain.AddSubCondition(cdtQ);
            }
            else if (!String.IsNullOrEmpty(Request["q"]))
            {
                string query = Request["q"].Replace("，", ",");
                if (query.LastIndexOf(',') > 0)
                {
                    string[] arrQuery = query.Split(',');
                    foreach (string aq in arrQuery)
                    {
                        if (!String.IsNullOrEmpty(aq))
                            cdtQ.AddSubCondition("OR", "MC", "=", aq);
                    }
                    query = query.Substring(query.LastIndexOf(',') + 1);
                }
                cdtQ.AddSubCondition("OR", "DM", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "MC", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "WB", "like", "%" + query + "%");
                cdtQ.AddSubCondition("OR", "PY", "like", "%" + query + "%");
                cdtMain.AddSubCondition(cdtQ);
            }
            if (String.IsNullOrEmpty(Request["id"]))
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    Condition cdtFilter = new Condition();
                    string Code = filter.Substring(0, 6) + "__0000";
                    //string[] fs = filter.Split(',');
                    //foreach (string f in fs)
                    cdtFilter.AddSubCondition("AND", "DM", "LIKE", Code);
                    cdtMain.AddSubCondition(cdtFilter);
                }
            }
            if (extCDT != null)
                cdtMain.AddSubCondition(extCDT);
            return DbUtilityManager.Instance.DefaultDbUtility.Query("V_D_FW_COMP", cdtMain, "*", "DM");
        }


        //获取当前用户可查机器编号
        protected string GetMachineNo()
        {
            int RoleLevel = Membership.Membership.CurrentUser.RoleLevel;
            List<UnCaseSenseHashTable> record;
            if (!Membership.Membership.CurrentUser.HaveAuthority("SYS.USER.SELECT_OTHOR_COMPANY"))
            {
                string COMPANY_ID = Membership.Membership.CurrentUser.CompanyId.ToString();
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
                string comId2 = new String(charArray);
                comId2 += "%";
                Condition cdtId = new Condition();
                cdtId.AddSubCondition("AND", "SSDW", "like", comId2);
                cdtId.AddSubCondition("AND", "DELETED_MARK", "=", "0");
                record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtId, "MACHINENO", null, null, -1, -1);
            }
            else
            {
                Condition cdtId = new Condition();
                cdtId.AddSubCondition("AND", "DELETED_MARK", "=", "0");
                record = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtId, "MACHINENO", null, null, -1, -1);
            }
            string zzjbh = "(";
            for (int i = 0; i < record.Count; i++)
            {
                zzjbh += "'" + record[i]["MACHINENO"] + "',";
            }
            zzjbh += "'0')";
            return zzjbh;
        }

        protected string GetCompanyNo()
        {
             int RoleLevel = Membership.Membership.CurrentUser.RoleLevel;          
              string COMPANY_ID = Membership.Membership.CurrentUser.CompanyId.ToString();
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
                string comId2 = new String(charArray);
                comId2 ="'"+ comId2+"%'";              
                return comId2;
        }
    }
}