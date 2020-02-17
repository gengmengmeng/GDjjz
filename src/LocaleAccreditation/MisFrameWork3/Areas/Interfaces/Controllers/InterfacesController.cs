using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;
using MisFrameWork3.Classes.Controller;
using MisFrameWork3.Classes.Membership;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using log4net;

namespace MisFrameWork3.Areas.Interfaces.Controllers
{
    public class InterfacesController : FWExtServiceController
    {
        [FWExtServiceMethod(Descript = "登陆接口")]
        [FWExtServiceParameters(Name = "uid", Descript = "用户名", DefaultValue = "admin")]
        [FWExtServiceParameters(Name = "pwd", Descript = "设备密码密码", DefaultValue = "123456")]
        public FWExtServiceResult Login(UnCaseSenseHashTable data, string uid, string pwd)
        {           
            FWExtServiceResult result = new FWExtServiceResult();
            if (!string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(pwd))
            {
                Condition cdtIds = new Condition();
                cdtIds.AddSubCondition("AND", "USER_ID", "=", uid);
                List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_USERS", cdtIds, "*", null, null, -1, -1);
                if (records.Count == 1)
                {
                    if (string.IsNullOrEmpty(records[0]["SB_PASSWD"].ToString())) {
                        result.RESULT = -1;
                        result.MSG = "未配置设备登录密码";
                        return result;
                    }
                    bool temp = pwd.Equals(records[0]["SB_PASSWD"].ToString());
                    if (temp)
                    {
                        result.RESULT = 1;
                        result.MSG = "登陆成功";
                    }
                    else
                    {
                        result.RESULT = -1;
                        result.MSG = "密码错误";
                    }
                }
                else
                {
                    result.RESULT = -2;
                    result.MSG = "账户不存在";
                }
                return result;
            }
            else {
                result.RESULT = -3;
                result.MSG = "账户或密码不能为空";
            }                      
            return result;
        }


        [FWExtServiceMethod(Descript = "登陆账户查询接口")]       
        [FWExtServiceParameters(Name = "MACHINENO", Descript = "设备ID", DefaultValue = "123456")]
        public FWExtServiceResult Query(UnCaseSenseHashTable data, string MACHINENO)
        {          
            FWExtServiceResult result = new FWExtServiceResult();
            try
            {
                if (!string.IsNullOrEmpty(MACHINENO))
                {
                    Condition cdtIds = new Condition();
                    cdtIds.AddSubCondition("AND", "SB_ID_V_D_MACHINE__MC", "like", "%" + MACHINENO + "%");
                    List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_USERS", cdtIds, "*", null, null, -1, -1);
                    if (records.Count != 0)
                    {
                        string[] users = new string[records.Count];
                        int count = records.Count;
                        for (int i = 0; i < count; i++)
                        {
                            users[i] = records[i]["USER_ID"].ToString();
                        }
                        result.DATA = new UnCaseSenseHashTable();
                        result.DATA.Add("UserList", Newtonsoft.Json.JsonConvert.SerializeObject(users));
                        //Newtonsoft.Json.JsonConvert.SerializeObject(users);
                        result.RESULT = 1;
                        result.MSG = "查询成功";
                    }
                    else
                    {
                        result.RESULT = -1;
                        result.MSG = "设备未添加账户";
                    }
                    return result;
                }
                else
                {
                    result.RESULT = -2;
                    result.MSG = "设备编号不能为空";
                }
            }
            catch (Exception e) {
                result.RESULT = -1;
                result.MSG = e.Message;
                return result;
            }
            return result;
        }


        [FWExtServiceMethod(Descript = "设备注册接口")]
        [FWExtServiceParameters(Name = "MACHINENO", Descript = "设备编号", DefaultValue = "AC66666")]
        [FWExtServiceParameters(Name = "SSDW", Descript = "所属单位编号")]
        [FWExtServiceParameters(Name = "SBFZR", Descript = "设备负责人账户")]
        [FWExtServiceParameters(Name = "Phone", Descript = "联系电话")]
        [FWExtServiceParameters(Name = "Address", Descript = "联系地址")]
        public FWExtServiceResult MachineRegister(UnCaseSenseHashTable data, string MACHINENO)
        {
            FWExtServiceResult result = new FWExtServiceResult();           
            string MACHINE_IP = "";
            if (!string.IsNullOrEmpty(MACHINENO))
            {
                Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
                try
                {
                    session.BeginTransaction();
                    Condition cdtIds = new Condition();
                    HttpRequest Request = System.Web.HttpContext.Current.Request;
                    if (string.IsNullOrEmpty(Request.ServerVariables.Get("Remote_Addr").ToString()))
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "获取设备IP地址为空";
                        return result;
                    }
                    else {
                        MACHINE_IP = Request.ServerVariables.Get("Remote_Addr").ToString();
                    }                        
                    cdtIds.AddSubCondition("AND", "MACHINENO", "=", MACHINENO);
                    List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtIds, "*", null, null, -1, -1);
                    if (records.Count != 0)
                    {
                        UnCaseSenseHashTable data3 = new UnCaseSenseHashTable();
                        data3["ID"] = records[0]["ID"];                       
                        data3["SBYXZT"] = "1";
                        data3["MACHINE_IP"] = MACHINE_IP;  
                        data3["REGISTER"] = DateTime.Now;
                        int r = DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "B_MACHINE", data3, false);
                        session.Commit();
                        session.Close();
                        result.RESULT = 1;
                        result.MSG = "更新注册成功";
                    }
                    if(records.Count == 0)
                    {
                        UnCaseSenseHashTable data2 = new UnCaseSenseHashTable();
                        data2["MACHINENO"] = MACHINENO;                       
                        data2["SBYXZT"] = "1";
                        data2["MACHINE_IP"] = MACHINE_IP;
                        data2["REGISTER"] = DateTime.Now;
                        int re = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord("B_MACHINE", data2);
                        //string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                        //String sql = "insert into S_DICT(LX,DM,V5,EDT_TIME)values(:LX,:DM,:V5,:EDT_TIME)";
                        //OracleConnection conn = new OracleConnection(connString);
                        //conn.Open();
                        //OracleCommand cmd = new OracleCommand(sql, conn);
                        //string LX = "Z451";
                        //cmd.Parameters.Add(":LX", OracleDbType.NVarchar2, 600).Value = LX;
                        //cmd.Parameters.Add(":DM", OracleDbType.NVarchar2, 600).Value = MACHINENO;
                        //cmd.Parameters.Add(":V5", OracleDbType.NVarchar2, 600).Value = "1";
                        //cmd.Parameters.Add(":EDT_TIME", OracleDbType.Date, 10000000).Value = DateTime.Now.Date;
                        //int rowsAffected = cmd.ExecuteNonQuery();
                        //if (rowsAffected == 0)
                        //{
                        //    conn.Close();
                        //    session.Rollback();
                        //    session.Close();
                        //    result.RESULT = -1;
                        //    result.MSG = "保存信息出错";
                        //    return result;
                        //}
                        session.Commit();
                        session.Close();
                        //conn.Close();
                        result.RESULT = 1;
                        result.MSG = "注册成功";
                    }
                }
                catch (Exception e)
                {
                    session.Rollback();
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = e.Message;
                    return result;
                }
            }
            else {
                result.RESULT = -1;
                result.MSG = "设备编号不能为空";
            }
            return result;
        }


        [FWExtServiceMethod(Descript = "设备是否注册接口")]
        [FWExtServiceParameters(Name = "MACHINENO", Descript = "设备编号", DefaultValue = "AC66666")]      
        public FWExtServiceResult MachineJudge(UnCaseSenseHashTable data, string MACHINENO)
        {
            FWExtServiceResult result = new FWExtServiceResult();        
            if (!string.IsNullOrEmpty(MACHINENO))
            {
                try
                {
                    Condition cdtIds = new Condition();
                    cdtIds.AddSubCondition("AND", "MACHINENO", "=", MACHINENO);
                    List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtIds, "*", null, null, -1, -1);
                    if (records.Count != 0)
                    {
                        if (records[0]["DISABLED"].ToString() == "1")
                        {
                            result.RESULT = 0;
                            result.MSG = "设备已注册但未启用";
                            
                        }
                        else
                        {
                            result.RESULT = 1;
                            result.MSG = "设备已注册启用";
                        }
                    }
                    else
                    {
                        result.RESULT = -1;
                        result.MSG = "设备未注册";
                    }
                }
                catch (Exception e)
                {                   
                    result.RESULT = -2;
                    result.MSG = e.Message;                    
                }
            }
            else
            {
                result.RESULT = -2;
                result.MSG = "设备编号不能为空";               
            }
            return result;
        }



        [FWExtServiceMethod(Descript = "设备状态记录接口")]
        [FWExtServiceParameters(Name = "MACHINENO", Descript = "设备编号", DefaultValue = "AC66666")]
        [FWExtServiceParameters(Name = "SBYXZT", Descript = "设备状态代码,1在线，2离线,3故障")]
        public FWExtServiceResult MachineState(UnCaseSenseHashTable data, string MACHINENO, string SBYXZT)
        {
            FWExtServiceResult result = new FWExtServiceResult();            
            if (!string.IsNullOrEmpty(MACHINENO))
            {
                Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
                try
                {
                    session.BeginTransaction();
                    Condition cdtIds = new Condition();
                    cdtIds.AddSubCondition("AND", "MACHINENO", "=", MACHINENO);
                    List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtIds, "*", null, null, -1, -1);
                    if (records.Count != 0)
                    {
                        if (records[0]["DISABLED"].ToString() == "1")
                        {
                            session.Close();
                            result.RESULT = 0;
                            result.MSG = "设备存在未启用";
                            return result;
                        }
                        else
                        {
                            UnCaseSenseHashTable data3 = new UnCaseSenseHashTable();
                            data3["ID"] = records[0]["ID"];
                            data3["SBYXZT"] = SBYXZT;
                            data3["UPDATE_ON"] = DateTime.Now;
                            int r = DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "B_MACHINE", data3, false);
                            session.Commit();
                            session.Close();
                            result.RESULT = 1;
                            result.MSG = "状态更改成功";
                        }
                    }
                    else
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "设备不存在";
                        return result;
                    }
                }
                catch (Exception e)
                {
                    session.Rollback();
                    session.Close();
                    result.RESULT = -2;
                    result.MSG = e.Message;
                    return result;
                }
            }
            else {
                result.RESULT = -2;
                result.MSG = "设备编号不能为空";
            }          
            return result;
        }
        static string UserMd5(string str)
        {
            string cl = str;
            string pwd = "";
            MD5 md5 = MD5.Create();//实例化一个md5对像
            // 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            // 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得
            for (int i = 0; i < s.Length; i++)
            {
                // 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 

                pwd = pwd + s[i].ToString("X2");

            }
            return pwd;
        }

        [FWExtServiceMethod(Descript = "制证记录接口")]
        [FWExtServiceParameters(Name = "SYSTEMID", Descript = "Guid")]
        [FWExtServiceParameters(Name = "CSRQ", Descript = "出生日期")]
        [FWExtServiceParameters(Name = "FFDW", Descript = "发放单位代码 ")]
        [FWExtServiceParameters(Name = "FFDWMC", Descript = "发放单位名称")]
        [FWExtServiceParameters(Name = "FWCS", Descript = "服务处所（工作单位）")]
        [FWExtServiceParameters(Name = "GMSFHM", Descript = "身份证号码")]
        [FWExtServiceParameters(Name = "HJDXZ", Descript = "户籍地详址")]
        [FWExtServiceParameters(Name = "HJDZSSXQ", Descript = "")]
        [FWExtServiceParameters(Name = "HJDZSSXQMC", Descript = "")]
        [FWExtServiceParameters(Name = "JZZYXQJZRQ", Descript = "居住证有效期截止日期")]
        [FWExtServiceParameters(Name = "JZZYXQQSRQ", Descript = "居住证有效期起始日期")]
        [FWExtServiceParameters(Name = "MZ", Descript = "民族代码")]
        [FWExtServiceParameters(Name = "MZMC", Descript = "民族名称")]
        [FWExtServiceParameters(Name = "RESERVATION01", Descript = "打印用的户籍地址")]
        [FWExtServiceParameters(Name = "RESERVATION02", Descript = "打印用的居住地址")]
        [FWExtServiceParameters(Name = "RESERVATION36", Descript = "签发日期")]
        [FWExtServiceParameters(Name = "SLBH", Descript = "受理编号")]
        [FWExtServiceParameters(Name = "XB", Descript = "性别")]
        [FWExtServiceParameters(Name = "XJZDZQZ", Descript = "现居住地址")]
        [FWExtServiceParameters(Name = "XM", Descript = "姓名")]
        [FWExtServiceParameters(Name = "ZP", Descript = "")]
        [FWExtServiceParameters(Name = "ZPID", Descript = "")]
        [FWExtServiceParameters(Name = "ZZSBID", Descript = "制证设备ID")]
        [FWExtServiceParameters(Name = "ZZSY", Descript = "暂|居住事由")]
        [FWExtServiceParameters(Name = "ZZXXXRSJ", Descript = "信息写入时间")]
        [FWExtServiceParameters(Name = "ZZLX", Descript = "")]
        [FWExtServiceParameters(Name = "ZZFSSJ", Descript = "")]
        [FWExtServiceParameters(Name = "ZZXXZZSFCG", Descript = "制证是否成功 （0失败，1成功）")]
        public FWExtServiceResult MakecardRecord(UnCaseSenseHashTable data, string SYSTEMID, string CSRQ, string FFDW, string FFDWMC, string FWCS, string GMSFHM, string HJDXZ, string HJDZSSXQ, string HJDZSSXQMC, string JZZYXQJZRQ, string JZZYXQQSRQ, string MZ, string MZMC, string RESERVATION01, string RESERVATION02, string RESERVATION36, string SLBH, string XB, string XJZDZQZ, string XM, string ZP, string ZPID, string ZZSBID, string ZZSY, string ZZXXXRSJ, string ZZLX, string ZZFSSJ, string ZZXXZZSFCG, string ZZXXCWLX, string ZZXXCWLXMC, string ZZZXPH)
        {

            FWExtServiceResult result = new FWExtServiceResult();
            try
            {
                if (!((!string.IsNullOrEmpty(ZZZXPH)&& ZZXXZZSFCG == "1"&& ZZZXPH!="") || ZZXXZZSFCG == "0")) {
                    result.RESULT = -1;
                    result.MSG = "制证芯片号不能为空";
                    return result;
                }

                    //string SYSTEMID = data["SYSTEMID"].ToString();
                    //DateTime CSRQ_Date = DateTime.ParseExact(CSRQ, "yyyy/MM/dd", null);string.IsNullOrEmpty(Request["cdt_combination"])
                    if (CSRQ == "" || JZZYXQJZRQ == "" || JZZYXQQSRQ == "" || RESERVATION36 == "" || ZZXXXRSJ == "" || ZP == "" || ZPID == "" || string.IsNullOrEmpty(CSRQ) || string.IsNullOrEmpty(JZZYXQJZRQ) || string.IsNullOrEmpty(JZZYXQQSRQ) || string.IsNullOrEmpty(RESERVATION36) || string.IsNullOrEmpty(ZZXXXRSJ) || string.IsNullOrEmpty(ZP) || string.IsNullOrEmpty(ZPID) || string.IsNullOrEmpty(ZZXXZZSFCG))
                    {
                        if (ZP == "" || ZPID == "" || string.IsNullOrEmpty(ZP) || string.IsNullOrEmpty(ZPID))
                        {
                            result.RESULT = -1;
                            result.MSG = "图像不能为空";
                        }
                        if (string.IsNullOrEmpty(ZZXXZZSFCG) || ZZXXZZSFCG == "")
                        {
                            result.RESULT = -1;
                            result.MSG = "制证结果不能为空";
                        }
                        else
                        {
                            result.RESULT = -1;
                            result.MSG = "时间不能为空";
                        }
                        return result;
                    }
                    else
                    {
                        byte[] ZP_byte = Convert.FromBase64String(ZP.Trim().Replace("%", "").Replace(",", "").Replace(" ", "+"));
                        byte[] ZP_1K_byte = Convert.FromBase64String(ZPID.Trim().Replace("%", "").Replace(",", "").Replace(" ", "+"));
                        DateTime CSRQ_Date = DateTime.ParseExact(CSRQ, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                        DateTime JZZYXQJZRQ_Date = DateTime.ParseExact(JZZYXQJZRQ, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                        DateTime JZZYXQQSRQ_Date = DateTime.ParseExact(JZZYXQQSRQ, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                        DateTime RESERVATION36_Date = DateTime.ParseExact(RESERVATION36, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                        DateTime ZZXXXRSJ_Date = DateTime.ParseExact(ZZXXXRSJ, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                        //string FFDW = data["FFDW"].ToString();
                        //string FFDWMC = data["FFDWMC"].ToString();
                        //string FWCS = data["FWCS"].ToString();
                        //string GMSFHM = data["GMSFHM"].ToString();
                        //string HJDXZ = data["HJDXZ"].ToString();
                        //string HJDZSSXQ = data["HJDZSSXQ"].ToString();
                        //string HJDZSSXQMC = data["HJDZSSXQMC"].ToString();
                        //string JZZYXQJZRQ = data["JZZYXQJZRQ"].ToString();
                        //string JZZYXQQSRQ = data["JZZYXQQSRQ"].ToString();
                        //string MZ = data["MZ"].ToString();
                        //string MZMC = data["MZMC"].ToString();
                        //string RESERVATION01 = data["RESERVATION01"].ToString();
                        //string RESERVATION02 = data["RESERVATION02"].ToString();
                        //string RESERVATION36 = data["RESERVATION36"].ToString();
                        //string SLBH = data["SLBH"].ToString();
                        //string XB = data["XB"].ToString();
                        //string XJZDZQZ = data["XJZDZQZ"].ToString();
                        //string XM = data["XM"].ToString();
                        //string ZP = data["ZP"].ToString();
                        //string ZP_1K = data["ZP_1K"].ToString();
                        //string ZZSBID = data["ZZSBID"].ToString(); 
                        //string ZZSY = data["ZZSY"].ToString();
                        //string ZZXXXRSJ = data["ZZXXXRSJ"].ToString();
                        //string ZZLX = data["ZZLX"].ToString();
                        //string ZZFSSJ = data["ZZFSSJ"].ToString();
                        //string ZZXXZZSFCG = data["ZZXXZZSFCG"].ToString();
                        string sql_jl = "insert into C_JZZ_TMP_TC(SYSTEMID,CSRQ,FFDW,FFDWMC,FWCS,GMSFHM,HJDXZ,HJDZSSXQ,HJDZSSXQMC,JZZYXQJZRQ,JZZYXQQSRQ,MZ,MZMC,RESERVATION01,RESERVATION02,RESERVATION36,SLBH,XB,XJZDZQZ,XM,ZZSBID,ZZSY,ZZXXXRSJ,ZZXXZZSFCG,ZZXXCWLX,ZZXXCWLXMC,ZZZXPH)values(:SYSTEMID,:CSRQ,:FFDW,:FFDWMC,:FWCS,:GMSFHM,:HJDXZ,:HJDZSSXQ,:HJDZSSXQMC,:JZZYXQJZRQ,:JZZYXQQSRQ,:MZ,:MZMC,:RESERVATION01,:RESERVATION02,:RESERVATION36,:SLBH,:XB,:XJZDZQZ,:XM,:ZZSBID,:ZZSY,:ZZXXXRSJ,:ZZXXZZSFCG,:ZZXXCWLX,:ZZXXCWLXMC,:ZZZXPH)";
                    string connString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["oracleyc"].ToString();
                    OracleConnection conn = new OracleConnection(connString);
                        OracleCommand cmd = new OracleCommand(sql_jl, conn);
                        cmd.Parameters.Add(":SYSTEMID", OracleDbType.NVarchar2, 600).Value = SYSTEMID;
                        cmd.Parameters.Add(":CSRQ", OracleDbType.Date, 600).Value = CSRQ_Date;
                        cmd.Parameters.Add(":FFDW", OracleDbType.NVarchar2, 600).Value = FFDW;
                        cmd.Parameters.Add(":FFDWMC", OracleDbType.NVarchar2, 600).Value = FFDWMC;
                        cmd.Parameters.Add(":FWCS", OracleDbType.NVarchar2, 600).Value = FWCS;
                        cmd.Parameters.Add(":GMSFHM", OracleDbType.NVarchar2, 600).Value = GMSFHM;
                        cmd.Parameters.Add(":HJDXZ", OracleDbType.NVarchar2, 600).Value = HJDXZ;
                        cmd.Parameters.Add(":HJDZSSXQ", OracleDbType.NVarchar2, 600).Value = HJDZSSXQ;
                        cmd.Parameters.Add(":HJDZSSXQMC", OracleDbType.NVarchar2, 600).Value = HJDZSSXQMC;
                        cmd.Parameters.Add(":JZZYXQJZRQ", OracleDbType.Date, 600).Value = JZZYXQJZRQ_Date;
                        cmd.Parameters.Add(":JZZYXQQSRQ", OracleDbType.Date, 600).Value = JZZYXQQSRQ_Date;
                        cmd.Parameters.Add(":MZ", OracleDbType.NVarchar2, 600).Value = MZ;
                        cmd.Parameters.Add(":MZMC", OracleDbType.NVarchar2, 600).Value = MZMC;
                        cmd.Parameters.Add(":RESERVATION01", OracleDbType.NVarchar2, 600).Value = RESERVATION01;
                        cmd.Parameters.Add(":RESERVATION02", OracleDbType.NVarchar2, 600).Value = RESERVATION02;
                        cmd.Parameters.Add(":RESERVATION36", OracleDbType.Date, 600).Value = RESERVATION36_Date;
                        cmd.Parameters.Add(":SLBH", OracleDbType.NVarchar2, 600).Value = SLBH;
                        cmd.Parameters.Add(":XB", OracleDbType.NVarchar2, 600).Value = XB;
                        cmd.Parameters.Add(":XJZDZQZ", OracleDbType.NVarchar2, 600).Value = XJZDZQZ;
                        cmd.Parameters.Add(":XM", OracleDbType.NVarchar2, 600).Value = XM;
                        cmd.Parameters.Add(":ZZSBID", OracleDbType.NVarchar2, 600).Value = ZZSBID;
                        cmd.Parameters.Add(":ZZSY", OracleDbType.NVarchar2, 600).Value = ZZSY;
                        cmd.Parameters.Add(":ZZXXXRSJ", OracleDbType.Date, 600).Value = ZZXXXRSJ_Date;
                        cmd.Parameters.Add(":ZZXXZZSFCG", OracleDbType.NVarchar2, 600).Value = ZZXXZZSFCG;
                        cmd.Parameters.Add(":ZZXXCWLX", OracleDbType.NVarchar2, 600).Value = ZZXXCWLX;
                        cmd.Parameters.Add(":ZZXXCWLXMC", OracleDbType.NVarchar2, 600).Value = ZZXXCWLXMC;
                        cmd.Parameters.Add(":ZZZXPH", OracleDbType.NVarchar2, 600).Value = ZZZXPH;
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            result.RESULT = -1;
                            result.MSG = "存储制证数据失败";                           
                        }
                        else
                        {
                            //byte[] ZP_byte = Convert.FromBase64String(ZP.Trim().Replace("%", "").Replace(",", "").Replace(" ", "+"));
                            //byte[] ZP_1K_byte = Convert.FromBase64String(ZP_1K.Trim().Replace("%", "").Replace(",", "").Replace(" ", "+")); ;                        
                            string querysql = "insert into C_JZZ_ZP_TC(SLBH,ZP,ZP_1K,SYSTEMID)values(:SLBH,:ZP,:ZP_1K,:SYSTEMID)";
                            OracleConnection connection = new OracleConnection(connString);
                            OracleCommand command = new OracleCommand(querysql, connection);
                            command.Parameters.Add(":SLBH", OracleDbType.NVarchar2, 600).Value = SLBH;
                            command.Parameters.Add(":ZP", OracleDbType.Blob, 2147483647).Value = ZP_byte;
                            command.Parameters.Add(":ZP_1K", OracleDbType.Blob, 2147483647).Value = ZP_1K_byte;
                            command.Parameters.Add(":SYSTEMID", OracleDbType.NVarchar2, 600).Value = SYSTEMID;
                            connection.Open();
                            int rowsAffected_ZP = command.ExecuteNonQuery();
                            connection.Close();
                            connection.Dispose();
                            if (rowsAffected_ZP == 0)
                            {                                
                                result.RESULT = -1;
                                result.MSG = "存储图片数据失败";
                            }
                            else
                            {
                                result.RESULT = 1;
                                result.MSG = "存储成功";                                
                            }
                        }
                    }               
            }
            catch (Exception e)
            {              
                result.RESULT = -1;
                result.MSG = e.Message;
                return result;
            }                   
            return result;
        }




        [FWExtServiceMethod(Descript = "白卡制证接口")]
        [FWExtServiceParameters(Name = "XPHM", Descript = "芯片号")]
        [FWExtServiceParameters(Name = "DATE", Descript = "初始化时间")]
        [FWExtServiceParameters(Name = "MACHINE", Descript = "机器编号")]
        [FWExtServiceParameters(Name = "OPERATE", Descript = "操作人")]
        [FWExtServiceParameters(Name = "CARD_STATION", Descript = "卡片状态")]
        public FWExtServiceResult CardInit(UnCaseSenseHashTable data, string XPHM, string DATE, string MACHINE, string OPERATE, string CARD_STATION)
        { 
            FWExtServiceResult result = new FWExtServiceResult();
            //log4net.ILog log = log4net.LogManager.GetLogger("testApp.Logging");
            //log.Info(DateTime.Now.ToString() + ": data=" + data + "XPHM=" + XPHM + "DATE=" + DATE + "MACHINE=" + MACHINE + "OPERATE=" + OPERATE + "CARD_STATION=" + CARD_STATION);
            if (!string.IsNullOrEmpty(XPHM))
            {
                Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
                try
                {                    
                    session.BeginTransaction();
                    Condition cdtIds = new Condition();
                    cdtIds.AddSubCondition("AND", "XPHM", "=", XPHM);
                    List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("B_CARD_MAKE", cdtIds, "*", null, null, -1, -1);
                    if (records.Count >= 1) {
                        session.Close();
                        result.RESULT = -2;
                        result.MSG = "芯片号已存在";
                        return result;
                    }
                    UnCaseSenseHashTable data3 = new UnCaseSenseHashTable();
                    if (string.IsNullOrEmpty(DATE)|| DATE == "") {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "初始化时间不能为空";
                        return result;
                    }
                    DateTime CREATE_TIME = DateTime.ParseExact(DATE, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                    data3["XPHM"] = XPHM;
                    data3["CARD_MACHINE"] = MACHINE;
                    data3["CARD_STATION"] = CARD_STATION;
                    data3["OPERATE"] = OPERATE;
                    data3["CREATE_TIME"] = CREATE_TIME;
                    int r = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord("B_CARD_MAKE", data3);
                    session.Commit();
                    session.Close();
                    result.RESULT = 1;
                    result.MSG = "保存成功";                        
                }
                catch (Exception e)
                {
                    session.Rollback();
                    session.Close();
                    result.RESULT = -2;
                    result.MSG = e.Message;                    
                    return result;
                }
            }
            else
            {
                result.RESULT = 0;
                result.MSG = "芯片号不能为空";
            }
            return result;
        }




        [FWExtServiceMethod(Descript = "签注记录接口")]
        [FWExtServiceParameters(Name = "GMSFHM", Descript = "身份证号码", DefaultValue = "139123177498129871")]
        [FWExtServiceParameters(Name = "XM", Descript = "姓名", DefaultValue = "王七")]
        [FWExtServiceParameters(Name = "JZDZZ_DZMC", Descript = "居住证住址地址名称", DefaultValue = "广州市黄浦区彩频路")]
        [FWExtServiceParameters(Name = "QFRQ", Descript = "签发日期", DefaultValue = "20190719")]
        [FWExtServiceParameters(Name = "JZNXJS_QSRQ", Descript = "居住证有效期限—起始日期", DefaultValue = "20190719")]
        [FWExtServiceParameters(Name = "JZNXJS_JZRQ", Descript = "居住证有效期限—结束日期", DefaultValue = "20190719")]
        [FWExtServiceParameters(Name = "QZ_RQ", Descript = "签注日期", DefaultValue = "20190719")]
        [FWExtServiceParameters(Name = "QFJG_GAJGMC", Descript = "签注单位", DefaultValue = "广州市天河区凤凰分局")]
        [FWExtServiceParameters(Name = "CXSJ", Descript = "擦写时间", DefaultValue = "20190719")]
        [FWExtServiceParameters(Name = "CXDW", Descript = "擦写单位", DefaultValue = "12323331221")]
        [FWExtServiceParameters(Name = "CXDWMC", Descript = "擦写单位名称", DefaultValue = "广州市公安局")]
        public FWExtServiceResult CardQZ(UnCaseSenseHashTable data, string GMSFHM, string XM, string JZDZZ_DZMC, string QFRQ, string JZNXJS_QSRQ, string JZNXJS_JZRQ, string QZ_RQ, string QFJG_GAJGMC, string CXSJ, string CXDW, string CXDWMC)
        {
            FWExtServiceResult result = new FWExtServiceResult();
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            try
            {
                session.BeginTransaction();
                UnCaseSenseHashTable data1 = new UnCaseSenseHashTable();
                //判断身份证号码是否为空
                if (string.IsNullOrEmpty(GMSFHM) || GMSFHM == "")
                {
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = "身份证号码不得为空";
                    return result;
                }
                data1["GMSFHM"] = GMSFHM;
                data1["XM"] = XM;
                data1["JZDZZ_DZMC"] = JZDZZ_DZMC;
                //判断签发时期是否为空
                if (string.IsNullOrEmpty(QFRQ) || QFRQ == "")
                {
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = "签发日期不得为空";
                    return result;
                }
                data1["QFRQ"] = QFRQ;
                //判断居住证有效期限—起始日期是否为空
                if (string.IsNullOrEmpty(JZNXJS_QSRQ) || JZNXJS_QSRQ == "")
                {
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = "居住证有效期限—起始日期不得为空";
                    return result;
                }
                data1["JZNXJS_QSRQ"] = JZNXJS_QSRQ;
                //判断居住证有效期限—结束日期是否为空
                if (string.IsNullOrEmpty(JZNXJS_JZRQ) || JZNXJS_JZRQ == "")
                {
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = "居住证有效期限—结束日期不得为空";
                    return result;
                }
                data1["JZNXJS_JZRQ"] = JZNXJS_JZRQ;
                //判断签注日期是否为空
                if (string.IsNullOrEmpty(QZ_RQ) || QZ_RQ == "")
                {
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = "签注日期不得为空";
                    return result;
                }
                data1["QZ_RQ"] = QZ_RQ;
                data1["QFJG_GAJGMC"] = QFJG_GAJGMC;
                data1["CXDW"] = CXDW;
                data1["CXDWMC"] = CXDWMC;
                //判断擦写时间是否为空
                if (string.IsNullOrEmpty(CXSJ) || CXSJ == "")
                {
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = "擦写时间不得为空";
                    return result;
                }
                data1["CXSJ"] = CXSJ;
                int r = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord("B_QZ", data1);
                session.Commit();
                session.Close();
                result.RESULT = 1;
                result.MSG = "保存成功";
            }
            catch (Exception e)
            {
                session.Rollback();
                session.Close();
                result.RESULT = -1;
                result.MSG = e.Message;
                return result;
            }
            return result;
        }





        [FWExtServiceMethod(Descript = "设备监控接口")]
        [FWExtServiceParameters(Name = "MACHINE_CODE", Descript = "设备编号", DefaultValue = "Z4510001")]
        [FWExtServiceParameters(Name = "COMPANY_CODE", Descript = "单位代码", DefaultValue = "45021123213")]
        [FWExtServiceParameters(Name = "GZCODE", Descript = "故障代码", DefaultValue = "2")]
        [FWExtServiceParameters(Name = "GZMS", Descript = "故障详情", DefaultValue = "提示错误代码404")]
        public FWExtServiceResult MachineMonitor(UnCaseSenseHashTable data, string MACHINE_CODE, string COMPANY_CODE, string GZCODE, string GZMS)
        {
            FWExtServiceResult result = new FWExtServiceResult();
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();
            if (!string.IsNullOrEmpty(MACHINE_CODE)&& MACHINE_CODE!="")
            {
                try
                {
                    UnCaseSenseHashTable data1 = new UnCaseSenseHashTable();
                    session.BeginTransaction();
                    Condition cdtIds = new Condition();
                    cdtIds.AddSubCondition("AND", "MACHINENO", "=", MACHINE_CODE);
                    List<UnCaseSenseHashTable> records = DbUtilityManager.Instance.DefaultDbUtility.Query("B_MACHINE", cdtIds, "*", null, null, -1, -1);
                    if (records.Count == 0)
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "设备表未添加此设备";
                        return result;
                    }
                    if (records[0]["SBYXZT"].ToString() == "3")
                    {
                        List<UnCaseSenseHashTable> records1 = DbUtilityManager.Instance.DefaultDbUtility.Query("select * from B_FAULT t where machine_code='" + MACHINE_CODE + "'and GZCODE='"+ GZCODE + "' order by create_on desc", -1, -1);
                        if (records1.Count == 0)
                        {
                            data1["MACHINE_CODE"] = MACHINE_CODE;
                            data1["COMPANY_CODE"] = COMPANY_CODE;
                            data1["GZCODE"] = GZCODE;
                            data1["GZMS"] = GZMS;
                            data1["CREATE_ON"] = DateTime.Now;
                            int s = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_FAULT", data1);
                            if (0 == s)
                            {
                                session.Rollback();
                                session.Close();
                                result.RESULT = -1;
                                result.MSG = "插入故障表失败";
                                return result;
                            }
                        }
                        else
                        {
                            data1["ID"] = records1[0]["ID"];
                            data1["GZCODE"] = GZCODE;
                            data1["GZMS"] = GZMS;
                            data1["CREATE_ON"] = DateTime.Now;
                            int a = DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "B_FAULT", data1, false);
                            if (0 == a)
                            {
                                session.Rollback();
                                session.Close();
                                result.RESULT = -1;
                                result.MSG = "更新故障表失败";
                                return result;
                            }
                        }
                    }
                    else {
                        data1["ID"] = records[0]["ID"];
                        data1["SBYXZT"] = "3";
                        int a = DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord(session, "B_MACHINE", data1, false);
                        if (0 == a)
                        {
                            session.Rollback();
                            session.Close();
                            result.RESULT = -1;
                            result.MSG = "更新设备表设备状态失败";
                            return result;
                        }
                        UnCaseSenseHashTable data2 = new UnCaseSenseHashTable();
                        data2["MACHINE_CODE"] = MACHINE_CODE;
                        data2["COMPANY_CODE"] = COMPANY_CODE;
                        data2["GZCODE"] = GZCODE;
                        data2["GZMS"] = GZMS;
                        data2["CREATE_ON"] = DateTime.Now;
                        int s = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_FAULT", data2);
                        if (0 == s)
                        {
                            session.Rollback();
                            session.Close();
                            result.RESULT = -1;
                            result.MSG = "插入故障表失败";
                            return result;
                        }
                    }                                                                                                  
                    result.RESULT = 1;
                    result.MSG = "保存成功";
                    session.Commit();
                    session.Close();
                }
                catch (Exception e)
                {
                    session.Rollback();
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = e.Message;
                    return result;
                }
            }
            else
            {
                session.Close();
                result.RESULT = -1;
                result.MSG = "设备编号不能为空";
            }
            return result;
        }




        [FWExtServiceMethod(Descript = "发放记录接口")]
        [FWExtServiceParameters(Name = "GMSFHM", Descript = "身份证号码", DefaultValue = "139123177498129871")]
        [FWExtServiceParameters(Name = "FFDW_DWDM", Descript = "单位代码", DefaultValue = "1391231")]
        [FWExtServiceParameters(Name = "FFDW_DWMC", Descript = "发放单位", DefaultValue = "广州市天河区凤凰分局")]
        [FWExtServiceParameters(Name = "FFRXM", Descript = "发放人姓名", DefaultValue = "王七")]
        [FWExtServiceParameters(Name = "FFRJH", Descript = "发放人警号", DefaultValue = "13912317")]
        [FWExtServiceParameters(Name = "FFRQ", Descript = "发放日期", DefaultValue = "20190719")]
        [FWExtServiceParameters(Name = "ZP", Descript = "现场照片", DefaultValue = "")]
        public FWExtServiceResult CardXcff(UnCaseSenseHashTable data, string GMSFHM, string FFDW_DWDM, string FFDW_DWMC, string FFRXM, string FFRJH, string FFRQ, string ZP)
        {
            FWExtServiceResult result = new FWExtServiceResult();
            Session session = DbUtilityManager.Instance.DefaultDbUtility.CreateAndOpenSession();           
            if (!string.IsNullOrEmpty(GMSFHM) && GMSFHM != "")
            {
                try
                {
                    UnCaseSenseHashTable data1 = new UnCaseSenseHashTable();
                    session.BeginTransaction();
                    data1["GMSFHM"] = GMSFHM;
                    //判断单位代码是否为空
                    if (string.IsNullOrEmpty(FFDW_DWDM) || FFDW_DWDM == "")
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "单位代码不得为空";
                        return result;
                    }
                    data1["FFDW_DWDM"] = FFDW_DWDM;
                    //判断单位名称是否为空
                    if (string.IsNullOrEmpty(FFDW_DWMC) || FFDW_DWMC == "")
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "单位名称不得为空";
                        return result;
                    }
                    data1["FFDW_DWMC"] = FFDW_DWMC;
                    //判断发放人姓名是否为空
                    if (string.IsNullOrEmpty(FFRXM) || FFRXM == "")
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "发放人姓名不得为空";
                        return result;
                    }
                    data1["FFRXM"] = FFRXM;
                    //判断发放人警号是否为空
                    if (string.IsNullOrEmpty(FFRJH) || FFRJH == "")
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "发放人警号不得为空";
                        return result;
                    }
                    data1["FFRJH"] = FFRJH;
                    //判断发放日期是否为空
                    if (string.IsNullOrEmpty(FFRQ) || FFRQ == "")
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "发放日期不得为空";
                        return result;
                    }
                    data1["FFRQ"] = FFRQ;
                    //判断现场照片是否为空
                    if (string.IsNullOrEmpty(ZP) || ZP == "")
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "现场照片不得为空";
                        return result;
                    }
                    byte[] ZP_byte = Convert.FromBase64String(ZP.Trim().Replace("%", "").Replace(",", "").Replace(" ", "+"));
                    data1["ZP"] = ZP_byte;
                    int s = DbUtilityManager.Instance.DefaultDbUtility.InsertRecord(session, "B_XCFF", data1);
                    if (0 == s)
                    {
                        session.Close();
                        result.RESULT = -1;
                        result.MSG = "更新发放表失败";
                        return result;
                    }
                    session.Commit();
                    session.Close();
                    result.RESULT = 1;
                    result.MSG = "保存成功";
                }
                catch (Exception e)
                {
                    session.Rollback();
                    session.Close();
                    result.RESULT = -1;
                    result.MSG = e.Message;
                    return result;
                }
            }
            else
            {
                session.Close();
                result.RESULT = -1;
                result.MSG = "身份证号码不得为空";
            }
            return result;
        }

    }
}


