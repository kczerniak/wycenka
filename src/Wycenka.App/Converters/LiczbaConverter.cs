using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Wycenka.Core.Obliczenia;

namespace Wycenka.App.Converters;

/// <summary>
/// decimal ↔ tekst w polskim formacie. Przy wpisywaniu akceptuje przecinek i kropkę
/// oraz proste działania (np. „4*2,5+3”). Parametr to format wyświetlania (domyślnie „0.####”).
/// </summary>
public sealed class LiczbaConverter : IValueConverter
{
    public static readonly LiczbaConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d
            ? d.ToString(parameter as string ?? "0.####", CultureInfo.CurrentCulture)
            : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var tekst = (value as string ?? "").Replace("zł", "").Trim();
        if (tekst.Length == 0) return 0m;
        if (Wyrazenie.SprobujObliczyc(tekst, out var wynik)) return wynik;
        return new BindingNotification(
            new FormatException($"„{tekst}” nie jest poprawną liczbą"),
            BindingErrorType.DataValidationError);
    }
}
