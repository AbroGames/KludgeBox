
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Services;

public class I18NService
{
    public record LocaleInfo(string Code, string Name, int MissingMessages, int UnnecessaryMessages);
    
    public const string DefaultLocale = "en";
    private const string EmptyTranslation = "[&\"\"]";
    private const string LocaleSeparatorSymbol = "_";
    
    public IReadOnlyList<LocaleInfo> Locales => _locales;
    
    private List<LocaleInfo> _locales;
    private SceneTree _sceneTree;
    
    [Logger] private ILogger _log;
    
    public I18NService()
    {
        Di.Process(this);
    }

    public void Init(SceneTree sceneTree)
    {
        _sceneTree = sceneTree;

        Translation baseTranslation = TranslationServer.GetTranslations()
            .FirstOrDefault(translation => translation.Locale.Equals(DefaultLocale));
        if (baseTranslation == null)
        {
            _log.Error("Could not find a translation for the base locale: '{baseLocale}'.", DefaultLocale);
            return;
        }
        
        _locales = TranslationServer.GetTranslations().Select(translation => new LocaleInfo(
                translation.Locale,
                TranslationServer.GetLanguageName(translation.Locale),
                CountMissingMessages(baseTranslation, translation),
                CountMissingMessages(translation, baseTranslation)))
            .ToList();
        _log.Information("Loaded {count} locales. Base locale '{baseLocale}' with {messages} messages.",
            _locales.Count, baseTranslation.Locale, baseTranslation.Messages.Count);

        foreach (LocaleInfo localeInfo in _locales)
        {
            if (localeInfo.MissingMessages > 0)
            {
                _log.Warning("Locale '{locale}' doesn't contain {messages} messages.", localeInfo.Code, localeInfo.MissingMessages);
            }
            if (localeInfo.UnnecessaryMessages > 0)
            {
                _log.Warning("Locale '{locale}' contain {messages} unnecessary messages.", localeInfo.Code, localeInfo.UnnecessaryMessages);
            }
        }
    }

    public LocaleInfo GetLocaleInfoByCode(string code)
    {
        code = GetLangPartOfLocale(code);
        return _locales.FirstOrDefault(localeInfo => 
            string.Equals(localeInfo.Code, code, StringComparison.OrdinalIgnoreCase),
            null);
    }
    
    public LocaleInfo GetLocaleInfoByName(string name)
    {
        return _locales.FirstOrDefault(localeInfo => 
            string.Equals(localeInfo.Name, name, StringComparison.OrdinalIgnoreCase),
            null);
    }

    public LocaleInfo GetCurrentLocaleInfo()
    {
        return GetLocaleInfoByCode(TranslationServer.GetLocale());
    }

    public bool SetCurrentLocale(string code)
    {
        code = GetLangPartOfLocale(code);
        if (GetLocaleInfoByCode(code) == null)
        {
            _log.Error("Could not find a locale with code '{code}'.", code);
            return false;
        }
        
        TranslationServer.SetLocale(code);
        return true;
    }

    public LocaleInfo GetUserOsLocaleInfo()
    {
        return GetLocaleInfoByCode(OS.GetLocaleLanguage());
    }
    
    public LocaleInfo GetUserOsLocaleInfoOrDefault()
    {
        return GetLocaleInfoByCode(OS.GetLocaleLanguage()) ?? GetLocaleInfoByCode(DefaultLocale);
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

    private string GetLangPartOfLocale(string code)
    {
        if (code != null && code.Contains(LocaleSeparatorSymbol))
        {
            code = code.Split(LocaleSeparatorSymbol)[0];
        }
        return code;
    }
}