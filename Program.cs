using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricDefinitions;
using Prometheus;

namespace custom_exporter
{
    public struct GaugeMetricStruct
    {
        private MetricProvider mMetricEndpoint;
        private Gauge mGauge;

        public GaugeMetricStruct(MetricProvider metricEndpoint, Gauge gauge)
        {
            mMetricEndpoint = metricEndpoint;
            mGauge = gauge;
        }

        public MetricProvider getMetricEndpoint()
        {
            return mMetricEndpoint;
        }

        public Gauge getGauge()
        {
            return mGauge;
        }
    }

    class Program
    {

        static private List<GaugeMetricStruct> mMetricStructs = new List<GaugeMetricStruct>();

        static void CreateMetricEndpoints()
        {
            // Read metric definition and create array of MetricDefinition objects.
            string definition = File.ReadAllText("metric_definition.json");
            MetricDefinition[] metricDefinitions = MetricDefinition.FromJson(definition);

            foreach (MetricDefinition def in metricDefinitions)
            {
                string service_name = def.ServiceName;
                string url = def.Url;
                foreach (Metric metric in def.Metrics)
                {

                    string api_endpoint = metric.ApiEndpoint != null ? url + metric.ApiEndpoint : null;
                    string metric_name = def.ServiceName + "_" + metric.MetricName;
                    string reponse_body_identifier = metric.DesiredResponseField;
                    AuthCredentials auth_credentials = def.AuthCredentials;
                    Dictionary<string, double> string_value_mapping = metric.StringValueMapping;
                    // Create MetricEndpoint which executes API calls
                    MetricProvider metricEndpoint = new MetricProvider(api_endpoint, metric_name, reponse_body_identifier, auth_credentials, string_value_mapping, metric.Program, metric.Argument);
                    // Create Prometheus Gauge
                    Gauge metricGauge = Metrics.CreateGauge(name: metric_name, help: metric_name);
                    mMetricStructs.Add(new GaugeMetricStruct(metricEndpoint, metricGauge));
                }
            }
        }
        static void Main()
        {
            CreateMetricEndpoints();
            // Start Metric server which serves metric endpoint at specified port
            var server = new MetricServer(hostname: "localhost", port: 1234);
            server.Start();

            while (true)
            {
                // Iterate over Metric Structs and gather data.
                foreach (GaugeMetricStruct metricStruct in mMetricStructs)
                {
                    Task<object> endpoint_task = metricStruct.getMetricEndpoint().get_value();
                    endpoint_task.Wait();
                    Console.WriteLine("enpoint_task result: {0}", endpoint_task.Result.ToString());
                    metricStruct.getGauge().Set((double)endpoint_task.Result);
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
}