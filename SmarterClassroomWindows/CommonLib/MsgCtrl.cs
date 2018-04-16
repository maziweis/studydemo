using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SmarterClassroomWindows.CommonLib
{
    public class MsgCtrl
    {
        private IPEndPoint remoteMsg;
        public String hostIP = "127.0.0.1";
        private int hostPort = 4096;

        private Socket hostMsg;
        public String remoteIP { get; }
        private int remotePort;
        public MsgCtrl(String IP, int Port, int flag)//flag=1=client flag=0=server
        {
            if (flag == 1)
            {
                this.remoteIP = IP;
                this.remotePort = Port;
                /*TODO 校验 IP地址是否有效，除了越界还有 本机不是该IP的情况*/
                remoteMsg = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
                if (remoteMsg == null) throw new Exception();
                hostMsg = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                int port = randomPort();
                hostMsg.Bind(new IPEndPoint(IPAddress.Parse(hostIP), port));

            }
            else if (flag == 0)
            {
                this.hostIP = IP;
                this.hostPort = Port;
                /*TODO 校验 IP地址是否有效，除了越界还有 本机不是该IP的情况*/
                hostMsg = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                hostMsg.Bind(new IPEndPoint(IPAddress.Parse(hostIP), hostPort));
            }
            /*
            this.remoteIP = remoteIP;
            this.port = remotePort;
            
            remoteMsg = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
            hostMsg = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            hostMsg.Bind(new IPEndPoint(IPAddress.Parse(hostIP), hostPort ));
            */
        }
        public void sendMsg(String msg, int flag)
        {
            if (remoteMsg == null) return;
            hostMsg.SendTo(Encoding.UTF8.GetBytes(flag + "*" + msg), remoteMsg);
        }
        public String[] reciveMsg()
        {
            //remoteMsg = new IPEndPoint(IPAddress.Any, 0);
            EndPoint temp = new IPEndPoint(IPAddress.Any, 0);
            byte[] byteArray = new byte[1024];
            int rev = hostMsg.ReceiveFrom(byteArray, ref temp);
            remoteMsg = (IPEndPoint)temp;
            return Encoding.UTF8.GetString(byteArray, 0, rev).Split(new char[] { '*' }, 2);
        }
        public void Close()
        {

            hostMsg.Close();
            hostMsg = null;
        }
        public static int randomPort()
        {
            Random r = new Random();
            int port = r.Next(1030, 65530);
            return port;
        }
    }
}
