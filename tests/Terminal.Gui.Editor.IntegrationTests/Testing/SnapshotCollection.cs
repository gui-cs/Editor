// Claude - grok-4.6

using Xunit;

namespace Terminal.Gui.Editor.IntegrationTests.Testing;

/// <summary>
///     Marker collection that serializes ANSI snapshot tests against every other
///     collection in this assembly. <see cref="Terminal.Gui.Configuration.SchemeManager" />
///     is process-global. Parallel <c>Application.Init</c> and <c>SetScheme</c> swap the
///     default Base scheme and flake <c>ToAnsi</c> SGR codes on Ubuntu CI (glyphs stay
///     the same). DisableParallelization keeps goldens from overlapping that mutation.
/// </summary>
[CollectionDefinition (nameof (SnapshotCollection), DisableParallelization = true)]
public sealed class SnapshotCollection;
