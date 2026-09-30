using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

/// <summary>Build-host tool invocation. Never passes font paths or charset values through a shell.</summary>
public sealed class GenerateMsdfAtlas : Task
{
    [Required] public string Generator { get; set; }
    [Required] public string Font { get; set; }
    [Required] public string ImageOutput { get; set; }
    [Required] public string MetadataOutput { get; set; }
    [Required] public string CompletionStamp { get; set; }
    [Required] public string Charset { get; set; }
    public int EmSize { get; set; } = 48;
    public float DistanceRange { get; set; } = 4;

    public override bool Execute()
    {
        if (EmSize <= 0 || DistanceRange <= 0 || float.IsNaN(DistanceRange) || float.IsInfinity(DistanceRange))
        {
            Log.LogError("MSDF EmSize and DistanceRange must be positive finite values.");
            return false;
        }
        var directory = Path.GetDirectoryName(Path.GetFullPath(ImageOutput));
        var staging = Path.Combine(directory, "pending-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.Delete(CompletionStamp);
            Directory.CreateDirectory(staging);
            var png = Path.Combine(staging, "atlas.png");
            var json = Path.Combine(staging, "atlas.json");
            Log.LogMessage(MessageImportance.High, "Generating MSDF atlas: {0}", Path.GetFileName(Font));
            if (!RunGenerator(png, json)) return false;
            if (!File.Exists(png) || !File.Exists(json) || new FileInfo(png).Length == 0 || new FileInfo(json).Length == 0)
            {
                Log.LogError("MSDF generator did not produce a nonempty PNG and JSON for {0}.", Font);
                return false;
            }
            File.Copy(png, ImageOutput, true);
            File.Copy(json, MetadataOutput, true);
            File.WriteAllText(CompletionStamp, "Generated successfully.");
            return true;
        }
        catch (Exception error)
        {
            Log.LogError("MSDF generation failed for {0}: {1}", Font, error.Message);
            return false;
        }
        finally
        {
            // Delete only the private, absolute staging directory beneath this output directory.
            if (Directory.Exists(staging) && Path.GetDirectoryName(Path.GetFullPath(staging)) == directory)
                Directory.Delete(staging, true);
        }
    }

    private bool RunGenerator(string png, string json)
    {
        var arguments = new[] { "-font", Font, "-chars", Charset, "-type", "msdf", "-format", "png",
            "-size", EmSize.ToString(CultureInfo.InvariantCulture), "-pxrange", DistanceRange.ToString(CultureInfo.InvariantCulture),
            "-yorigin", "bottom", "-nokerning", "-imageout", png, "-json", json };
        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo(Generator, string.Join(" ", arguments.Select(QuoteArgument)))
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Log.LogMessage(MessageImportance.Normal, stdout.GetAwaiter().GetResult());
            var diagnostic = stderr.GetAwaiter().GetResult();
            if (process.ExitCode == 0)
            {
                if (diagnostic.Length > 0) Log.LogMessage(MessageImportance.Normal, diagnostic);
                return true;
            }
            Log.LogError("msdf-atlas-gen exited with code {0}: {1}", process.ExitCode, diagnostic);
            return false;
        }
    }

    // ProcessStartInfo.Arguments uses quoted argument parsing, not shell expansion, on .NET build hosts.
    private static string QuoteArgument(string value)
    {
        var result = new StringBuilder("\"");
        var slashes = 0;
        foreach (var character in value)
        {
            if (character == '\\') { slashes++; continue; }
            result.Append('\\', character == '"' ? slashes * 2 + 1 : slashes);
            result.Append(character);
            slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }
}
