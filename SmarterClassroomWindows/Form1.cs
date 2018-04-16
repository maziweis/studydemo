using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.Configuration;
using System.Net;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Xml;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.Wave;
using System.Threading;
using SmarterClassroomWindows.CommonLib;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace SmarterClassroomWindows
{
    public partial class IndexForm : Form
    {
        #region 评测参数
        delegate void SetTextCallback(string text, int msgId);
        private WaveFileWriter writer;
        private WaveInStream wi;
        //传入参数
        private string appKey = "t135";
        private string secretKey = "1eb07a38f3834b7ea666934cb0ce3085";
        private string userId = "ssound_text";
        private string audioType = "wav";
        private string coreType = "en.word.score";
        private string coreProvideType = "native";
        private string reftext = "";
        private int sampleRate = 16000;
        private string audioUrl = "";
        private string wavePath = "";
        public CustomMessageQueue myMsgQueue = null;
        private static ssound_callback _callback;
        private static readonly int AIENGINE_MESSAGE_TYPE_JSON = 1;
        private static readonly int AIENGINE_OPT_GET_VERSION = 1;
        IntPtr m_engine;
        [DllImport("ssound.dll")]
        public static extern IntPtr ssound_new(string cfg);

        [DllImport("ssound.dll")]
        public static extern int ssound_delete(IntPtr engine);

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)] // 1.4.0
        public delegate int ssound_callback(IntPtr usrdata, [MarshalAs(UnmanagedType.LPStr)] string id, int type, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeParamIndex = 4)] byte[] message, int size);

        [DllImport("ssound.dll")]
        public static extern int ssound_start(IntPtr engine, string param, byte[] id, [MarshalAs(UnmanagedType.FunctionPtr)]ssound_callback callback, IntPtr usrdata);

        [DllImport("ssound.dll")]
        public static extern int ssound_feed(IntPtr engine, byte[] data, int size);

        [DllImport("ssound.dll")]
        public static extern int ssound_stop(IntPtr engine);

        [DllImport("ssound.dll")]
        public static extern int ssound_cancel(IntPtr engine);

        [DllImport("ssound.dll")]
        public static extern int ssound_opt(IntPtr engine, int opt, byte[] data, int size);

        public string result = ""; //返回的结果
        public static string[] strArgs;
        public int openEkFlag = 0;
        private static string DEFAULT_URL = ConfigurationManager.AppSettings["DefaultURL"];          //站点访问地址
        private static string IfFile = ConfigurationManager.AppSettings["IfFile"];          //站点访问地址
        [System.Runtime.InteropServices.DllImport("winInet.dll")]
        private static extern bool InternetGetConnectedState(ref int dwFlag, int dwReserved);
        private const int INTERNET_CONNECTION_MODEM = 1;
        private const int INTERNET_CONNECTION_LAN = 2;
        public static IntPtr Intptrw;
        public ChromiumWebBrowser wb = null;
        public static WebClient client = new WebClient();
        public static JsonFile jsonFile1 = new JsonFile();
        public static string LuYinPath = null;//录音保存名称
        public static string thearID = null;//结束录音用户ID
        public static string ResID = null;//资源ID
        public static string UploadUrl = null;//上传地址
        static ProcessStartInfo psi = new ProcessStartInfo();//进程启动时设置的一组值       
        #endregion
        #region Socket参数
        /*服务端*/
        SynchronizationContext m_SyncContext = null;
        string ip;
        public static string PortStr1 = ConfigurationManager.AppSettings["PortStr"]; //文件下载地址
        int portStr = Convert.ToInt32(PortStr1);
        private static Socket serverSocket;
        private static Socket cSocket;
        public static List<Socket> socketList=new List<Socket>();
        private static string clientIp;//安卓IP
        const int ClientPoint = 20000;//安卓端口
        const int BagLength = 1000;
        static bool IsTime = false;//默认为假，电子书下载完后为true
        #endregion
        #region     Form方法
        public IndexForm(string[] args)
        {
            InitializeComponent();
            //m_SyncContext = SynchronizationContext.Current;
            _callback = callback;//初始化回调函数 
            strArgs = args;
        }
        private void IndexForm_Load(object sender, EventArgs e)
        {
            //string path = @"F:\SVN工作目录\优教学（V3.0）\A_项目组工作区\1_软件工程\1_5程序开发\1_5_1源代码\online\applications\SmarterClassroomWindows\SmarterClassroomWindows\bin\x86\Debug\Page\data\9\266.zip";
            //string direc = Regex.Split(path, ".zip")[0];
            //Helper.UnZipFile(path, direc);
            IndexForm.Intptrw = this.Handle;
            //JsEvent.postTeachPage_n("");
            //getBook();
            socket();
            waitCon();
            initEngineOnce();//初始化录音引擎            
        }
        private void IndexForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_engine != IntPtr.Zero)
            {
                ssound_delete(m_engine);
            }
            m_engine = IntPtr.Zero;
            AudioHelper.mciSendString("close movie", "", 0, 0);
            CloseSocket();
            //Cef.Shutdown();
            Thread th = new Thread(CloseProcess);
            th.IsBackground = false;
            th.Start();
        }
        public static void CloseProcess()
        {
            try
            {
                Process[] allProcess = Process.GetProcesses();
                foreach (Process p in allProcess)
                {
                    if (p.ProcessName == "CefSharp.BrowserSubprocess" || p.ProcessName == "SmarterClassroomWindows")
                    {
                        for (int i = 0; i < p.Threads.Count; i++)
                            p.Threads[i].Dispose();
                        p.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                new Helper().InsertErrorMsg(ex.Message, "关闭进程错误");
            }
        }
        public void waitCon()
        {
            if (!IsTime)
            {
                Thread.Sleep(100);
                waitCon();
            }
            else
                GoToWeb();
        }
        #endregion
        #region 线程打开易课
        /// <summary>
        /// 根据参数判断是否需要打开EasyControl3
        /// </summary>
        private void OpenEasyControl3()
        {
            System.Threading.Thread t = new System.Threading.Thread(StartExe);
            t.Start();
        }
        /// <summary>
        /// 线程执行打开EasyControl3的操作
        /// </summary>
        private void StartExe()
        {
            try
            {
                //获取易课exe的路径
                string ekUrl = ConfigurationManager.AppSettings["EKURL"];
                if (ekUrl != string.Empty && (File.Exists(ekUrl)))
                {
                    Process.Start(ekUrl);
                }
            }
            catch (Exception e)
            {
                new Helper().InsertErrorMsg(e.Message, "打开易课错误信息");
            }
        }
        #endregion
        #region 线程执行js注册 修改配置文件等功能
        /// <summary>
        /// 线程执行js注册 修改配置文件等功能
        /// </summary>
        private void OpterOtherFun()
        {
            System.Threading.Thread tother = new System.Threading.Thread(StartOther);
            tother.Start();
        }

        private void StartOther()
        {
            try
            {
                ///修改配置文件
                UpdDownLoadFileURL();
            }
            catch (Exception e)
            {
                new Helper().InsertErrorMsg(e.Message, "程序加载错误");
            }
        }
        #endregion
        #region PIPE
        private void openPIPE()
        {
            System.Threading.Thread tp = new System.Threading.Thread(StartPIPE);
            tp.IsBackground = true;
            tp.Start();
        }
        private void StartPIPE()
        {
            while (true)
            {
                try
                {
                    //打开pipe管道，当易课程序启动时通过管道控制页面跳转
                    System.IO.Pipes.NamedPipeServerStream _pipeServer =
                            new System.IO.Pipes.NamedPipeServerStream("KSEXE", System.IO.Pipes.PipeDirection.InOut, 2);
                    _pipeServer.WaitForConnection();
                    StreamReader sr = new StreamReader(_pipeServer);
                    string recData = sr.ReadLine();
                    if (recData != string.Empty)
                    {
                        //MessageBox.Show("管道接收到的数据\n" + recData);
                        PIPEMod pm = new Helper().Deserialize<PIPEMod>(recData);
                        if (pm != null)
                        {
                            if (pm.FunName == "Load")
                            {
                                strArgs = new string[1];
                                strArgs[0] = pm.FunData;//{"FunData":"{\"account\":\"mzw\",\"password\":\"CEDDDE06F9915A27\",\"webUrl\":\"http:\\/\\/192.168.3.187:6012\\/api\\/\",\"apiName\":\"Students\\/\",\"classID\":\"-1\",\"param\":\"AttLesson\\/Page\\/Teaching.aspx?BookID=707&UserID=49a92e48-1f39-4382-9273-0a0d521162c6&EditionID=18&SubjectID=3&GradeID=5&ClassID=-1&BookType=1&page=IndexPage&DXTP=true\",\"ClassName\":\"公共班级\",\"gradeName\":\"公共年级\",\"clsName\":\"公共班级\"}","FunName":"Load"}

                                wb.Load(ConfigurationManager.AppSettings["DefaultURL"] + "/DefaultW.aspx");
                            }
                            //从易课+客户端回调，返回学生ID，资源ID，题目下发编号，改变学生显示状态（点亮）
                            if (pm.FunName == "ResData")
                            {
                                wb.ExecuteScriptAsync("dianliangStu('" + pm.FunData + "');");
                            }
                        }
                    }
                    sr.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        #endregion
        #region  跳转页面，修改配置，全屏        
        /// <summary>
        /// 修改下载文件存放路径
        /// </summary>
        private void UpdDownLoadFileURL()
        {
            try
            {
                string suser = System.Environment.UserName;
                string FileURL = ConfigurationManager.AppSettings["DownLoadFileURL"]; //文件下载地址
                                                                                      //修改临时文件下载路径
                if (!Directory.Exists(FileURL))
                {
                    string tempPath = FileURL;
                    //盘符判断
                    List<string> dirLs = System.IO.Directory.GetLogicalDrives().ToList();
                    if (!dirLs.Contains(FileURL.Substring(0, 3).ToLower()) && !dirLs.Contains(FileURL.Substring(0, 3).ToUpper()))
                    {
                        FileURL = dirLs[dirLs.Count > 2 ? 1 : 0] + FileURL.Substring(3);
                    }

                    if (FileURL.Contains("ReplaceString"))
                    {
                        if (FileURL != "" && FileURL != null)
                        {
                            tempPath = FileURL.Replace("ReplaceString", suser);
                            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                            cfa.AppSettings.Settings["DownLoadFileURL"].Value = tempPath;
                            cfa.Save();
                            ConfigurationManager.RefreshSection("appSettings");// 刷新命名节;

                            JsEvent.DownLoadFileURL = tempPath;
                        }
                    }
                    if (!Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(FileURL);
                    }
                }
            }
            catch (Exception e)
            {
                new Helper().InsertErrorMsg(e.Message, "修改文件下载目录失败");
            }
        }
        private void GoToWeb()
        {
            try
            {
                #region 加载代码块 
                ///判断网络连接状态
                //System.Int32 dwFlag = new Int32();
                //if (!InternetGetConnectedState(ref dwFlag, 0))
                //{
                //    MessageBox.Show("网络未连接");
                //    Application.Exit();
                //}
                //全屏
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle; //不显示边框
                this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                this.Hide(); //先调用其隐藏方法 然后再显示出来,这样就会全屏,且任务栏不会出现.如果不加这句 可能会出现假全屏即任务栏还在下面.
                this.Show();
                Control.CheckForIllegalCrossThreadCalls = false;
                //获取文件的物理路径
                string path = AppDomain.CurrentDomain.BaseDirectory + DEFAULT_URL;
                //if (IfFile == "true")//如果是本地的，使用file协议
                //{
                //转换为File协议路径
                DEFAULT_URL = "file://" + path.Replace("\\", "/");

                //}
                if (strArgs != null && strArgs.Length > 0 && strArgs[0].Length > 2)
                {
                    #region 读取注册表
                    RegistryKey key;
                    key = Registry.CurrentUser;
                    RegistryKey s = key.OpenSubKey("SOFTWARE\\ClovSoft\\EasyControl3\\KSEXE");
                    string tempstr = s.GetValue("PARAM").ToString();
                    s.Close();
                    #endregion
                    PIPEMod pm = new Helper().Deserialize<PIPEMod>(tempstr);
                    if (pm != null)
                    {
                        strArgs = new string[1];
                        strArgs[0] = pm.FunData;
                        Param Param= new Helper().Deserialize<Param>(strArgs[0]);
                        new Helper().InsertErrorMsg(Param.param, "Param");
                        DEFAULT_URL += "?" + Param.param;
                        new Helper().InsertErrorMsg(strArgs[0], "安卓参数");
                    }
                    //DEFAULT_URL = DEFAULT_URL + "/DefaultW.aspx";
                }
                else
                {
                    DEFAULT_URL += "?" + "BookID=266&UserID=9&EditionID=3&SubjectID=2&GradeID=5&ClassID=42&BookType=1&BookName=SZ4B";
                }
                new Helper().InsertErrorMsg(DEFAULT_URL, "dizhi");
                wb = new ChromiumWebBrowser(DEFAULT_URL);
                ///////////禁用右键///////////
                wb.MenuHandler = new MenuHandler();
                wb.Dock = DockStyle.Fill;
                this.Controls.Add(wb);
                //wb.ShowDevTools();
                if (strArgs != null && strArgs.Length > 0 && strArgs[0].Length > 2)
                {
                    CallBackFullScreen1();
                }
                OpterOtherFun();
                /////////////注册类 供js调用wimform函数///////////////
                wb.RegisterJsObject("callHostFunction", new JsEvent(wb, this));

                if (openEkFlag == 1)
                {
                    //OpenEasyControl3();
                }
                #endregion
                //通信
                //openPIPE();
            }
            catch (Exception eload)
            {
                new Helper().InsertErrorMsg(eload.Message, "程序加载错误");
            }
        }
        public void CallBackFullScreen()
        {
            //窗口全屏 / 还原操作
            if (this.FormBorderStyle == FormBorderStyle.None)//如果当前的窗体是最大化            
            {
                this.MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
                this.FormBorderStyle = FormBorderStyle.Sizable;//可调整大小的边框 
                //wb.Dock = System.Windows.Forms.DockStyle.Fill;
                this.WindowState = FormWindowState.Maximized;//把当前窗体还原默认大小 
                this.Hide(); //先调用其隐藏方法 然后再显示出来,这样就会全屏,且任务栏不会出现.如果不加这句 可能会出现假全屏即任务栏还在下面.
                this.Show();
            }
            else
            {
                this.MaximumSize = Screen.PrimaryScreen.Bounds.Size;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;//将该窗体的边框设置为无,也就是没有标题栏以及窗口边框的
                //wb.Dock = System.Windows.Forms.DockStyle.Right;
                this.WindowState = FormWindowState.Maximized;//将该窗体设置为最大化 
                this.Hide(); //先调用其隐藏方法 然后再显示出来,这样就会全屏,且任务栏不会出现.如果不加这句 可能会出现假全屏即任务栏还在下面.
                this.Show();
            }
        }
        public void CallBackFullScreen1()
        {
            if (this.FormBorderStyle != FormBorderStyle.FixedSingle)//如果当前的窗体是最大化            
            {
                this.MaximumSize = Screen.PrimaryScreen.Bounds.Size;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;//将该窗体的边框设置为无,也就是没有标题栏以及窗口边框的
                                                            //wb.Dock = System.Windows.Forms.DockStyle.Right;
                this.WindowState = FormWindowState.Maximized;//将该窗体设置为最大化 
                this.Hide(); //先调用其隐藏方法 然后再显示出来,这样就会全屏,且任务栏不会出现.如果不加这句 可能会出现假全屏即任务栏还在下面.
                this.Show();
            }
        }
        #endregion
        #region   录音，评测
        private int callback(IntPtr usrdata, string record_id, int type, byte[] message, int size)
        {
            if (stop == "is")
            {
                return 0;
            }
            int score = 0;
            if (type == AIENGINE_MESSAGE_TYPE_JSON)
            {
                IndexForm aiEngine = (IndexForm)GCHandle.FromIntPtr(usrdata).Target;
                aiEngine.result = Encoding.UTF8.GetString(message);
                string mess = Encoding.UTF8.GetString(message);
                message messg = JsEvent.DecodeJson<message>(mess);
                if (messg.result != null)
                {
                    score = int.Parse(messg.result.overall);
                }
            }
            try
            {
                if (JsEvent.parameter != null && JsEvent.parameter.zimu != null && JsEvent.parameter.zimu != "")
                {
                    if (JsEvent.cbId != "-1")
                    {
                        mesage msag = new mesage();
                        msag.code = 0;
                        msag.msg = "";
                        msag.data = new data();
                        msag.data.score = score;
                        string res = JsEvent.EnSerialize<mesage>(msag);
                        JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + JsEvent.cbId + "','" + res + "');");
                    }
                    else
                    {
                        JsEvent.wb.ExecuteScriptAsync("cpgetScore('" + score + "');");
                    }
                }
            }
            catch (Exception ee)
            {
                new Helper().InsertErrorMsg("窗体关闭" + ee.StackTrace, ee.Message);
                throw;
            }
            return 0;
        }
        public void startPinCe()
        {
            startRun();
            myMsgQueue.StartThread();
        }
        public void stopPinCe()
        {
            try
            {
                stop = "";
                wi.StopRecording();
                wi.Dispose();
                wi = null;
                writer.Close();
                writer.Dispose();
                writer = null;
                ssound_stop(m_engine);
            }
            catch (Exception ee)
            {
                new Helper().InsertErrorMsg("停止录音录音" + ee.StackTrace, ee.Message);
                throw;
            }
            LameWavToMp3(@"data\" + LuYinPath + ".wav", @"data\" + LuYinPath + ".mp3");//将wav转为mp3并上传到智慧校园文件服务器
        }
        string stop = "";
        public void RecordStop()
        {
            stop = "is";
            wi.StopRecording();
            wi.Dispose();
            wi = null;
            writer.Close();
            writer.Dispose();
            writer = null;
            if (JsEvent.parameter != null && JsEvent.parameter.zimu != null && JsEvent.parameter.zimu != "")
            {
                ssound_stop(m_engine);
            }
        }
        public void startRun()
        {
            myMsgQueue = new CustomMessageQueue();
            if (JsEvent.parameter != null)
            {
                setParams(userId, JsEvent.parameter.type, sampleRate, audioUrl);
            }
            else
            {
                setParams(userId, coreType, sampleRate, audioUrl);
            }
            run();//打开评测引擎
            Start();//初始化录音流
        }
        private void Start()
        {
            try
            {
                wi = new WaveInStream(0, new WaveFormat(16000, 16, 1), this);
                wi.DataAvailable += new EventHandler<WaveInEventArgs>(wi_DataAvailable);
                String data = DateTime.Now.ToString();
                String newdata = data.Replace("/", "").Replace(":", "").Replace(" ", "");
                Directory.Delete("./data", true);
                Directory.CreateDirectory("./data");
                wavePath = "./data/" + newdata + ".wav";
                LuYinPath = newdata;
                writer = new WaveFileWriter(wavePath, wi.WaveFormat);
                wi.StartRecording();
            }
            catch (Exception ee)
            {
                new Helper().InsertErrorMsg(ee.Message, "初始化录音流异常");
                throw;
            }
        }
        void wi_DataAvailable(object sender, WaveInEventArgs e)
        {
            writer.WriteData(e.Buffer, 0, e.BytesRecorded);
            ssound_feed(m_engine, e.Buffer, e.Buffer.Length);
        }
        public void run()
        {
            if (JsEvent.parameter.zimu == null || JsEvent.parameter.zimu == "")
            {
                reftext = "hi";
            }
            else
                reftext = JsEvent.parameter.zimu;
            string param = "";
            if (coreType == "en.word.score" || coreType == "en.sent.score" || coreType == "en.pred.score" || coreType == "cn.word.score" || coreType == "cn.sent.score")
            {
                param = "{\"coreProvideType\": \"" + coreProvideType + "\","
                            + " \"app\": {\"userId\": \"" + userId + "\"},"
                            + " \"audio\": {\"audioType\": \"" + audioType + "\","
                            + "\"sampleRate\": " + sampleRate + ","
                            + "\"channel\": 1,"
                            + "\"sampleBytes\": 2},"
                            + " \"request\": {\"coreType\": \"" + coreType + "\","
                            + " \"attachAudioUrl\": 1,"
                            + " \"rank\": 100,"
                            + " \"precision\":1,"
                            + " \"refText\":\"" + reftext + "\"}}";
            }
            else
            {
                param = "{\"coreProvideType\": \"cloud\","
                            + " \"app\": {\"userId\": \"" + userId + "\"},"
                            + " \"audio\": {\"audioType\": \"" + audioType + "\","
                            + "\"sampleRate\": " + sampleRate + ","
                            + "\"channel\": 1,"
                            + "\"sampleBytes\": 2},"
                            + " \"request\": " + reftext + "}";
            }

            int rv;
            byte[] record_id = new byte[64];
            byte[] device_id = new byte[64];
            byte[] version = new byte[64];
            byte[] buf = new byte[4096];
            ssound_opt(IntPtr.Zero, AIENGINE_OPT_GET_VERSION, version, version.Length);
            rv = ssound_start(m_engine, param, record_id, _callback, GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Normal)));
            if (rv != 0)
            {
                new Helper().InsertErrorMsg("rv初始化ssound_start失败", "run方法");
                return;
            }
        }
        public void initEngineOnce()
        {
            string cfg = "{\"appKey\":\"" + appKey + "\",\"secretKey\": \"" + secretKey + "\", \"cloud\": {\"enable\":1, \"server\":\"ws://api.cloud.ssapi.cn:8080\"},\"native\": {\"en.word.score\": {\"res\": \"resource/eval/bin/eng.wrd.pydnn.16bit\"},\"en.sent.score\": {\"res\": \"resource/eval/bin/eng.snt.pydnn.16bit\"},\"en.pred.score\": {\"res\": \"resource/eval/bin/eng.pred.pydnn.16bit\"}}}";
            m_engine = ssound_new(cfg);
        }
        //设置传入参数
        public void setParams(string _userId, string _coreType, int _sampleRate, string _audioUrl)
        {
            userId = _userId;
            coreType = _coreType;
            sampleRate = _sampleRate;
            audioUrl = _audioUrl;
        }
        private static void LameWavToMp3(string wavFile, string outmp3File)
        {
            string path = @"data\" + LuYinPath + ".mp3";
            try
            {
                psi.FileName = @"Lame\lame.exe";
                psi.Arguments = "-V2 " + wavFile + " " + outmp3File;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process p = Process.Start(psi);
                p.WaitForExit();
                p.Dispose();
                p.Close();
            }
            catch (Exception ee)
            {
                new Helper().InsertErrorMsg("wav转mp3" + ee.StackTrace, ee.Message);
                throw;
            }
            CheckPath(path);
        }
        public static void CheckPath(string path)
        {
            if (File.Exists(path))
            {
                UploadLuYin(path);
            }
            else
            {
                Thread.Sleep(1000);
                CheckPath(path);
            }
        }
        public static void UploadLuYin(string path)
        {
            try
            {
                string url = "";
                jsonFile1.FileID = ResID;
                if (JsEvent.parameter != null && JsEvent.parameter.zimu != null && JsEvent.parameter.zimu != "")
                {
                    jsonFile1.FileID = ResID + JsEvent.parameter.wordIndex;
                }
                jsonFile1.UserName = thearID;
                jsonFile1.ResourceStyle = 101;
                string JsonFile = JsEvent.EnSerialize<JsonFile>(jsonFile1);
                byte[] bt = client.UploadFile(UploadUrl + "?JsonFile=" + JsonFile, path);
                string temp = System.Text.Encoding.UTF8.GetString(bt);
                List<string> ls = temp.Split(',').ToList();
                if (ls != null && ls.Count == 8)
                {
                    List<string> ls2 = ls[6].Split(',').ToList();
                    url = ls2[0].Replace("\"FilePath\":\"", "").Replace("\"}", "");
                    string strGUID = System.Guid.NewGuid().ToString(); //直接返回字符串类型
                    url += "?ran=" + strGUID;
                    if (JsEvent.cbId != "-1")
                    {
                        mesage msag = new mesage();
                        msag.code = 0;
                        msag.msg = "";
                        msag.data = new data();
                        msag.data.recordFileUrl = url;
                        string res = JsEvent.EnSerialize<mesage>(msag);
                        bool s = JsEvent.wb.Enabled;
                        JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + JsEvent.cbId + "','" + res + "');");
                    }
                    else
                    {
                        JsEvent.wb.ExecuteScriptAsync("cpGetLuyinPath('" + url + "');");
                    }
                }
                else
                {
                    if (JsEvent.cbId != "-1")
                    {
                        JsEvent.wb.ExecuteScriptAsync("sClassJSBridge.handleMessage('" + JsEvent.cbId + "','" + "上传失败" + "');");
                    }
                    else
                    {
                        JsEvent.wb.ExecuteScriptAsync("cpGetLuyinPath('上传失败');");
                    }
                }
            }
            catch (Exception ex)
            {
                new Helper().InsertErrorMsg(ex.StackTrace, ex.Message);
                throw;
            }
        }
        #endregion
        #region Socket
        public void socket()
        {
            ip = getIPAddress();
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, ProtocolType.Udp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ip), portStr));  //绑定IP地址：端口
            serverSocket.Listen(0);    //设定最多10个排队连接请求        
            //通过Clientsoket发送数据
            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
        }
        /// <summary>
        /// socket等待链接
        /// </summary>
        private static void ListenClientConnect()
        {
            while (true)
            {
                if (serverSocket != null)
                {
                    try
                    {
                        string time = DateTime.Now.ToString();
                        Socket clientSocket = serverSocket.Accept();
                        new Helper().InsertErrorMsg(""+ clientSocket.RemoteEndPoint, "安卓连接socket");
                        socketList.Add(clientSocket);
                        cSocket = clientSocket;
                        string Url = clientSocket.RemoteEndPoint.ToString();
                        clientIp = Regex.Split(Url, ":")[0];
                        recive(clientSocket);
                        //Thread createThread = new Thread(recive);
                        //createThread.Start(clientSocket);
                    }
                    catch (Exception ex)
                    {
                        new Helper().InsertErrorMsg(ex.Message, "socket异常");
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// socket连接后监听消息
        /// </summary>
        /// <param name="clientSocket"></param>
        public static void recive(object clientSocket)
        {
            while (true)
            {
                string msg = TransferFiles.receiveFileMsg((Socket)clientSocket);
                //new Helper().InsertErrorMsg(msg, "recive收到安卓消息");
                if (msg.Contains("0X11"))
                {
                    Create(clientSocket, msg);
                }
                else if (msg.Contains("0X12"))
                {
                    Message(clientSocket, msg);
                }
                else if (msg.Contains("0X13"))
                {
                    Application.Exit();
                }
            }
        }
        public static void CloseSocket()
        {
            if (socketList.Count > 0)
            {
                foreach (var s in socketList)
                {
                    s.Close();
                }
            }
        }
        public static void Message(object clientSocket, string msg)
        {
            try
            {
                new Helper().InsertErrorMsg(msg, "收到返回消息");
                Socket client = clientSocket as Socket;
                msg = Regex.Split(msg, "0X12:")[1];
                long bagSize = Convert.ToInt64(msg);
                byte[] data = TransferFiles.ReceiveMsg(client, bagSize);
                string datamsg = Encoding.UTF8.GetString(data, 0, data.Length);
                JsEvent.SendMsg(datamsg);
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
        /// 接收安卓文件
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="msg"></param>
        public static void Create(object clientSocket, string msg)
        {
            //FileStream MyFileStream = null;
            try
            {
                new Helper().InsertErrorMsg(msg, "收到安卓文件");
                Socket client = clientSocket as Socket;
                msg = Regex.Split(msg, "0X11:")[1];
                string[] fileinfo = Regex.Split(msg, "&");//文件地址&
                string pp = "Page" + "\\" + fileinfo[0];
                string fullPath = Path.Combine(Environment.CurrentDirectory, pp);
                //string filename = System.IO.Path.GetFileName(fullPath);
                FileInfo fileInfo = new FileInfo(fullPath);
                if (File.Exists(fullPath)&&!fullPath.ToLower().Contains(".json")&& fileInfo.Length!=0)
                {
                    Message message = new Message() { cbId = "sendFileStatus_n", data = fileinfo[0], Status = true };
                    string strmessag = JsEvent.EnSerialize<Message>(message);
                    //Thread createThread = new Thread(getJson);
                    //createThread.Start(strmessag);
                    getJson(strmessag);
                    return;
                }
                else
                {
                    Message message = new Message() { cbId = "sendFileStatus_n", data = fileinfo[0], Status = false };
                    string strmessag = JsEvent.EnSerialize<Message>(message);
                    //Thread createThread = new Thread(getJson);
                    //createThread.Start(strmessag);
                    getJson(strmessag);
                }             
                long bagSize = Convert.ToInt64(fileinfo[1]);
                TransferFiles.ReceiveData(client, bagSize, fullPath);
                IsTime = true;
            }
            catch (Exception ex)
            {
                new Helper().InsertErrorMsg(ex.Message+"||"+ex.StackTrace, "create收文件异常");
                return;
            }
            //关闭文件流   
            //MyFileStream.Close();
            //关闭套接字   
            //client.Close();
        }
        public static void getJson(object msg)
        {
            Socket s = TransferFiles.Connect(clientIp, ClientPoint);
            try
            {
                new Helper().InsertErrorMsg(s.RemoteEndPoint+"||"+msg.ToString(), "向安卓发消息");
                TransferFiles.sendJsonMsg(s, msg.ToString());
            }
            catch (Exception ex)
            {
                new Helper().InsertErrorMsg(ex.Message, "连接安卓Socket失败");
            }
        }
        /// <summary>
        /// 获取本机IP
        /// </summary>
        /// <returns></returns>
        private static string getIPAddress()
        {
            // 获得本机局域网IP地址  
            IPAddress[] AddressList = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            if (AddressList.Length < 1)
            {
                return "";
            }
            return AddressList[0].ToString();
        }
        #endregion
        #region  下载资源
        public static void getResource()
        {
            string webapi_url = "http://192.168.3.2:8029/";
            HttpClient myHttpClient = new HttpClient();
            myHttpClient.BaseAddress = new Uri(webapi_url); //webapi_url
            HttpResponseMessage httpResponseMessage = myHttpClient.GetAsync("GetFiles.ashx?FileID=49c5beff-a9e6-4bec-8290-fcdcd309de7e").Result;
            //创建一个新文件   
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                byte[] data = httpResponseMessage.Content.ReadAsByteArrayAsync().Result;
                string fullPath = Path.Combine(Environment.CurrentDirectory, httpResponseMessage.Content.Headers.ContentDisposition.FileName.Replace("%20", " "));
                // 把 byte[] 写入文件   
                FileStream fs = new FileStream(fullPath, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(data);
                bw.Close();
                fs.Close();
            }
            else
            {

            }
        }
        //"fileURL":下载链接
        //"filePath":文件目录
        //"isDownLoad":是否可下载的文件，如果不能下载就拿到数据写本地文件
        //"MD5":用于文件比对
        //"other":预留字段

        public static void getBook()
        {
            string webapi_url = "http://192.168.3.187:1777/";
            HttpClient myHttpClient = new HttpClient();
            myHttpClient.BaseAddress = new Uri(webapi_url); //webapi_url
            HttpResponseMessage httpResponseMessage = myHttpClient.GetAsync("BookHandler.ashx?Book=RJYW4A").Result;
            //创建一个新文件   
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                byte[] data = httpResponseMessage.Content.ReadAsByteArrayAsync().Result;
                string fullPath = Path.Combine(Environment.CurrentDirectory + "\\Page\\data", httpResponseMessage.Content.Headers.ContentDisposition.FileName.Replace("%20", " "));
                // 把 byte[] 写入文件   
                FileStream fs = new FileStream(fullPath, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(data);
                bw.Close();
                fs.Close();
                string bookPath = Regex.Split(fullPath, ".zip")[0] + "\\ebook";
                Helper.UnZipFile(fullPath, bookPath);
            }
            else
            {

            }
        }
        public static void getRes()
        {
            string webapi_url = "http://192.168.3.187:6012/";
            HttpClient myHttpClient = new HttpClient();
            myHttpClient.BaseAddress = new Uri(webapi_url); //webapi_url
            HttpResponseMessage httpResponseMessage = myHttpClient.GetAsync("api/GetSchRes/123").Result;
            //创建一个新文件   
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                //getBook();
                byte[] data = httpResponseMessage.Content.ReadAsByteArrayAsync().Result;
                var fnm = httpResponseMessage.Content.Headers.ContentDisposition.FileName.Replace("%20", " ");
                string fullPath = Path.Combine(Environment.CurrentDirectory + "\\Page\\data", fnm);
                // 把 byte[] 写入文件   
                FileStream fs = new FileStream(fullPath, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(data);
                bw.Close();
                fs.Close();
                if (fullPath.Contains(".zip"))
                {
                    string bookPath = Regex.Split(fullPath, ".zip")[0];
                    Helper.UnZipFile(fullPath, bookPath);
                }
            }
            else
            {

            }
        }
        #endregion
        private void btn_Debug_Click(object sender, EventArgs e)
        {
            wb.ShowDevTools();
        }
    }
    #region 自定义消息队列线程
    public struct CustomMessage
    {
        public int Message;
        public string param;
    }

    public class CustomMessageQueue
    {
        private System.Threading.Thread th;
        public CustomMessage Msg = new CustomMessage();
        public delegate bool PerTranslateMessageHandler(ref CustomMessage m);
        public PerTranslateMessageHandler PerTranslateMessage;
        private void ThreadProc()
        {
            while (Msg.Message != -1) //enum -1 for exit thread
            {
                if (Msg.Message != 0)
                {
                    if (PerTranslateMessage != null)
                    {
                        if (PerTranslateMessage.Invoke(ref Msg))
                        {
                            Msg.Message = 0; //Set message to unused
                            System.Threading.Monitor.Enter(this);
                            System.Threading.Monitor.Wait(this);
                            System.Threading.Monitor.Exit(this);
                            continue;
                        }
                    }
                    // DefaultMessageTranslate();
                }
                System.Threading.Monitor.Enter(this);
                System.Threading.Monitor.Wait(this);
                System.Threading.Monitor.Exit(this);
            }
        }
        public CustomMessageQueue()
        {
            th = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadProc));
            PerTranslateMessage = null;
        }
        public void StartThread()
        {
            try
            {
                th.Start();
            }
            catch
            {
                int nLayer = GC.GetGeneration(th);
                GC.Collect(nLayer);
                th = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadProc));
                th.Start();
            }
        }
    }
    /// <summary>
    /// 从安卓获取json消息类
    /// </summary>
    public class Message
    {
        public string cbId { get; set; }
        public string data { get; set; }
        public bool Status { get; set; }
        
    }
    public class Param
    {
        public string param { get; set; }
    }
    #endregion
}
