using System.Reflection.Emit;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using MetricDefinitions;
using Prometheus;
using CommandLine;
using PrometheusMetricManagement;


namespace custom_exporter {
    class Program {

        public class Options {
            [Option('m', "metric-definition", Required = false, HelpText = "file which contains a metric-definition", Default = "metric_definition.json")]
            public string metricDefinitionFile { get; set; }

            [Option('p', "port", Required = false, HelpText = "Port on which we run the server", Default = 8888)]
            public int metricServerPort { get; set; }
        }

        static private PrometheusMetricManager mPrometheusMetricManager = new PrometheusMetricManager();

        static void CreateMetricProviders(string metricDefinitionFile) {
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
                    SortedDictionary<string, string> labels = metric.Labels;
                    ResponseType responseType = metric.ResponseType;
                    ExecutionType executionType = metric.ExecutionType;

                    // Create MetricEndpoint which executes API calls or executes a program/script
                    MetricProvider metricEndpoint = null;
                    if (executionType == ExecutionType.SCRIPT && metric.Program != null && metric.Argument != null) {
                        metricEndpoint = new MetricProvider(metric_name, metric.Program, metric.Argument, labels, executionType);
                    } else if (executionType == ExecutionType.API_CALL && api_endpoint != null) {
                        metricEndpoint = new MetricProvider(api_endpoint, metric_name, reponse_body_identifier, auth_credentials, string_value_mapping, labels, responseType, executionType);
                    }

                    // Create Prometheus Gauges
                    GaugeConfiguration config = new GaugeConfiguration();
                    if (labels != null) {
                        config.LabelNames = labels.Keys.ToArray();
                    }
                    Gauge metricGauge = Metrics.CreateGauge(name: metric_name, help: metric_name, config);
                    mPrometheusMetricManager.AddMetricStruct(metricEndpoint, metricGauge);
                }
            }
        }
        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o => {
                       MetricServer server = null;
                       // Start Metric server which serves metric endpoint at specified port
                       try {
                           // We need this for Docker, else the metricserver is not reachable on the exposed port.
                           // I assume this way it serves on 0.0.0.0:PORT and thus listens on all interfaces.
                           server = new MetricServer(o.metricServerPort);
                           server.Start();
                       } catch (HttpListenerException e) {
                           server = new MetricServer("localhost", o.metricServerPort);
                           server.Start();
                       }

                       // Create Metric Providers from the given metric_definition and store them. 
                       CreateMetricProviders(o.metricDefinitionFile);
                       while (true) {
                           // Iterate over Metric Structs and gather data.
                           List<MetricStruct<Gauge>> gaugeStructs = mPrometheusMetricManager.GetGauges();
                           foreach (MetricStruct<Gauge> metricStruct in gaugeStructs) {

                               MetricProvider metricProvider = metricStruct.getMetricProvider();
                               Task<object> endpoint_task = metricProvider.GetValue();
                               SortedDictionary<string, string> labels = metricProvider.GetLabels();
                               Gauge gauge = metricStruct.getPrometheusMetric();

                               endpoint_task.Wait();
                               Console.WriteLine("{0} result: {1}", metricProvider.GetMetricName(), endpoint_task.Result.ToString());
                               object result = endpoint_task.Result;

                               if (labels != null) {
                                   gauge.WithLabels(labels.Values.ToArray()).Set((double)endpoint_task.Result);
                               } else {
                                   gauge.Set((double)endpoint_task.Result);
                               }
                           }
                           Thread.Sleep(TimeSpan.FromSeconds(1));
                       }
                   });
        }
    }
}