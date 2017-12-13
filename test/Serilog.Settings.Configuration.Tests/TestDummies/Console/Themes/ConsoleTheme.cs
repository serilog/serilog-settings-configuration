using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Settings.Configuration.Tests.TestDummies.Console.Themes
{
    public abstract class ConsoleTheme
    {
        public static ConsoleTheme None { get; } = new EmptyConsoleTheme();
    }
}
