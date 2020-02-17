using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support.SqlClient;

namespace MisFrameWork3
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            //初始化配置文件            
            string serverType = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["server_type"].ConnectionString.ToLower();
            string strConn = System.Web.Configuration.WebConfigurationManager.ConnectionStrings[serverType].ConnectionString;
            MisFrameWork.core.db.Support.IDataBaseUtility dbu = null;
            switch(serverType)
            {
                case "sqlserver":dbu = new SqlClientDataBaseUtility(strConn);break;
                case "oracle":dbu = new MisFrameWork.core.db.Support.Oracle.OracleDataBaseUtility(strConn);break;
                case "sqlite":dbu = new MisFrameWork.core.db.Support.Sqlite3.Sqlite3DataBaseUtility(strConn);break;
                //case "mysql":dbu = new MisFrameWork.core.db.Support.MySql.MySqlDataBaseUtility(strConn);break;
                case "access":dbu = new MisFrameWork.core.db.Support.Access.AccessDataBaseUtility(strConn);break;
            }
            
            dbu.IsOutputSql = System.IO.Directory.Exists("c:\\hblogs");//debug日志是否输入sql语句，为了不重新编译程序，我们以外部的文件夹作为判断
            //dbu.UseStaticSqlStatement = true;//是否使用
            DbUtilityManager.Instance.RegistDbUitility("default", dbu);
            //DbUtilityManager.Instance.DefaultDbUtility.UseStaticSqlStatement = true;
            DbUtilityManager.Instance.DefaultDbUtility.DbOperateListenerManager.LoadListenerFromSpringIocFile(System.Web.HttpRuntime.AppDomainAppPath + "Configs\\db_listeners_config_"+serverType+".config");
            DbUtilityManager.Instance.DefaultDbUtility.DbOperateListenerManager.LoadDictionaryTableMaper(System.Web.HttpRuntime.AppDomainAppPath + "Configs\\db_table_dic_map_config.config");
            //DbUtilityManager.Instance.RegistDbUitility("sqlite3", new Sqlite3DataBaseUtility("Data Source=" + System.Web.HttpRuntime.AppDomainAppPath + "/db/mysqlite3.s3db"));
            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(System.Web.HttpRuntime.AppDomainAppPath + "Configs\\log4net.config"));
        }
    }
}
