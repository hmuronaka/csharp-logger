using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using h.mu.Logger;

namespace LoggerTest
{
    class Program
    {
        static h.mu.Logger.LogFileWriter logger = new h.mu.Logger.LogFileWriter(Properties.Settings1.Default, "log_");

        static void Main(string[] args)
        {
            logger.info("BEGIN {0}", "STR");
//            logger.logLevel = LogLevel.ERROR;
            for (int i = 0; i < 10; i++)
            {
                logger.debug("DEBUG文字列");
                logger.info("INFO文字列");
                logger.warn("WARN文字列");
                logger.error("ERROR文字列");
                logger.error("ERROR", new Exception("GADG"));
            }
            logger.info("END {0}", "STR");
        }
    }
}
