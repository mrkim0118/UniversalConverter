namespace UniversalConverter.Services;

public static class LanguageService
{
    public static bool IsKorean { get; private set; } =
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko";

    public static event Action<bool>? LanguageChanged;

    public static void Toggle()
    {
        IsKorean = !IsKorean;
        LanguageChanged?.Invoke(IsKorean);
    }
}
