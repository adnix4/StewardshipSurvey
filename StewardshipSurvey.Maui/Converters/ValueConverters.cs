using System.Globalization;

namespace StewardshipSurvey.Maui.Converters;

/// <summary>
/// True when a string has content. Used to show an error label only when there is an error.
/// <para>
/// The pages referenced a <c>StringToBoolConverter</c> that was never written, along with
/// <c>InvertedBoolConverter</c> and a <c>SelectedInterestConverter</c>. A missing converter is
/// a runtime XAML failure, not a compile error, which is part of why it went unnoticed in a
/// project that had never been run.
/// </para>
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Negates a bool, so a button can be enabled while the page is not loading.</summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && !b;
}
