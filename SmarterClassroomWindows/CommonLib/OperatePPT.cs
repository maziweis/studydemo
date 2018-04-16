using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OFFICECORE = Microsoft.Office.Core;
using POWERPOINT = Microsoft.Office.Interop.PowerPoint;
using System.Runtime.InteropServices;
using System;
using Microsoft.Win32;
using System.Threading;

namespace SmarterClassroomWindows
{

    public class OperatePPT
    {
        #region=========基本的参数信息=======
        POWERPOINT.Application objApp = null;
        POWERPOINT.Presentation objPresSet = null;
        POWERPOINT.SlideShowWindows objSSWs;
        POWERPOINT.SlideShowTransition objSST;
        POWERPOINT.SlideShowSettings objSSS;
        POWERPOINT.SlideRange objSldRng;

        bool bAssistantOn;
        double pixperPoint = 0;
        double offsetx = 0;
        double offsety = 0;
        #endregion

        #region===========操作方法==============
        /// <summary>
        /// 打开PPT文档并播放显示。
        /// </summary>
        /// <param name="filePath">PPT文件路径</param>
        public void PPTOpen(string filePath)
        {
            new Helper().InsertErrorMsg(filePath, "打开38ppt文件");
            if (filePath.ToLower().Contains("ppt"))
            {
                //Thread th1 = new Thread(new ParameterizedThreadStart(dakaippt));
                //th1.IsBackground = true;
                //th1.Start(filePath);
                //return;
                IntPtr hwndRecvWindow = IntPtr.Zero;
                try
                {
                    RegistryKey key;
                    key = Registry.CurrentUser;
                    RegistryKey s = key.OpenSubKey("SOFTWARE\\ClovSoft\\EasyControl3\\SET");

                    string sd = s.GetValue("MAINWND").ToString();
                    hwndRecvWindow = (IntPtr)UInt32.Parse(sd);
                    //hwndRecvWindow = Marshal.StringToHGlobalAnsi(sd);
                    //hwndRecvWindow = (IntPtr)s.GetValue("MAINWND");
                    s.Close();
                }
                catch (Exception e)
                {
                    new Helper().InsertErrorMsg(filePath, "打开56ppt文件");
                    Thread th = new Thread(new ParameterizedThreadStart(dakaippt));
                    th.IsBackground = true;
                    th.Start(filePath);
                    return;
                }

                //接收端的窗口句柄  
                //IntPtr hwndRecvWindow = ImportFromDLL.FindWindow(null, strDlgTitle);
                if (hwndRecvWindow == IntPtr.Zero)
                {
                    new Helper().InsertErrorMsg(filePath, "打开67ppt文件");
                    if (this.objApp != null) { return; }
                    string path = @"" + filePath;
                    Thread th = new Thread(new ParameterizedThreadStart(dakaippt));
                    th.IsBackground = true;
                    th.Start(filePath);
                    //Console.WriteLine("请先启动接收消息程序");
                    return;
                }

                //自己的窗口句柄  
                IntPtr hwndSendWindow = IndexForm.Intptrw;
                if (hwndSendWindow == IntPtr.Zero)
                {
                    Console.WriteLine("获取自己的窗口句柄失败，请重试");
                    return;
                }
                //填充COPYDATA结构  
                ImportFromDLL.COPYDATASTRUCT copydata = new ImportFromDLL.COPYDATASTRUCT();
                copydata.cbData = Encoding.Unicode.GetBytes(filePath).Length; //长度 注意不要用strText.Length;  
                copydata.lpData = filePath;                                   //内容  
                copydata.dwData = 1;
                new Helper().InsertErrorMsg(filePath, "打开89ppt文件");
                ImportFromDLL.SendMessage(hwndRecvWindow, ImportFromDLL.WM_COPYDATA, hwndSendWindow, ref copydata);
            }
            else
            {
                new Helper().InsertErrorMsg(filePath, "打开92ppt文件");
                Thread th = new Thread(new ParameterizedThreadStart(dakaippt));
                th.IsBackground = true;
                th.Start(filePath);
                return;
            }
            //Thread.Sleep(1000);
            //防止连续打开多个PPT程序.
            //try
            //{
            //    objApp = new POWERPOINT.Application();
            //    //以非只读方式打开,方便操作结束后保存.
            //    objPresSet = objApp.Presentations.Open(filePath, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse);

            //    //Prevent Office Assistant from displaying alert messages:
            //    bAssistantOn = objApp.Assistant.On;
            //    objApp.Assistant.On = false;

            //    objSSS = this.objPresSet.SlideShowSettings;
            //    objSSS.Run();
            //}
            //catch (Exception ex)
            //{
            //    this.objApp.Quit();
            //}
        }


        private static void dakaippt(object name)
        {
            //System.Diagnostics.Process process = new System.Diagnostics.Process();
            //process.StartInfo.FileName = name.ToString();
            //process.StartInfo.Arguments = "";
            //process.StartInfo.WorkingDirectory = "";
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardInput = true;
            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardError = true;
            //process.StartInfo.ErrorDialog = false;
            //process.StartInfo.CreateNoWindow = true;
            //process.Start();
            Process.Start(name.ToString());
        }
        public class ImportFromDLL
        {
            public const int WM_COPYDATA = 0x004A;

            //启用非托管代码  
            [StructLayout(LayoutKind.Sequential)]
            public struct COPYDATASTRUCT
            {
                public int dwData;    //not used  
                public int cbData;    //长度  
                [MarshalAs(UnmanagedType.LPStr)]
                public string lpData;
            }

            [DllImport("User32.dll")]
            public static extern int SendMessage(
                IntPtr hWnd,     // handle to destination window   
                int Msg,         // message  
                IntPtr wParam,    // first message parameter   
                ref COPYDATASTRUCT pcd // second message parameter   
            );

            [DllImport("User32.dll", EntryPoint = "FindWindow")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("Kernel32.dll", EntryPoint = "GetConsoleWindow")]
            public static extern IntPtr GetConsoleWindow();

        }

        /// <summary>
        /// 自动播放PPT文档.
        /// </summary>
        /// <param name="filePath">PPTy文件路径.</param>
        /// <param name="playTime">翻页的时间间隔.【以秒为单位】</param>
        public void PPTAuto(string filePath, int playTime)
        {
            //防止连续打开多个PPT程序.
            if (this.objApp != null) { return; }

            objApp = new POWERPOINT.Application();
            objPresSet = objApp.Presentations.Open(filePath, OFFICECORE.MsoTriState.msoCTrue, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse);

            // 自动播放的代码（开始）
            int Slides = objPresSet.Slides.Count;
            int[] SlideIdx = new int[Slides];
            for (int i = 0; i < Slides; i++) { SlideIdx[i] = i + 1; };
            objSldRng = objPresSet.Slides.Range(SlideIdx);
            objSST = objSldRng.SlideShowTransition;
            //设置翻页的时间.
            objSST.AdvanceOnTime = OFFICECORE.MsoTriState.msoCTrue;
            objSST.AdvanceTime = playTime;
            //翻页时的特效!
            objSST.EntryEffect = POWERPOINT.PpEntryEffect.ppEffectCircleOut;

            //Prevent Office Assistant from displaying alert messages:
            bAssistantOn = objApp.Assistant.On;
            objApp.Assistant.On = false;

            //Run the Slide show from slides 1 thru 3.
            objSSS = objPresSet.SlideShowSettings;
            objSSS.StartingSlide = 1;
            objSSS.EndingSlide = Slides;
            objSSS.Run();

            //Wait for the slide show to end.
            objSSWs = objApp.SlideShowWindows;
            while (objSSWs.Count >= 1) System.Threading.Thread.Sleep(playTime * 100);

            this.objPresSet.Close();
            this.objApp.Quit();
        }

        /// <summary>
        /// PPT下一页。
        /// </summary>
        public void NextSlide()
        {
            if (this.objApp != null)
                this.objPresSet.SlideShowWindow.View.Next();
        }
        /// <summary>
        /// PPT上一页。
        /// </summary>
        public void PreviousSlide()
        {
            if (this.objApp != null)
                this.objPresSet.SlideShowWindow.View.Previous();
        }

        ///// <summary>
        ///// 对当前的PPT页面进行图片插入操作。
        ///// </summary>
        ///// <param name="alImage">图片对象信息数组</param>
        ///// <param name="offsetx">插入图片距离左边长度</param>
        ///// <param name="pixperPoint">距离比例值</param>
        ///// <returns>是否添加成功！</returns>
        //public bool InsertToSlide(List<PPTOBJ> listObj)
        //{
        //    bool InsertSlide = false;
        //    if (this.objPresSet != null)
        //    {
        //        this.SlideParams();
        //        int slipeint = objPresSet.SlideShowWindow.View.CurrentShowPosition;

        //        foreach (PPTOBJ myobj in listObj)
        //        {
        //            objPresSet.Slides[slipeint].Shapes.AddPicture(
        //                 myobj.Path,           //图片路径
        //                 OFFICECORE.MsoTriState.msoFalse,
        //                 OFFICECORE.MsoTriState.msoTrue,
        //                 (float)((myobj.X - this.offsetx) / this.pixperPoint),       //插入图片距离左边长度
        //                 (float)(myobj.Y / this.pixperPoint),       //插入图片距离顶部高度
        //                 (float)(myobj.Width / this.pixperPoint),   //插入图片的宽度
        //                 (float)(myobj.Height / this.pixperPoint)   //插入图片的高度
        //              );
        //        }
        //        InsertSlide = true;
        //    }
        //    return InsertSlide;
        //}

        ///// <summary>
        ///// 计算InkCanvas画板上的偏移参数，与PPT上显示图片的参数。
        ///// 用于PPT加载图片时使用
        ///// </summary>
        //private void SlideParams()
        //{
        //    double slideWidth = this.objPresSet.PageSetup.SlideWidth;
        //    double slideHeight = this.objPresSet.PageSetup.SlideHeight;
        //    double inkCanWidth = SystemParameters.PrimaryScreenWidth;//inkCan.ActualWidth;
        //    double inkCanHeight = SystemParameters.PrimaryScreenHeight;//inkCan.ActualHeight ;

        //    if ((slideWidth / slideHeight) > (inkCanWidth / inkCanHeight))
        //    {
        //        this.pixperPoint = inkCanHeight / slideHeight;
        //        this.offsetx = 0;
        //        this.offsety = (inkCanHeight - slideHeight * this.pixperPoint) / 2;
        //    }
        //    else
        //    {
        //        this.pixperPoint = inkCanHeight / slideHeight;
        //        this.offsety = 0;
        //        this.offsetx = (inkCanWidth - slideWidth * this.pixperPoint) / 2;
        //    }
        //}
        ///// <summary>
        ///// 关闭PPT文档。
        ///// </summary>
        //public void PPTClose()
        //{
        //    //装备PPT程序。
        //    if (this.objPresSet != null)
        //    {
        //        //判断是否退出程序,可以不使用。
        //        //objSSWs = objApp.SlideShowWindows;
        //        //if (objSSWs.Count >= 1)
        //        //{
        //        if (MessageBox.Show("是否保存修改的笔迹!", "提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
        //            this.objPresSet.Save();
        //        //}
        //        //this.objPresSet.Close();
        //    }
        //    if (this.objApp != null)
        //        this.objApp.Quit();

        //    GC.Collect();
        //}
        #endregion
    }
}