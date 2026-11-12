using HardwareRegistry.IntegrationTests.Infrastructure;

// Parallelization is off in xunit.runner.json: every test resets the one shared database.
[assembly: AssemblyFixture(typeof(TestEnvironment))]
