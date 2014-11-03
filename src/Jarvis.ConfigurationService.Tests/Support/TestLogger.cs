using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Tests.Support
{
    public class TestLogger
    {
        public Int32 ErrorCount { get; set; }

        public List<String> Logs { get; set; }

        public TestLogger()
        {
            Logs = new List<string>();
        }
        public void Log(String message, Boolean isError, Exception ex)
        {
            if (isError) ErrorCount++;
            Logs.Add(message);
        }
    }
}
