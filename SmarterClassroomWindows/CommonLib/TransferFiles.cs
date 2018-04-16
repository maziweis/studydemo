using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmarterClassroomWindows.CommonLib
{
    public class TransferFiles
    {
        const int BagLength = 1000;        
        public static int SendData(Socket s, byte[] data)
        {
            int total = 0;
            int size = data.Length;
            int dataleft = size;
            int sent;

            while (total < size)
            {
                sent = s.Send(data, total, dataleft, SocketFlags.None);
                total += sent;
                dataleft -= sent;
            }
            return total;
        }        
        public static int SendVarData(Socket s, byte[] data)
        {
            int total = 0;
            int size = data.Length;
            int dataleft = size;
            int sent;
            byte[] datasize = new byte[4];
            try
            {
                datasize = BitConverter.GetBytes(size);
                sent = s.Send(datasize);

                while (total < size)
                {
                    sent = s.Send(data, total, dataleft, SocketFlags.None);
                    total += sent;
                    dataleft -= sent;
                }

                return total;
            }
            catch
            {
                return 3;

            }
        }
        public static void sendFileMsg(Socket s, string filename, int bagnum, int lastbag)
        {
            string msg = filename + "&" + bagnum + "&" + lastbag;
            byte[] data = Encoding.UTF8.GetBytes(msg);
            s.Send(data);
        }
        public static void sendJsonMsg(Socket s, string msg)
        {            
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                int total = 0;
                int size = data.Length;
                int dataleft = size;
                int sent;
                while (total < size)
                {
                    sent = s.Send(data, total, dataleft, SocketFlags.None);                             
                    total += sent;
                    dataleft -= sent;
                }
                s.Close();
                //new Helper().InsertErrorMsg(msg, "给安卓发消息完毕");
            }
            
            catch (Exception ex)
            {
                new Helper().InsertErrorMsg(ex.Message, "给安卓发消息异常");
                throw;
            }
        }
        public static byte[] ReceiveData(Socket s, long size, string path)
        {
            string direc = Path.GetDirectoryName(path);
            if (!Directory.Exists(direc))
            {
                Directory.CreateDirectory(direc);
            }
            FileStream MyFileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            int total = 0;
            int dataleft = (int)size;
            byte[] data = new byte[size];
            int recv;
            while (total < size)
            {
                recv = s.Receive(data, total, dataleft, SocketFlags.None);
                if (recv == 0)
                {
                    data = null;
                    break;
                }

                total += recv;
                dataleft -= recv;
            }
            MyFileStream.Write(data, 0, data.Length);
            MyFileStream.Close();
            if (path.ToLower().Contains("zip")&&File.Exists(path))
            {
                string savePath = Regex.Split(path, ".zip",RegexOptions.IgnoreCase)[0];
                Helper.UnZipFile(path, savePath);
            }
            //string sd = Encoding.Unicode.GetString(data);
            return data;
        }
        public static byte[] ReceiveMsg(Socket s,long size)
        {
            int total = 0;
            int dataleft = (int)size;
            byte[] data = new byte[size];
            int recv;
            while (true)
            {
                //recv = s.Receive(data);
                recv = s.Receive(data, total, dataleft, SocketFlags.None);
                if (recv == 0)
                {
                    break;
                }
                total += recv;
                dataleft -= recv;
            }
            //string sd = Encoding.UTF8.GetString(data,0,data.Length);
            return data;
        }
        public static string receiveFileMsg(Socket s)
        {
            int size = 100;
            byte[] data = new byte[size];
            int recv;
            try
            {
                recv = s.Receive(data);
                string msg = Encoding.UTF8.GetString(data);
                return msg;
            }
            catch (Exception ex)
            {
                return ex.Message;
                throw;
            }
        }
        public static byte[] ReceiveVarData(Socket s)
        {
            int total = 0;
            int recv;
            //byte[] datasize = new byte[10];
            //recv = s.Receive(datasize, 0, 10, SocketFlags.None);
            //s.Send(datasize);
            //string msg = Encoding.UTF8.GetString(datasize);
            //int size =Convert.ToInt32(Regex.Split(msg, ":")[1]);
            int size = BagLength;
            int dataleft = size;
            byte[] data = new byte[size];
            //while (total < size)
            //{
            recv = s.Receive(data, total, dataleft, SocketFlags.None);
            //new Helper().InsertErrorMsg(recv.ToString(), "recv");
            //if (recv == 0)
            //{
            //    break;
            //}
            //total += recv;
            //dataleft -= recv;
            //}
            //s.Send(datasize);
            return data;
        }
        public static Socket Connect(string IP, int Port)
        {
            //指向远程服务端节点
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(IP), Port);
            //创建套接字
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //连接到发送端
            try
            {
                client.Connect(ipep);
                return client;
            }
            catch(Exception ex)
            {
                new Helper().InsertErrorMsg(ex.Message + "||" + ex.StackTrace, "Socket 连接失败");
                return null;
            }
        }
    }
}
