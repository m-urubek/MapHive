using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapHive.Models
{
    public class Log
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int SeverityId { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string Exception { get; set; }
        public string StackTrace { get; set; }
        public string UserName { get; set; }
        public string RequestPath { get; set; }
        public string AdditionalData { get; set; }

        // Navigation property
        public LogSeverity Severity { get; set; }
    }
} 