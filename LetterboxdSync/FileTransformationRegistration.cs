using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

/// <summary>
/// Registers the index.html transformation with the File Transformation plugin once the server has
/// started (so all plugin assemblies are loaded). Uses reflection because plugins live in separate
/// assembly load contexts and cannot reference each other directly.
/// </summary>
public class FileTransformationRegistration : IHostedService
{
    private readonly ILogger<FileTransformationRegistration> _logger;

    public FileTransformationRegistration(ILogger<FileTransformationRegistration> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            RegisterWithFileTransformation();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not register with the File Transformation plugin. The per-user Letterboxd menu will be unavailable. Install it from https://github.com/IAmParadox27/jellyfin-plugin-file-transformation");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void RegisterWithFileTransformation()
    {
        var fileTransformationAssembly = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(assembly => assembly.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) ?? false);

        if (fileTransformationAssembly == null)
        {
            _logger.LogInformation("File Transformation plugin not found; the per-user Letterboxd menu will be unavailable.");
            return;
        }

        var pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
        var registerMethod = pluginInterfaceType?.GetMethod("RegisterTransformation", BindingFlags.Public | BindingFlags.Static);

        if (registerMethod == null)
        {
            _logger.LogWarning("File Transformation plugin found but RegisterTransformation could not be located (API changed?).");
            return;
        }

        // The parameter is a Newtonsoft JObject; build it via its static Parse(string) method so we
        // don't need a compile-time reference to Newtonsoft.Json.
        var payloadType = registerMethod.GetParameters()[0].ParameterType;
        var parseMethod = payloadType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });

        if (parseMethod == null)
        {
            _logger.LogWarning("Could not find JObject.Parse to build the File Transformation payload.");
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            id = Plugin.Instance!.Id.ToString(),
            fileNamePattern = "index.html",
            callbackAssembly = typeof(IndexHtmlTransformer).Assembly.FullName,
            callbackClass = typeof(IndexHtmlTransformer).FullName,
            callbackMethod = nameof(IndexHtmlTransformer.TransformIndexHtml),
        });

        var payload = parseMethod.Invoke(null, new object[] { payloadJson });
        registerMethod.Invoke(null, new[] { payload });

        _logger.LogInformation("Registered index.html transformation with the File Transformation plugin.");
    }
}
