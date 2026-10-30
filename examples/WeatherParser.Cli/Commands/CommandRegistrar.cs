using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Cli.Commands;

/// <summary>
/// Registers the handlers and the output-mode writer in the composition root (CL-09, CL-12).
/// </summary>
public sealed class CommandRegistrar : ICommandRegistrar
{
    /// <inheritdoc/>
    public void Register(IServiceCollection services, CommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(request);

        services.AddSingleton<IExportErrorMessages, ExportErrorMessages>();
        services.AddSingleton<ILibraryErrors, LibraryErrors>();
        services.AddSingleton<IFailureClassifier, FailureClassifier>();
        services.AddSingleton<IFileOperations, FileOperations>();
        services.AddSingleton<IOutputDestination, OutputDestination>();

        // JSON and XML mode register no publisher and never construct the MQTT serializer (REQ-067).
        if (request.Common.Output == OutputFormat.Mqtt)
        {
            services.AddSingleton<IMessagePublisher, UnavailableMessagePublisher>();
            services.AddSingleton<IResultWriter, MqttResultWriter>();
        }
        else
        {
            services.AddSingleton<IResultWriter, DocumentResultWriter>();
        }

        // A parser or refresher that serves only another product or flag is never constructed (CL-17, MF-01).
        switch (request)
        {
            case SelectDwdRequest:
                services.AddSingleton<ICommandHandler<SelectDwdRequest>, SelectDwdHandler>();
                break;
            case SelectMosmixRequest select:
                services.AddSingleton<ICommandHandler<SelectMosmixRequest>>(provider => new SelectMosmixHandler(
                    provider.GetRequiredService<IMosmixLocationSelector>(),
                    provider.GetRequiredService<IResultWriter>(),
                    select.RefreshMosmixSEvidence ? provider.GetRequiredService<IMosmixSEvidenceRefresher>() : null));
                break;
            case ParseDwdRequest:
                services.AddSingleton<ICommandHandler<ParseDwdRequest>, ParseDwdHandler>();
                break;
            case ParseOstHessenRequest:
                services.AddSingleton<ICommandHandler<ParseOstHessenRequest>, ParseOstHessenHandler>();
                break;
            case ParseMosmixRequest parse:
                services.AddSingleton<ICommandHandler<ParseMosmixRequest>>(provider => new ParseMosmixHandler(
                    parse.Product == MosmixProduct.MosmixL ? provider.GetRequiredService<IMosmixLParser>() : null,
                    parse.Product == MosmixProduct.MosmixS ? provider.GetRequiredService<IMosmixSParser>() : null,
                    provider.GetRequiredService<IResultWriter>(),
                    provider.GetRequiredService<IExportErrorMessages>(),
                    provider.GetRequiredService<ILibraryErrors>(),
                    provider.GetRequiredService<IFailureClassifier>(),
                    provider.GetRequiredService<ILogger<ParseMosmixHandler>>()));
                break;
        }
    }
}
