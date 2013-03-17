using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using h.mu.Logger;

namespace LoggerTest
{
    class Program
    {
        static void Main(string[] args)
        {

            RollingFileWriter writer = new RollingFileWriter(@"..\logdir", "test", "log", 1024, 2);
            writer.open();
            for (int i = 0; i < 100; i++)
            {
                writer.writeLine("0123456789");
            }
            writer.close();
        }
    }
}
