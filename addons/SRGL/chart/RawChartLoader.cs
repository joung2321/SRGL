namespace SRGL;

using Godot;
using System;
using System.IO;
using System.Text.Json;

public static class RawChartLoader
{
    public static RawChart Load(string path)
    {
        // get extension (lower case only)
        string ext = Path.GetExtension(path).ToLower();

        switch(ext)
        {
            case ".json":
            return ParseJson(path);
            
            default:
            throw new FormatException(path);
        }
    }

    private static RawChart ParseJson(string path)
    {
        // check if a file exists
        if(!Godot.FileAccess.FileExists(path))
        {
            GD.PrintErr("Chart file not found: ", path);
            return null;
        }

        // read the file
        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        string json = file.GetAsText();

        // parsing option
        JsonSerializerOptions option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // parse json
        try
        {
            return JsonSerializer.Deserialize<RawChart>(json, option);
        }
        catch(System.Exception e)
        {
            GD.PrintErr("JSON parsing error: ", e.Message);
            return null;
        }
    }
}