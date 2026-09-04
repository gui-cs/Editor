// Claude - Fable 5

using Ted;
using Terminal.Gui.Configuration;
using Xunit;

namespace Terminal.Gui.Editor.ConfigTests;

/// <summary>
///     End-to-end proof that Terminal.Gui's <see cref="TuiConfigurationBuilder" /> is the read
///     authority for ted's settings: a nested <c>"EditorSettings"</c> section (the shape
///     <c>ted.config.json</c> persists) is loaded and applied to the <see cref="Ted.TedApp" />'s
///     <see cref="Terminal.Gui.Editor.Editor" />, mirroring ted's startup bootstrap.
///     <para>
///         This project exists solely for configuration tests that mutate the process-global
///         <c>EditorSettings.Defaults</c> facade, which cannot share a process with parallel tests —
///         <c>xunit.runner.json</c> disables assembly and collection parallelization here. See
///         CLAUDE.md "Testing tiers".
///     </para>
/// </summary>
public class TedConfigurationTests
{
    [Fact]
    public void TuiConfigurationBuilder_Applies_EditorSettings_To_TedApp ()
    {
        try
        {
            // Mirror TerminalGuiConfigurationBootstrap: a per-app builder whose highest-priority
            // source (RuntimeConfig) carries the nested MEC shape ted.config.json persists.
            TuiConfigurationBuilder builder = new ("ted");

            builder.RuntimeConfig =
                """
                {
                  "EditorSettings": {
                    "WordWrap": true,
                    "ShowTabs": true,
                    "LineNumbers": false,
                    "IndentSize": 2
                  }
                }
                """;

            EditorSettings.Apply (builder.Configuration);

            using TedApp app = new ();

            // Assert via the Editor instance (TedApp seeds it from the EditorSettings statics).
            Assert.True (app.Editor.WordWrap);
            Assert.True (app.Editor.ShowTabs);
            Assert.False (app.Editor.GutterOptions.HasFlag (GutterOptions.LineNumbers));
            Assert.Equal (2, app.Editor.IndentationSize);
        }
        finally
        {
            // Restore declared defaults so a later config test in this assembly starts clean.
            EditorSettings.ResetDefaults ();
        }
    }
}
