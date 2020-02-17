using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
using MisFrameWork3.Classes.Authorize;
using System.Collections;

namespace MisFrameWork3.Areas.Card_Xcff.Controllers
{
    public class Card_XcffController : FWBaseController
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

        public ActionResult ViewFormPhoto()
        {
            return View();
        }


        public ActionResult GetPhoto()//业务主界面数据查询函数
        {
            //接收的基本查询参数有： id,limit,offset,search,sort,order            
            //__TIPS__*:根据表结构，修改以下函数的参数
            string GMSFHM = Request["GMSFHM"].ToString();
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            Hashtable data = new Hashtable();
            try
            {
                Condition cdtIds = new Condition();
                cdtIds.AddSubCondition("AND", "GMSFHM", "=", GMSFHM);
                List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query(session, "B_XCFF", cdtIds, "*", null, null, -1, -1);
                if (records.Count >=1) {
                 byte[] imgByte = (byte[])records[0]["ZP"];
                 data["photoUrl"] = imgByte;
                }

                
                return JsonDateObject(data);
            }
            catch (Exception ee)
            {
                return Json(new { success = false, message = ee.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult JsonDataList()//业务主界面数据查询函数
        {
            //接收的基本查询参数有： id,limit,offset,search,sort,order            
            //__TIPS__*:根据表结构，修改以下函数的参数
            return QueryDataFromEasyUIDataGrid("B_XCFF", "FFRQ", "GMSFHM,FFDW_DWMC,FFDW_DWDM,FFRXM,FFRJH", null, "*");
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