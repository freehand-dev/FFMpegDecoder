using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Hosting.WindowsServices;
using FFMpegDecoder.Services;
using System.Reflection;
using System.Text;
using FFMpegDecoder.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using FFMpegDecoder.Infrastructure.Metrics;
using FFMpegDecoder.Infrastructure;
using OpenTelemetry.Exporter;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

if (WindowsServiceHelpers.IsWindowsService())
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: Program.ServiceName, serviceInstanceId: Program.InstanceName, serviceVersion: Program.GetVersion());

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.Sources.Clear();
        IHostEnvironment env = context.HostingEnvironment;
        #region WorkingDirectory
        string workingDirectory = env.ContentRootPath;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            workingDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "FreeHand", env.ApplicationName);

        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            workingDirectory = System.IO.Path.Combine($"/opt/", env.ApplicationName, "etc", env.ApplicationName);
        }
        if (!System.IO.Directory.Exists(workingDirectory))
            System.IO.Directory.CreateDirectory(workingDirectory);

        config.SetBasePath(workingDirectory);

        // add workingDirectory service configuration
        config.AddInMemoryCollection(new Dictionary<string, string>
           {
              {"WorkingDirectory", workingDirectory}
              ,
           });
        #endregion

        //
        Console.WriteLine($"$Env:EnvironmentName={env.EnvironmentName}");
        Console.WriteLine($"$Env:ApplicationName={env.ApplicationName}");
        Console.WriteLine($"$Env:ContentRootPath={env.ContentRootPath}");
        Console.WriteLine($"WorkingDirectory={workingDirectory}");
        Console.WriteLine($"CurrentDirectory={Directory.GetCurrentDirectory()}");

        config.AddJsonFile($"{env.ApplicationName}.json", optional: true, reloadOnChange: true);
        config.AddIniFile($"{env.ApplicationName}.conf", optional: true, reloadOnChange: true);

        config.AddCommandLine(args);
        config.AddEnvironmentVariables();

        string? localConfig = context.Configuration.GetValue<string>("local-config");
        Console.WriteLine($"Config={localConfig ?? "none"}");

        Program.InstanceName = context.Configuration.GetValue<string>("name") ?? Program.InstanceName;
        Console.WriteLine($"ServiceName={Program.ServiceName}; InstanceName={Program.InstanceName}");

        // 
        if (!string.IsNullOrEmpty(Program.InstanceName))
        {
            config.AddJsonFile($"{Program.InstanceName}.json", optional: true, reloadOnChange: true);
            config.AddIniFile($"{Program.InstanceName}.conf", optional: true, reloadOnChange: true);
        }

        //
        if (!string.IsNullOrEmpty(localConfig))
        {
            switch (System.IO.Path.GetExtension(localConfig))
            {
                case ".json":
                    config.AddJsonFile(localConfig, optional: true, reloadOnChange: true);
                    break;
                case ".ini":
                case ".conf":
                    config.AddIniFile(localConfig, optional: true, reloadOnChange: true);
                    break;
                default:
                    break;
            }
        }
    })
    .ConfigureServices((context, services) =>
    {
        // FFMpegOutputParser
        services.AddSingleton<FFMpegOutputParser>();

        // FFMpegProgressListener
        services.AddHostedService<FFMpegProgressListener>();

        // Configuration FFMpegOptions
        services.Configure<DecoderOptions>(context.Configuration.GetSection("Decoder"));

        // FFMpegWorker
        services.AddHostedService<FFMpegWorker>();



        // OpenTelemetry
        services.AddOpenTelemetryMetrics(builder =>
        {
            builder.SetResourceBuilder(resourceBuilder);
            builder.AddMeter(FFMpegProgressMeter.Meter.Name);
            builder.AddOtlpExporter((exporterOptions, metricReaderOptions) =>
            {
                context.Configuration.GetSection("OpenTelemetry:OtlpExporter").Bind(exporterOptions);
                System.Console.WriteLine($"OTLP Exporter is using {exporterOptions.Protocol} protocol and endpoint {exporterOptions.Endpoint}");

                //
                //metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                //metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                //
                //exporterOptions.HttpClientFactory = () =>
                //{
                //    HttpClient client = new HttpClient();
                //    client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value");
                //    return client;
                //};
            });
            //builder.AddConsoleExporter();
        });


    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();

        logging.AddConfiguration((IConfiguration)context.Configuration.GetSection("Logging"));
        logging.AddConsole();


        // serilog
        string serilogPath = Path.Combine(context.Configuration["WorkingDirectory"] ?? Directory.GetCurrentDirectory(), $"logs\\{Program.InstanceName}_.txt");
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(serilogPath,
                        rollingInterval: RollingInterval.Day,
                        
                        rollOnFileSizeLimit: true)
            .CreateLogger();
        logger.Information($"Logging path is {Path.GetDirectoryName(serilogPath)}");
        logger.Information($"WorkingDirectory is {context.Configuration["WorkingDirectory"] ?? "none"}");
        logger.Information($"GetCurrentDirectory is {Directory.GetCurrentDirectory()}");
        logging.AddSerilog(logger);


        // OpenTelemetry
        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.AddOtlpExporter(otlpOptions =>
            {
                    context.Configuration.GetSection("OpenTelemetry:OtlpExporter").Bind(options);
            });
/*
            options.AddConsoleExporter(options =>
            {
                options.MetricReaderType = MetricReaderType.Periodic;
                options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
            });
*/
            // Export the body of the message
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
        });
    })
    .UseWindowsService(options =>
    {
        options.ServiceName = Program.GetServiceName();
    })
    .UseSystemd()
    .Build();


await host.RunAsync();


partial class Program
{
    public static string InstanceName = "Default";
    public static readonly string ServiceName = "FreeHand FFMpegDecoder";

    public static void PrintProductVersion()
    {
        var assembly = typeof(Program).Assembly;
        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Starting {product} v{version}...");
        Console.ResetColor();
    }


    public static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "unknown";
    }
    public static string GetServiceName()
    {
        return String.Format("{0} ({1})", Program.ServiceName, Program.InstanceName);
    }
}
