using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace 数据采集系统.通用类
{
    public  class IniFile
    {
        public string Path;
        public string strFilePath = Application.StartupPath;

        public IniFile (string path)
        {
            this.strFilePath += path;
            this.Path = this.strFilePath;
        }

        public IniFile(string cd, string path)
        {
            this.Path = cd + path;
        }

        #region "声明变量"
        /// <summary>
        /// 写入INI文件
        /// </summary>
        /// <param name="section">节点名称[如[TypeName]]</param>
        /// <param name="key">键</param>
        /// <param name="val">值</param>
        /// <param name="filepath">文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);
        /// <summary>
        /// 读取INI文件
        /// </summary>
        /// <param name="section">节点名称</param>
        /// <param name="key">键</param>
        /// <param name="def">值</param>
        /// <param name="retval">stringbulider对象</param>
        /// <param name="size">字节大小</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);

        /// <summary>
        /// 读取INI文件（INT型）
        /// </summary>
        /// <param name="section">节点名称</param>
        /// <param name="key">键</param>
        /// <param name="nDefault">没找到时返回的默认值</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>读取的INT值</returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileInt(string section, string key, int nDefault, string filePath);

        #endregion

        /// <summary>
        /// ini写操作
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <param name="iValue">值</param>
        public void IniWriteValue(string section, string key, string iValue)
        {
            WritePrivateProfileString(section, key, iValue, this.Path);
        }

        /// <summary>
        /// ini读操作（INT型）
        /// </summary>
        /// <param name="section">节点名称</param>
        /// <param name="key">键</param>
        /// <param name="nDefault">没找到时返回的默认值</param>
        /// <returns>读取的INT值</returns>
        public int IniReadInt(string section,string key,int nDefault)
        {
            return GetPrivateProfileInt(section, key, nDefault, this.Path);
        }
        public int IniReadInt(string section, string key, int nDefault, string filePath)
        {
            return GetPrivateProfileInt(section, key, nDefault, filePath);
        }
        /// <summary>
        /// ini读操作
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <returns>返回读取值</returns>
        public string IniReadValue(string section, string key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", temp, 255, this.Path);
            return temp.ToString();
        }

        public string IniReadValueWithPath(string section, string key, string filePath)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", temp, 255, filePath);
            return temp.ToString();
        }

        public string IniReadValue(string section, string key,string defaultValue)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", temp, 255, this.Path);
            if (temp.ToString() == "")
                return defaultValue;
            else
                return temp.ToString();
        }

        
        public static string IniReadValue(string section, string key, string defaultValue, string filePath)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", temp, 255, filePath);
            if (temp.ToString() == "")
                return defaultValue;
            else
                return temp.ToString();
        }
    }
}
