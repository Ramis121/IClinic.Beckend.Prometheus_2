using Prometheus;
using System;

namespace IClinic.Beckend.Prometheus.Service.Options
{
    public sealed class EventCounterAdapterOptions
    {
        public static readonly EventCounterAdapterOptions Default = new();

        public Func<string, bool> EventSourceFilterPredicate { get; set; } = _ => true;

        public CollectorRegistry Registry { get; set; } = Metrics.DefaultRegistry;
    }
}
