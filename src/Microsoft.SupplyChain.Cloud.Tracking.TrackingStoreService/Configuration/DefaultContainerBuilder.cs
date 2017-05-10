﻿using System;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.SupplyChain.Cloud.Registration.DiscoveryService.Configuration;
using Microsoft.SupplyChain.Cloud.Tracking.TrackingStoreService.Controllers;
using Microsoft.SupplyChain.Framework;
using Microsoft.SupplyChain.Framework.Command;
using Microsoft.SupplyChain.Framework.Interceptors;

namespace Microsoft.SupplyChain.Cloud.Tracking.TrackingStoreService.Configuration
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
      
        private void BuildControllers()
        {
            _container.Register(Component.For<TrackerReadonlyStoreController>()
                .ImplementedBy<TrackerReadonlyStoreController>()
                .Interceptors(InterceptorReference.ForKey("ConsoleInterceptor")).Anywhere
                .LifestyleTransient());
        }

        private void BuildServiceAgents()
        {
           
        }

        public IServiceLocator Build()
        {
            BuildAndRegisterServiceLocator();
            BuildInterceptors();
            BuildServiceAgents();
            BuildCommands();
            BuildControllers();
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