using System;
using System.Collections.Generic;
using System.Text;

namespace AWS_Learning.Monitoring.Worker.Configuration
{
    public sealed class MonitoringOptions
    {
        public const string SectionName = "Monitoring";

        public string ServiceUrl { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public string QueueUrl { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public int IntervalSeconds { get; set; } = 60;
    }
}
