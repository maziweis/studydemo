using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading;
using System.IO;
using System.Net;
//using NAudio.Wave;

namespace SmarterClassroomWindows
{
    public class AudioHelper
    {
        IntPtr m_engine;
        //http://www.cnblogs.com/xiaofengfeng/p/3573331.html 
        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        //该函数有四个参数：
        //第一个参数：要发送的命令字符串。字符串结构是:[命令][设备别名][命令参数].
        //第二个参数：返回信息的缓冲区,为一指定了大小的字符串变量.
        //第三个参数：缓冲区的大小,就是字符变量的长度.
        //第四个参数：回调方式，一般设为零
        //返回值：函数执行成功返回零，否则返回错误代码
        public static extern int mciSendString(
        string lpstrCommand,
        string lpstrReturnString,
        int uReturnLength,
        int hwndCallback
       );

        public void RecordStart()
        {
            mciSendString("close movie", "", 0, 0);
            mciSendString("set wave bitpersample 16", "", 0, 0);//设为16位
            mciSendString("set wave samplespersec 16000", "", 0, 0);//设为16000Hz
            mciSendString("set wave channels 1", "", 0, 0);//设为立体声：
            mciSendString("set wave format tag pcm", "", 0, 0); //实现PCM格式（不一定正确）：
            mciSendString("open new type WAVEAudio alias movie", "", 0, 0);//开始录音：别名为movie
            mciSendString("record movie", "", 0, 0);
        }

        /// <summary>
        /// 结束录音
        /// </summary>
        /// <param name="recordURL">录音存放主文件夹地址</param>
        /// <param name="thearID">教师ID</param>
        /// <param name="ResID">资源文件ID</param>
        public string RecordEnd(string recordURL, string thearID, string ResID, string UploadUrl)
        {

            //Stop();
            //string URL = System.Configuration.ConfigurationManager.AppSettings["ResouseServer"];
            string URL = UploadUrl;
            DateTime dt = DateTime.Now;
            //System.Environment.CurrentDirectory
            string path = (recordURL == string.Empty ? "D:" : recordURL) + "\\Record\\" + thearID.Replace("-", "");// + "\\" + dt.Year + "\\" + dt.Month;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //string fileName = dt.ToFileTime().ToString();

            mciSendString("stop movie", "", 0, 0); //停止录音
            //string path1 = path + "\\" + ResID.Replace("-", "") + ".wav";
            string path2 = path + "\\" + ResID.Replace("-", "") + ".mp3";
            if (JsEvent.parameter != null && JsEvent.parameter.zimu != null && JsEvent.parameter.zimu != "")
            {
                //path1 = path + "\\" + ResID.Replace("-", "") + JsEvent.parameter.wordIndex + ".wav";
                path2 = path + "\\" + ResID.Replace("-", "") + JsEvent.parameter.wordIndex + ".mp3";
            }         
            int a = mciSendString("save movie " + path2, "", 0, 0); //保存mp3给前端
                                                                    //mciSendString("save movie " + path1, "", 0, 0); //保存wav测评            
            mciSendString("close movie", "", 0, 0);                                          //mciSendString("close movie", "", 0, 0);
            if (a == 0)
            {
                try
                {
                    //上传文件
                    //return path + "\\" + fileName + ".mp3";
                    string url = "";
                    WebClient client = new WebClient();
                    //JsEvent je = new JsEvent(null, null);
                    JsonFile jsonFile1 = new JsonFile();
                    jsonFile1.FileID = ResID;
                    if (JsEvent.parameter != null && JsEvent.parameter.zimu != null && JsEvent.parameter.zimu != "")
                    {
                        jsonFile1.FileID = ResID + JsEvent.parameter.wordIndex;
                    }
                    jsonFile1.UserName = thearID;
                    jsonFile1.ResourceStyle = 101;
                    string JsonFile = JsEvent.EnSerialize<JsonFile>(jsonFile1);
                    byte[] bt = client.UploadFile(URL + "?JsonFile=" + JsonFile, path2);
                    string temp = System.Text.Encoding.UTF8.GetString(bt);
                    List<string> ls = temp.Split(',').ToList();
                    if (ls != null && ls.Count == 8)
                    {
                        List<string> ls2 = ls[6].Split(',').ToList();
                        url = ls2[0].Replace("\"FilePath\":\"", "").Replace("\"}", "");
                        return url;
                    }
                    else
                    {
                        //return "false上传错误1" + path;
                        return "";
                    }
                }
                catch (Exception ex)
                {
                    //return "false上传错误2" + ex.Message;
                    return "";
                }

            }
            else
            {
                //return "false保存失败" + path;
                return "";
            }
        }

        /// <summary>
        /// 播放录音
        /// </summary>
        /// <param name="recordURL">音频地址</param>
        public void RecorPlay(System.Media.SoundPlayer sp)
        {
            sp.Play();
            //sp.sto
        }
        /// <summary>
        /// 停止录音，不保存
        /// </summary>
        public void RecordStop()
        {
            //mciSendString("stop movie", "", 0, 0); //停止录音
            //mciSendString("close movie", "", 0, 0);
        }

    }
    public class JsonFile
    {
        public string FileID { get; set; }
        public string UserName { get; set; }
        public int ResourceStyle { get; set; }

    }


}
