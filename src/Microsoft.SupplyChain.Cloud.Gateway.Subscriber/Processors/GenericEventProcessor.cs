﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Microsoft.SupplyChain.Services.Contracts;

namespace Microsoft.SupplyChain.Cloud.Gateway.Subscriber.Processors
{
    public class GenericEventProcessor : IEventProcessor
    {
        private PartitionContext _partitionContext;
        private Stopwatch _checkpointStopWatch;
        private readonly ILogger _logger;

        public GenericEventProcessor(ILogger logger)
        {
            _logger = logger;
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        public Task OpenAsync(PartitionContext context)
        {
            _logger.InfoFormat("EventProcessor initialize.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset);
            _partitionContext = context;
            _checkpointStopWatch = new Stopwatch();
            _checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            try
            {
                foreach (var eventData in messages)
                {
                    byte[] binaryPayload = eventData.GetBytes();

                    var sensor = JsonConvert.DeserializeObject<Sensor>(Encoding.UTF8.GetString(binaryPayload));

                    _logger.InfoFormat("Message received partition: {0} sensor name: {1}, GatwayId: {2}", _partitionContext.Lease.PartitionId, sensor.Name, sensor.GatewayId);
                }

                // Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
                if (_checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
                {
                    await context.CheckpointAsync();
                    lock (this)
                    {
                        _checkpointStopWatch.Reset();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.FatalFormat("Error in processing payload: ", ex);
            }
        }
    }
}
