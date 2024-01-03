using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    ///  Provide helper methods to raise events for testing purpose
    /// </summary>
    internal static class RuntimeTestHelper
    {

        /// <summary>
        /// Tracer.
        /// Use tracing or logging only from AIP.
        /// </summary>
        private static readonly Tracer tracer = Tracer.CreateTracer(typeof(RuntimeTestHelper));

        internal static EventWaitHandle CreateEvent(string eventName, bool createLocalEvent = true)
        {
            return CreateEvent(eventName, out bool _, createLocalEvent);
        }

        internal static EventWaitHandle CreateEvent(
            string eventName,
            out bool created,
            bool createLocalEvent = true
            )
        {
            // For providing secure access control.

            // This method is called only when System Bootstrapping right now.
            // Locking should be implemented before calling this method from multiple threads.
            EventWaitHandleSecurity eventWaitHandleSecurity = null;

            if (!createLocalEvent)
            {
                eventWaitHandleSecurity = new EventWaitHandleSecurity();
                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                NTAccount userAccount = sid.Translate(typeof(NTAccount)) as NTAccount;
                string everyOne = userAccount == null ? "Everyone" : userAccount.Value;
                EventWaitHandleAccessRule rule = new EventWaitHandleAccessRule(
                    everyOne,
                    EventWaitHandleRights.FullControl,
                    AccessControlType.Allow
                    );
                eventWaitHandleSecurity.AddAccessRule(rule);
            }

            // event wait handle is created with secure access control.
            var nativeEvent =
                new EventWaitHandle(
                    true,
                    EventResetMode.ManualReset,
                    eventName,
                    out created,
                    eventWaitHandleSecurity
                    );

            return nativeEvent;
        }

        /// <summary>
        /// Signals an inter-process event
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="isLocalEvent">Boolean indicating if the event is a system wide event and
        /// should be accessible outside the process
        /// </param>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This is required only during IPAP testing")]
        internal static void SignalEvent(string eventName, bool isLocalEvent = true)
        {
            try
            {
                var nativeEvent = CreateEvent(eventName, isLocalEvent);
                nativeEvent.Set();
                nativeEvent.Dispose();
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Reset an inter-process event
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="isLocalEvent">Boolean indicating if the event is a system wide event and
        /// should be accessible outside the process
        /// </param>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This is required only during IPAP testing")]
        internal static void ResetEvent(string eventName, bool isLocalEvent = true)
        {
            try
            {
                var nativeEvent = CreateEvent(eventName, isLocalEvent);
                nativeEvent.Reset();
                nativeEvent.Dispose();
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Raises an event for the test so that the test code could induce some failure
        /// or test scenario for testing.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="isLocalEvent"></param>
        internal static void NotifyAndWaitForTestEvent(string eventName, bool isLocalEvent = true)
        {

            //if (PlatformDeployment.IsProductDeployment)
            //{
            //    return;
            //}

            var continueEventName = eventName + "_Continue";
            using (var continueEvent = CreateEvent(continueEventName, out var createdNew, isLocalEvent))
            {
                var eventObj = CreateEvent(eventName, isLocalEvent);
                tracer.TraceInfo("Raised event: " + eventName);
                eventObj.Set();
                eventObj.Dispose();
                if (createdNew)
                {
                    tracer.TraceInfo("Not waiting for event " + continueEventName);
                    return;
                }
                tracer.TraceInfo("Waiting for event: " + continueEventName);
                continueEvent.WaitOne();
            }
        }
    }
}
