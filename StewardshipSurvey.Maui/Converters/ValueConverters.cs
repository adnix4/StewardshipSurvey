using System.Globalization;

namespace StewardshipSurvey.Maui.Converters;

/// <summary>
/// True when a string has content. Used to show an error label only when there is an error.
/// <para>
/// The pages used to ask for a <c>StringToBoolConverter</c> for this, which was never written.
/// Rather than add a second name for one behaviour, those bindings now point here. A missing
/// converter is a runtime XAML failure rather than a compile error, which is why the project
/// built clean for as long as it did with three of them unresolved.
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

/// <summary>
/// Binds one radio button in a group to a string property: checked when the property equals
/// this button's <c>ConverterParameter</c>.
/// <para>
/// The contact-preference buttons on the profile page bind
/// <c>IsChecked="{Binding PreferredContact, ConverterParameter='Phone'}"</c> and the two
/// equivalents, against a single string that is "Phone", "Text" or "Email".
/// </para>
/// </summary>
public class StringToValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.Ordinal);

    /// <summary>
    /// Checking a button writes its own parameter back. Unchecking writes nothing:
    /// within a radio group the button being selected reports true immediately after the
    /// previous one reports false, so acting on the false would blank the property and then
    /// set it again - and if the group were ever cleared entirely it would silently discard
    /// the member's answer.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool isChecked && isChecked
            ? parameter as string ?? string.Empty
            : Binding.DoNothing;
}
