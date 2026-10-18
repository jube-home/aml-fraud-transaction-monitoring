/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Diagnostics;
using log4net;
using Microsoft.Extensions.Logging;

namespace Jube.Service.Observability
{
    public sealed class Log4NetLogger(ILog log) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace or LogLevel.Debug => log.IsDebugEnabled,
                LogLevel.Information => log.IsInfoEnabled,
                LogLevel.Warning => log.IsWarnEnabled,
                LogLevel.Error => log.IsErrorEnabled,
                LogLevel.Critical => log.IsFatalEnabled,
                _ => false
            };
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    log.Debug(message, exception);
                    break;
                case LogLevel.Information:
                    log.Info(message, exception);
                    break;
                case LogLevel.Warning:
                    log.Warn(message, exception);
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, message);
                    break;
                default:
                    log.Error(message, exception);
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, message);
                    break;
            }
        }
    }
}