using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricDefinitions;
using Prometheus;
using CommandLine;


namespace custom_exporter {
    public struct GaugeMetricStruct {
        private MetricProvider mMetricProvider;
        private Gauge mGauge;

        public GaugeMetricStruct(MetricProvider metricProvider, Gauge gauge) {
            mMetricProvider = metricProvider;
            mGauge = gauge;
        }

        public MetricProvider getMetricProvider() {
            return mMetricProvider;
        }

        public Gauge getGauge() {
            return mGauge;
        }
    }

    class Program {

        public class Options {
            [Option('m', "metric-definition", Required = false, HelpText = "file which contains a metric-definition", Default = "metric_definition.json")]
            public string metricDefinitionFile { get; set; }
        }

        static private List<GaugeMetricStruct> mMetricStructs = new List<GaugeMetricStruct>();

        static void CreateMetricEndpoints(string metricDefinitionFile) {
            // Read metric definition and create array of MetricDefinition objects.
            string definition = File.ReadAllText(metricDefinitionFile);
            MetricDefinition[] metricDefinitions = MetricDefinition.FromJson(definition);

            foreach (MetricDefinition def in metricDefinitions) {
                string service_name = def.ServiceName;
                string url = def.Url;
                foreach (Metric metric in def.Metrics) {

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
        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o => {
                       CreateMetricEndpoints(o.metricDefinitionFile);
                   });
            // Start Metric server which serves metric endpoint at specified port
            var server = new MetricServer(8888);
            server.Start();

            while (true) {
                // Iterate over Metric Structs and gather data.
                foreach (GaugeMetricStruct metricStruct in mMetricStructs) {
                    Task<object> endpoint_task = metricStruct.getMetricProvider().GetValue();
                    endpoint_task.Wait();
                    Console.WriteLine("{0} result: {1}", metricStruct.getMetricProvider().GetMetricName(), endpoint_task.Result.ToString());
                    object result = endpoint_task.Result;
                    metricStruct.getGauge().Set((double)endpoint_task.Result);
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
}