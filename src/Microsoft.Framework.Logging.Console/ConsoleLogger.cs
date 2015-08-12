// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Text;
using Microsoft.Framework.Logging.Console.Internal;

namespace Microsoft.Framework.Logging.Console
{


    public class ConsoleLogger : ILogger
    {
        private const int _indentation = 2;
        private readonly string _name;
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly object _lock = new object();

        public ConsoleColors ConsoleColors { get; private set; }

        public Action<object, LogLevel, IConsole> SetConsole { get; set; }



        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter)
        {
            ConsoleColors = new ConsoleColors();
            _name = name;
            _filter = filter ?? ((category, logLevel) => true);
            Console = new LogConsole();
        }

        public IConsole Console { get; set; }
        protected string Name { get { return _name; } }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var message = string.Empty;
            var values = state as ILogValues;
            if (formatter != null)
            {
                message = formatter(state, exception);
            }
            else if (values != null)
            {
                var builder = new StringBuilder();
                FormatLogValues(
                    builder,
                    values,
                    level: 1,
                    bullet: false);
                message = builder.ToString();
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }
            }
            else
            {
                message = LogFormatter.Formatter(state, exception);
            }
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            lock (_lock)
            {
                SetConsoleColor(logLevel);

                if (SetConsole != null) SetConsole(state, logLevel, Console);

                try
                {
                    Console.WriteLine(FormatMessage(logLevel, message));
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        private string FormatMessage(LogLevel logLevel, string message)
        {
            var logLevelString = GetRightPaddedLogLevelString(logLevel);
            return $"{logLevelString}: [{_name}] {message}";
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(_name, logLevel);
        }

        // sets the console text color to reflect the given LogLevel
        private void SetConsoleColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    if (ConsoleColors.CriticalBackground != null) Console.BackgroundColor = (ConsoleColor)ConsoleColors.CriticalBackground;
                    if (ConsoleColors.CriticalForeground != null) Console.ForegroundColor = (ConsoleColor)ConsoleColors.CriticalForeground;
                    break;
                case LogLevel.Error:
                    if (ConsoleColors.ErrorForeground != null) Console.ForegroundColor = (ConsoleColor)ConsoleColors.ErrorForeground;
                    if (ConsoleColors.ErrorBackground != null) Console.BackgroundColor = (ConsoleColor)ConsoleColors.ErrorBackground; //
                    break;
                case LogLevel.Warning:
                    if (ConsoleColors.WarningForeground != null) Console.ForegroundColor = (ConsoleColor)ConsoleColors.WarningForeground;
                    if (ConsoleColors.WarningBackground != null) Console.BackgroundColor = (ConsoleColor)ConsoleColors.WarningBackground; //
                    break;
                case LogLevel.Information:
                    if (ConsoleColors.InformationForeground != null) Console.ForegroundColor = (ConsoleColor)ConsoleColors.InformationForeground;
                    if (ConsoleColors.InformationBackground != null) Console.BackgroundColor = (ConsoleColor)ConsoleColors.InformationBackground; //
                    break;
                case LogLevel.Verbose:
                    if (ConsoleColors.VerboseForeground != null) Console.ForegroundColor = (ConsoleColor)ConsoleColors.VerboseForeground; //
                    if (ConsoleColors.VerboseBackground != null) Console.BackgroundColor = (ConsoleColor)ConsoleColors.VerboseBackground; //
                    break;
                case LogLevel.Debug:
                    if (ConsoleColors.DebugForeground != null) Console.ForegroundColor = (ConsoleColor)ConsoleColors.DebugForeground; //
                    if (ConsoleColors.DebugBackground != null) Console.BackgroundColor = (ConsoleColor)ConsoleColors.DebugBackground; //
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

        }

        public IDisposable BeginScopeImpl(object state)
        {
            return new NoopDisposable();
        }

        private void FormatLogValues(StringBuilder builder, ILogValues logValues, int level, bool bullet)
        {
            var values = logValues.GetValues();
            if (values == null)
            {
                return;
            }
            var isFirst = true;
            foreach (var kvp in values)
            {
                builder.AppendLine();
                if (bullet && isFirst)
                {
                    builder.Append(' ', level * _indentation - 1)
                           .Append('-');
                }
                else
                {
                    builder.Append(' ', level * _indentation);
                }
                builder.Append(kvp.Key)
                       .Append(": ");
                if (kvp.Value is IEnumerable && !(kvp.Value is string))
                {
                    foreach (var value in (IEnumerable)kvp.Value)
                    {
                        if (value is ILogValues)
                        {
                            FormatLogValues(
                                builder,
                                (ILogValues)value,
                                level + 1,
                                bullet: true);
                        }
                        else
                        {
                            builder.AppendLine()
                                   .Append(' ', (level + 1) * _indentation)
                                   .Append(value);
                        }
                    }
                }
                else if (kvp.Value is ILogValues)
                {
                    FormatLogValues(
                        builder,
                        (ILogValues)kvp.Value,
                        level + 1,
                        bullet: false);
                }
                else
                {
                    builder.Append(kvp.Value);
                }
                isFirst = false;
            }
        }

        private static string GetRightPaddedLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return "debug   ";
                case LogLevel.Verbose:
                    return "verbose ";
                case LogLevel.Information:
                    return "info    ";
                case LogLevel.Warning:
                    return "warning ";
                case LogLevel.Error:
                    return "error   ";
                case LogLevel.Critical:
                    return "critical";
                default:
                    return "unknown ";
            }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    public class ConsoleColors
    {


        public ConsoleColor? WarningForeground { get; set; }
        public ConsoleColor? WarningBackground { get; set; }
        public ConsoleColor? VerboseForeground { get; set; }
        public ConsoleColor? VerboseBackground { get; set; }
        public ConsoleColor? ErrorForeground { get; set; }
        public ConsoleColor? ErrorBackground { get; set; }
        public ConsoleColor? InformationForeground { get; set; }
        public ConsoleColor? InformationBackground { get; set; }
        public ConsoleColor? CriticalForeground { get; set; }
        public ConsoleColor? CriticalBackground { get; set; }
        public ConsoleColor? DebugForeground { get; set; }
        public ConsoleColor? DebugBackground { get; set; }

        public ConsoleColors()
        {
            CriticalBackground = ConsoleColor.Red;
            CriticalForeground = ConsoleColor.White;

            ErrorForeground = ConsoleColor.Red;
            ErrorBackground = null; // don't change

            WarningForeground = ConsoleColor.Yellow;
            WarningBackground = null; // don't change

            InformationForeground = ConsoleColor.White;
            InformationBackground = null; // don't change

            VerboseForeground = ConsoleColor.Gray;
            VerboseBackground = null; // don't change

            DebugForeground = ConsoleColor.Gray;
            DebugBackground = null; // don't change

            WarningForeground = ConsoleColor.White;
        }
    }
}