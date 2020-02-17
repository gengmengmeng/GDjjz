using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MisFrameWork3.Classes
{
    public class UpdateFileStruct
    {
        /// <summary>
        /// 相对路径
        /// </summary>
        public string path { get; set; }

        /// <summary>
        /// 文件md5
        /// </summary>
        public string md5 { get; set; }

        /// <summary>
        /// 下载的路径
        /// </summary>
        public string url { get; set; }
    }

    /// <summary>
    /// 更新命令结构
    /// </summary>
    public class UpdateStruct
    {
        /// <summary>
        /// 替换文件前命令
        /// </summary>
        public List<string> beforeCmd { get; set; }

        /// <summary>
        /// 替换文件后命令
        /// </summary>
        public List<string> afterCmd { get; set; }

        /// <summary>
        /// 文件列表
        /// </summary>
        public List<UpdateFileStruct> files { get; set; }
    }
}