using Prometheus;
using System;
using System.Diagnostics;

namespace IClinic.Beckend.Prometheus.Service.Options
{
    public class DiagnosticSourceAdapterOptions
    {
        internal static readonly DiagnosticSourceAdapterOptions Default = new DiagnosticSourceAdapterOptions();

        public Func<DiagnosticListener, bool> ListenerFilterPredicate = _ => true;

        public CollectorRegistry Registry = Metrics.DefaultRegistry;
    }
}
