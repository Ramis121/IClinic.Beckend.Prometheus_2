using IClinic.Beckend.Prometheus.Service.Options;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace IClinic.Beckend.Prometheus.Service.CounterAdapter
{
    public sealed class EventCounterAdapter : IDisposable
    {
        public static IDisposable StartListening() => new EventCounterAdapter(Options.EventCounterAdapterOptions.Default);

        public static IDisposable StartListening(Options.EventCounterAdapterOptions options) => new EventCounterAdapter(options);

        private EventCounterAdapter(Options.EventCounterAdapterOptions options)
        {
            _options = options;
            _metricFactory = Metrics.WithCustomRegistry(_options.Registry);

            _gauge = _metricFactory.CreateGauge("dotnet_gauge", "Values from .NET aggregating EventCounters (count per second).", new GaugeConfiguration
            {
                LabelNames = new[] { "source", "name", "display_name" }
            });
            _counter = _metricFactory.CreateCounter("dotnet_counter", "Values from .NET incrementing EventCounters.", new CounterConfiguration
            {
                LabelNames = new[] { "source", "name", "display_name" }
            });

            _listener = new Listener(OnEventSourceCreated, OnEventWritten);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        private readonly Options.EventCounterAdapterOptions _options;
        private readonly IMetricFactory _metricFactory;

        private readonly Gauge _gauge;
        private readonly Counter _counter;

        private readonly Listener _listener;

        private bool OnEventSourceCreated(EventSource source)
        {
            return _options.EventSourceFilterPredicate(source.Name);
        }

        private void OnEventWritten(EventWrittenEventArgs args)
        {
            if (args.EventName != "EventCounters")
                return;

            if (args.Payload == null)
                return; 

            var eventSourceName = args.EventSource.Name;

            foreach (var item in args.Payload)
            {
                if (item is not IDictionary<string, object> e)
                    continue;

                if (!e.TryGetValue("Name", out var nameWrapper))
                    continue;

                var name = nameWrapper as string;

                if (name == null)
                    continue; 

                if (!e.TryGetValue("DisplayName", out var displayNameWrapper))
                    continue;

                var displayName = displayNameWrapper as string ?? "";

                if (e.TryGetValue("Increment", out var increment))
                {
                    var value = increment as double?;

                    if (value == null)
                        continue; 

                    _counter.WithLabels(eventSourceName, name, displayName).Inc(value.Value);
                }
                else if (e.TryGetValue("Mean", out var mean))
                {
                    var value = mean as double?;

                    if (value == null)
                        continue; 

                    _gauge.WithLabels(eventSourceName, name, displayName).Set(value.Value);
                }
            }
        }

        private sealed class Listener : EventListener
        {
            public Listener(Func<EventSource, bool> onEventSourceCreated, Action<EventWrittenEventArgs> onEventWritten)
            {
                _onEventSourceCreated = onEventSourceCreated;
                _onEventWritten = onEventWritten;

                foreach (var eventSource in _preRegisteredEventSources)
                    OnEventSourceCreated(eventSource);

                _preRegisteredEventSources.Clear();
            }

            private readonly List<EventSource> _preRegisteredEventSources = new List<EventSource>();

            private readonly Func<EventSource, bool> _onEventSourceCreated;
            private readonly Action<EventWrittenEventArgs> _onEventWritten;

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (_onEventSourceCreated == null)
                {
                    _preRegisteredEventSources.Add(eventSource);
                    return;
                }

                if (!_onEventSourceCreated(eventSource))
                    return;

                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, new Dictionary<string, string?>()
                {
                    ["EventCounterIntervalSec"] = "1"
                });
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                _onEventWritten(eventData);
            }
        }
    }
}
