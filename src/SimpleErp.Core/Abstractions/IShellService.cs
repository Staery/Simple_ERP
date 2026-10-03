namespace SimpleErp.Core.Abstractions;

/// <summary>Hands files and folders over to the operating system.</summary>
public interface IShellService
{
    /// <summary>Opens a file or folder with its default application.</summary>
    /// <exception cref="InvalidOperationException">The operating system could not open it.</exception>
    void Open(string path);
}
