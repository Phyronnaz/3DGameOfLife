using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public static class Log
    {
        public delegate void LogDelegate(string s);
        public static event LogDelegate ComputationTimeLog;
        public static event LogDelegate TrianglesTimesLog;
        public static event LogDelegate CacheLog;
        public static event LogDelegate WarningsLog;
        public static event LogDelegate ErrorsLog;

        public static void ComputationTime(long time)
        {
            ComputationTimeLog("Computation time: " + time.ToString() + "ms");
        }
        public static void TrianglesTime(long time)
        {
            TrianglesTimesLog("Triangles computation time: " + time + "ms");
        }

        public static void Cache(int cache)
        {
            CacheLog("Cache: " + cache.ToString());
        }

        public static void Warning(string s)
        {
            WarningsLog(s);
        }

        public static void Error(string s)
        {
            ErrorsLog(s);
        }
    }
}
