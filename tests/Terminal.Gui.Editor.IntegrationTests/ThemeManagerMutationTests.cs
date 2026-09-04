// Claude - grok-4.6

using System.Collections.Immutable;
using Ted;
using Terminal.Gui.Configuration;
using Terminal.Gui.Editor.IntegrationTests.Testing;
using Xunit;

namespace Terminal.Gui.Editor.IntegrationTests;

/// <summary>
///     Marker collection that serializes <see cref="ThemeManagerMutationTests" /> against every
///     other test collection in this assembly. These tests assign <see cref="ThemeManager.Theme" />,
///     a process-global that rewrites <see cref="SchemeManager" /> in place. Running them beside
///     ANSI snapshots (File menu, Find/Replace) lets the other theme leak into goldens.
/// </summary>
[CollectionDefinition (nameof (ThemeManagerMutationCollection), DisableParallelization = true)]
public sealed class ThemeManagerMutationCollection;

/// <summary>
///     Tests that legitimately change <see cref="ThemeManager.Theme" />. Restore the original
///     theme in <c>finally</c> so a later snapshot does not inherit the leftover name or colors.
/// </summary>
[Collection (nameof (ThemeManagerMutationCollection))]
public class ThemeManagerMutationTests
{
    [Fact]
    public async Task ThemeDropDown_Selection_Changes_Active_Theme ()
    {
        await using AppFixture<TedApp> fx = new (() => new TedApp (configPath: TedTestConfig.NewPath ()));

        ImmutableList<string> names = ThemeManager.GetThemeNames ();

        if (names.Count < 2)
        {
            return;
        }

        var original = ThemeManager.Theme;
        var target = names.First (n => n != original);

        try
        {
            fx.Top.ThemeDropDown.Text = target;

            Assert.Equal (target, ThemeManager.Theme);
        }
        finally
        {
            ThemeManager.Theme = original;
        }
    }
}
