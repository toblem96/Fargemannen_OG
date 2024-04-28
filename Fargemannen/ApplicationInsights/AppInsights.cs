using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fargemannen.ApplicationInsights
{
    internal static class AppInsights
    {
        public static TelemetryClient TelemetryClient { get; private set; }

        static AppInsights()
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = "116d7820-3999-4f42-b896-fb353bf8b905";
            TelemetryClient = new TelemetryClient(configuration);
        }

        public static void TrackEvent(string eventName)
        {
            TelemetryClient.TrackEvent(eventName);
        }

        public static void TrackMetric(string metricName, double value)
        {
            TelemetryClient.TrackMetric(metricName, value);
        }

        public static void TrackException(Exception exception, IDictionary<string, string> properties)
        {
            TelemetryClient.TrackException(exception, properties);
        }
    }
}
