
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Services;

public class I18NService
{
    public record LocaleInfo(string Code, string Name, int MissingMessages);
    
    private const string BaseLocaleForStart = "en";
    private const string EmptyTranslation = "[&\"\"]";
    
    public IReadOnlyDictionary<string, LocaleInfo> LocaleInfoByCode => _localeInfoByCode;
    public Translation BaseTranslation { get; private set; }
    
    private Dictionary<string, LocaleInfo> _localeInfoByCode;
    private SceneTree _sceneTree;
    
    [Logger] private ILogger _log;
    
    public I18NService()
    {
        Di.Process(this);
    }

    public void Init(SceneTree sceneTree)
    {
        _sceneTree = sceneTree;

        BaseTranslation = TranslationServer.GetTranslations()
            .FirstOrDefault(translation => translation.Locale.Equals(BaseLocaleForStart));
        if (BaseTranslation == null)
        {
            BaseTranslation = TranslationServer.GetTranslations().First();
            _log.Warning("Could not find a translation for the base locale: '{baseLocale}'. Use '{useLocale}' locale as base.'", 
                BaseLocaleForStart, BaseTranslation.Locale);
        }
        
        _localeInfoByCode = TranslationServer.GetTranslations().ToDictionary(translation => translation.Locale,
            translation => new LocaleInfo(
                translation.Locale, 
                TranslationServer.GetLanguageName(translation.Locale), 
                CountMissingMessages(BaseTranslation, translation)));
        _log.Information("Loaded {count} locales. Base locale '{baseLocale}' with {messages} messages.",
            _localeInfoByCode.Count, BaseTranslation.Locale, BaseTranslation.Messages.Count);

        foreach (LocaleInfo localeInfo in _localeInfoByCode.Values)
        {
            if (localeInfo.MissingMessages > 0)
            {
                _log.Warning("Locale '{locale}' doesn't contain {messages} messages.", localeInfo.Code, localeInfo.MissingMessages);
            }
        }
    }

    public string Tr(StringName message, StringName context = null) => 
        _sceneTree.Tr(message, context);

    public string TrN(StringName message, StringName pluralMessage, int n, StringName context = null) =>
        _sceneTree.TrN(message, pluralMessage, n, context);

    private int CountMissingMessages(Translation baseLocale, Translation secondLocale)
    {
        int count = 0;
        foreach (Variant key in baseLocale.Messages.Keys)
        {
            if (!secondLocale.Messages.ContainsKey(key) ||
                secondLocale.Messages[key].AsString().Length == 0 ||
                secondLocale.Messages[key].AsString().Equals(EmptyTranslation))
            {
                count++;
            }
        }
        
        return count;
    }
}