namespace SRGL;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using SRGL.Common;

// source generator context for RawChart
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(RawChart))]
internal sealed partial class RawChartJsonContext: JsonSerializerContext {}

public static class RawChartLoader
{
    /// <summary>
    /// Parses a chart file as RawChart, sorts arrays, and verifies.
    /// </summary>
    public static RawChart Load(string path)
    {
        // get extension (lower case only)
        string ext = Path.GetExtension(path).ToLower();
        RawChart rc; // return value

        // parse a chart file
        switch(ext)
        {
            case ".json":
            rc = ParseJson(path);
            break;

            default:
            throw new NotSupportedException($"unsupported chart file format: {ext}");
        }

        // sort arrays
        Array.Sort(rc.Tempos, (a, b) => a.StartTick.CompareTo(b.StartTick));
        Array.Sort(rc.TimeSignatures, (a, b) => a.StartTick.CompareTo(b.StartTick));
        Array.Sort(rc.SvChanges, (a, b) => a.StartTick.CompareTo(b.StartTick));
        Array.Sort(rc.Notes, (a, b) => a.StartTick.CompareTo(b.StartTick));

        // verify rc
        RawChartVerifier.Verify(rc);
        
        return rc;
    }

    private static RawChart ParseJson(string path)
    {
        // check if a file exists
        if(!Godot.FileAccess.FileExists(path))
        {
            throw new FileNotFoundException($"chart file not found: {path}");
        }

        // read the file
        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        
        // parse json
        try
        {
            return JsonSerializer.Deserialize(json, RawChartJsonContext.Default.RawChart);
        }
        catch(System.Exception e)
        {
            throw new SrglException($"JSON parsing error: {path}\n{e.Message}");
        }
    }
}