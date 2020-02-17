using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using MisFrameWork.core;
using MisFrameWork.core.db;
using MisFrameWork.core.db.Support;

namespace MisFrameWork3.Classes.Membership
{
    public class FWUserInfo
    {
        UnCaseSenseHashTable userData = new UnCaseSenseHashTable();
        List<UnCaseSenseHashTable> enabledRoles = new List<UnCaseSenseHashTable>();
        List<UnCaseSenseHashTable> disabledRoles = new List<UnCaseSenseHashTable>();
        List<String> operateList = new List<string>();
        int roleLevel = 10000;
        public UnCaseSenseHashTable GetUserData()
        {
            return userData;
        }

        public List<UnCaseSenseHashTable> GetRolesId(bool isDisabled)
        {
            if (isDisabled)
                return disabledRoles;
            else
                return enabledRoles;
        }

        public bool LoadData(string uid)
        {
            try
            {
                Random rnd = new Random();
                UnCaseSenseHashTable updateData = new UnCaseSenseHashTable();
                updateData["USER_ID"] = uid;
                updateData["LOGINED_FLAGS"] = rnd.Next() * 123456;
                DbUtilityManager.Instance.DefaultDbUtility.UpdateRecord("FW_S_USERS", updateData, false);
                userData = DbUtilityManager.Instance.DefaultDbUtility.GetOneRecord("FW_S_USERS", uid);
                if (userData == null)
                    return false;
            }
            catch (Exception e)
            {
                return false;
            }
            //加载角色数据
            Condition cdtRoles = new Condition("AND", "USER_ID", "=", uid);
            List<UnCaseSenseHashTable> roles = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_USERS_M_ROLES", cdtRoles, "*", null);
            //查询出所有权限
            Condition cdtOperate = new Condition();
            Condition cdtOperateID = new Condition();

            foreach (UnCaseSenseHashTable r in roles)
            {
                UnCaseSenseHashTable rDetail = DbUtilityManager.Instance.DefaultDbUtility.GetOneRecord("FW_S_ROLES", r.GetIntValue("ROLE_ID"),"ID,SORT_CODE,ROLE_NAME,DISABLED");
                if (rDetail.GetIntValue("DISABLED", 0)==0)
                {
                    if (roleLevel > rDetail.GetIntValue("SORT_CODE"))
                        roleLevel = rDetail.GetIntValue("SORT_CODE");
                    enabledRoles.Add(rDetail);
                    cdtOperateID.AddSubCondition("OR", "ROLE_ID", "=", rDetail["ID"]);
                }
                else
                    disabledRoles.Add(rDetail);
            }
            cdtOperate.AddSubCondition("AND", "DISABLED", "=", 0);
            cdtOperate.AddSubCondition(cdtOperateID);
            List<UnCaseSenseHashTable> operateData = DbUtilityManager.Instance.DefaultDbUtility.Query("FW_S_ROLES_OPERATE", cdtOperate, "OPERATE_ID", null, "OPERATE_ID",-1,-1);
            foreach (UnCaseSenseHashTable record in operateData)
                operateList.Add(record["OPERATE_ID"].ToString());
            return true;
        }

        public string UserId
        {
            get { return (string)userData["USER_ID"]; }
        }
        public string UserName
        {
            get { return (string)userData["USER_NAME"]; }
        }

        public string Password
        {
            get { return (string)userData["USER_PASSWD"]; }
        }

        public string CompanyId
        {
            get { return (string)userData["COMPANY_ID"]; }
        }

        public string CompanyName
        {
            get { return (string)userData["COMPANY_ID_V_D_FW_COMP__MC"]; }
        }

        /// <summary>
        /// 角色级别，越小角色等级越高，小的等级不能修改大等级的权限
        /// </summary>
        public int RoleLevel
        {
            get { return roleLevel; }
        }

        public int LoginedFlags
        {
            get { return userData.GetIntValue("LOGINED_FLAGS", 0); }
        }

        public Boolean Disabled
        {
            get { return userData.GetIntValue("DISABLED",0)==1; }
        }

        /// <summary>
        /// 用于需要验证单点登陆的业务
        /// </summary>
        /// <returns></returns>
        public bool CheckLoginedFlags()
        {
            try
            {
                UnCaseSenseHashTable userInfo = DbUtilityManager.Instance.DefaultDbUtility.GetOneRecord("FW_S_USERS", UserId, "USER_ID,LOGINED_FLAGS");
                if (userInfo == null)
                    return false;
                return (userInfo.GetIntValue("LOGINED_FLAGS", 0) == this.LoginedFlags);

            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool HaveAuthority(string ywid)
        {
            if (roleLevel == 0)
                return true;
            foreach (string operateId in operateList)
            {
                if (operateId.Equals(ywid))
                    return true;
            }
            return false;
        }
    }
}