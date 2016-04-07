﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Raavel.Payload;
using System.Web;

namespace Raavel
{
    internal class NotificationFactory
    {
        private const string ExpDetailsTabName = "exceptionDetails";
        private const string ServerVariablesTabName = "serverVariables";
        private const string formValuesTabName = "formValues";
        private const string browserValuesTabName = "browserValues";
        private Configuration Config { get; set; }

        public NotificationFactory(Configuration config)
        {
            Config = config;
        }

        public Notification CreateFromError(Event error)
        {
            var notification = CreateNotificationBase();
            var eventInfo = CreateEventInfoBase(error);
            RecursiveAddExceptionInfo(error, error.Exception, error.CallTrace, eventInfo);
            if (eventInfo == null)
                return null;

            notification.Events.Add(eventInfo);
            return notification;
        }

        private Notification CreateNotificationBase()
        {
            var notifierInfo = new NotifierInfo
            {
                Name = Notifier.Name,
                Version = Notifier.Version,
                Url = Notifier.Url.AbsoluteUri
            };

            var notification = new Notification
            {
                ApiKey = Config.ApiKey,
                Notifier = notifierInfo,
                Events = new List<EventInfo>()
            };
            return notification;
        }

        private EventInfo CreateEventInfoBase(Event errorData)
        {
            var appInfo = new AppInfo
            {
                Version = Config.AppVersion,
                ReleaseStage = Config.ReleaseStage,
                AppArchitecture = Diagnostics.AppArchitecture,
                ClrVersion = Diagnostics.ClrVersion
            };

            var userInfo = new UserInfo
            {
                Id = errorData.UserId,
                Email = errorData.UserEmail,
                Name = errorData.UserName
            };

            var deviceInfo = new DeviceInfo
            {
                OSVersion = Diagnostics.DetectedOSVersion,
                ServicePack = Diagnostics.ServicePack,
                OSArchitecture = Diagnostics.OSArchitecture,
                ProcessorCount = Diagnostics.ProcessorCount,
                MachineName = Diagnostics.MachineName,
                HostName = Diagnostics.HostName
            };

            var eventInfo = new EventInfo
            {
                App = appInfo,
                Device = deviceInfo,
                Severity = errorData.Severity,
                User = userInfo,
                Context = errorData.Context,
                GroupingHash = errorData.GroupingHash,
                Exceptions = new List<ExceptionInfo>()
            };
            return eventInfo;
        }

        /// <summary>
        /// Starting with an exception, will recursively add exception infos to event infos.
        /// If an aggregate exception is encountered, all inner exceptions will be added
        /// </summary>
        /// <param name="error">The error to create new event infos</param>
        /// <param name="exp">The exception to add to the current event</param>
        /// <param name="callStack">The stack of notify call</param>
        /// <param name="currentEvent">The current event info to add the exception to</param>
        private void RecursiveAddExceptionInfo(Event error,
                                               Exception exp,
                                               StackTrace callStack,
                                               EventInfo currentEvent)
        {
            // If we have no more exceptions, return the generated event info
            if (exp == null) return;

            // Parse the exception and add it to the current event info stack
            var expInfo = ExceptionParser.GenerateExceptionInfo(exp, callStack, Config);
            if (expInfo != null)
                currentEvent.Exceptions.Add(expInfo);

            // If the exception has no inner exception, then we are finished. Generate metadata for the last exception
            // and set the final metadata for the event info
            if (exp.InnerException == null)
            {
                FinaliseEventInfo(currentEvent, error, exp);
            }
            else
            {
#if !NET35
                // Check if the current exception contains more than 1 inner exception
                // if it does, then recurse through them seperately
                var aggExp = exp as AggregateException;
                if (aggExp != null && aggExp.InnerExceptions.Count > 1)
                {
                    foreach (var inner in aggExp.InnerExceptions)
                    {
                        RecursiveAddExceptionInfo(error, inner, callStack, currentEvent);
                    }
                }
                else
                {
                    // Otherwise just move to the next inner exception
                    RecursiveAddExceptionInfo(error, exp.InnerException, callStack, currentEvent);
                }
#else
                RecursiveAddExceptionInfo(error, exp.InnerException, callStack, currentEvent);
#endif
            }
        }

        /// <summary>
        /// Finalises the event info by adding final metadata 
        /// </summary>
        /// <param name="eventInfo">The event info to finalise</param>
        /// <param name="error">The responsible event</param>
        /// <param name="lastException">The root exception of the event info</param>
        private void FinaliseEventInfo(EventInfo eventInfo, Event error, Exception lastException)
        {
            var expMetaData = new Metadata();

            expMetaData.AddToTab(ExpDetailsTabName, "runtimeEnding", error.IsRuntimeEnding);

            if (lastException.HelpLink != null)
                expMetaData.AddToTab(ExpDetailsTabName, "helpLink", lastException.HelpLink);

            if (lastException.Source != null)
                expMetaData.AddToTab(ExpDetailsTabName, "source", lastException.Source);

            if (lastException.TargetSite != null)
                expMetaData.AddToTab(ExpDetailsTabName, "targetSite", lastException.TargetSite);

            HttpContext context = HttpContext.Current;
            if (context != null)
            {
                HttpRequest request = null;
                try
                {
                    request = context.Request;
                }
                catch (HttpException ex)
                {
                    Trace.WriteLine("Error retrieving HttpRequest {0}", ex.Message);
                }

                if (request != null)
                {
                    foreach (var _key in request.ServerVariables.AllKeys)
                    {
                        expMetaData.AddToTab(ServerVariablesTabName, _key, request.ServerVariables[_key]);
                    }

                    foreach (var _key in request.Form.AllKeys)
                    {
                        expMetaData.AddToTab(formValuesTabName, _key, request.Form[_key]);
                    }

                    expMetaData.AddToTab(browserValuesTabName, "BROWSER_NAME", request.Browser.Browser);
                    expMetaData.AddToTab(browserValuesTabName, "BROWSER_PLATFORM", request.Browser.Platform);
                    expMetaData.AddToTab(browserValuesTabName, "BROWSER_MAJOR_VERSION", request.Browser.MajorVersion);
                    expMetaData.AddToTab(browserValuesTabName, "BROWSER_MINOR_VERSION", request.Browser.MinorVersion);
                    expMetaData.AddToTab(browserValuesTabName, "BROWSER_IS_CRAWLER", request.Browser.Crawler);
                    expMetaData.AddToTab(browserValuesTabName, "BROWSER_SUPPORT_COOKIES", request.Browser.Cookies);
                    expMetaData.AddToTab(browserValuesTabName, "BROWSER_EMCA_SCRIPT_VERSION", request.Browser.EcmaScriptVersion);
                }

                if (String.IsNullOrEmpty(error.UserId))
                {
                    if (!String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                    {
                        error.UserId = HttpContext.Current.User.Identity.Name;
                    }
                    else if (HttpContext.Current.Session != null && !String.IsNullOrEmpty(HttpContext.Current.Session.SessionID))
                    {
                        error.UserId = HttpContext.Current.Session.SessionID;
                    }
                    else
                    {
                        error.UserId = HttpContext.Current.Request.UserHostAddress;
                    }
                }
            }

            var metaData = Metadata.CombineMetadata(Config.Metadata, error.Metadata, expMetaData);
            metaData.FilterEntries(Config.IsEntryFiltered);
            eventInfo.Metadata = metaData.MetadataStore;
        }
    }
}
