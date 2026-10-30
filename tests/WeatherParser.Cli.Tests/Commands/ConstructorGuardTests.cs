using System;
using System.Globalization;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Cli;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Cli.Tests.Commands;

[TestClass]
[TestCategory("T-P3-20")]
public sealed class ConstructorGuardTests
{
    /// <summary>
    /// Tests that the constructors of command handler classes reject null dependencies with ArgumentNullException.
    /// </summary>
    [TestMethod]
    [DataRow(typeof(SelectDwdHandler), "selector")]
    [DataRow(typeof(SelectDwdHandler), "writer")]
    [DataRow(typeof(SelectMosmixHandler), "selector")]
    [DataRow(typeof(SelectMosmixHandler), "writer")]
    [DataRow(typeof(ParseDwdHandler), "parser")]
    [DataRow(typeof(ParseDwdHandler), "writer")]
    [DataRow(typeof(ParseDwdHandler), "messages")]
    [DataRow(typeof(ParseDwdHandler), "libraryErrors")]
    [DataRow(typeof(ParseDwdHandler), "classifier")]
    [DataRow(typeof(ParseDwdHandler), "logger")]
    [DataRow(typeof(ParseMosmixHandler), "writer")]
    [DataRow(typeof(ParseMosmixHandler), "messages")]
    [DataRow(typeof(ParseMosmixHandler), "libraryErrors")]
    [DataRow(typeof(ParseMosmixHandler), "classifier")]
    [DataRow(typeof(ParseMosmixHandler), "logger")]
    [DataRow(typeof(ParseOstHessenHandler), "parser")]
    [DataRow(typeof(ParseOstHessenHandler), "identity")]
    [DataRow(typeof(ParseOstHessenHandler), "writer")]
    [DataRow(typeof(ParseOstHessenHandler), "messages")]
    [DataRow(typeof(ParseOstHessenHandler), "logger")]
    [DataRow(typeof(DocumentResultWriter), "jsonSerializer")]
    [DataRow(typeof(DocumentResultWriter), "xmlSerializer")]
    [DataRow(typeof(DocumentResultWriter), "destination")]
    [DataRow(typeof(MqttResultWriter), "serializer")]
    [DataRow(typeof(MqttResultWriter), "queue")]
    [DataRow(typeof(OutputDestination), "files")]
    [DataRow(typeof(OutputDestination), "logger")]
    [DataRow(typeof(CommandExecutor), "commandRegistrar")]
    [DataRow(typeof(CommandExecutor), "libraryErrors")]
    public void Constructor_NullDependency_ThrowsArgumentNullException(Type type, string parameterName)
    {
        var constructor = type.GetConstructors().Single();
        var arguments = new object?[constructor.GetParameters().Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            var parameter = constructor.GetParameters()[i];
            if (parameter.Name == parameterName)
            {
                arguments[i] = null;
            }
            else
            {
                var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(parameter.ParameterType))!;
                arguments[i] = mock.Object;
            }
        }

        Action act = () => Activator.CreateInstance(type, arguments);
        var exception = act.Should().Throw<TargetInvocationException>().Which.InnerException;
        exception.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be(parameterName);
    }
}
