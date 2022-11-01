using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    static class Logger
    {
        static bool is_server_ready = false;
        
        public static void Log(string message)
        {
            if (is_server_ready)
                TcpSimpleServer.Instance.Broadcast(message);
            else
                Console.WriteLine(message);
        }
    }
}
