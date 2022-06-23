using MediatR;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IClinic.Beckend.Prometheus.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly EventCounterAdapter adapter;
        private readonly EventCounterAdapterOptions _eventCounter;
        private readonly Counter counter = Metrics.CreateCounter("my_counter", "Metrict counter");
        private readonly Gauge _counter = Metrics.CreateGauge("myapp_jobs_queued", "Number of jobs waiting for processing in the queue.");
        private readonly IMediator _mediator;
        public ValuesController(IMediator mediator, EventCounterAdapter options, EventCounterAdapterOptions eventCounter)
        {
            _mediator = mediator;
            _eventCounter = eventCounter;
            adapter = options;
        }
        // GET: api/<ValuesController>
        [HttpGet]
        public IActionResult Get()
        {
            adapter.
            return Ok(_mediator.Send(new EventCounterAdapter(_eventCounter)));
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }
    }
}
