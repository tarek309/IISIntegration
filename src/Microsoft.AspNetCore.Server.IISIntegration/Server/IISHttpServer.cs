// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Server.IISIntegration.IISDelegates;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISHttpServer : IServer
    {
        private static PFN_REQUEST_HANDLER _requestHandler = HandleRequest;
        private static PFN_SHUTDOWN_HANDLER _shutdownHandler = HandleShutdown;
        private static PFN_ASYNC_COMPLETION _onAsyncCompletion = OnAsyncCompletion;

        private IISContextFactory _iisContextFactory;
        private readonly BufferPool _bufferPool = new MemoryPool();
        internal GCHandle _httpServerHandle;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly IAuthenticationSchemeProvider _authentication;
        private IISFunctions _iisFunctions;
        private readonly IISOptions _options;

        public IFeatureCollection Features { get; } = new FeatureCollection();
        public IISHttpServer(IApplicationLifetime applicationLifetime, IAuthenticationSchemeProvider authentication, IOptions<IISOptions> options)
            : this(applicationLifetime, authentication, options, new DefaultIISFunctions())
        {
        }

        internal IISHttpServer(IApplicationLifetime applicationLifetime, IAuthenticationSchemeProvider authentication, IOptions<IISOptions> options, IISFunctions iisFunctions)
        {
            _applicationLifetime = applicationLifetime;
            _authentication = authentication;
            _iisFunctions = iisFunctions;
            _options = options.Value;
            if (_options.ForwardWindowsAuthentication)
            {
                authentication.AddScheme(new AuthenticationScheme(IISDefaults.AuthenticationScheme, _options.AuthenticationDisplayName, typeof(IISServerAuthenticationHandler)));
            }
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            _httpServerHandle = GCHandle.Alloc(this);

            _iisContextFactory = new IISContextFactory<TContext>(_bufferPool, application, _options, _iisFunctions);
            // Start the server by registering the callback
            _iisFunctions.RegisterCallbacks(_requestHandler, _shutdownHandler, _onAsyncCompletion, (IntPtr)_httpServerHandle, (IntPtr)_httpServerHandle);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: Drain pending requests

            // Stop all further calls back into managed code by unhooking the callback

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_httpServerHandle.IsAllocated)
            {
                _httpServerHandle.Free();
            }

            _bufferPool.Dispose();
        }

        private static REQUEST_NOTIFICATION_STATUS HandleRequest(IntPtr pHttpContext, IntPtr pvRequestContext)
        {
            // Unwrap the server so we can create an http context and process the request
            var server = (IISHttpServer)GCHandle.FromIntPtr(pvRequestContext).Target;

            var context = server._iisContextFactory.CreateHttpContext(pHttpContext);

            var task = context.ProcessRequestAsync();

            // This should never fail
            if (task.IsCompleted)
            {
                context.Dispose();
                return ConvertRequestCompletionResults(task.Result);
            }

            task.ContinueWith((t, state) => CompleteRequest((IISHttpContext)state, t), context);

            return REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        }

        private static bool HandleShutdown(IntPtr pvRequestContext)
        {
            var server = (IISHttpServer)GCHandle.FromIntPtr(pvRequestContext).Target;
            server._applicationLifetime.StopApplication();
            return true;
        }

        private static REQUEST_NOTIFICATION_STATUS OnAsyncCompletion(IntPtr pvManagedHttpContext, int hr, int bytes)
        {
            var context = (IISHttpContext)GCHandle.FromIntPtr(pvManagedHttpContext).Target;
            context.OnAsyncCompletion(hr, bytes);
            return REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        }

        private static void CompleteRequest(IISHttpContext context, Task<bool> completedTask)
        {
            // Post completion after completing the request to resume the state machine
            context.PostCompletion(ConvertRequestCompletionResults(completedTask.Result));

            // Dispose the context
            context.Dispose();
        }

        private static REQUEST_NOTIFICATION_STATUS ConvertRequestCompletionResults(bool success)
        {
            return success ? REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_CONTINUE
                                                     : REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
        }

        private class IISContextFactory<T> : IISContextFactory
        {
            private readonly IHttpApplication<T> _application;
            private readonly BufferPool _bufferPool;
            private readonly IISOptions _options;
            private readonly IISFunctions _iisFunctions;

            public IISContextFactory(BufferPool bufferPool, IHttpApplication<T> application, IISOptions options, IISFunctions iisFunctions)
            {
                _application = application;
                _bufferPool = bufferPool;
                _options = options;
                _iisFunctions = iisFunctions;
            }

            public IISHttpContext CreateHttpContext(IntPtr pHttpContext)
            {
                return new IISHttpContextOfT<T>(_bufferPool, _application, pHttpContext, _options, _iisFunctions);
            }
        }
    }

    // Over engineering to avoid allocations...
    internal interface IISContextFactory
    {
        IISHttpContext CreateHttpContext(IntPtr pHttpContext);
    }
}
