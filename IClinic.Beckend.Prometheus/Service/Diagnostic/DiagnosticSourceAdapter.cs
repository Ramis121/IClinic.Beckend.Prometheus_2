using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IClinic.Beckend.Prometheus.Service.Diagnostic
{
    public sealed class DiagnosticSourceAdapter : IDisposable
    {
        public static IDisposable StartListening(DiagnosticSourceAdapterOptions options) => new DiagnosticSourceAdapter(options);
        private DiagnosticSourceAdapter(DiagnosticSourceAdapterOptions options)
        {
            Counter metrics = Metrics.WithCustomRegistry(options.Registry)
                .CreateCounter("diagnostic events total", "Total count of events received via the DiagnosticSource infrastructure.", new CounterConfiguration
                {
                    LabelNames = new[]
                    {
                        "source",
                        "event"
                    }
                });
            var newListenerObserver = new NewListenerObserver(OnNewListener);
            _newListenerSubscription = DiagnosticListener.AllListeners.Subscribe(newListenerObserver);
        }

        private readonly DiagnosticSourceAdapterOptions _options;
        private readonly Counter _counter;
        private readonly IDisposable _newListenerSubscription;

        private readonly Dictionary<string, IDisposable> _newEventSubscription = new Dictionary<string, IDisposable>();
        private readonly object _newEventSubscriptionLock = new object();

        private void OnNewListener(DiagnosticListener listener)
        {
            lock(_newEventSubscriptionLock)
            {
                if (_newEventSubscription.TryGetValue(listener.Name, out var oldSubcription))
                {
                    oldSubcription.Dispose();
                    _newEventSubscription.Remove(listener.Name);
                }
                if (!_options.ListenerFilterPredicate(listener))
                    return;

                var listenerName = listener.Name;
                var newEventObserver = new NewEventObserver(kvp => OnEvent(listenerName, kvp.Key, kvp.Value));
                _newEventSubscription[listenerName] = listener.Subscribe(newEventObserver);
            }
        }

        private void OnEvent(string listenerName, string eventName, object? payload)
        {
            _counter.WithLabels(listenerName, eventName).Inc();
        }


        private sealed class NewListenerObserver : IObserver<DiagnosticListener>
        {
            private readonly Action<DiagnosticListener> _onNewListener;

            public NewListenerObserver(Action<DiagnosticListener> onNewListener)
            {
                _onNewListener = onNewListener;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(DiagnosticListener listener)
            {
                _onNewListener(listener);
            }
        }

        private sealed class NewEventObserver : IObserver<KeyValuePair<string, object?>>
        {
            private readonly Action<KeyValuePair<string, object?>> _onEvent;
            public NewEventObserver(Action<KeyValuePair<string, object?>> onEvent)
            {
                _onEvent = onEvent;
            }
            public void OnCompleted()
            {
                throw new NotImplementedException();
            }

            public void OnError(Exception error)
            {
                throw new NotImplementedException();
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                _onEvent(value);
            }
        }
        public void Dispose()
        {
            _newListenerSubscription.Dispose();
            lock (_newEventSubscriptionLock)
            {
                foreach (var sub in _newEventSubscription.Values)
                    sub.Dispose();
            }
        }
    }
}
