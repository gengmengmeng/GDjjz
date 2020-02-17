using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Web.Mvc;
using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace MisFrameWork3.Classes.Controller
{
    public class FWExtServiceController : System.Web.Mvc.Controller
    {
        public class FWExtServiceResult
        {
            public int RESULT = 0;
            public string MSG = "";
            public UnCaseSenseHashTable DATA = new UnCaseSenseHashTable();           
        }

        public class FWExtServiceMethodAttribute : Attribute
        {
            public string Descript
            {
                get;
                set;
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
        public class FWExtServiceParametersAttribute : Attribute
        {

            public string Name
            {
                get;
                set;
            }

            public string Descript
            {
                get;
                set;
            }

            public string DefaultValue
            {
                get;
                set;
            }

            public string ValueFromFile
            {
                get;
                set;
            }            
        }

        protected ActionResult JsonDateObject(object data)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
            this.Response.ContentType = "application/json";
            return Content(JsonConvert.SerializeObject(data, Formatting.Indented, timeConverter));
        }

        public virtual ActionResult Interface()
        {
            if (this.Request["info"]!=null)
                return ViewInterfaceInfo();
            if (this.Request["query"]!=null)
                return JsonInterfaceInfo();
            UnCaseSenseHashTable package = new UnCaseSenseHashTable();
            try
            {
                if (this.Request.Unvalidated["data"]!=null)
                    package.LoadFromXml(this.Request.Unvalidated["data"]);
                else
                    package.LoadFromJson(this.Request.Unvalidated["json"]);
                string action = package["ACTION"].ToString();
                UnCaseSenseHashTable pdata = (UnCaseSenseHashTable)package["DATA"];
                //通过反射调用
                return InvoteAction(action,pdata);
            }
            catch(Exception ee)
            {
                return JsonDateObject(new { RESULT=-1,MSG=ee.Message});
            }
        }

        private ActionResult InvoteAction(string actionName,UnCaseSenseHashTable pdata)
        {
            Type type = this.GetType();
            try
            {
                MethodInfo method = type.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (method==null)
                {
                    return JsonDateObject(new { RESULT=-2,MSG="功能函数 【"+actionName+"】 不存在"});
                }
                ParameterInfo[] pis = method.GetParameters();
                object[] invoteParams = new object[pis.Length];
                invoteParams[0] = pdata;
                for(int i=1;i<pis.Length;i++)
                {
                    if (pis[i].ParameterType.Name.ToLower().StartsWith("int"))
                    {
                        if (!pdata.HasValue(pis[i].Name))
                            return JsonDateObject(new { RESULT=-2,MSG="数值参数 【"+actionName+"】 未赋值"});
                        invoteParams[i] = pdata.GetIntValue(pis[i].Name);
                    }
                    else if (pis[i].ParameterType.Name.ToLower().StartsWith("decimal"))
                    {
                        if (!pdata.HasValue(pis[i].Name))
                            return JsonDateObject(new { RESULT=-2,MSG="数值参数 【"+actionName+"】 未赋值"});
                        invoteParams[i] = pdata.GetDecimalValue(pis[i].Name);
                    }
                    else
                    {
                        invoteParams[i] = pdata[pis[i].Name];
                    }
                }
                FWExtServiceResult result = (FWExtServiceResult)method.Invoke(this,invoteParams);
                return JsonDateObject(result);
            }
            catch(Exception ee)
            {
                return JsonDateObject(new { RESULT=-1,MSG=ee.Message});
            }
        }

        private UnCaseSenseHashTable QueryInterfaceInfo()
        {
            Type type = this.GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance|BindingFlags.Public);
            UnCaseSenseHashTable result = new UnCaseSenseHashTable();
            foreach(MethodInfo mi in methods)
            {
                if (mi.ReturnParameter.ParameterType!=typeof(FWExtServiceResult))
                    continue;
                UnCaseSenseHashTable ushMethod = new UnCaseSenseHashTable();
                IEnumerable<Attribute> methodAttributes = mi.GetCustomAttributes(typeof(FWExtServiceMethodAttribute));
                if (methodAttributes.Count()>0)
                    ushMethod["DESCRIPT"] = ((FWExtServiceMethodAttribute)methodAttributes.ElementAt(0)).Descript;
                else
                    ushMethod["DESCRIPT"] = "没有编写函数说明";
                
                ushMethod["NAME"]=mi.Name;
                List<UnCaseSenseHashTable> ps = new List<UnCaseSenseHashTable>();
                ushMethod["PARAMS"]=ps;
                ParameterInfo[] pis = mi.GetParameters();
                //查询描述
                IEnumerable<Attribute> attributes = mi.GetCustomAttributes(typeof(FWExtServiceParametersAttribute));
                UnCaseSenseHashTable ushAttributes = new UnCaseSenseHashTable();
                foreach (FWExtServiceParametersAttribute attr in attributes)
                {
                    UnCaseSenseHashTable ushAttr = new UnCaseSenseHashTable();
                    ushAttr["DESCRIPT"] = attr.Descript;
                    if (!String.IsNullOrEmpty(attr.ValueFromFile))
                    {
                        try
                        {
                            string filePath = System.Web.HttpRuntime.AppDomainAppPath + "ext_service_sample\\"+attr.ValueFromFile;
                            FileStream fs = new FileStream(filePath, FileMode.Open);
                            byte[] bs = new byte[fs.Length];
                            fs.Read(bs,0,(int)fs.Length);
                            fs.Close();
                            ushAttr["DEFAULT_VALUE"] = Convert.ToBase64String(bs);
                        }
                        catch(Exception e)
                        {
                            ushAttr["DEFAULT_VALUE"] = "服务器缺少样本文件:"+attr.ValueFromFile;
                        }
                    }
                    else if (!String.IsNullOrEmpty(attr.DefaultValue))
                        ushAttr["DEFAULT_VALUE"] = attr.DefaultValue;
                    ushAttributes[attr.Name] = ushAttr;
                }
                foreach (ParameterInfo pi in pis)
                {
                    if (pi.Position==0)
                        continue;
                    UnCaseSenseHashTable ushParam = new UnCaseSenseHashTable();
                    ushParam["NAME"]=pi.Name;
                    ushParam["TYPE"]=pi.ParameterType.Name;
                    ushParam["DESCRIPT"]="没有参数说明";
                    ushParam["DEFAULT_VALUE"]="";
                    if (ushAttributes.HasValue(pi.Name))
                    {
                        UnCaseSenseHashTable ushAttr = (UnCaseSenseHashTable)ushAttributes[pi.Name];
                        ushParam["DESCRIPT"] = ushAttr["DESCRIPT"];
                        ushParam["DEFAULT_VALUE"] = ushAttr["DEFAULT_VALUE"];
                    }
                    ps.Add(ushParam);
                }                
                result[mi.Name] = ushMethod;
            }
            return result;
        }

        public string InterfaceHelperCode(UnCaseSenseHashTable info)
        {
            StringBuilder code = new StringBuilder();
            code.Append("//引用 CommonServiceDemo、MisFramework.core 项目以后，能直接使用这个类\r\n");
            code.Append("using System;\r\n");
            code.Append("using System.Collections.Generic;\r\n");
            code.Append("using System.ComponentModel;\r\n");
            code.Append("using System.Data;\r\n");
            code.Append("using System.Drawing;\r\n");
            code.Append("using System.Linq;\r\n");
            code.Append("using System.Text;\r\n");
            code.Append("using System.Threading.Tasks;\r\n");
            code.Append("using System.Windows.Forms;\r\n");
            code.Append("using System.IO;\r\n");
            code.Append("using System.Net;\r\n");
            code.Append("using MisFrameWork.core;\r\n");
            code.Append("namespace CommonServiceDemo\r\n");
            code.Append("{\r\n");
            code.Append("    public class "+this.GetType().Name.Replace("Controller","")+"Client:ServiceHelper\r\n");
            code.Append("    {\r\n");
            code.Append("        public string ServiceURL{get;set}\r\n");
            #region 函数开始
            foreach (string actionKey in info.Keys)
            {
                List<UnCaseSenseHashTable> paramInfo = (List<UnCaseSenseHashTable>)((UnCaseSenseHashTable) info[actionKey])["PARAMS"];
                string p = "";
                for (int i=0;i<paramInfo.Count;i++)
                {
                    if (i>0)
                        p+=", ";
                    p+=paramInfo[i]["TYPE"]+" "+paramInfo[i]["NAME"];
                }
                code.Append("        public FWExtServiceResult "+((UnCaseSenseHashTable)info[actionKey])["NAME"]+"("+p+")\r\n");
                code.Append("        {\r\n");
                code.Append("            UnCaseSenseHashTable pdata = new UnCaseSenseHashTable();\r\n");
                for (int i=0;i<paramInfo.Count;i++)                
                    code.Append("            pdata[\""+((UnCaseSenseHashTable)paramInfo[i])["NAME"]+"\"]="+((UnCaseSenseHashTable)paramInfo[i])["NAME"]+";\r\n");
                code.Append("            UnCaseSenseHashTable resData = this.PostData(ServiceURL,pdata,true);\r\n");
                code.Append("            FWExtServiceResult result = new FWExtServiceResult(resData);\r\n");
                code.Append("            return result;\r\n");
                code.Append("        }\r\n");
            }
            #endregion
            code.Append("    }\r\n");
            code.Append("}\r\n");
            return code.ToString();
        }

        public ActionResult ViewInterfaceInfo()
        {
            UnCaseSenseHashTable info = QueryInterfaceInfo();
            ViewBag.Info = info;
            ViewBag.Code = InterfaceHelperCode(info);
            return View("~/Views/Shared/ViewFWExtServiceInfo.cshtml");
        }

        public ActionResult ViewInterceHelperCode()
        {
            string str = "出错";
            if ("csharp".Equals(Request["code"]))
            {
                UnCaseSenseHashTable info = QueryInterfaceInfo();
                str = InterfaceHelperCode(info);
            }
            this.Response.ContentType = "text/plain";
            return Content(str);
        }

        private ActionResult JsonInterfaceInfo()
        {
            return JsonDateObject(QueryInterfaceInfo());            
        }
    }
}