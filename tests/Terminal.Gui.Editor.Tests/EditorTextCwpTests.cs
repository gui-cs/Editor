// Claude - Fable 5

using Terminal.Gui.Editor.Document;
using Terminal.Gui.ViewBase;
using Xunit;

namespace Terminal.Gui.Editor.Tests;

/// <summary>
///     CWP contract tests for the <c>new</c> <see cref="Editor.Text" /> property (Terminal.Gui 2.5
///     made <see cref="View.Text" /> non-virtual). Both set paths must keep the
///     <see cref="TextDocument" /> and the base <see cref="View" /> text mirror in sync, and must
///     raise <see cref="View.TextChanging" /> / <see cref="View.TextChanged" /> exactly once:
///     <list type="bullet">
///         <item>
///             <description>direct: <c>editor.Text = value</c> (the <c>new</c> setter)</description>
///         </item>
///         <item>
///             <description>polymorphic: <c>((View)editor).Text = value</c> (the base setter + <c>OnTextChanged</c> sync)</description>
///         </item>
///     </list>
/// </summary>
public class EditorTextCwpTests
{
    [Fact]
    public void Text_Set_Writes_Document_And_Base_Mirror ()
    {
        Editor editor = new ();

        editor.Text = "hello";

        Assert.Equal ("hello", editor.Document!.Text);
        Assert.Equal ("hello", editor.Text);
        Assert.Equal ("hello", ((View)editor).Text);
    }

    [Fact]
    public void Text_Set_Raises_TextChanging_And_TextChanged_Exactly_Once ()
    {
        Editor editor = new ();
        var changingCount = 0;
        var changedCount = 0;
        editor.TextChanging += (_, _) => changingCount++;
        editor.TextChanged += (_, _) => changedCount++;

        editor.Text = "hello";

        Assert.Equal (1, changingCount);
        Assert.Equal (1, changedCount);
    }

    [Fact]
    public void Text_Set_Cancelled_By_TextChanging_Leaves_Document_And_Skips_TextChanged ()
    {
        Editor editor = new ();
        editor.Text = "before";
        editor.TextChanging += (_, args) => args.Cancel = true;
        var changedCount = 0;
        editor.TextChanged += (_, _) => changedCount++;

        editor.Text = "after";

        Assert.Equal ("before", editor.Document!.Text);
        Assert.Equal ("before", ((View)editor).Text);
        Assert.Equal (0, changedCount);
    }

    [Fact]
    public void Base_View_Text_Set_Syncs_Document ()
    {
        Editor editor = new ();
        View baseRef = editor;

        baseRef.Text = "poly";

        Assert.Equal ("poly", editor.Document!.Text);
        Assert.Equal ("poly", editor.Text);
        Assert.Equal ("poly", baseRef.Text);
    }

    [Fact]
    public void Base_View_Text_Set_Raises_TextChanging_And_TextChanged_Exactly_Once ()
    {
        Editor editor = new ();
        View baseRef = editor;
        var changingCount = 0;
        var changedCount = 0;
        baseRef.TextChanging += (_, _) => changingCount++;
        baseRef.TextChanged += (_, _) => changedCount++;

        baseRef.Text = "poly";

        Assert.Equal (1, changingCount);
        Assert.Equal (1, changedCount);
    }

    [Fact]
    public void Base_View_Text_Set_Cancelled_By_TextChanging_Leaves_Document ()
    {
        Editor editor = new ();
        editor.Text = "before";
        View baseRef = editor;
        baseRef.TextChanging += (_, args) => args.Cancel = true;
        var changedCount = 0;
        baseRef.TextChanged += (_, _) => changedCount++;

        baseRef.Text = "after";

        Assert.Equal ("before", editor.Document!.Text);
        Assert.Equal ("before", editor.Text);
        Assert.Equal (0, changedCount);
    }

    [Fact]
    public void Text_Set_RoundTrips_Between_Direct_And_Base_Paths ()
    {
        Editor editor = new ();
        View baseRef = editor;

        editor.Text = "one";
        Assert.Equal ("one", baseRef.Text);

        baseRef.Text = "two";
        Assert.Equal ("two", editor.Text);
        Assert.Equal ("two", editor.Document!.Text);

        editor.Text = "three";
        Assert.Equal ("three", baseRef.Text);
        Assert.Equal ("three", editor.Document!.Text);
    }

    [Fact]
    public void Text_Set_Survives_Throwing_TextChanged_Subscriber ()
    {
        Editor editor = new ();

        EventHandler thrower = (_, _) => throw new InvalidOperationException ("subscriber failure");
        editor.TextChanged += thrower;

        // The subscriber's exception escapes the setter (standard .NET event semantics)...
        Assert.Throws<InvalidOperationException> (() => editor.Text = "first");

        // ...but the editor must not be left in a corrupt state: a later polymorphic set
        // must still sync the Document (regression guard for a stuck re-entrancy flag).
        editor.TextChanged -= thrower;
        ((View)editor).Text = "second";

        Assert.Equal ("second", editor.Document!.Text);
        Assert.Equal ("second", editor.Text);
    }
}
