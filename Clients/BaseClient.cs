﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Raavel.ConfigurationStorage;
using Raavel.Handlers;
using System.Web;

#if !NET35
using System.Threading.Tasks;
#endif

namespace Raavel.Clients
{
    /// <summary>
    /// The main class used to encapsulate a client to Raavel
    /// </summary>
    public class BaseClient
    {
        /// <summary>
        /// The notifier used by the client to send notifications to Raavel
        /// </summary>
        internal Notifier notifier;

        /// <summary>
        /// The handler used to handle app level exceptions and notify Raavel accordingly
        /// </summary>
        internal UnhandledExceptionHandler unhandledExceptionHandler = new UnhandledExceptionHandler();

#if !NET35
        /// <summary>
        /// The handler used to handle task app level exceptions and notify Raavel accordingly
        /// </summary>
        internal TaskExceptionHandler taskExceptionHandler = new TaskExceptionHandler();
#endif

        /// <summary>
        /// Gets the configuration of the client, allowing users to config it
        /// </summary>
        public Configuration Config { get; private set; }

        /// <summary>
        /// The regex that validates an API key
        /// </summary>
        private Regex apiRegex = new Regex("^[a-fA-F0-9]{32}$");

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseClient"/> class. Will use all the default settings and will 
        /// automatically hook into uncaught exceptions.
        /// </summary>
        /// <param name="apiKey">The Raavel API key to send notifications with</param>
        public BaseClient(string apiKey)
            : this(new BaseStorage(apiKey))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseClient"/> class. Provides the option to automatically 
        /// hook into uncaught exceptions. Allows injection of dependant classes
        /// </summary>
        /// <param name="configStorage">The configuration of the client</param>
        public BaseClient(IConfigurationStorage configStorage)
        {
            Initialize(configStorage);
        }

        public BaseClient()
        {
            Initialize(ConfigSection.Settings);
        }

        /// <summary>
        /// Enables auto notification, using the default exception handler
        /// </summary>
        public void StartAutoNotify()
        {
            unhandledExceptionHandler.InstallHandler(HandleDefaultException);
#if !NET35
            taskExceptionHandler.InstallHandler(HandleDefaultException);
#endif
        }

        /// <summary>
        /// Disables auto notification, removing the handler
        /// </summary>
        public void StopAutoNotify()
        {
            unhandledExceptionHandler.UninstallHandler();
#if !NET35
            taskExceptionHandler.UninstallHandler();
#endif
        }

        /// <summary>
        /// Notifies Raavel of an exception
        /// </summary>
        /// <param name="exception">The exception to send to Raavel</param>
        public void Notify(Exception exception)
        {
            var error = new Event(exception);
            error.Severity = Severity.Warning;
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an exception
        /// </summary>
        /// <param name="message">The message to send to Raavel</param>
        public void Notify(string message)
        {
            Message exception = new Message(message);
            var error = new Event(exception);
            error.Severity = Severity.Info;
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an exception, with an associated severity level
        /// </summary>
        /// <param name="exception">The exception to send to Raavel</param>
        /// <param name="severity">The associated severity of the exception</param>
        public void Notify(Exception exception, Severity severity)
        {
            var error = new Event(exception);
            error.Severity = severity;
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an exception, with an associated severity level
        /// </summary>
        /// <param name="message">The message to send to Raavel</param>
        /// <param name="severity">The associated severity of the exception</param>
        public void Notify(string message, Severity severity)
        {
            Message exception = new Message(message);
            var error = new Event(exception);
            error.Severity = severity;
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an exception with associated meta data
        /// </summary>
        /// <param name="exception">The exception to send to Raavel</param>
        /// <param name="data">The metadata to send with the exception</param>
        public void Notify(Exception exception, Metadata data)
        {
            var error = new Event(exception);
            error.Severity = Severity.Warning;
            error.Metadata.AddMetadata(data);
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an exception with associated meta data
        /// </summary>
        /// <param name="message">The message to send to Raavel</param>
        /// <param name="data">The metadata to send with the exception</param>
        public void Notify(string message, Metadata data)
        {
            Message exception = new Message(message);
            var error = new Event(exception);
            error.Severity = Severity.Info;
            error.Metadata.AddMetadata(data);
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an exception, with an associated severity level and meta data
        /// </summary>
        /// <param name="exception">The exception to send to Raavel</param>
        /// <param name="severity">The associated severity of the exception</param>
        /// <param name="data">The metadata to send with the exception</param>
        public void Notify(Exception exception, Severity severity, Metadata data)
        {
            var error = new Event(exception);
            error.Severity = severity;
            error.Metadata.AddMetadata(data);
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an exception, with an associated severity level and meta data
        /// </summary>
        /// <param name="message">The message to send to Raavel</param>
        /// <param name="severity">The associated severity of the exception</param>
        /// <param name="data">The metadata to send with the exception</param>
        public void Notify(string message, Severity severity, Metadata data)
        {
            Message exception = new Message(message);
            var error = new Event(exception);
            error.Severity = severity;
            error.Metadata.AddMetadata(data);
            Notify(error);
        }

        /// <summary>
        /// Notifies Raavel of an error event
        /// </summary>
        /// <param name="errorEvent">The event to report on</param>
        public void Notify(Event errorEvent)
        {
            // Do nothing if we don't have an error event
            if (errorEvent == null)
                return;

            // Do nothing if we are not a release stage that notifies
            if (!Config.IsNotifyReleaseStage())
                return;

            // Ignore the error if the exception it contains is one of the classes to ignore
            if (errorEvent.Exception == null ||
                Config.IsClassToIgnore(errorEvent.Exception.GetType().Name))
                return;

            Config.RunInternalBeforeNotifyCallbacks(errorEvent);
            Config.AddConfigToEvent(errorEvent);

            if (!Config.RunBeforeNotifyCallbacks(errorEvent))
                return;

            notifier.Send(errorEvent);
        }

#if !NET35
        // Async variants of the Notify functions above
        public Task NotifyAsync(Exception error)
        {
            return Task.Factory.StartNew(() =>
            {
                Notify(error);
            });
        }

        public Task NotifyAsync(Exception error, Metadata metadata)
        {
            return Task.Factory.StartNew(() =>
            {
                Notify(error, metadata);
            });
        }

        public Task NotifyAsync(Exception error, Severity severity)
        {
            return Task.Factory.StartNew(() =>
            {
                Notify(error, severity);
            });
        }

        public Task NotifyAsync(Exception error, Severity severity, Metadata metadata)
        {
            return Task.Factory.StartNew(() =>
            {
                Notify(error, severity, metadata);
            });
        }
#endif

        /// <summary>
        /// Initialize the client with dependencies
        /// </summary>
        /// <param name="configStorage">The configuration to use</param>
        protected void Initialize(IConfigurationStorage configStorage)
        {
            if (configStorage == null || string.IsNullOrEmpty(configStorage.ApiKey) || !apiRegex.IsMatch(configStorage.ApiKey))
            {
                Logger.Error("You must provide a valid Raavel API key");
                throw new ArgumentException("You must provide a valid Raavel API key");
            }
            else
            {
                Config = new Configuration(configStorage);
                notifier = new Notifier(Config);
                Config.BeforeNotify(error =>
                {
                    if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Params != null)
                    {
                        if (String.IsNullOrEmpty(error.Context) && HttpContext.Current.Request.Path != null)
                        {
                            error.Context = HttpContext.Current.Request.Path.ToString();
                        }

                        if (String.IsNullOrEmpty(error.UserId))
                        {
                            if (!String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                            {
                                error.UserId = HttpContext.Current.User.Identity.Name;
                            }
                            else if (HttpContext.Current.Session != null && !String.IsNullOrEmpty(HttpContext.Current.Session.SessionID))
                            {
                                if (HttpContext.Current.Session.IsNewSession){
                                    HttpContext.Current.Session[HttpContext.Current.Session.SessionID] = "";
                                }

                                error.UserId = HttpContext.Current.Session.SessionID;
                            }
                            else
                            {
                                error.UserId = HttpContext.Current.Request.UserHostAddress;
                            }
                        }
                    }
                });

                // Install a default exception handler with this client
                if (Config.AutoNotify)
                    StartAutoNotify();

                // Set up some defaults for all clients
                if (Debugger.IsAttached && String.IsNullOrEmpty(Config.ReleaseStage)) Config.ReleaseStage = "development";
#if !MONO
                // Use the application version defined for the Click-Once application, if it is one
                if (String.IsNullOrEmpty(Config.AppVersion) &&
                    System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    Config.AppVersion = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                }
#endif
                if (String.IsNullOrEmpty(Config.AppVersion) && Assembly.GetEntryAssembly() != null)
                {
                    Config.AppVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
                }

                Initialized();
            }
        }

        /// <summary>
        /// Allows subclasses to have a centralized initialize function which is called once the base
        /// client has finished initializing.
        /// </summary>
        protected void Initialized()
        {
            // The base client doesn't need any further initialisation
        }

        /// <summary>
        /// The default handler to use when we receive unmanaged exceptions
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="runtimeEnding">True if the unmanaged exception handler indicates that the runtime will end</param>
        protected void HandleDefaultException(Exception exception, bool runtimeEnding)
        {
            var error = new Event(exception, runtimeEnding);
            Notify(error);
        }
    }
}
