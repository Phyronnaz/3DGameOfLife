using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public static class Log
    {
        public delegate void LogDelegate(string s);

        public static bool Enabled = true;
        public static event LogDelegate ComputationTimeLog;
        public static event LogDelegate TrianglesTimesLog;
        public static event LogDelegate WarningsLog;
        public static event LogDelegate ErrorsLog;


        public static void LogComputationTime(long time)
        {
            if (Enabled)
            {
                ComputationTimeLog("Computation time: " + time.ToString() + "ms");
            }
        }
        public static void LogTrianglesTime(long time)
        {
            if (Enabled)
            {
                TrianglesTimesLog("Triangles computation time: " + time + "ms");
            }
        }

        public static void LogWarning(string s)
        {
            if (Enabled)
            {
                WarningsLog(s);
            }
        }

        public static void LogError(string s)
        {
            if (Enabled)
            {
                ErrorsLog(s);
            }
        }
    }
}
