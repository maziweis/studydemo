using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace SmarterClassroomWindows
{
    /// <summary>
    /// 公共帮助类
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="msgType"></param>
        public void InsertErrorMsg(string msg, string msgType)
        {
            try
            {
                string strFullPath = Directory.GetCurrentDirectory();
                string filePath = strFullPath + "\\error.txt";

                //追加打开文件 ，写入内容
                FileStream fs = new FileStream(filePath, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("[" + msgType + "]" + msg.ToString());
                sw.Dispose();
                sw.Close();
                fs.Close();
            }
            catch
            {

            }
        }

        /// <summary>
        /// 解压缩文件
        /// </summary>
        /// <param name="ZipFileName">压缩文件</param>
        /// <param name="TargetFolder">目标文件夹</param>
        public static void UnZipFile(string ZipFileName, string TargetFolder)
        {
            if (ZipFileName.ToLower().Contains("rar"))
            {
                if (!Directory.Exists(TargetFolder))
                {
                    Directory.CreateDirectory(TargetFolder);
                }
                using (Stream stream = File.OpenRead(ZipFileName))
                {
                    var reader = ReaderFactory.Open(stream);
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            reader.WriteEntryToDirectory(TargetFolder);
                        }
                    }
                }
            }
            else
            {
                new Helper().InsertErrorMsg(ZipFileName, "解压ppt文件");
                if (!File.Exists(ZipFileName))
                    return;
                FileStream FileS = File.OpenRead(ZipFileName);
                ZipInputStream zipInStream = new ZipInputStream(FileS);
                string FolderPath = Path.GetDirectoryName(TargetFolder);
                if (!Directory.Exists(TargetFolder))
                {
                    Directory.CreateDirectory(FolderPath);
                }
                ZipEntry tEntry;
                while ((tEntry = zipInStream.GetNextEntry()) != null)
                {
                    string fileName = Path.GetFileName(tEntry.Name);
                    string tempPath = TargetFolder + "\\" + Path.GetDirectoryName(tEntry.Name);
                    if (!Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(tempPath);
                    }
                    if (fileName != null && fileName.Length > 0)
                    {
                        //string strEn=tEntry.Name.Replace("/","");
                        FileStream streamWriter = File.Create(TargetFolder + "\\" + tEntry.Name);
                        byte[] data = new byte[2048];
                        System.Int32 size;
                        try
                        {
                            do
                            {
                                size = zipInStream.Read(data, 0, data.Length);
                                streamWriter.Write(data, 0, size);
                            } while (size > 0);
                        }
                        catch (System.Exception ex)
                        {
                            throw ex;
                        }
                        streamWriter.Close();
                    }
                }
                zipInStream.Close();
            }
        }
        public bool DeCompressRAR(string sourceFilePath, string destinationPath)
        {
            try
            {
                string SeverDir = @"D:\工具\winrar";//rar.exe的要目录 
                //Process ProcessDecompression = new Process();
                //ProcessDecompression.StartInfo.FileName = SeverDir + "\\rar.exe";
                string FolderPath = Path.GetDirectoryName(destinationPath);
                //if (!Directory.Exists(destinationPath))
                //{
                //    Directory.CreateDirectory(destinationPath);
                //}
                Directory.CreateDirectory(sourceFilePath);
                string cmd = string.Format("x {0} {1} -y",
                               destinationPath,
                               sourceFilePath);
                //ProcessDecompression.StartInfo.Arguments = cmd;
                //ProcessDecompression.StartInfo.Arguments = " X " + sourceFilePath + " " + destinationPath;
                ProcessStartInfo startinfo = new ProcessStartInfo();
                Process process = new Process();
                //string rarName = "1.rar"; //将要解压缩的 .rar 文件名（包括后缀）  
                //string path = @"C:\images1"; //文件解压路径（绝对）  
                //string rarPath = @"D:\zip";  //将要解压缩的 .rar 文件的存放目录（绝对路径）  
                //string rarexe = @"c:\Program Files\WinRAR\WinRAR.exe";  //WinRAR安装位置  
                //解压缩命令，相当于在要压缩文件(rarName)上点右键->WinRAR->解压到当前文件夹  
                //string cmd = string.Format("x {0} {1} -y", rarName,path);
                startinfo.FileName = SeverDir + "\\rar.exe";
                startinfo.Arguments = cmd;                          //设置命令参数  
                startinfo.WindowStyle = ProcessWindowStyle.Hidden;  //隐藏 WinRAR 窗口  
                startinfo.WorkingDirectory = FolderPath;
                process.StartInfo = startinfo;
                process.Start();
                //ProcessDecompression.Start();
                //while (!ProcessDecompression.HasExited)
                //{
                //    //nothing to do here. 
                //}
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        /// <summary>  
        /// 解压  
        /// </summary>  
        /// <param name="unRarPatch">解压后文件存放路径</param>  
        /// <param name="rarPatch">rar或zip路径</param>  
        /// <param name="rarName">rar或zip文件名称</param>  
        /// <returns></returns>  
        public string unCompressRAR(string unRarPatch, string rarPatch, string rarName)
        {
            string the_rar;
            RegistryKey the_Reg;
            object the_Obj;
            string the_Info;
            try
            {
                the_Reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinRAR.exe");
                the_Obj = the_Reg.GetValue("");
                the_rar = the_Obj.ToString();
                the_Reg.Close();
                //the_rar = the_rar.Substring(1, the_rar.Length - 7);  

                if (Directory.Exists(unRarPatch) == false)
                    Directory.CreateDirectory(unRarPatch);

                the_Info = "x " + rarName + " " + unRarPatch + " -y";

                ProcessStartInfo the_StartInfo = new ProcessStartInfo();
                the_StartInfo.FileName = the_rar;
                the_StartInfo.Arguments = the_Info;
                the_StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                the_StartInfo.WorkingDirectory = rarPatch;//获取压缩包路径  
                the_StartInfo.UseShellExecute = false;
                Process the_Process = new Process();
                the_Process.StartInfo = the_StartInfo;

                the_Process.Start();
                the_Process.WaitForExit();
                the_Process.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return unRarPatch;
        }
        //public string unRAR(string unRarPatch, string rarPatch, string rarName)
        //    {
        //        String the_rar;
        //        RegistryKey the_Reg;
        //        Object the_Obj;
        //        String the_Info;
        //        ProcessStartInfo the_StartInfo;
        //        Process the_Process;
        //        try
        //        {
        //            the_Reg = Registry.ClassesRoot.OpenSubKey(@"ApplicationsWinRAR.exeShellOpenCommand");
        //            the_Obj = the_Reg.GetValue("");
        //            the_rar = the_Obj.ToString();
        //            the_Reg.Close();
        //            the_rar = the_rar.Substring(1, the_rar.Length - 7);
        //            Directory.CreateDirectory(Server.MapPath(unRarPatch));
        //            the_Info = "e   " + rarName + "  " + Server.MapPath(unRarPatch) + " -y";
        //            the_StartInfo = new ProcessStartInfo();
        //            the_StartInfo.FileName = the_rar;
        //            the_StartInfo.Arguments = the_Info;
        //            the_StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        //            the_StartInfo.WorkingDirectory = Server.MapPath(rarPatch);//获取压缩包路径  
        //            the_Process = new Process();
        //            the_Process.StartInfo = the_StartInfo;
        //            the_Process.Start();
        //            the_Process.WaitForExit();
        //            the_Process.Close();
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }
        //        return Server.MapPath(unRarPatch);
        //    }
        /// <summary>  
        /// //解压  必须装有WinRar  
        /// </summary>  
        /// <param name="rarfilepath"></param> //压缩文件路劲  
        /// <param name="filepath"></param>   //解压保存路径  
        //protected string uncompress(string rarfilepath, string filepath)
        //{
        //    try
        //    {
        //        string rar;
        //        RegistryKey reg;
        //        string args;
        //        ProcessStartInfo startInfo;
        //        Process process;
        //        reg = Registry.ClassesRoot.OpenSubKeypplications/WinRar.exe/Shell/Open/Command");//WinRar位置  
        //        rar = reg.GetValue("").ToString();
        //        reg.Close();
        //        rar = rar.Substring(1, rar.Length - 7);
        //        args = " X " + rarfilepath + " " + filepath;
        //        startInfo = new ProcessStartInfo();
        //        startInfo.FileName = rar;
        //        startInfo.Arguments = args;
        //        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        //        process = new Process();
        //        process.StartInfo = startInfo;
        //        process.Start();
        //        return "success";
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.ToString();
        //    }
        //}
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
        /// <summary>
        /// 数据序列化成Json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public string EnSerialize<T>(T data)
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
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// base64ToString
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetStringByBase(string data)
        {
            if (data != string.Empty)
            {
                char[] charBuffer = data.ToCharArray();
                byte[] bytes = Convert.FromBase64CharArray(charBuffer, 0, charBuffer.Length);
                string returnstr = Encoding.Default.GetString(bytes);
                return returnstr;
            }
            else
            {
                return "";
            }
        }
    }
    /// <summary>
    /// PIPE通信Mod
    /// </summary>
    public class PIPEMod
    {
        /// <summary>
        /// PIPE方法名
        /// </summary>
        public string FunName { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string FunData { get; set; }

    }
}
