using System.Diagnostics;
using System.Text.Json.Serialization;
using System.IO;
using DotRush.Common.Extensions;

namespace DotRush.Debugging.NetCore.Models;

public class ProcessInfo {
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }

    public ProcessInfo() { }
    public ProcessInfo(Process process) {
        Id = process.Id;

        string? fileName = process.MainModule?.FileName;

        if (!string.IsNullOrEmpty(fileName)) 
        {
            char[] split = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            int idx = fileName.LastIndexOfAny(split);
            string processName = fileName.Substring(idx + 1);            
            Name = processName;
        }
        if (string.IsNullOrEmpty(Name)) 
        {
            Name = process.ProcessName;
        }
        StartTime = SafeExtensions.Invoke(() => process.StartTime.ToShortTimeString());
    }
}