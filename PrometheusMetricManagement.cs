using System;
using System.Collections.Generic;
using Prometheus;

namespace PrometheusMetricManagement {
    public struct MetricStruct<T> {
        private readonly MetricProvider mMetricProvider;
        private readonly T mPrometheusMetric;
        public MetricStruct(MetricProvider metricProvider, T prometheusMetric) {
            mMetricProvider = metricProvider;
            mPrometheusMetric = prometheusMetric;
        }

        public MetricProvider getMetricProvider() {
            return mMetricProvider;
        }
        public T getPrometheusMetric() {
            return mPrometheusMetric;
        }
    }

    public class PrometheusMetricManager {
        private List<MetricStruct<Gauge>> mGauges;
        private List<MetricStruct<Summary>> mSummaries;
        private List<MetricStruct<Histogram>> mHistograms;
        private List<MetricStruct<Counter>> mCounters;

        public PrometheusMetricManager() {
            mGauges = new List<MetricStruct<Gauge>>();
            mSummaries = new List<MetricStruct<Summary>>();
            mHistograms = new List<MetricStruct<Histogram>>();
            mCounters = new List<MetricStruct<Counter>>();
        }

        public void AddMetricStruct(MetricProvider metricProvider, object prometheusMetric) {
            if (prometheusMetric is Gauge) {
                mGauges.Add(new MetricStruct<Gauge>(metricProvider, (Gauge)prometheusMetric));
            } else if (prometheusMetric is Summary) {
                mSummaries.Add(new MetricStruct<Summary>(metricProvider, (Summary)prometheusMetric));
            } else if (prometheusMetric is Histogram) {
                mHistograms.Add(new MetricStruct<Histogram>(metricProvider, (Histogram)prometheusMetric));
            } else if (prometheusMetric is Counter) {
                mCounters.Add(new MetricStruct<Counter>(metricProvider, (Counter)prometheusMetric));
            } else {
                throw new InvalidOperationException("Type " + prometheusMetric.GetType().ToString() + " is not supported");
            }
        }

        public List<MetricStruct<Gauge>> GetGauges() {
            return mGauges;
        }

    }
}