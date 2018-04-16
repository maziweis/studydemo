using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmarterClassroomWindows
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            try
            {
                Process[] allProcess = Process.GetProcesses();
                foreach (Process p in allProcess)
                {
                    if (p.ProcessName == "CefSharp.BrowserSubprocess")
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //if (args.Length == 0)
            //{
            //    IndexForm inFm = new IndexForm(null);
            //    inFm.Activate();
            //    Application.Run(inFm);
            //}
            //else
            //{
            //    IndexForm inFm = new IndexForm(args);
            //    inFm.Activate();
            //    Application.Run(inFm);
            //}
            #region 原主窗体运行块
            bool createdNew;//返回是否赋予了使用线程的互斥体初始所属权 
            System.Threading.Mutex mutex = new System.Threading.Mutex(true, "SmarterClassroomWindows", out createdNew);
            //该程序一次只能运行一个
            if (mutex.WaitOne(0, false))
            {
                if (createdNew) //赋予了线程初始所属权，也就是首次使用互斥体   
                {
                    //MessageBox.Show("第一个实例");
                    if (args.Length == 0)
                    {
                        IndexForm inFm = new IndexForm(null);
                        inFm.Activate();
                        Application.Run(inFm);
                        mutex.ReleaseMutex();
                        //MessageBox.Show("请使用互动课堂打开!");
                        //Application.Exit();
                    }
                    else
                    {
                        try
                        {
                            IndexForm inFm = new IndexForm(args);
                            inFm.Activate();
                            Application.Run(inFm);
                            mutex.ReleaseMutex();
                        }
                        catch (Exception ee)
                        {
                            new Helper().InsertErrorMsg(ee.StackTrace, ee.Message);
                        }
                    }
                }
                else
                {
                    Application.Exit();
                }
            }
            else
            {
                //try
                //{
                //    Process[] allProcess = Process.GetProcesses();
                //    foreach (Process p in allProcess)
                //    {
                //        if (p.ProcessName == "SmarterClassroomWindows")
                //        {
                //            for (int i = 0; i < p.Threads.Count; i++)
                //                p.Threads[i].Dispose();
                //            if (!p.HasExited)
                //                p.Kill();
                //        }
                //    }
                //    if (args.Length == 0)
                //    {
                //        IndexForm inFm = new IndexForm(null);
                //        inFm.Activate();
                //        Application.Run(inFm);
                //        mutex.ReleaseMutex();
                //    }
                //    else
                //    {
                //        IndexForm inFm = new IndexForm(args);
                //        inFm.Activate();
                //        Application.Run(inFm);
                //        mutex.ReleaseMutex();
                //    }
                //}
                //catch (Exception ee)
                //{
                //    new Helper().InsertErrorMsg(ee.StackTrace, ee.Message);
                //}
                MessageBox.Show("已经运行一个实例");
                Application.Exit();
            }
            #endregion
        }
    }
}
