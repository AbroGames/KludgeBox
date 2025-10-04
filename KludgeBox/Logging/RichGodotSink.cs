using System.Globalization;
using Godot;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace KludgeBox.Logging;

public class RichGodotSink : ILogEventSink
{
    private readonly ITextFormatter _formatter;

    private static readonly Color PropertyColor = Colors.LightBlue;
    private static readonly Color ParameterColor = Colors.LightGreen;
    private static readonly Color ErrorColor = Colors.Red;

    public RichGodotSink(string outputTemplate, IFormatProvider formatProvider)
    {
        _formatter = new TemplateRenderer(outputTemplate, formatProvider);
    }

    public void Emit(LogEvent logEvent)
    {
        using TextWriter writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        writer.Flush();
        
        GD.PrintRich(writer.ToString());

        if (logEvent.Exception is null) return;
        
        if (logEvent.Level >= LogEventLevel.Error)
            GD.PushError(logEvent.Exception);
        else
            GD.PushWarning(logEvent.Exception);
    }

    private class TemplateRenderer : ITextFormatter
    {
        private delegate void Renderer(LogEvent logEvent, TextWriter output);

        private readonly Renderer[] _renderers;
        private readonly IFormatProvider _formatProvider;

        public TemplateRenderer(string outputTemplate, IFormatProvider formatProvider)
        {
            this._formatProvider = formatProvider;

            MessageTemplate template = new MessageTemplateParser().Parse(outputTemplate);
            _renderers = template.Tokens.Select(
            token => token switch
            {
                TextToken textToken => (evt, output) => RenderText(textToken, evt, output),
                PropertyToken propertyToken => propertyToken.PropertyName switch
                {
                    OutputProperties.LevelPropertyName
                        => (logEvent, output) => output.Write(FormatLogLevel(logEvent.Level)),
                    OutputProperties.MessagePropertyName
                        => (logEvent, output) => RenderMessageWithColor(logEvent, output, formatProvider),
                    OutputProperties.NewLinePropertyName
                        => (_, _) => {},
                    OutputProperties.TimestampPropertyName
                        => RenderTimestamp(propertyToken.Format),
                    _
                        => RenderProperty(propertyToken.PropertyName, propertyToken.Format),
                },
                _ => null,
            }
            ).OfType<Renderer>().ToArray();
        }

        private void RenderText(TextToken textToken, LogEvent logEvent, TextWriter output)
        {
            string prefix = "";
            string postfix = "";

            if (logEvent.Level >= LogEventLevel.Error)
            {
                prefix = $"[color={ErrorColor.ToHtml()}]";
                postfix = "[/color]";
            } 
            
            output.Write($"{prefix}{textToken.Text}{postfix}");
        }
        private string FormatLogLevel(LogEventLevel logLevel, string text = null)
        {
            string color = logLevel switch
            {
                LogEventLevel.Debug => Colors.DarkGray.ToHtml(),
                LogEventLevel.Information => Colors.White.ToHtml(),
                LogEventLevel.Warning => Colors.Yellow.ToHtml(),
                LogEventLevel.Error => Colors.Red.ToHtml(),
                LogEventLevel.Fatal => Colors.Purple.ToHtml(),
                _ => Colors.LightGray.ToHtml(),
            };

            var logLevelString = $"[color=#{color}]{text ?? logLevel.ToString()}[/color]";
            
            return logLevelString;
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            foreach (var renderer in _renderers)
                renderer.Invoke(logEvent, output);
        }

        private Renderer RenderTimestamp(string format)
        {
            Func<LogEvent, string> f = _formatProvider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter
                ? (logEvent) => formatter.Format(format, logEvent.Timestamp, _formatProvider)
                : (logEvent) => logEvent.Timestamp.ToString(format, _formatProvider ?? CultureInfo.InvariantCulture);

            Func<LogEvent, string> fw = (logEvent) => FormatLogLevel(logEvent.Level, f(logEvent));
            
            return (logEvent, output) => output.Write(fw(logEvent));
        }
        
        private void RenderMessageWithColor(LogEvent logEvent, TextWriter output, IFormatProvider formatProvider)
        {
            foreach (var token in logEvent.MessageTemplate.Tokens)
            {
                switch (token)
                {
                    case TextToken textToken:
                        RenderText(textToken, logEvent, output);
                        break;
                    case PropertyToken propertyToken:
                        if (logEvent.Properties.TryGetValue(propertyToken.PropertyName, out var propertyValue))
                        {
                            var strWriter = new StringWriter();
                            propertyValue.Render(strWriter, propertyToken.Format, formatProvider);
                            var rawText = strWriter.ToString();

                            if (rawText.StartsWith("\"") && rawText.EndsWith("\""))
                            {
                                rawText = rawText.Substring(1, rawText.Length - 2);
                            }
                            
                            output.Write($"[color={MixError(ParameterColor, logEvent.Level >= LogEventLevel.Error).ToHtml()}]{rawText}[/color]");
                        }
                        else 
                        {
                            output.Write($"[color=#{ErrorColor.ToHtml()}]??{propertyToken.PropertyName}??[/color]");
                        }
                        break;
                }
            }
        }

        private Renderer RenderProperty(string propertyName, string format)
        {
            return delegate (LogEvent logEvent, TextWriter output)
            {
                if (logEvent.Properties.TryGetValue(propertyName, out var propertyValue))
                {
                    var strWriter = new StringWriter();
                    propertyValue.Render(strWriter, format, _formatProvider);
                    var rawText = strWriter.ToString();

                    // Удаляем ведущие и конечные кавычки, если это строка
                    if (rawText.StartsWith("\"") && rawText.EndsWith("\""))
                    {
                        rawText = rawText.Substring(1, rawText.Length - 2);
                    }

                    output.Write($"[color=#{MixError(PropertyColor, logEvent.Level >= LogEventLevel.Error).ToHtml()}]{rawText}[/color]");
                }
            };
        }

        private Color MixError(Color source, bool isError)
        {
            if (isError)
            {
                return source.Lerp(ErrorColor, 0.5f);
            }
            
            return source;
        }
    }
}

public static partial class GodotSinkExtensions
{
    private const string DefaultGodotSinkOutputTemplate = "[{Timestamp:HH:mm:ss}] {Message:lj}";

    public static LoggerConfiguration GodotRich(this LoggerSinkConfiguration configuration,
                                            string outputTemplate = DefaultGodotSinkOutputTemplate,
                                            IFormatProvider formatProvider = null)
    {
        return configuration.Sink(new RichGodotSink(outputTemplate, formatProvider));
    }
}