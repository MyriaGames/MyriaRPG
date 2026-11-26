using System.Windows.Input;
using Myria.Wpf.Utils;
using Xunit;

namespace Myria.Wpf.Tests;

/// <summary>
/// Every button in the WPF client is bound to a RelayCommand, so a defect here disables or
/// wrongly enables UI everywhere at once. The generic overload's parameter handling is the subtle
/// part: WPF passes CommandParameter as object?, and when it isn't a T (a null from an unselected
/// list, or the wrong type from a mistyped binding) the command silently substitutes default(T)
/// instead of refusing. That's easy to break and hard to spot by clicking around.
/// </summary>
public class RelayCommandTests
{
    // ---- Non-generic ------------------------------------------------------------------------

    [Fact]
    public void Execute_InvokesTheSuppliedAction()
    {
        var invoked = 0;
        var command = new RelayCommand(() => invoked++);

        command.Execute(null);

        Assert.Equal(1, invoked);
    }

    /// <summary>Without a predicate the command is always enabled — the common case for a plain button.</summary>
    [Fact]
    public void CanExecute_IsTrueWhenNoPredicateWasGiven()
    {
        var command = new RelayCommand(() => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_FollowsThePredicate()
    {
        var allowed = false;
        var command = new RelayCommand(() => { }, () => allowed);

        Assert.False(command.CanExecute(null));

        allowed = true;
        Assert.True(command.CanExecute(null));
    }

    /// <summary>
    /// The parameter is ignored entirely by the non-generic form; a bound CommandParameter must
    /// not change whether the button is enabled.
    /// </summary>
    [Fact]
    public void CanExecute_IgnoresTheParameter()
    {
        var command = new RelayCommand(() => { }, () => true);

        Assert.True(command.CanExecute(null));
        Assert.True(command.CanExecute("anything"));
        Assert.True(command.CanExecute(42));
    }

    /// <summary>
    /// RaiseCanExecuteChanged is how a ViewModel tells WPF to re-evaluate a button's enabled
    /// state. If it stops firing, buttons get stuck in whatever state they were first bound in.
    /// </summary>
    [Fact]
    public void RaiseCanExecuteChanged_NotifiesSubscribers()
    {
        var command = new RelayCommand(() => { });
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        command.RaiseCanExecuteChanged();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void RaiseCanExecuteChanged_IsSafeWithNoSubscribers()
    {
        var command = new RelayCommand(() => { });

        command.RaiseCanExecuteChanged();   // must not throw
    }

    // ---- Generic ----------------------------------------------------------------------------

    [Fact]
    public void Generic_Execute_PassesAMatchingParameterThrough()
    {
        string? seen = null;
        var command = new RelayCommand<string>(s => seen = s);

        command.Execute("hello");

        Assert.Equal("hello", seen);
    }

    /// <summary>
    /// A null CommandParameter — an unselected ListBox item, say — is not a string, so the
    /// command falls through to default(T) rather than throwing. Pinning this documents that
    /// handlers must cope with null, and stops the fallback being "fixed" into an exception that
    /// would surface as an unhandled crash from a button click.
    /// </summary>
    [Fact]
    public void Generic_Execute_SubstitutesDefaultWhenTheParameterIsNotOfTypeT()
    {
        var invoked = 0;
        string? seen = "unset";
        var command = new RelayCommand<string>(s => { seen = s; invoked++; });

        command.Execute(null);

        Assert.Equal(1, invoked);
        Assert.Null(seen);
    }

    [Fact]
    public void Generic_Execute_SubstitutesDefaultForAWrongTypedParameter()
    {
        int seen = -1;
        var command = new RelayCommand<int>(i => seen = i);

        command.Execute("not an int");

        Assert.Equal(0, seen);
    }

    [Fact]
    public void Generic_CanExecute_IsTrueWhenNoPredicateWasGiven()
    {
        var command = new RelayCommand<string>(_ => { });

        Assert.True(command.CanExecute("anything"));
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void Generic_CanExecute_FollowsThePredicateForAMatchingParameter()
    {
        var command = new RelayCommand<string>(_ => { }, s => s == "ok");

        Assert.True(command.CanExecute("ok"));
        Assert.False(command.CanExecute("no"));
    }

    [Fact]
    public void Generic_CanExecute_EvaluatesThePredicateAgainstDefaultForAMismatch()
    {
        var command = new RelayCommand<string>(_ => { }, s => s is null);

        Assert.True(command.CanExecute(null));
        Assert.True(command.CanExecute(42));      // not a string → predicate sees default(string) == null
        Assert.False(command.CanExecute("set"));
    }

    /// <summary>
    /// The generic overload guards its execute delegate but the non-generic one does not — an
    /// asymmetry worth pinning so it's a deliberate choice rather than an accident someone
    /// "tidies up" in one direction without noticing the other.
    /// </summary>
    [Fact]
    public void Generic_RejectsANullExecuteDelegate()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand<string>(null!));
    }

    [Fact]
    public void Generic_RaiseCanExecuteChanged_NotifiesSubscribers()
    {
        var command = new RelayCommand<string>(_ => { });
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        command.RaiseCanExecuteChanged();

        Assert.Equal(1, raised);
    }

    /// <summary>Both forms must actually satisfy ICommand — that's how XAML binds to them at all.</summary>
    [Fact]
    public void BothFormsImplementICommand()
    {
        Assert.IsAssignableFrom<ICommand>(new RelayCommand(() => { }));
        Assert.IsAssignableFrom<ICommand>(new RelayCommand<string>(_ => { }));
    }
}
