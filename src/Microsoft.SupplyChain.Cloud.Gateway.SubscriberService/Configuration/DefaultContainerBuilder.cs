﻿using System;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.SupplyChain.Cloud.Administration.Contracts;
using Microsoft.SupplyChain.Cloud.Gateway.Contracts;
using Microsoft.SupplyChain.Cloud.Gateway.SubscriberService.Commands;
using Microsoft.SupplyChain.Cloud.Gateway.SubscriberService.Processors;
using Microsoft.SupplyChain.Cloud.Gateway.SubscriberService.ServiceAgents;
using Microsoft.SupplyChain.Cloud.Tracking.Contracts;
using Microsoft.SupplyChain.Framework;
using Microsoft.SupplyChain.Framework.Command;
using Microsoft.SupplyChain.Framework.Interceptors;
using IDeviceMovementServiceAgent = Microsoft.SupplyChain.Cloud.Gateway.Contracts.IDeviceMovementServiceAgent;
using ISmartContractStoreServiceAgent = Microsoft.SupplyChain.Cloud.Gateway.Contracts.ISmartContractStoreServiceAgent;

namespace Microsoft.SupplyChain.Cloud.Gateway.SubscriberService.Configuration
{
    public class DefaultContainerBuilder : IContainerBuilder
    {
        private readonly IWindsorContainer _container;
        private bool _disposed = false;
        private WindsorServiceLocator _windsorServiceLocator;

        public DefaultContainerBuilder()
        {
            _container = new WindsorContainer();
            _container.Register(Component.For<IWindsorContainer>().Instance(_container));
        }

        public IWindsorContainer Container => _container;

        public virtual void BuildCommands()
        {
            _container.Register(Component.For<ICommandAbstractFactory>()
                        .ImplementedBy<CommandAbstractFactory>()
                        .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                        .LifestyleSingleton());         

            _container.Register(Component.For<ICommand<IoTHubSubscriberContext>>()
                .ImplementedBy<IoTHubSubscriberCommand>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());

            _container.Register(Component.For<ICommand<BlockchainContractBootstrapperContext>>()
                .ImplementedBy<BlockchainContractBootstrapperCommand>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());

        }

        public void BuildServiceAgents()
        {
            _container.Register(Component.For<IBlockchainServiceAgent>()
                .ImplementedBy<EthereumServiceAgent>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());

            _container.Register(Component.For<IDeviceStoreService>()
                .Instance(new ServiceProxyFactory()
                    .CreateServiceProxy<IDeviceStoreService>(
                        new Uri("fabric:/Microsoft.SupplyChain.Cloud.Administration/DeviceStoreService")))
                .LifestyleSingleton()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor"))
                .Anywhere);

            _container.Register(Component.For<ISmartContractStoreService>()
                .Instance(new ServiceProxyFactory()
                    .CreateServiceProxy<ISmartContractStoreService>(
                        new Uri("fabric:/Microsoft.SupplyChain.Cloud.Administration/SmartContractStoreService")))
                .LifestyleSingleton()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor"))
                .Anywhere);

            _container.Register(Component.For<ITrackingStoreService>()
                .Instance(new ServiceProxyFactory()
                    .CreateServiceProxy<ITrackingStoreService>(
                        new Uri("fabric:/Microsoft.SupplyChain.Cloud.Tracking/TrackingStoreService")))
                .LifestyleSingleton()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor"))
                .Anywhere);

            _container.Register(Component.For<IDeviceMovementServiceAgent>()
                .ImplementedBy<EthereumDeviceMovementServiceAgent>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());

            _container.Register(Component.For<ISmartContractStoreServiceAgent>()
                .ImplementedBy<SmartContractStoreServiceAgent>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());

            _container.Register(Component.For<IDeviceStoreServiceAgent>()
                .ImplementedBy<DeviceStoreServiceAgent>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());

            _container.Register(Component.For<ITrackerStoreServiceAgent>()
                .ImplementedBy<TrackerStoreServiceAgent>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());

        }


        private void BuildInterceptors()
        {
            _container.Register(Component.For<IInterceptor>()
                                    .ImplementedBy<ConsoleOutputInterceptor>()
                                    .Named("ConsoleInterceptor"));

        }

        private void BuildAndRegisterServiceLocator()
        {
            _windsorServiceLocator = new WindsorServiceLocator(_container);
            ServiceLocator.SetLocatorProvider(() => _windsorServiceLocator);

            // now register the service locator with castle..
            _container.Register(Component.For<IServiceLocator>().Instance(_windsorServiceLocator));
        }

        private void BuildProcessors()
        {
            _container.Register(Component.For<IEventProcessor>()
                     .ImplementedBy<GenericEventProcessor>()
                     .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                     .LifestyleSingleton());
        }

        public virtual void BuildRepositories()
        {
        }



        public IServiceLocator Build()
        {
            BuildAndRegisterServiceLocator();
            BuildInterceptors();
            BuildCommands();
            BuildServiceAgents();
            BuildProcessors();
            BuildRepositories();
            return _windsorServiceLocator;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _container.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
