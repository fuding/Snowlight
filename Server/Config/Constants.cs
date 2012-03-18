using System;
using System.Text;
using System.IO;

namespace Snowlight.Config
{
    public static class Constants
    {
        public static readonly string           ConsoleTitle                = "Snowlight";
        public static readonly int              ConsoleWindowWidth          = 90;
        public static readonly int              ConsoleWindowHeight         = 30;
        public static readonly string           DataFileDirectory           = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar;
        public static readonly string           LogFileDirectory            = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar;
        public static readonly string           LangFileDirectory           = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "lang" + Path.DirectorySeparatorChar;
        public static readonly Encoding         DefaultEncoding             = Encoding.Default;
        public static readonly char             LineBreakChar               = Convert.ToChar(13);
    }
}
