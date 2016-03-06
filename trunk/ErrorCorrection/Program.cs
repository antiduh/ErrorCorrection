using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.IO;
using ErrorCorrection.ByteImpl;
using ErrorCorrection.IntImpl;

namespace ErrorCorrection
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IntImplTester.DoTests();
            Console.Out.Flush();
        }
    }
}
