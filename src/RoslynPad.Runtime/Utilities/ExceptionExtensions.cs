using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace RoslynPad.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="Exception"/>.
    /// </summary>
    internal static class ExceptionExtensions
    {
        private const string EndOfInnerExceptionStack = "--- End of inner exception stack trace ---";
        private const string AggregateExceptionFormatString = "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}";
        private const string AsyncStackTraceExceptionData = "AsyncFriendlyStackTrace";

        private static Func<Exception, string> GetStackTraceString => 
            ReflectionUtil.GenerateGetField<Exception, string>("_stackTraceString");

        private static readonly Func<Exception, string> GetRemoteStackTraceString =
            ReflectionUtil.GenerateGetField<Exception, string>("_remoteStackTraceString");

        /// <summary>
        /// Gets an async-friendly <see cref="Exception"/> string using <see cref="StackTraceExtensions"/>.
        /// Includes special handling for <see cref="AggregateException"/>s.
        /// </summary>
        /// <param name="exception">The exception to format.</param>
        /// <returns>An async-friendly string representation of an <see cref="Exception"/>.</returns>
        public static string ToAsyncString(this Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            if (exception is AggregateException aggregate)
            {
                return ToAsyncAggregateString(exception, aggregate.InnerExceptions);
            }

            if (exception is ReflectionTypeLoadException typeLoadException)
            {
                return ToAsyncAggregateString(exception, typeLoadException.LoaderExceptions);
            }

            return ToAsyncStringCore(exception, includeMessageOnly: false);
        }

        /// <summary>
        /// Prepares an <see cref="Exception"/> for serialization by including the async-friendly
        /// stack trace as additional <see cref="Exception.Data"/>.
        /// Note that both the original and the new stack traces will be serialized.
        /// This method operates recursively on all inner exceptions,
        /// including ones in an <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void PrepareForAsyncSerialization(this Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            if (exception.Data[AsyncStackTraceExceptionData] != null ||
                GetStackTraceString(exception) != null)
                return;

            exception.Data[AsyncStackTraceExceptionData] = GetAsyncStackTrace(exception);

            if (exception is AggregateException aggregate)
            {
                foreach (var innerException in aggregate.InnerExceptions)
                {
                    innerException.PrepareForAsyncSerialization();
                }
            }
            else
            {
                exception.InnerException?.PrepareForAsyncSerialization();
            }
        }

        private static string ToAsyncAggregateString(Exception exception, IList<Exception> inner)
        {
            var s = ToAsyncStringCore(exception, includeMessageOnly: true);
            for (int i = 0; i < inner.Count; i++)
            {
                s = string.Format(CultureInfo.InvariantCulture, AggregateExceptionFormatString, s,
                    Environment.NewLine, i, inner[i].ToAsyncString(), "<---", Environment.NewLine);
            }
            return s;
        }

        private static string ToAsyncStringCore(Exception exception, bool includeMessageOnly)
        {
            var message = exception.Message;
            var className = exception.GetType().ToString();
            var s = message.Length <= 0 ? className : className + ": " + message;

            var innerException = exception.InnerException;
            if (innerException != null)
            {
                if (includeMessageOnly)
                {
                    do
                    {
                        s += " ---> " + innerException.Message;
                        innerException = innerException.InnerException;
                    } while (innerException != null);
                }
                else
                {
                    s += " ---> " + innerException.ToAsyncString() + Environment.NewLine +
                         "   " + EndOfInnerExceptionStack;
                }
            }

            s += Environment.NewLine + GetAsyncStackTrace(exception);

            return s;
        }

        private static string GetAsyncStackTrace(Exception exception)
        {
            var stackTrace = exception.Data[AsyncStackTraceExceptionData] ??
                             GetStackTraceString(exception) ??
                             new StackTrace(exception, true).ToAsyncString();
            var remoteStackTrace = GetRemoteStackTraceString(exception);
            return remoteStackTrace + stackTrace;
        }
    }
}