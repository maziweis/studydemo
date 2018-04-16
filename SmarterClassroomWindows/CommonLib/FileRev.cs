using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmarterClassroomWindows.CommonLib
{
    public class FileRev
    {
        private UDPServer server;
        private String filePath = @"D:\";
        public FileRev()
        {
        }
        public void ReciveFile(UDPServer server, String fileName, long fileSize)
        {
            //FileStream fs = new FileStream(filePath + fileName, FileMode.Create, FileAccess.Write);
            /*
            CancellationTokenSource ct = new CancellationTokenSource();
            Thread t = Thread.CurrentThread;
            ct.Token.Register(()=> {
                if (t.IsAlive)
                {
                    fs.Flush();
                    fs.Close();
                    t.Abort();
                }
                
            });
            ct.CancelAfter(20000);
            */
            this.server = server;
            server.fileName = fileName;
            server.fileSize = fileSize;
            server.filePath = this.filePath;
            ThreadStart st = new ThreadStart(server.reciveFile);
            Thread t = Util.newThread(st);
            //server.closeSocket();
        }
        public void StopSend()
        {
            server.stopRev();
            File.Delete(filePath + server.fileName);
            server.closeSocket();
            /*
            CancellationTokenSource ct = new CancellationTokenSource();
            ct.Token.Register(() =>
            {
                File.Delete(filePath+server.fileName);
                server.closeSocket();
            });
            ct.CancelAfter((int)server.fileSize / 1000>1050? (int)server.fileSize / 1000:10050);
            */
        }
    }
}
