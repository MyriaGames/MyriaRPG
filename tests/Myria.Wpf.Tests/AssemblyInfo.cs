using Xunit;

// Myria.Wpf leans heavily on static, process-wide state (Navigation.Current, MainWindow.Instance,
// ServerApiService's token, ThemeManager's dictionaries, and Myria.Lib.Core's content registries).
// Running test classes in parallel would let them interfere nondeterministically, so
// parallelization is disabled assembly-wide — the same decision Myria.Lib.Tests already made.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
