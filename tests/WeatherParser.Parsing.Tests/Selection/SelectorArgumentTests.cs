using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class SelectorArgumentTests
{
    [TestMethod]
    public void CdcStationSelector_NullArguments_Throw()
    {
        var options = new CdcCatalogEndpoints();
        var source = new Mock<ICdcCatalogSource>().Object;
        var deriver = new Mock<ICdcAvailabilityDeriver>().Object;
        var parameterFactory = new Mock<ISearchParameterFactory>().Object;
        var searchAreaFilter = new Mock<ISearchAreaFilter>().Object;
        var ordering = new Mock<IListEntryOrdering>().Object;
        var timeProvider = new Mock<TimeProvider>().Object;
        var logger = new Mock<ILogger<CdcStationSelector>>().Object;

        FluentActions.Invoking(() => new CdcStationSelector(null!, source, deriver, parameterFactory, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("options");

        FluentActions.Invoking(() => new CdcStationSelector(options, null!, deriver, parameterFactory, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("source");

        FluentActions.Invoking(() => new CdcStationSelector(options, source, null!, parameterFactory, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("deriver");

        FluentActions.Invoking(() => new CdcStationSelector(options, source, deriver, null!, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("parameterFactory");

        FluentActions.Invoking(() => new CdcStationSelector(options, source, deriver, parameterFactory, null!, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("searchAreaFilter");

        FluentActions.Invoking(() => new CdcStationSelector(options, source, deriver, parameterFactory, searchAreaFilter, null!, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("ordering");

        FluentActions.Invoking(() => new CdcStationSelector(options, source, deriver, parameterFactory, searchAreaFilter, ordering, null!, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("timeProvider");

        FluentActions.Invoking(() => new CdcStationSelector(options, source, deriver, parameterFactory, searchAreaFilter, ordering, timeProvider, null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("logger");
    }

    [TestMethod]
    public void MosmixLocationSelector_NullArguments_Throw()
    {
        var options = new MosmixCatalogEndpoints();
        var source = new Mock<IMosmixCatalogSource>().Object;
        var evidenceCache = new Mock<IMosmixEvidenceCache>().Object;
        var availabilityResolver = new Mock<IMosmixAvailabilityResolver>().Object;
        var parameterFactory = new Mock<ISearchParameterFactory>().Object;
        var searchAreaFilter = new Mock<ISearchAreaFilter>().Object;
        var ordering = new Mock<IListEntryOrdering>().Object;
        var timeProvider = new Mock<TimeProvider>().Object;
        var logger = new Mock<ILogger<MosmixLocationSelector>>().Object;

        FluentActions.Invoking(() => new MosmixLocationSelector(null!, source, evidenceCache, availabilityResolver, parameterFactory, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("options");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, null!, evidenceCache, availabilityResolver, parameterFactory, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("source");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, source, null!, availabilityResolver, parameterFactory, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("evidenceCache");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, source, evidenceCache, null!, parameterFactory, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("availabilityResolver");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, source, evidenceCache, availabilityResolver, null!, searchAreaFilter, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("parameterFactory");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, source, evidenceCache, availabilityResolver, parameterFactory, null!, ordering, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("searchAreaFilter");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, source, evidenceCache, availabilityResolver, parameterFactory, searchAreaFilter, null!, timeProvider, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("ordering");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, source, evidenceCache, availabilityResolver, parameterFactory, searchAreaFilter, ordering, null!, logger))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("timeProvider");

        FluentActions.Invoking(() => new MosmixLocationSelector(options, source, evidenceCache, availabilityResolver, parameterFactory, searchAreaFilter, ordering, timeProvider, null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("logger");
    }
}
