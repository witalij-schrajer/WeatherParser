using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text;
using System.Reflection;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public sealed class MqttMessageSerializerTests
{
    private MockRepository _mocks = null!;
    private IMqttTopicOptionsValidator _optionsValidator = null!;
    private IMqttTopicValidator _topicValidator = null!;
    private IJsonExportSerializer _json = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _optionsValidator = new MqttTopicOptionsValidator(new MqttTopicTemplateParser());
        _topicValidator = new MqttTopicValidator();
        _json = new JsonExportSerializer();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    [TestMethod]
    public async Task SerializeAsync_Observation_TopicFromSourcePlaceholder()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();
        var message = await serializer.SerializeAsync(dataset, default);

        message.Topic.Should().Be("haus/weather/measurements/dwd:00044/state");
        message.MessageType.Should().Be(MqttMessageType.Observation);
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeTrue();
    }

    [TestMethod]
    public async Task SerializeAsync_Forecast_TopicFromProductAndMosmixId()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var run = ForecastTestData.ValidRun();
        var message = await serializer.SerializeAsync(run, default);

        message.Topic.Should().Be("haus/weather/forecasts/MOSMIX_L/10637");
        message.MessageType.Should().Be(MqttMessageType.Forecast);
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeTrue();
    }

    [TestMethod]
    public async Task SerializeAsync_Forecast_TopicFromProductAndMosmixId_MosmixS()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var run = ForecastTestData.ValidRun();
        run.Product = MosmixProduct.MosmixS;
        run.Source = "dwd-mosmix-s:10637";

        var message = await serializer.SerializeAsync(run, default);

        message.Topic.Should().Be("haus/weather/forecasts/MOSMIX_S/10637");
        message.MessageType.Should().Be(MqttMessageType.Forecast);
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeTrue();
    }

    [TestMethod]
    public async Task SerializeAsync_StationLists_TopicFromLiteralAndProvider()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var cdc = ListTestData.ValidCdcList();
        cdc.Source = "dwd:cdc-10min-now";
        var message1 = await serializer.SerializeAsync(cdc, default);

        message1.Topic.Should().Be("haus/weather/stations/dwd/cdc");
        message1.MessageType.Should().Be(MqttMessageType.CdcStationList);
        message1.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message1.Retain.Should().BeFalse();

        var mosmix = ListTestData.ValidMosmixList();
        mosmix.Source = "dwd:mosmix-stationskatalog";
        var message2 = await serializer.SerializeAsync(mosmix, default);

        message2.Topic.Should().Be("haus/weather/locations/dwd/mosmix");
        message2.MessageType.Should().Be(MqttMessageType.MosmixLocationList);
        message2.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message2.Retain.Should().BeFalse();
    }

    [TestMethod]
    public async Task SerializeAsync_QosAndRetainFromMapping()
    {
        var options = Options(
            (MqttMessageType.Observation, "o/{source}"),
            (MqttMessageType.CdcStationList, "s/{provider}")
        );
        options.Mappings![MqttMessageType.Observation]!.QualityOfService = MqttQualityOfService.ExactlyOnce;
        options.Mappings[MqttMessageType.Observation]!.Retain = false;
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();
        var message = await serializer.SerializeAsync(dataset, default);

        message.Topic.Should().Be("o/dwd:00044");
        message.MessageType.Should().Be(MqttMessageType.Observation);
        message.QualityOfService.Should().Be(MqttQualityOfService.ExactlyOnce);
        message.Retain.Should().BeFalse();

        // Second part with default mappings
        var cdcList = ListTestData.ValidCdcList();
        cdcList.Source = "dwd:cdc-10min-now";
        var message2 = await serializer.SerializeAsync(cdcList, default);

        message2.Topic.Should().Be("s/dwd");
        message2.MessageType.Should().Be(MqttMessageType.CdcStationList);
        message2.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message2.Retain.Should().BeFalse();
    }

    [TestMethod]
    public async Task SerializeAsync_MessageTypeFromRootType()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var cdcList = ListTestData.ValidCdcList();
        cdcList.Source = "dwd:cdc-10min-now";
        var message1 = await serializer.SerializeAsync(cdcList, default);
        message1.MessageType.Should().Be(MqttMessageType.CdcStationList);

        var mosmixList = ListTestData.ValidMosmixList();
        mosmixList.Source = "dwd:mosmix-stationskatalog";
        var message2 = await serializer.SerializeAsync(mosmixList, default);
        message2.MessageType.Should().Be(MqttMessageType.MosmixLocationList);

        var obs = await serializer.SerializeAsync(ObservationTestData.ValidDataset(), default);
        obs.MessageType.Should().Be(MqttMessageType.Observation);

        var fc = await serializer.SerializeAsync(ForecastTestData.ValidRun(), default);
        fc.MessageType.Should().Be(MqttMessageType.Forecast);
    }

    [TestMethod]
    public async Task SerializeAsync_ExportDocument_ThrowsMessageTypeUnsupported()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var act = () => serializer.SerializeAsync(new ExportDocument(), default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.MessageTypeUnsupported, null);
        ex.Message.Should().Contain("ExportDocument");
    }

    [TestMethod]
    public async Task SerializeAsync_OtherTypes_ThrowMessageTypeUnsupported()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var act1 = () => serializer.SerializeAsync((object)ObservationTestData.ValidDataset(), default);
        var ex1 = await ShouldFail(act1, MqttMessageErrorIds.MessageTypeUnsupported, null);
        ex1.Message.Should().Contain("Object");

        var act2 = () => serializer.SerializeAsync("text", default);
        var ex2 = await ShouldFail(act2, MqttMessageErrorIds.MessageTypeUnsupported, null);
        ex2.Message.Should().Contain("String");

        var act3 = () => serializer.SerializeAsync(new ForecastStep(), default);
        var ex3 = await ShouldFail(act3, MqttMessageErrorIds.MessageTypeUnsupported, null);
        ex3.Message.Should().Contain("ForecastStep");
    }

    [TestMethod]
    public async Task SerializeAsync_UnmappedType_ThrowsMappingMissing_WithoutSerializing()
    {
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        var jsonMock = _mocks.Create<IJsonExportSerializer>();
        var serializer = Create(options, jsonMock.Object);

        var act = () => serializer.SerializeAsync(ForecastTestData.ValidRun(), default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.MappingMissing, MqttMessageType.Forecast);
        ex.Message.Should().Contain("Forecast");

        jsonMock.VerifyAll();
    }

    [TestMethod]
    public async Task SerializeAsync_UnsafePlaceholderValue_ThrowsPlaceholderValueInvalid()
    {
        var options = Options((MqttMessageType.Observation, "o/{provider}/{station_id}"));
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();
        dataset.Source = "dwd:a/b";
        dataset.Location.StationId = "a/b";

        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.PlaceholderValueInvalid, MqttMessageType.Observation);
        ex.Message.Should().Contain("station_id");
    }

    [TestMethod]
    public async Task SerializeAsync_ResolvedTopicTooLong_ThrowsTopicInvalid()
    {
        var options = Options((MqttMessageType.Observation, new string('a', 65530) + "/{source}"));
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();

        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.TopicInvalid, MqttMessageType.Observation);
        ex.Message.Should().Contain("Observation");
    }

    [TestMethod]
    public async Task SerializeAsync_NullRoot_ThrowsArgumentNull()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var act = () => serializer.SerializeAsync<ObservationDataset>(null!, default);
        var ex = await act.Should().ThrowAsync<ArgumentNullException>();
        ex.Which.ParamName.Should().Be("root");
    }

    [TestMethod]
    public void Constructor_InvalidOptions_ThrowsOptionsInvalid()
    {
        var act1 = () => Create(Options((MqttMessageType.Observation, "o/{station}")));
        var ex1 = act1.Should().Throw<MqttMessageException>().Which;
        ex1.ErrorId.Should().Be(MqttMessageErrorIds.OptionsInvalid);
        ex1.MessageType.Should().Be(MqttMessageType.Observation);

        var act2 = () => Create(null!);
        var ex2 = act2.Should().Throw<ArgumentNullException>().Which;
        ex2.ParamName.Should().Be("options");
    }

    [TestMethod]
    public async Task AddWeatherParsing_ResolvesMessageSerializer()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        serializer.Should().BeOfType<MqttMessageSerializer>();

        await ShouldFail(() => serializer.SerializeAsync(ObservationTestData.ValidDataset(), default), MqttMessageErrorIds.MappingMissing, MqttMessageType.Observation);
    }

    [TestMethod]
    [DataRow("observation")]
    [DataRow("forecast")]
    [DataRow("cdc")]
    [DataRow("mosmix")]
    public async Task SerializeAsync_PayloadEqualsJsonExportBytes(string type)
    {
        var options = AllTypes();
        var serializer = Create(options);

        var root = Root(type);

        using var expectedStream = new MemoryStream();
        MqttMessage message2 = null!;

        switch (type)
        {
            case "observation":
                var dataset = (ObservationDataset)root;
                message2 = await serializer.SerializeAsync(dataset, default);
                await new JsonExportSerializer().SerializeAsync(dataset, expectedStream, default);
                break;
            case "forecast":
                var run = (ForecastRun)root;
                message2 = await serializer.SerializeAsync(run, default);
                await new JsonExportSerializer().SerializeAsync(run, expectedStream, default);
                break;
            case "cdc":
                var cdcList = (CdcStationList)root;
                message2 = await serializer.SerializeAsync(cdcList, default);
                await new JsonExportSerializer().SerializeAsync(cdcList, expectedStream, default);
                break;
            case "mosmix":
                var mosmixList = (MosmixLocationList)root;
                message2 = await serializer.SerializeAsync(mosmixList, default);
                await new JsonExportSerializer().SerializeAsync(mosmixList, expectedStream, default);
                break;
        }

        var expectedBytes = expectedStream.ToArray();
        message2.Payload.ToArray().Should().Equal(expectedBytes);
    }

    [TestMethod]
    public async Task SerializeAsync_PayloadUtf8WithoutBom()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();
        var message = await serializer.SerializeAsync(dataset, default);

        var payload = message.Payload.ToArray();
        payload.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
        payload[0].Should().Be((byte)'{');
        Encoding.UTF8.GetString(payload).Should().Contain("\"source\":\"dwd:00044\"");
    }

    [TestMethod]
    public async Task SerializeAsync_InvalidRoot_ThrowsSerializationFailed_WithInnerException()
    {
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        var serializer = Create(options);

        var dataset = new ObservationDataset { Source = "dwd:00044" };
        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.SerializationFailed, MqttMessageType.Observation);
        ex.InnerException.Should().BeOfType<ExportSerializationException>();
    }

    [TestMethod]
    public async Task SerializeAsync_UnassignedPlaceholderMembers_ThrowSerializationFailed_BeforeTopicResolution()
    {
        // Test case 1
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        var serializer = Create(options);

        var dataset = new ObservationDataset();
        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.SerializationFailed, MqttMessageType.Observation);
        ex.InnerException.Should().BeOfType<ExportSerializationException>();

        // Test case 2
        options = Options((MqttMessageType.Observation, "o/{provider}/{station_id}"));
        serializer = Create(options);

        dataset = new ObservationDataset { Source = "dwd:00044" };
        act = () => serializer.SerializeAsync(dataset, default);
        ex = await ShouldFail(act, MqttMessageErrorIds.SerializationFailed, MqttMessageType.Observation);
        ex.InnerException.Should().BeOfType<ExportSerializationException>();

        // Test case 3
        options = Options((MqttMessageType.Forecast, "f/{product}/{mosmix_id}"));
        serializer = Create(options);

        var run = new ForecastRun();
        act = () => serializer.SerializeAsync(run, default);
        ex = await ShouldFail(act, MqttMessageErrorIds.SerializationFailed, MqttMessageType.Forecast);
        ex.InnerException.Should().BeOfType<ExportSerializationException>();
    }

    [TestMethod]
    public async Task SerializeAsync_PlaceholderReadThrows_MappedToSerializationFailed()
    {
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        var jsonMock = _mocks.Create<IJsonExportSerializer>();
        jsonMock.Setup(s => s.SerializeAsync(It.IsAny<ObservationDataset>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((ObservationDataset _, Stream destination, CancellationToken _) => destination.WriteAsync("{}"u8.ToArray()).AsTask());
        var serializer = Create(options, jsonMock.Object);

        var dataset = new ObservationDataset();
        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.SerializationFailed, MqttMessageType.Observation);
        ex.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [TestMethod]
    public async Task SerializeAsync_CancelledBeforeStart_ThrowsOperationCanceled_NoSerialization()
    {
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        var serializer = Create(options);

        var act = () => serializer.SerializeAsync(ObservationTestData.ValidDataset(), new CancellationToken(true));
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task SerializeAsync_CancelledDuringSerialization_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        var jsonMock = _mocks.Create<IJsonExportSerializer>();
        jsonMock.Setup(s => s.SerializeAsync(It.IsAny<ObservationDataset>(), It.IsAny<Stream>(), It.Is<CancellationToken>(t => t == cts.Token)))
            .Returns((ObservationDataset _, Stream _, CancellationToken token) =>
            {
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });
        var serializer = Create(options, jsonMock.Object);

        var act = () => serializer.SerializeAsync(ObservationTestData.ValidDataset(), cts.Token);
        (await act.Should().ThrowExactlyAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(cts.Token);
    }

    [TestMethod]
    public async Task SerializeAsync_SameRootTwice_IdenticalMessages()
    {
        var options = AllTypes();
        var serializer = Create(options);

        var run = ForecastTestData.ValidRun();
        var message1 = await serializer.SerializeAsync(run, default);
        var message2 = await serializer.SerializeAsync(run, default);

        message1.Topic.Should().Be(message2.Topic);
        message1.Payload.ToArray().Should().Equal(message2.Payload.ToArray());
        message1.QualityOfService.Should().Be(message2.QualityOfService);
        message1.Retain.Should().Be(message2.Retain);
        message1.MessageType.Should().Be(message2.MessageType);
    }

    [TestMethod]
    [DataRow("catalog")]
    [DataRow(":catalog")]
    public async Task SerializeAsync_ListSourceWithoutProviderPrefix_ThrowsPlaceholderValueInvalid(string source)
    {
        var options = Options((MqttMessageType.CdcStationList, "s/{provider}"));
        var serializer = Create(options);

        var list = ListTestData.ValidCdcList();
        list.Source = source;
        var act = () => serializer.SerializeAsync(list, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.PlaceholderValueInvalid, MqttMessageType.CdcStationList);
        ex.Message.Should().Contain("CdcStationList").And.Contain("provider");
    }

    [TestMethod]
    public async Task SerializeAsync_PayloadAtBound_Accepted()
    {
        var dataset = ObservationTestData.ValidDataset();
        var size = await JsonSize(dataset);
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        options.MaxPayloadBytes = size;
        var serializer = Create(options);

        var message = await serializer.SerializeAsync(dataset, default);
        message.Payload.Length.Should().Be(size);
    }

    [TestMethod]
    public async Task SerializeAsync_PayloadOneByteAbove_ThrowsPayloadTooLarge()
    {
        var dataset = ObservationTestData.ValidDataset();
        var size = await JsonSize(dataset);
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        options.MaxPayloadBytes = size - 1;
        var serializer = Create(options);

        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.PayloadTooLarge, MqttMessageType.Observation);
        ex.Message.Should().Contain("Observation");
    }

    [TestMethod]
    public void EffectiveBound_ProtocolFormula()
    {
        MqttMessageSerializer.EffectiveBound(null, "a").Should().Be(268435449);
        MqttMessageSerializer.EffectiveBound(null, new string('a', 65535)).Should().Be(268369915);
        MqttMessageSerializer.EffectiveBound(null, "\u00E4").Should().Be(268435448);
        MqttMessageSerializer.EffectiveBound(100, "abc").Should().Be(100);
        MqttMessageSerializer.EffectiveBound(268435450, "abcdefghij").Should().Be(268435440);
    }

    [TestMethod]
    public async Task SerializeAsync_OversizedPayloadAndInvalidTopic_ThrowsTopicInvalid()
    {
        var options = Options((MqttMessageType.Observation, new string('a', 65530) + "/{source}"));
        options.MaxPayloadBytes = 1;
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();
        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.TopicInvalid, MqttMessageType.Observation);
        ex.Message.Should().Contain("Observation");
    }

    [TestMethod]
    public async Task SerializeAsync_OversizedPayloadAndInvalidPlaceholder_ThrowsPlaceholderValueInvalid()
    {
        var options = Options((MqttMessageType.Observation, "o/{provider}/{station_id}"));
        options.MaxPayloadBytes = 1;
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();
        dataset.Source = "dwd:a/b";
        dataset.Location.StationId = "a/b";
        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.PlaceholderValueInvalid, MqttMessageType.Observation);
        ex.Message.Should().Contain("Observation");
    }

    [TestMethod]
    public async Task SerializeAsync_NullMaxPayloadBytes_UsesProtocolBound()
    {
        var dataset = ObservationTestData.ValidDataset();
        var size = await JsonSize(dataset);
        var options = Options((MqttMessageType.Observation, "o/{source}"));
        var serializer = Create(options);

        var message = await serializer.SerializeAsync(dataset, default);
        message.Payload.Length.Should().Be(size);
    }

    [TestMethod]
    public void PayloadBuffer_StoresAtMostCapPlusOne_AndRecordsOverflow()
    {
        var buffer = new MqttPayloadBuffer(4);
        buffer.Write(new byte[] { 1, 2, 3 }, 0, 3);
        buffer.Write(new byte[] { 4 }, 0, 1);
        buffer.Overflowed.Should().BeFalse();
        buffer.Length.Should().Be(4);

        buffer.Write(new byte[] { 5, 6, 7, 8, 9, 10 }, 0, 6);
        buffer.Overflowed.Should().BeTrue();
        buffer.Length.Should().Be(5);
        buffer.ToArray().Should().Equal(new byte[] { 1, 2, 3, 4, 5 });

        buffer.Write(new byte[] { 6 }, 0, 1);
        buffer.Length.Should().Be(5);

        Action act = () => new MqttPayloadBuffer(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public async Task SerializeAsync_FailureMessages_NoPayloadContent()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = "secret-marker";

        var options = Options((MqttMessageType.Observation, "o/{source}"));
        options.MaxPayloadBytes = 1;
        var serializer = Create(options);
        var act = () => serializer.SerializeAsync(dataset, default);
        var ex = await ShouldFail(act, MqttMessageErrorIds.PayloadTooLarge, MqttMessageType.Observation);
        ex.Message.Should().Contain("Observation").And.NotContain("secret-marker");

        options = Options((MqttMessageType.Observation, new string('a', 65530) + "/{source}"));
        options.MaxPayloadBytes = 1;
        serializer = Create(options);
        act = () => serializer.SerializeAsync(dataset, default);
        ex = await ShouldFail(act, MqttMessageErrorIds.TopicInvalid, MqttMessageType.Observation);
        ex.Message.Should().Contain("Observation").And.NotContain("secret-marker");

        options = Options((MqttMessageType.Forecast, "f/{source}"));
        serializer = Create(options);
        act = () => serializer.SerializeAsync(dataset, default);
        ex = await ShouldFail(act, MqttMessageErrorIds.MappingMissing, MqttMessageType.Observation);
        ex.Message.Should().Contain("Observation").And.NotContain("secret-marker");
    }

    [TestMethod]
    public void MqttMessageSerializer_HasNoLoggerDependency()
    {
        var type = typeof(MqttMessageSerializer);
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var field in fields)
        {
            if (field.FieldType.Name.StartsWith("ILogger"))
                Assert.Fail("Found ILogger dependency");
        }
    }

    [TestMethod]
    public async Task SerializeAsync_AllPlaceholders_FilledFromRoot()
    {
        var options = Options(
            (MqttMessageType.Observation, "o/{provider}/{station_id}/{source}"),
            (MqttMessageType.Forecast, "f/{provider}/{source}/{product}/{mosmix_id}")
        );
        var serializer = Create(options);

        var dataset = ObservationTestData.ValidDataset();
        var message1 = await serializer.SerializeAsync(dataset, default);
        message1.Topic.Should().Be("o/dwd/00044/dwd:00044");

        var run = ForecastTestData.ValidRun();
        var message2 = await serializer.SerializeAsync(run, default);
        message2.Topic.Should().Be("f/dwd-mosmix-l/dwd-mosmix-l:10637/MOSMIX_L/10637");
    }

    [TestMethod]
    public async Task PayloadBuffer_StreamMembers()
    {
        using var buffer = new MqttPayloadBuffer(4);
        buffer.CanRead.Should().BeFalse();
        buffer.CanSeek.Should().BeFalse();
        buffer.CanWrite.Should().BeTrue();

        buffer.Write(new byte[] { 1, 2 }, 0, 2);
        buffer.Position.Should().Be(2);
        buffer.Flush();

        Action act1 = () => buffer.Position = 1;
        act1.Should().Throw<NotSupportedException>();

        Func<int> act2 = () => buffer.Read(new byte[1], 0, 1);
        act2.Should().Throw<NotSupportedException>();

        Action act3 = () => buffer.Seek(0, SeekOrigin.Begin);
        act3.Should().Throw<NotSupportedException>();

        Action act4 = () => buffer.SetLength(1);
        act4.Should().Throw<NotSupportedException>();

        await buffer.WriteAsync(new byte[] { 3 }, 0, 1, default);
        buffer.Length.Should().Be(3);

        var act5 = () => buffer.WriteAsync(new byte[] { 4 }, 0, 1, new CancellationToken(true));
        await act5.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public void PayloadCap_SubtractsMinimumTopicLength()
    {
        var parser = new MqttTopicTemplateParser();
        Func<string, MqttMessageType, MqttTopicTemplate> Parse = (text, type) => parser.Parse(text, type);
        MqttMessageSerializer.PayloadCap(null, Parse("s/dwd", MqttMessageType.CdcStationList)).Should().Be(268435445);
        MqttMessageSerializer.PayloadCap(null, Parse(new string('a', 65535), MqttMessageType.CdcStationList)).Should().Be(268369915);
        MqttMessageSerializer.PayloadCap(null, Parse("o/{source}", MqttMessageType.Observation)).Should().Be(268435447);
        MqttMessageSerializer.PayloadCap(100, Parse("o/{source}", MqttMessageType.Observation)).Should().Be(100);
        MqttMessageSerializer.PayloadCap(268435450, Parse("\u00E4/{source}", MqttMessageType.Observation)).Should().Be(268435446);
    }

    private MqttMessageSerializer Create(MqttTopicOptions options, IJsonExportSerializer? json = null) => new(options, _optionsValidator, _topicValidator, json ?? _json);

    private static MqttTopicOptions Options(params (MqttMessageType Type, string Template)[] mappings)
    {
        var options = new MqttTopicOptions();
        foreach (var (type, template) in mappings)
        {
            if (options.Mappings == null)
                options.Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>();
            options.Mappings[type] = new() { TopicTemplate = template };
        }
        return options;
    }

    private static MqttTopicOptions AllTypes() => Options((MqttMessageType.Observation, "haus/weather/measurements/{source}/state"), (MqttMessageType.Forecast, "haus/weather/forecasts/{product}/{mosmix_id}"), (MqttMessageType.CdcStationList, "haus/weather/stations/{provider}/cdc"), (MqttMessageType.MosmixLocationList, "haus/weather/locations/{provider}/mosmix"));

    private static object Root(string name) => name switch
    {
        "observation" => ObservationTestData.ValidDataset(),
        "forecast" => ForecastTestData.ValidRun(),
        "cdc" => CdcList(),
        _ => MosmixList()
    };

    private static CdcStationList CdcList()
    {
        var list = ListTestData.ValidCdcList();
        list.Source = "dwd:cdc-10min-now";
        return list;
    }

    private static MosmixLocationList MosmixList()
    {
        var list = ListTestData.ValidMosmixList();
        list.Source = "dwd:mosmix-stationskatalog";
        return list;
    }

    private static async Task<MqttMessageException> ShouldFail(Func<Task> act, string errorId, MqttMessageType? type)
    {
        var ex = await act.Should().ThrowAsync<MqttMessageException>();
        ex.Which.ErrorId.Should().Be(errorId);
        ex.Which.MessageType.Should().Be(type);
        return ex.Which;
    }

    private static async Task<int> JsonSize(ObservationDataset dataset)
    {
        using var stream = new MemoryStream();
        await new JsonExportSerializer().SerializeAsync(dataset, stream, default);
        return (int)stream.Length;
    }
}
