using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Net;
using System.Configuration;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.Threading;
using System.Web.Script.Serialization;
using OMCS.Tools;
using System.Text.RegularExpressions;
using System.Drawing;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Xml;

namespace SmarterClassroomWindows
{
    /// <summary>
    /// js交互操作方法
    /// </summary>
    public class JsEvent
    {
        //委托打开文件类型
        //public delegate void OpenFileType(string filetype);
        public static parameter parameter;//角色扮演字幕（检测麦克风时传过来）用来判断是否语音评测；
        public static parameter1 parameter1;//角色扮演字幕（检测麦克风时传过来）用来判断是否语音评测；1.3后新版本使用
        public static string cbId = "-1";//回调函数ID
        public static string DownLoadFileURL = ConfigurationManager.AppSettings["DownLoadFileURL"]; //文件下载地址
        public static ChromiumWebBrowser wb;
        private IndexForm indexForm;
        private string mp3Url = "";
        public static IntPtr engine;
        public static string pathwav;
        AudioHelper recorder = new AudioHelper();
        System.Media.SoundPlayer sp;
        public JsEvent(ChromiumWebBrowser wb, IndexForm indexForm)
        {
            JsEvent.wb = wb;
            this.indexForm = indexForm;
        }
        #region 录音
        /// <summary> 
        /// 方法名首字母小写，否则js会出现找不到方法 
        /// 开始录音
        /// </summary> 
        public void recordStart()
        {
            //开始录音
            mp3Url = "";
            indexForm.startPinCe();
        }
        /// <summary>
        /// 开始录音1.3版本
        /// </summary>
        public void startRecord_n(string a)
        {
            //开始录音
            mp3Url = "";
            indexForm.startPinCe();
        }
        /// <summary> 
        /// 方法名首字母小写，否则js会出现找不到方法 
        /// 结束录音
        /// </summary> 
        public void recordEnd(string thearID, string ResID, string UploadUrl)
        {
            IndexForm.thearID = thearID;
            IndexForm.ResID = ResID.Replace("-", "");
            IndexForm.UploadUrl = UploadUrl;
            indexForm.stopPinCe();
        }
        /// <summary>
        /// 结束录音1.3版本
        /// </summary>
        /// <param name="data"></param>
        public void endRecord_n(string data)
        {
            resRecord resRecord = DecodeJson<resRecord>(data);
            cbId = resRecord.cbId;
            IndexForm.thearID = resRecord.userId;
            IndexForm.ResID = resRecord.resourcerecord.Replace("-", "");
            IndexForm.UploadUrl = resRecord.host;
            indexForm.stopPinCe();
        }
        /// <summary>
        /// 方法名首字母小写，否则js会出现找不到方法
        /// 播放录音
        /// </summary>
        public void recordPlay()
        {
            //播放录音
            sp = new System.Media.SoundPlayer(mp3Url);
            recorder.RecorPlay(sp);
        }
        /// <summary>
        /// 停止录音
        /// </summary>
        public void recordStop()
        {
            indexForm.RecordStop();
            //recorder.RecordStop();
        }
        /// <summary>
        /// 停止录音，强制中断
        /// </summary>
        public void stopRecord_n(string a)
        {
            indexForm.RecordStop();
        }
        /// <summary>
        /// 方法名首字母小写，否则js会出现找不到方法
        /// 暂停\关闭播放
        /// </summary>
        public void recordPlayClose()
        {
            try
            {
                sp.Stop();
            }
            catch (Exception e)
            {
            }

        }
        #endregion
        #region  PPT
        /// <summary>
        /// 方法名首字母小写，否则js会出现找不到方法 
        /// 打开PPT
        /// </summary>
        public void openPPT(string ID)
        {
            ID = ID.Replace("*", "");
            string path = (DownLoadFileURL == string.Empty ? (System.Environment.CurrentDirectory + "/tempfile")
                                                : DownLoadFileURL) + "/" + ID; //存放文件路径
            if (File.Exists(path))
            {
                if (path.ToLower().Contains("zip"))
                {
                    string[] resultString = Regex.Split(path, ".ZIP", RegexOptions.IgnoreCase);
                    path = OpenZip(resultString[0]);
                }

                else if (path.ToLower().Contains("rar"))
                {
                    string[] resultString = Regex.Split(path, ".rar", RegexOptions.IgnoreCase);
                    path = OpenZip(resultString[0]);
                }
                OperatePPT openppt = new OperatePPT();
                openppt.PPTOpen(path);
            }
            else
            {
                new Helper().InsertErrorMsg(ID, "找不到文件资源");
            }
        }
        public string OpenZip(string path1)
        {
            DirectoryInfo SourceDir = new DirectoryInfo(path1);
            FileSystemInfo[] filepath = SourceDir.GetFileSystemInfos();
            string path = filepath[0].FullName;
            foreach (FileSystemInfo fs in filepath)
            {
                if (fs.Extension == "")
                {
                    path1 = fs.FullName;
                    path = OpenZip(path1);
                    break;
                }
                else if ((fs.Extension.ToLower() == ".ppt" || fs.Extension == ".pptx" /*|| fs.Extension == ".xls" || fs.Extension == ".xlsx" || fs.Extension == ".doc" || fs.Extension == ".docx"*/) && !fs.ToString().Contains("~$"))
                {
                    path = SourceDir + "/" + fs.ToString();
                    break;
                }
            }
            return path;
        }
        /// <summary>
        /// 方法名首字母小写，否则js会出现找不到方法 
        /// 下载资源
        /// </summary>
        public void downLoadFile(string jsonData)
        {
            Thread tDownload = new Thread(new ParameterizedThreadStart(downLiadFileFunction));
            tDownload.IsBackground = true;
            tDownload.Start(jsonData);
        }
        /// <summary>
        /// 下载文件线程调用方法
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public void downLiadFileFunction(object jsonData)
        {
            //jsonData = "[{\"ID\":\"682fe6f2-ad00-4efe-a2dc-b538f1f72408\",\"fileUrl\":\"http://192.168.3.2:8888/2016/01/31/682fe6f2-ad00-4efe-a2dc-b538f1f72408.zip\"}]";
            if (DownLoadFileURL != string.Empty)
            {
                try
                {
                    //验证文件目录是否存在
                    if (!Directory.Exists(DownLoadFileURL))
                    {
                        Directory.CreateDirectory(DownLoadFileURL);
                    }
                    List<object> lsObj = new List<object>();
                    //解析json
                    IList<FileMsg> fileMsg = Deserialize<IList<FileMsg>>(jsonData.ToString());
                    WebClient client = new WebClient();
                    if (fileMsg != null && fileMsg.Count > 0)
                    {
                        //循环列表下载文件
                        foreach (FileMsg file in fileMsg)
                        {
                            int dloadFlag = 0;
                            //文件后缀
                            string fileExtension = "";
                            try
                            {
                                //检查文件是否存在
                                //string path = DownLoadFileURL + "/" + System.IO.Path.GetFileName(URLAddress); //存放文件路径
                                fileExtension = System.IO.Path.GetExtension(file.fileUrl);
                                string path = DownLoadFileURL + "/" + file.FileName + fileExtension; //存放文件路径 
                                path = path.Replace("*", "");                    
                                //string path = DownLoadFileURL + "/" + file.ID + ".ppt"; //存放文件路径 无后缀路径
                                if (!File.Exists(path))
                                {
                                    //File.Delete(path);
                                    #region  文件不存在 下载
                                    HttpWebRequest request = WebRequest.Create(file.fileUrl) as HttpWebRequest;
                                    //发送请求并获取相应回应数据
                                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                                    //直到request.GetResponse()程序才开始向目标网页发送Post请求
                                    Stream responseStream = response.GetResponseStream();
                                    //创建本地文件写入流
                                    Stream stream = new FileStream(path, FileMode.Create);
                                    byte[] bArr = new byte[1024];
                                    int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                                    while (size > 0)
                                    {
                                        stream.Write(bArr, 0, size);
                                        size = responseStream.Read(bArr, 0, (int)bArr.Length);
                                    }
                                    stream.Close();
                                    responseStream.Close();
                                    //判断文件下载完成
                                    if (File.Exists(path))
                                    {
                                        dloadFlag = 1;
                                        //如果是压缩包 操作解压文件
                                        if (fileExtension.Contains(".zip") || fileExtension.Contains(".ZIP") ||
                                                fileExtension.Contains(".rar") || fileExtension.Contains(".RAR"))
                                        {
                                            //Helper h = new Helper();
                                            Helper.UnZipFile(path, DownLoadFileURL + "\\" + file.FileName);
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    //文件存在 直接返回结果，等同于下载成功
                                    dloadFlag = 1;
                                }
                            }
                            catch (Exception e)
                            {
                                new Helper().InsertErrorMsg(e.Message, "下载ppt文件失败");
                                dloadFlag = 0;
                            }
                            lsObj.Add(new { ID = file.FileName, Flag = dloadFlag, FileExtension = fileExtension });

                        }
                    }
                    System.Web.Script.Serialization.JavaScriptSerializer jsc =
                                        new System.Web.Script.Serialization.JavaScriptSerializer();
                    string retStr = jsc.Serialize(lsObj);
                    if (fileMsg[0].isLessonView != "LessonView")
                    {
                        wb.ExecuteScriptAsync("updateBtnState('" + retStr + "');");
                    }
                }
                catch (Exception errorPath)
                {
                    new Helper().InsertErrorMsg(errorPath.Message, "下载文件路径错误");
                }
            }
            else
            {
                new Helper().InsertErrorMsg("下载失败", "下载文件路径为空");
            }
        }
        #endregion
        #region  页面调用方法------------------------------------------------------------
        /// <summary>
        /// 方法名首字母小写，否则js会出现找不到方法 
        /// 上课
        /// </summary>
        public void callBackLesson()
        {
            string URL = ConfigurationManager.AppSettings["DefaultURL"];
        }
        /// <summary> 
        /// 方法名首字母小写，否则js会出现找不到方法 
        /// 全屏
        /// </summary> 
        public void callBackFullScreen()
        {
            indexForm.CallBackFullScreen();
        }
        public void callBackFullScreen1()
        {
            indexForm.CallBackFullScreen1();
        }
        /// <summary> 
        /// 方法名首字母小写，否则js会出现找不到方法 
        /// 退出
        /// </summary> 
        public void callBackExit()
        {
            //string FileURL1 = ConfigurationManager.AppSettings["DownLoadFileURL"]; //文件下载地址
            //退出程序
            wb.ExecuteScriptAsync("fromformtext('退出账号,关闭窗体')");
            Application.Exit();
        }
        /// <summary>
        /// 中间页调用方法返回易课参数
        /// </summary>
        /// <returns></returns>
        public string getUserEKLoginData()
        {
            if (IndexForm.strArgs != null && IndexForm.strArgs.Count() > 0)
            {
                return IndexForm.strArgs[0].ToString().Replace("\\t\\t", "");
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 检测机器上面是否有麦克风，有/true,没有/false
        /// </summary>
        /// <returns></returns>
        public bool existMicro(string zimuD)
        {
            if (zimuD == "")
            {
                JsEvent.parameter = null;
            }
            else
            {
                JsEvent.parameter = Deserialize<parameter>(zimuD);
            }
            IList<MicrophoneInformation> microphones = SoundDevice.GetMicrophones();
            bool existMicro = false;
            if (microphones != null && microphones.Count > 0)
            {
                foreach (MicrophoneInformation mi in microphones)
                {
                    if (mi.Name.Contains("麦克风") || mi.Name.ToLower().Contains("microphone"))
                    {
                        existMicro = true;
                    }
                }
            }
            return existMicro;
        }
        /// <summary>
        /// 检测机器上面是否有麦克风，1.3版本
        /// </summary>
        /// <param name="zimuD"></param>
        /// <returns></returns>
        public void recordAuthority_n(string zimuD)
        {
            if (zimuD == "")
            {
                JsEvent.parameter = null;
            }
            else
            {
                parameter = new parameter();
                parameter1 = Deserialize<parameter1>(zimuD);
                parameter.zimu = parameter1.captions;
                parameter.wordIndex = parameter1.wordIndex.ToString();
                parameter.type = parameter1.recordType;
            }
            IList<MicrophoneInformation> microphones = SoundDevice.GetMicrophones();
            bool existMicro = false;
            if (microphones != null && microphones.Count > 0)
            {
                foreach (MicrophoneInformation mi in microphones)
                {
                    if (mi.Name.Contains("麦克风") || mi.Name.ToLower().Contains("microphone"))
                    {
                        existMicro = true;
                    }
                }
            }
            mesage msag = new mesage();
            if (existMicro)
            {
                msag.code = 0;
                msag.msg = "检测有麦克风";
                msag.data = null;
            }
            else
            {
                msag.code = 100;
                msag.msg = "检测没有麦克风";
                msag.data = null;
            }
            string res = EnSerialize<mesage>(msag);
            JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + parameter1.cbId + "','" + res + "');");
        }
        public void getName_n(string s)
        {
            name1 nam = DecodeJson<name1>(s);
            JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + nam.cbId + "','" + nam.name + "');");
        }
        #endregion
        #region  本地化页面调用方法---------------------------------------------------------------------
        //http请求会自动将对象转为json传输，本地化方法不行 \\192.168.3.116 大军共享页面
        /// <summary>
        /// 获取页面水滴json
        /// </summary>
        /// <param name="s">userid:book:pageid</param>
        public void selBookPageData_n(string s)
        {
            IndexForm.getJson(s);
            //clr_pageInit nam = DecodeJson<clr_pageInit>(s);
            //string Func = nam.cbId;
            //string jsons = null;
            //try
            //{
            //    string[] pagenum = Regex.Split(nam.Pages, ",");
            //    List<int> pages2 = new List<int>();
            //    List<string> contents = new List<string>();
            //    foreach (var p in pagenum)
            //    {
            //        if (!string.IsNullOrWhiteSpace(p))
            //        {
            //            pages2.Add(int.Parse(p));
            //        }
            //    }
            //    foreach (var p in pages2)
            //    {
            //        string pgn = "page" + p.ToString().PadLeft(3, '0');
            //        string folderFullName = AppDomain.CurrentDomain.BaseDirectory + "Page\\data\\" + nam.UserID + "\\" + nam.BookID + "\\resource\\" + pgn + "\\" + pgn + ".json"; 
            //        if (File.Exists(folderFullName))
            //        {
            //            string str = GetConfigStr(folderFullName);
            //            contents.Add(str);
            //        }
            //    }
            //    if (contents.Count != 0)
            //    {
            //        jsons = EnSerialize<List<string>>(contents);
            //    }
            //    string re = EnSerialize<LocalFunMsg>(GetLocalFunMsg(jsons));
            //    JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + re + ");");
            //}
            //catch (Exception ex)
            //{
            //    LocalFunMsg fas = GetLocalFunMsg("", 1,false,  ex.Message);
            //    JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + fas + ");");
            //}
        }
        /// <summary>
        /// 获取互动课件配置首页html
        /// </summary>
        /// <param name="s"></param>
        public void wareEntry_n(string s)
        {
            clr_pageInit nam = DecodeJson<clr_pageInit>(s);
            string Func = nam.cbId;
            string json = "";
            try
            {
                string warep1 = @"Page\data\" + nam.UserID + "\\" + nam.BookID + "\\resource\\page" + nam.PageID.PadLeft(3, '0');
                DirectoryInfo TheFolder1 = new DirectoryInfo(warep1);
                FileInfo file = TheFolder1.GetFiles(nam.FileID + "*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (!file.FullName.Contains(".zip"))
                {
                    wareEntry wareEntry = new wareEntry { Url = file.FullName, Json = json };
                    string wareEntry1 = EnSerialize<wareEntry>(wareEntry);
                    //string re = EnSerialize<LocalFunMsg>(GetLocalFunMsg(wareEntry));
                    LocalFunMsg re = GetLocalFunMsg(wareEntry1);
                    string res = EnSerialize<LocalFunMsg>(re);
                    JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + res + ");");
                }
                else
                {
                    string warep = @"Page\data\" + nam.UserID + "\\" + nam.BookID + "\\resource\\page" + nam.PageID.PadLeft(3, '0') + "\\" + nam.FileID;
                    string folderFullName = warep;
                    DirectoryInfo TheFolder = new DirectoryInfo(folderFullName);
                    DirectoryInfo[] directory = TheFolder.GetDirectories();
                    folderFullName = AppDomain.CurrentDomain.BaseDirectory + warep + "\\" + directory[0].Name + "\\" + directory[0].Name + ".html";
                    string configJson = directory[0].FullName + "\\config.json";
                    if (File.Exists(configJson))
                    {
                        json = GetConfigStr(configJson);
                    }
                    wareEntry wareEntry = new wareEntry { Url = folderFullName, Json = json };
                    string wareEntry1 = EnSerialize<wareEntry>(wareEntry);
                    //string re = EnSerialize<LocalFunMsg>(GetLocalFunMsg(wareEntry));
                    LocalFunMsg re = GetLocalFunMsg(wareEntry1);
                    string res = EnSerialize<LocalFunMsg>(re);
                    JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + res + ");");
                }
            }
            catch (Exception ex)
            {
                LocalFunMsg fas = GetLocalFunMsg("", 1, false, ex.Message);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + fas + ");");
            }
        }
        /// <summary>
        /// 获取电子书dbjs
        /// </summary>
        /// <param name="s"></param>
        public void getDbJsById_n(string s)
        {
            clr_pageInit nam = DecodeJson<clr_pageInit>(s);
            try
            {
                string bookFullName = AppDomain.CurrentDomain.BaseDirectory + @"Page\data\" + nam.UserID + "\\" + nam.BookID;
                DirectoryInfo TheFolder = new DirectoryInfo(bookFullName);
                DirectoryInfo[] directory = TheFolder.GetDirectories();
                string folderFullName = null;
                for (int i = 0; i < directory.Length; i++)
                {
                    if (File.Exists(bookFullName + "\\" + directory[i] + "\\db.js"))
                    {
                        folderFullName = bookFullName + "\\" + directory[i] + "\\db.js";
                        break;
                    }
                }
                string json = "";
                if (File.Exists(folderFullName))
                {
                    json = GetConfigStr(folderFullName);
                    json = Regex.Split(json, "var dbJson='")[1];
                    json = Regex.Split(json, "';")[0];
                }
                //string re = EnSerialize<LocalFunMsg>(GetLocalFunMsg(json));
                LocalFunMsg re = GetLocalFunMsg(json);
                string res = EnSerialize<LocalFunMsg>(re);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + nam.cbId + "'," + res + ");");

            }
            catch (Exception ex)
            {
                LocalFunMsg fas = GetLocalFunMsg("", 1, false, ex.Message);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + nam.cbId + "'," + fas + ");");
            }
        }
        /// <summary>
        /// 获取电子书目录
        /// </summary>
        /// <param name="s"></param>
        public void getCatalogByBookId_n(string s)
        {
            clr_pageInit nam = DecodeJson<clr_pageInit>(s);
            string Func = nam.cbId;
            try
            {
                string folderFullName = AppDomain.CurrentDomain.BaseDirectory + @"Page\data\" + nam.UserID + "\\" + nam.BookID + "\\catalog.json";
                string json = "";
                if (File.Exists(folderFullName))
                {
                    json = GetConfigStr1(folderFullName);
                }
                //string re = EnSerialize<LocalFunMsg>(GetLocalFunMsg(json));
                LocalFunMsg re = GetLocalFunMsg(json);
                string res = EnSerialize<LocalFunMsg>(re);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + res + ");");
            }
            catch (Exception ex)
            {
                LocalFunMsg fas = GetLocalFunMsg("", 1, false, ex.Message);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + fas + ");");
            }
        }
        /// <summary>
        /// 获取教学地图
        /// </summary>
        /// <param name="s"></param>
        public void getCurTeachMap_n(string s)
        {
            IndexForm.getJson(s);
            //clr_pageInit nam = DecodeJson<clr_pageInit>(s);
            //string Func = nam.cbId;
            //try
            //{
            //    string folderFullName = AppDomain.CurrentDomain.BaseDirectory + @"Page\data\" + nam.UserID + "\\" + nam.BookID + "\\resource\\" + nam.UnitID + ".json";
            //    string json = "";
            //    if (File.Exists(folderFullName))
            //    {
            //        json = GetConfigStr(folderFullName);
            //    }
            //    // string re = EnSerialize<LocalFunMsg>(GetLocalFunMsg(json));
            //    LocalFunMsg re = GetLocalFunMsg(json);
            //    string res = EnSerialize<LocalFunMsg>(re);
            //    JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + res + ");");
            //}
            //catch (Exception ex)
            //{
            //    LocalFunMsg fas = GetLocalFunMsg("", 1,false,  ex.Message);
            //    JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + fas + ");");
            //}
        }
        /// <summary>
        /// 写本地用户操作日志
        /// </summary>
        /// <param name="operatorInfo"></param>
        /// <param name="Func"></param>
        public void userOperLog_n(string s)
        {
            IndexForm.getJson(s);
            //operatorInfo = "{\"UserID\":\"49a92e48-1f39-4382-9273-0a0d521162c6\",\"UserType\":12,\"OperID\":1102,\"Content\":\"备课模块\"}";
            string folderFullName = AppDomain.CurrentDomain.BaseDirectory + @"Page\data\userLog.json";
            FileStream fs;
            StreamWriter sw;
            if (File.Exists(folderFullName))
            //验证文件是否存在，有则追加，无则创建
            {
                fs = new FileStream(folderFullName, FileMode.Append, FileAccess.Write);
            }
            else
            {
                fs = new FileStream(folderFullName, FileMode.Create, FileAccess.Write);
            }
            sw = new StreamWriter(fs);
            sw.WriteLine(s + ";");
            sw.Close();
            fs.Close();
        }
        /// <summary>
        /// 创建或更新本地老师授课翻页记录
        /// </summary>
        /// <param name="s"></param>
        /// <param name="Func"></param>
        public void postTeachPage_n(string s)
        {
            IndexForm.getJson(s);
            //bool ishave = false;
            ////page = "{\"UserID\":\"49a92e48-1f39-4382-9273-0a0d521162c6\",\"BookType\":null,\"Stage\":null,\"SubjectID\":1,\"EditionID\":27,\"GradeID\":3,\"BookID\":3107,\"UnitID\":290075,\"PageNum\":15,\"PageInitID\":\"A61C09F1-E387-71B1-2E13-0D01E93E5D27\",\"ClassID\":-1,\"CreateTime\":\"/Date(1520579894000)/\",\"AspxName\":\"Teaching\",\"UnitName\":\"2 秋天的图画\"}";            
            //try
            //{
            //    clr_pageInit clr_pageInit = DecodeJson<clr_pageInit>(s);
            //    string folderFullName = AppDomain.CurrentDomain.BaseDirectory + @"Page\data\teachData.json";
            //    FileStream fs;
            //    StreamWriter sw;
            //    if (File.Exists(folderFullName))
            //    //验证文件是否存在，有则追加，无则创建
            //    {
            //        string json = GetConfigStr(folderFullName);
            //        if (json == "")
            //        {
            //            fs = new FileStream(folderFullName, FileMode.Append, FileAccess.Write);
            //        }
            //        else
            //        {
            //            List<string> ss = Regex.Split(json, ";").ToList();                        
            //            for (int i = 0; i < ss.Count - 1; i++)
            //            {
            //                clr_pageInit clr_pageInit1 = DecodeJson<clr_pageInit>(ss[i]);
            //                if (clr_pageInit.UserID == clr_pageInit1.UserID && clr_pageInit.ClassID == clr_pageInit1.ClassID && clr_pageInit.BookID.ToString() == clr_pageInit1.BookID.ToString() && clr_pageInit.AspxName == clr_pageInit1.AspxName)
            //                {
            //                    ss[i] = s;
            //                    ishave = true;
            //                    break;
            //                }
            //            }
            //            if (ishave)
            //            {
            //                string strTemp1 = string.Join(";", ss.ToArray());
            //                s = strTemp1;
            //                fs = new FileStream(folderFullName, FileMode.Create, FileAccess.Write);
            //            }
            //            else
            //            {
            //                fs = new FileStream(folderFullName, FileMode.Append, FileAccess.Write);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        fs = new FileStream(folderFullName, FileMode.Create, FileAccess.Write);
            //    }
            //    sw = new StreamWriter(fs,Encoding.UTF8);
            //    if (ishave)
            //    {
            //        sw.WriteLine(s);
            //    }
            //    else
            //    {
            //        sw.WriteLine(s + ";");
            //    }
            //    sw.Close();
            //    fs.Close();
            //}
            //catch (Exception ex)
            //{

            //    throw;
            //}
        }
        public void getTeachPage_n(string s)
        {
            clr_pageInit nam = DecodeJson<clr_pageInit>(s);
            string Func = nam.cbId;
            try
            {
                string folderFullName = AppDomain.CurrentDomain.BaseDirectory + @"Page\data\" + nam.UserID + "\\" + nam.UserID + nam.BookID + nam.ClassID + ".json";
                string json = null;
                if (File.Exists(folderFullName))
                {
                    json = GetConfigStr(folderFullName);
                }
                LocalFunMsg re = GetLocalFunMsg(json);
                string res = EnSerialize<LocalFunMsg>(re);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + res + ");");
            }
            catch (Exception ex)
            {
                LocalFunMsg fas = GetLocalFunMsg("", 1, false, ex.Message);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + Func + "'," + fas + ");");
            }
        }
        /// <summary>
        /// 传入文件地址，读出字符串
        /// </summary>
        /// <param name="folderFullName"></param>
        /// <returns></returns>
        public static string GetConfigStr(string folderFullName)
        {
            FileStream fileStream = new FileStream(folderFullName, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream, System.Text.Encoding.Default);
            string json = streamReader.ReadToEnd();
            fileStream.Flush();
            fileStream.Close();
            streamReader.Dispose();
            streamReader.Close();
            return json;
        }
        public static string GetConfigStr1(string folderFullName)
        {
            FileStream fileStream = new FileStream(folderFullName, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
            string json = streamReader.ReadToEnd();
            fileStream.Flush();
            fileStream.Close();
            streamReader.Dispose();
            streamReader.Close();
            return json;
        }
        public static LocalFunMsg GetLocalFunMsg(object data, int code = 0, bool success = true, string message = "")
        {
            return new LocalFunMsg() { Code = code, Data = data, Success = success, Message = "" };
        }
        public static void SendMsg(string s)
        {
            Message nam = JsEvent.DecodeJson<Message>(s);
            try
            {
                string re = EnSerialize<LocalFunMsg>(GetLocalFunMsg(nam.data));
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + nam.cbId + "'," + re + ");");
            }
            catch (Exception ex)
            {
                LocalFunMsg fas = GetLocalFunMsg("", 1, false, ex.Message);
                JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + nam.cbId + "'," + fas + ");");
            }
        }
        #endregion
        /// <summary>
        /// 从页面获取资源数据传给易课
        /// </summary>
        /// <param name="json"></param>
        public void exchangeData(string json)
        {
            IntPtr hwndRecvWindow = IntPtr.Zero;
            try
            {
                RegistryKey key;
                key = Registry.CurrentUser;
                ///////////////////////////////待确认//////////////////////////////////////
                RegistryKey s = key.OpenSubKey("SOFTWARE\\ClovSoft\\EasyControl3\\SET");
                string sd = s.GetValue("MAINWND").ToString();
                hwndRecvWindow = (IntPtr)UInt32.Parse(sd);
                s.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("未检测到易课");
                return;
            }
            //自己的窗口句柄  
            IntPtr hwndSendWindow = IndexForm.Intptrw;
            if (hwndSendWindow == IntPtr.Zero)
            {
                return;
            }
            //填充COPYDATA结构  
            OperatePPT.ImportFromDLL.COPYDATASTRUCT copydata = new OperatePPT.ImportFromDLL.COPYDATASTRUCT();
            copydata.cbData = Encoding.Unicode.GetBytes(json).Length; //长度 注意不要用strText.Length;  
            copydata.lpData = json;                                   //内容  
            copydata.dwData = 2;
            OperatePPT.ImportFromDLL.SendMessage(hwndRecvWindow, OperatePPT.ImportFromDLL.WM_COPYDATA, hwndSendWindow, ref copydata);
        }
        #region  序列化------------------------------------------------------------------
        /// <summary>
        /// 反序列化Json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="JsonString"></param>
        /// <returns></returns>
        public T Deserialize<T>(string JsonString)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonString)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(ms);
            }

        }
        public static string JsonSerializer<T>(T t)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            MemoryStream ms = new MemoryStream();
            ser.WriteObject(ms, t);
            string jsonString = Encoding.UTF8.GetString(ms.ToArray());
            ms.Close();
            //替换Json的Date字符串
            string p = @"\\/Date\((\d+)\+\d+\)\\/";
            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertJsonDateToDateString);
            Regex reg = new Regex(p);
            jsonString = reg.Replace(jsonString, matchEvaluator);
            return jsonString;
        }
        /// <summary>
        /// 将Json序列化的时间由/Date(1294499956278+0800)转为字符串
        /// </summary>
        private static string ConvertJsonDateToDateString(Match m)
        {
            string result = string.Empty;
            DateTime dt = new DateTime(1970, 1, 1);
            dt = dt.AddMilliseconds(long.Parse(m.Groups[1].Value));
            dt = dt.ToLocalTime();
            result = dt.ToString("yyyy-MM-dd HH:mm:ss");
            return result;
        }
        /// <summary>
        /// 数据序列化成Json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string EnSerialize<T>(T data)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));//data.GetType()
                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, data);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch (Exception ee)
            {
                return null;
            }
        }

        public static T DecodeJson<T>(string json) where T : new()
        {
            T obj;
            if (!String.IsNullOrEmpty(json))
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                obj = (T)serializer.Deserialize(json, typeof(T));
            }
            else
            {
                obj = default(T);
            }
            return obj;
        }
        #endregion        
    }
    #region   各种类
    public class name1
    {
        public string cbId { get; set; }
        public string name { get; set; }
    }
    public class configjson
    {
        public string title { get; set; }
    }
    public class wareEntry
    {
        public string Url { get; set; }
        public string Json { get; set; }
    }
    public class FileMsg
    {
        //资源文件ID
        public string ID { get; set; }
        //资源文件地址
        public string fileUrl { get; set; }
        //资源文件名
        public string FileName { get; set; }
        public string isLessonView { get; set; }
    }
    public class MovieData
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public string ResourceID { get; set; }
        /// <summary>
        /// 文件地址
        /// </summary>
        public string FileUrl { get; set; }
    }
    /// <summary>
    /// 先声语音测评回调消息
    /// </summary>
    public class message
    {
        public result result { get; set; }
    }
    public class result
    {
        public string overall { get; set; }
    }
    /// <summary>
    /// 角色扮演检测麦克风时，传过来的参数
    /// </summary>
    public class parameter
    {
        public string zimu { get; set; }
        public string type { get; set; }
        public string isAgain { get; set; }
        public string wordIndex { get; set; }
    }
    public class parameter1
    {
        public string cbId { get; set; }//回调函数id
        public string resourceName { get; set; }//课件名称
        public string recordType { get; set; }//资源类型
        public string captions { get; set; }//字幕
        public int wordIndex { get; set; }//当前局序号
    }
    /// <summary>
    /// 回应请求
    /// </summary>
    public class mesage
    {
        public int code { get; set; }
        public string msg { get; set; }
        public data data { get; set; }
    }
    public class LocalFunMsg
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
    public class data
    {
        public int score { get; set; }
        public string recordFileUrl { get; set; }
    }
    public class resRecord
    {
        public string cbId { get; set; }   //回调函数ID
        public string host { get; set; }    //string 服务器地址
        public string resourcerecord { get; set; }   //string 资源hash
        public int resourceType { get; set; }   // number 文件类型
        public string userId { get; set; }   //string 用户id

    }
    /// <summary>
    /// 智慧教室写操作日志
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="userType">用户类型</param>
    /// <param name="OperId">操作类型Id</param>
    /// <param name="content">内容</param>
    public class Operator
    {
        public string userId { get; set; }
        public int userType { get; set; }
        public int OperId { get; set; }
        public string content { get; set; }
    }
    public partial class clr_pageInit
    {
        public string cbId { get; set; }
        public string Pages { get; set; }
        public string PageID { get; set; }
        public string FileID { get; set; }
        public string PageInitID { get; set; }
        public string UserID { get; set; }
        public Nullable<int> GradeID { get; set; }
        public Nullable<int> ClassID { get; set; }
        public string BookID { get; set; }
        public Nullable<int> SubjectID { get; set; }
        public Nullable<int> UnitID { get; set; }
        public Nullable<int> PageNum { get; set; }
        public object CreateTime { get; set; }
        public string AspxName { get; set; }
        public Nullable<int> EditionID { get; set; }
        public Nullable<int> BookType { get; set; }
        public Nullable<int> Stage { get; set; }
        public string UnitName { get; set; }
    }
    #endregion
}
