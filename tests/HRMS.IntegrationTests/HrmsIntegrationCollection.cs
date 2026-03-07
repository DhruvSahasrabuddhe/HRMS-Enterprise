namespace HRMS.IntegrationTests;

/// <summary>
/// Defines a test collection that shares a single <see cref="HrmsWebApplicationFactory"/>
/// instance across all web-application integration tests, preventing multiple in-process
/// application startups and avoiding the Serilog "logger already frozen" issue.
/// </summary>
[CollectionDefinition(Name)]
public class HrmsIntegrationCollection : ICollectionFixture<HrmsWebApplicationFactory>
{
    /// <summary>Collection name used in <c>[Collection]</c> attributes on test classes.</summary>
    public const string Name = "HRMS Integration";
}
