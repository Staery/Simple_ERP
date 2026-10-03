using System.ComponentModel;
using System.Diagnostics;
using SimpleErp.Core.Abstractions;

namespace SimpleErp.Services;

/// <summary>Opens files and folders with the user's default applications.</summary>
internal sealed class ShellService : IShellService
{
    public void Open(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Could not open '{path}': {ex.Message}", ex);
        }
    }
}
