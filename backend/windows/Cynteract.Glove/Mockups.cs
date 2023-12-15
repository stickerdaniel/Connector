using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cynteract.CGlove
{
    public class Glove
    {
        public string comPort;
        public string bleId;
    }
    public delegate void RawGloveDataCallback(DataReceive data);
    public class GloveInformation
    {
        public const int vibrationNumber = 10;
    }
    public enum CSubConsoleType { Glove };
    public class CConsole
    {
        public static void Log(Object message, CSubConsoleType tag)
        {
            Console.WriteLine(message);
        }
    }
}
