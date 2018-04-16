using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmarterClassroomWindows.CommonLib
{
    public class UDPServer
    {
        private Socket hostData;

        public String fileName { get; set; }
        public String filePath { get; set; }
        public long fileSize { get; set; }

        /*本来是用于停止传输的，现在用来用于超时停止UDP传输*/
        private volatile Boolean canRev = true;
        public String IP { get; }
        private int port;
        public UDPServer(String ipadd, int port)
        {
            /*校验 IP地址是否有效，除了越界还有 本机不是该IP的情况*/
            this.IP = ipadd;
            this.port = port;
            initSocket();
        }
        public void closeSocket()
        {
            hostData.Close();
            //hostMsg.Close();
        }
        public void initSocket()
        {
            /*校验 IP地址是否有效，除了越界还有 本机不是该IP的情况*/
            hostData = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            hostData.Bind(new IPEndPoint(IPAddress.Parse(IP), port));

            //hostMsg = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //hostMsg.Bind(new IPEndPoint(IPAddress.Parse(IP), port - 100));
        }
        public void reciveFile()
        {
            FileStream fs = new FileStream(filePath + fileName, FileMode.Append, FileAccess.Write);

            CancellationTokenSource ct = new CancellationTokenSource();
            ct.Token.Register(() => {
                canRev = false;
            });
            ct.CancelAfter((int)fileSize / 300 > 1000 ? (int)fileSize / 300 : 1000);

            long length = 0;
            while (length < fileSize)
            {
                if (canRev)
                {
                    int rev = ReciveData(fs);
                    if (rev == -1) break;//还没设过-1
                    else length += rev;
                    //fs.Seek(length, SeekOrigin.Begin);
                }
                else break;

            }
            ct.Dispose();
            fs.Flush();
            fs.Close();
            //Thread.CurrentThread.Abort();
            //reciveData(fs);
            /*
            fs.Flush();
            fs.Close();
            */
        }
        public void stopRev()
        {
            canRev = false;
        }
        private int ReciveData(FileStream fs)
        {
            EndPoint temp = new IPEndPoint(IPAddress.Any, 0);
            byte[] byteArray = new byte[1024];
            if (hostData.Available != 0)
            {
                int rev = hostData.ReceiveFrom(byteArray, ref temp);
                fs.Write(byteArray, 0, rev);
                return rev;
            }
            return 0;

        }
    }
}
