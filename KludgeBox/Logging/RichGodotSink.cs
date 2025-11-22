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
    private readonly Func<bool> _godotPushEnableProvider;

    private static readonly Color PropertyColor = Colors.DodgerBlue;
    private static readonly Color ParameterColor = Colors.Green;
    private static readonly Color ErrorColor = Colors.Red;
    
    private static readonly int LevelMaxLength = Enum.GetNames(typeof(LogEventLevel)).Max(s => s.Length);
    private static readonly int PropertyMaxLength = 25;

    public RichGodotSink(string outputTemplate, IFormatProvider formatProvider, Func<bool> godotPushEnableProvider)
    {
        _formatter = new TemplateRenderer(outputTemplate, formatProvider);
        _godotPushEnableProvider = godotPushEnableProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        using TextWriter writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        writer.Flush();
        string logText = writer.ToString();
        
        GD.PrintRich(logText);
        if (logEvent.Exception is not null) GD.Print(logEvent.Exception); 
        
        // Если сообщение по уровню критичности ниже Warning, то заканчиваем обработку
        if (logEvent.Level < LogEventLevel.Warning) return;
        // Если функция Godot Push отключена, то заканчиваем обработку
        if (_godotPushEnableProvider == null || !_godotPushEnableProvider()) return;

        using StringWriter godotWriter = new StringWriter();
        logEvent.RenderMessage(godotWriter);
        string godotText = godotWriter.ToString();

        if (logEvent.Level is LogEventLevel.Warning)
        {
            if (logEvent.Exception is null) 
                GD.PushWarning(godotText);
            else
                GD.PushWarning(godotText + " | ", logEvent.Exception);
        }
        
        if (logEvent.Level is LogEventLevel.Error or LogEventLevel.Fatal)
        {
            if (logEvent.Exception is null) 
                GD.PushError(godotText);
            else
                GD.PushError(godotText + " | ", logEvent.Exception);
        }
    }

    private class TemplateRenderer : ITextFormatter
    {
        private delegate void Renderer(LogEvent logEvent, TextWriter output);

        private readonly Renderer[] _renderers;
        private readonly IFormatProvider _formatProvider;

        public TemplateRenderer(string outputTemplate, IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;

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
                LogEventLevel.Information => Colors.DimGray.ToHtml(),
                LogEventLevel.Warning => Colors.DarkOrange.ToHtml(),
                LogEventLevel.Error => Colors.Red.ToHtml(),
                LogEventLevel.Fatal => Colors.Purple.ToHtml(),
                _ => Colors.LightGray.ToHtml(),
            };
            
            var rawLevelText = text ?? logLevel.ToString();
            var paddedLevelText = rawLevelText.PadLeft(LevelMaxLength);
            var logLevelString = $"[color=#{color}]{paddedLevelText}[/color]";
    
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

                    //Выравниваем по максимальной длине
                    if (rawText.Length > PropertyMaxLength)
                    {
                        rawText = rawText.Substring(0,PropertyMaxLength - 2) + "..";
                    }
                    else
                    {
                        rawText = rawText.PadLeft(PropertyMaxLength);
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
                                            IFormatProvider formatProvider = null,
                                            Func<bool> godotPushEnableProvider = null)
    {
        return configuration.Sink(new RichGodotSink(outputTemplate, formatProvider, godotPushEnableProvider));
    }
}