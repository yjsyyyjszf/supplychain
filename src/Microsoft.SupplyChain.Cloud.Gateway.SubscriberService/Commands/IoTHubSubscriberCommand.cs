﻿using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.SupplyChain.Cloud.Gateway.SubscriberService.Processors;
using Microsoft.SupplyChain.Framework.Command;

namespace Microsoft.SupplyChain.Cloud.Gateway.SubscriberService.Commands
{
    public class IoTHubSubscriberCommand : BaseCommand<IoTHubSubscriberContext>
    {
        private readonly ISubscriberService _subscriberService;

        public IoTHubSubscriberCommand(ISubscriberService subscriberService)
        {
            _subscriberService = subscriberService;
        }

        protected override async Task DoExecuteAsync(IoTHubSubscriberContext context)
        {
            // fire up the event processor host.
            var eventProcessorHost = new EventProcessorHost(Guid.NewGuid().ToString(), context.IoTHubDeviceToCloudName, context.IoTHubConsumerGroupName, context.IoTHubConnectionString, context.IoTHubStorageConnectionString, "messages-events");
            await eventProcessorHost.RegisterEventProcessorFactoryAsync(new GenericEventProcessorFactory());
        }

        protected override void DoInitialize(IoTHubSubscriberContext context)
        {
            // get all iot hub config data from the service fabric config package.
            var configurationPackage = _subscriberService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            var iotHubSection = configurationPackage.Settings.Sections["IoTHub"].Parameters;
            context.IoTHubConnectionString = iotHubSection["IoTHubConnectionString"].Value;
            context.IoTHubStorageConnectionString = iotHubSection["IoTHubAzureStorageConnectionString"].Value;
            context.IoTHubDeviceToCloudName = iotHubSection["IoTHubDeviceToCloudName"].Value;
            context.IoTHubConsumerGroupName = iotHubSection["IoTHubConsumerGroupName"].Value;
            base.DoInitialize(context);
        }

        protected override ExceptionAction HandleError(IoTHubSubscriberContext context, Exception exception)
        {
           // _logger.ErrorFormat("Error processing event hub subscriber command: {0}", exception);
            return ExceptionAction.Rethrow;
        }
    }
}
