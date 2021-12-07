using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.ReactNative;
using Microsoft.ReactNative.Managed;

using Restup.Webserver;
using Restup.Webserver.File;
using Restup.Webserver.Http;
using Restup.WebServer.Logging;

namespace FPStaticServer
{
    [ReactModule("FPStaticServer")]
    class FPStaticServer
    {
        [ReactConstant]
        public double E = Math.E;

        [ReactConstant("Pi")]
        public double PI = Math.PI;


		public string ServerUrl { get; private set; }
		public string ServerRootPath { get; private set; }
		public int ServerPort { get; private set; }
		public bool ServerIsRunning { get; private set; }
		public bool LocalHostOnly { get; private set; }
		public bool KeepAlive { get; private set; }

		private HttpServer server;

		[ReactMethod("start")]
		public async void start(int port, string root, bool localOnly, bool keepAlive, IReactPromise<string> promise)
		{

			Debug.WriteLine(port);
			Debug.WriteLine(root);
			Debug.WriteLine(localOnly);

            ServerPort = port;
            ServerRootPath = root.Replace('/', '\\');
            LocalHostOnly = localOnly;
            KeepAlive = keepAlive;
            ServerUrl = "http://localhost";
            ServerIsRunning = false;

            var configuration = new HttpServerConfiguration()
              .ListenOnPort(port)
              .RegisterRoute(new StaticFileRouteHandler(ServerRootPath))
              .EnableCors();

            LogManager.SetLogFactory(new DebugLogFactory());
            server = new HttpServer(configuration);

            try
            {
                await server.StartServerAsync();
                ServerIsRunning = true;
                promise.Resolve(ServerUrl + ":" + ServerPort);
            }
            catch (Exception ex)
            {
                Stop();
                promise.Reject(new ReactError { Message = "COULDNT START SERVER : " + ex.Message });
            }
        }

		[ReactMethod("stop")]
		public void Stop()
		{
			if (server != null) server.StopServer();
		}

		[ReactMethod("isRunning")]
		public async void IsRunning(IReactPromise<bool> promise)
		{
			promise.Resolve(ServerIsRunning);
		}

		public void OnDestroy()
		{
			Stop();
		}

		public async void OnResume()
		{
			if (server != null && !ServerIsRunning)
			{
				await server.StartServerAsync();
			}
		}

		public void OnSuspend()
		{
			Stop();
		}

		public class DebugLogger : AbstractLogger
		{
			protected override bool IsLogEnabled(LogLevel trace)
			{
				// Ignore level, log everything
				return true;
			}

			protected override void LogMessage(string message, LogLevel loggingLevel, Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"{loggingLevel}: {message}");
				System.Diagnostics.Debug.WriteLine($"{ex}");
			}

			protected override void LogMessage(string message, LogLevel loggingLevel, params object[] args)
			{
				System.Diagnostics.Debug.WriteLine($"{loggingLevel}: {(string.Format(message, args))}");
			}
		}

		public class DebugLogFactory : ILogFactory
		{
			private ILogger _debugLogger;

			public DebugLogFactory()
			{
				_debugLogger = new DebugLogger();
			}

			public void Dispose()
			{
				_debugLogger = null;
			}

			public ILogger GetLogger(string name)
			{
				return _debugLogger;
			}

			public ILogger GetLogger<T>()
			{
				return _debugLogger;
			}
		}
	}

}