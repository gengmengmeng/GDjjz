using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace AutoUpdateWeb.Class
{
    public class GVar
    {
        private static string planFloder;
        private static string planFloder2;
        /// <summary>
        /// 更新文件目录
        /// </summary>
        public static string PlanFloder
        {
            get
            {
                if (string.IsNullOrEmpty(planFloder))
                {
                    string p = ConfigurationManager.AppSettings["SingleUploadFolder"];
                    if (p.IndexOf(":") < 0)
                        p = HttpContext.Current.Server.MapPath("~/" + p);
                    if (!p.EndsWith("\\")) p += "\\";
                    planFloder = p;
                }
                return planFloder;
            }
        }
        public static string PlanFloder2
        {
            get
            {
                if (string.IsNullOrEmpty(planFloder2))
                {
                    string p = ConfigurationManager.AppSettings["PlanFolder"];
                    if (p.IndexOf(":") < 0)
                        p = HttpContext.Current.Server.MapPath("~/" + p);
                    if (!p.EndsWith("\\")) p += "\\";
                    planFloder2 = p;
                }
                return planFloder2;
            }
        }
        private static string backupFloder;
        public static string BackupFloder
        {
            get
            {
                if (string.IsNullOrEmpty(backupFloder))
                {
                    string p = ConfigurationManager.AppSettings["BackupFolder"];
                    if (p.IndexOf(":") < 0)
                        p = HttpContext.Current.Server.MapPath("~/" + p);
                    if (!p.EndsWith("\\")) p += "\\";
                    backupFloder = p;
                }
                return backupFloder;
            }
        }

        /// <summary>
        /// 是否只能从服务器本地登录
        /// </summary>
        public static bool LocalOnly { get; set; }

        /// <summary>
        /// 登录用户
        /// </summary>
        public static string LoginUser { get; set; }


        /// <summary>
        /// 登录密码
        /// </summary>
        public static string LoginPwd { get; set; }
    }
}