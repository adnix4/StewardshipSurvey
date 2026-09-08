# Fonts

Empty on purpose.

`MauiProgram` used to call `fonts.AddFont("OpenSans-Regular.ttf", ...)` and `App.xaml` set
`FontFamily="OpenSansRegular"`, but no font files have ever been in this repository, so the
family resolved to nothing and every control silently fell back to the platform default.

Both references were removed rather than left pointing at files that do not exist. To use
OpenSans (or anything else), drop the `.ttf` files in here - the csproj already globs this
folder as `MauiFont` - and restore the `AddFont` calls and the `FontFamily` setter.
