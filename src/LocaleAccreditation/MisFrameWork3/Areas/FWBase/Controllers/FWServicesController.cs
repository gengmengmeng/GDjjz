using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using MisFrameWork.core.db;
using MisFrameWork.core;
using MisFrameWork3.Classes.Membership;

namespace MisFrameWork3.Areas.FWBase.Controllers
{
    public class FWServicesController : Controller
    {
        // GET: FWBase/FWServices
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 接收参数：
        ///     Request.Files["imgFile"]
        ///     Request.QueryString["dir"]
        /// </summary>
        /// <returns></returns>
        public ActionResult JsonUploadFile()
        {
            String aspxUrl = "/";

            //文件保存目录路径
            String savePath = "/attached/";

            //文件保存目录URL
            String saveUrl = aspxUrl + "attached/";

            //定义允许上传的文件扩展名
            Hashtable extTable = new Hashtable();
            Hashtable extTableFileSize = new Hashtable();

            extTableFileSize.Add("image", "2000000");//2M
            extTableFileSize.Add("flash", "5000000");//5M
            extTableFileSize.Add("media", "20000000");//20M
            extTableFileSize.Add("file", "15000000");//15M

            //__TIPS__: 支持哪些文件在这里过滤，客户端可以传任意文件
            extTable.Add("image", "gif,jpg,jpeg,png,bmp");
            extTable.Add("flash", "swf,flv");
            extTable.Add("media", "swf,flv,mp3,mp4,wav,wma,wmv,mid,avi,mpg,asf,rm,rmvb");
            extTable.Add("file", "pdf,doc,docx,xls,xlsx,ppt,htm,html,txt,zip,rar,gz,bz2");            

            HttpPostedFileBase imgFile = Request.Files["imgFile"];
            if (imgFile == null)
            {
                return showError("请选择文件。");
            }

            String dirPath = Server.MapPath(savePath);
            if (!Directory.Exists(dirPath))
            {
                return showError("上传目录不存在。");
            }

            String dirName = Request.QueryString["dir"];
            if (String.IsNullOrEmpty(dirName))
            {
                dirName = "image";
            }

            if (!extTable.ContainsKey(dirName))
            {
                return showError("目录名不正确。");
            }

            String fileName = imgFile.FileName;
            String fileExt = Path.GetExtension(fileName).ToLower();
            int fileMaxSize = int.Parse(extTableFileSize[dirName].ToString());
            if (imgFile.InputStream == null || imgFile.InputStream.Length > fileMaxSize)
            {
                return showError("上传文件大小超过限制("+ fileMaxSize + ")。");
            }

            if (String.IsNullOrEmpty(fileExt) || Array.IndexOf(((String)extTable[dirName]).Split(','), fileExt.Substring(1).ToLower()) == -1)
            {
                return showError("上传文件扩展名是不允许的扩展名。\n只允许" + ((String)extTable[dirName]) + "格式。");
            }

            //创建文件夹
            dirPath += dirName + "/";
            saveUrl += dirName + "/";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            String ymd = DateTime.Now.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
            dirPath += ymd + "/";
            saveUrl += ymd + "/";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            String newFileName = Guid.NewGuid().ToString() + fileExt;
            String filePath = dirPath + newFileName;
            String fileUrl = saveUrl + newFileName;
            try
            {
                imgFile.SaveAs(filePath);
                UnCaseSenseHashTable data = new UnCaseSenseHashTable();
                data["ID"] = fileUrl;
                data["DIR"] = dirName;
                data["SRC_FILE_NAME"] = fileName;
                data["FILE_EXT"] = fileExt.ToLower();
                data["YMD"] = ymd;
                data["FILE_SIZE"] = imgFile.InputStream.Length;
                if (0==DbUtilityManager.Instance.DefaultDbUtility.InsertRecord("FW_S_ATTACHED", data))
                    return showError("保存数据库记录失败。");
            }
            catch (Exception e)
            {
                return showError("保存时出错："+e.Message);
            }
            Hashtable hash = new Hashtable();
            hash["error"] = 0;
            hash["url"] = fileUrl;
            hash["title"] = fileName;
            return Json(hash, JsonRequestBehavior.AllowGet);
        }

        public ActionResult JsonUploadFileManager()
        {
            String aspxUrl = "/";

            //根目录路径，相对路径
            String rootPath = "/attached/";
            //根目录URL，可以指定绝对路径，比如 http://www.yoursite.com/attached/
            String rootUrl = aspxUrl + "attached/";
            //图片扩展名
            String fileTypes = "gif,jpg,jpeg,png,bmp";

            String currentPath = "";
            String currentUrl = "";
            String currentDirPath = "";
            String moveupDirPath = "";

            String dirPath = Server.MapPath(rootPath);
            String dirName = Request.QueryString["dir"];
            if (!String.IsNullOrEmpty(dirName))
            {
                if (Array.IndexOf("image,flash,media,file".Split(','), dirName) == -1)
                {
                    Response.Write("Invalid Directory name.");
                    Response.End();
                }
                dirPath += dirName + "/";
                rootUrl += dirName + "/";
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
            }

            //根据path参数，设置各路径和URL
            String path = Request.QueryString["path"];
            path = String.IsNullOrEmpty(path) ? "" : path;
            if (path == "")
            {
                currentPath = dirPath;
                currentUrl = rootUrl;
                currentDirPath = "";
                moveupDirPath = "";
            }
            else
            {
                currentPath = dirPath + path;
                currentUrl = rootUrl + path;
                currentDirPath = path;
                moveupDirPath = Regex.Replace(currentDirPath, @"(.*?)[^\/]+\/$", "$1");
            }

            //排序形式，name or size or type
            String order = Request.QueryString["order"];
            order = String.IsNullOrEmpty(order) ? "" : order.ToLower();

            //不允许使用..移动到上一级目录
            if (Regex.IsMatch(path, @"\.\."))
            {
                Response.Write("Access is not allowed.");
                Response.End();
            }
            //最后一个字符不是/
            if (path != "" && !path.EndsWith("/"))
            {
                Response.Write("Parameter is not valid.");
                Response.End();
            }
            //目录不存在或不是目录
            if (!Directory.Exists(currentPath))
            {
                Response.Write("Directory does not exist.");
                Response.End();
            }

            //遍历目录取得文件信息
            string orderBy = "CREATE_ON desc";
            switch (order)
            {
                case "size":
                    orderBy = "FILE_SIZE desc";
                    break;
                case "type":
                    orderBy = "FILE_EXT desc";
                    break;
                case "name":
                    orderBy = "CREATE_ON desc";
                    break;
            }

            Condition cdtUser = new Condition();
            cdtUser.AddSubCondition("OR", "CREATE_BY", "is", "EXPR:NULL");
            cdtUser.AddSubCondition("OR", "CREATE_BY", "=", Membership.CurrentUser.UserId);

            Condition cdtFile = new Condition();
            cdtFile.AddSubCondition(cdtUser);
            cdtFile.AddSubCondition("AND", "CREATE_ON", ">=", DateTime.Now.AddDays(-10));
            cdtFile.AddSubCondition("AND", "DIR", "=", dirName);
            
            List<UnCaseSenseHashTable> dataDir;
            if (String.IsNullOrEmpty(path))
                dataDir = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_ATTACHED", cdtFile, "YMD", "YMD desc", "YMD", 0, 500);
            else
            {
                cdtFile.AddSubCondition("AND", "YMD", "=", path.Replace("/","").Replace("\\",""));
                dataDir = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_ATTACHED", cdtFile, "*", orderBy, null, 0, 2000);
            }
            
            Hashtable result = new Hashtable();
            result["moveup_dir_path"] = moveupDirPath;
            result["current_dir_path"] = currentDirPath;
            result["current_url"] = currentUrl;
            result["total_count"] = dataDir.Count;
            List<Hashtable> dirFileList = new List<Hashtable>();
            result["file_list"] = dirFileList;
            for (int i = 0; i < dataDir.Count; i++)
            {
                Hashtable hash = new Hashtable();
                if (String.IsNullOrEmpty(path))
                {
                    hash["is_dir"] = true;
                    hash["has_file"] = 10;
                    hash["is_photo"] = false;
                    hash["filesize"] = 0;
                    hash["filename"] = dataDir[i]["YMD"];
                }
                else
                {
                    hash["is_dir"] = false;
                    hash["has_file"] = false;
                    hash["filesize"] = dataDir[i].GetIntValue("FILE_SIZE");
                    hash["is_photo"] = (Array.IndexOf(fileTypes.Split(','), dataDir[i]["FILE_EXT"].ToString().Substring(1).ToLower()) >= 0);
                    hash["filetype"] = dataDir[i]["FILE_EXT"].ToString().Substring(1);
                    string fileName = dataDir[i]["ID"].ToString();
                    fileName = fileName.Substring(fileName.LastIndexOf('/') + 1);
                    hash["filename"] = fileName;
                    hash["title"] = dataDir[i]["SRC_FILE_NAME"];
                }
                hash["datetime"] = dataDir[i].GetDateValue("CREATE_ON").ToString("yyyy-MM-dd HH:mm:ss");
                dirFileList.Add(hash);
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private ActionResult showError(int code,string message)
        {
            Hashtable hash = new Hashtable();
            hash["error"] = code;
            hash["message"] = message;
            return Json(hash, JsonRequestBehavior.AllowGet);
        }
        private ActionResult showError( string message)
        {
           return showError(1, message);
        }
    }
}