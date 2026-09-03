using System.Reflection;
using System.Runtime.CompilerServices;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Editor.IntegrationTests.Testing;

/// <summary>
///     Sets <c>DisableRealDriverIO=1</c> before any test runs so the ANSI driver does not attempt
///     real console I/O. Without this, the full integration test suite hangs on local machines
///     (the env var is set in CI via the workflow YAML but was missing for local runs).
/// </summary>
internal static class TestEnvironment
{
    private static readonly MethodInfo LoadHardCodedSchemes =
        typeof (SchemeManager).GetMethod (
            "LoadToHardCodedDefaults",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException (
            "SchemeManager.LoadToHardCodedDefaults is missing. Snapshots cannot reset the process-global scheme table.");

    private static readonly Lock PristineLock = new ();
    private static Dictionary<string, Scheme>? _pristineSchemes;

    [ModuleInitializer]
    internal static void Init ()
    {
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");

        // Wcwidth 4.0.1 WideTable.GetTable does an unlocked Dictionary.TryGetValue
        // beside a locked insert. Parallel Application.Init (macOS CI) can NRE in
        // Dictionary.FindValue on that first write. Populate the latest table on
        // this thread before xUnit starts the suite.
        _ = "x".GetColumns ();

        // Capture hardcoded schemes before any Application.Init can theme the
        // cached Menu/Dialog instances in place.
        LoadHardCodedSchemes.Invoke (null, null);
        CapturePristineSchemes ();
    }

    /// <summary>
    ///     Pins a clone of the captured hardcoded scheme on <paramref name="view" /> so
    ///     later <see cref="SchemeManager" /> races cannot change its draw colors.
    /// </summary>
    internal static void PinPristineScheme (View view, string schemeName)
    {
        ArgumentNullException.ThrowIfNull (view);

        lock (PristineLock)
        {
            CapturePristineSchemes ();

            if (_pristineSchemes is { } map && map.TryGetValue (schemeName, out Scheme? scheme))
            {
                view.SetScheme (new Scheme (scheme));
            }
        }
    }

    /// <summary>
    ///     Replaces <see cref="SchemeManager" />'s process-global table with the hardcoded
    ///     defaults. <see cref="AppFixture{TRunnable}" /> calls this after Init so snapshot
    ///     <c>ToAnsi</c> does not inherit a Base scheme leftover from another test
    ///     (White/Black <c>[97m[40m</c> vs the golden default <c>[39m[49m</c>).
    /// </summary>
    internal static void RestoreHardCodedSchemes ()
    {
        LoadHardCodedSchemes.Invoke (null, null);

        // HardCodedDictionary returns cached Scheme instances. Other tests mutate
        // those objects in place; putting the cache back would restore the dirty
        // colors. AddScheme clones from the module-init snapshot so each fixture
        // gets a private copy.
        lock (PristineLock)
        {
            CapturePristineSchemes ();

            foreach (KeyValuePair<string, Scheme> kv in _pristineSchemes!)
            {
                SchemeManager.AddScheme (kv.Key, new Scheme (kv.Value));
            }
        }
    }

    private static void CapturePristineSchemes ()
    {
        if (_pristineSchemes is not null)
        {
            return;
        }

        _pristineSchemes = new Dictionary<string, Scheme> (StringComparer.InvariantCultureIgnoreCase);

        foreach (KeyValuePair<string, Scheme?> kv in SchemeManager.Schemes)
        {
            if (kv.Value is { } scheme)
            {
                _pristineSchemes[kv.Key] = new Scheme (scheme);
            }
        }
    }
}
