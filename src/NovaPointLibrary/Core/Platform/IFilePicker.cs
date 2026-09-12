using System.Threading.Tasks;

namespace NovaPointLibrary.Core.Platform
{
    // Task-returning even though WPF's dialog is synchronous: Avalonia's StorageProvider is
    // async-only, and a synchronous signature here would force a second pass over every call site.
    public interface IFilePicker
    {
        Task<string?> PickFileAsync(string title, string filter);
    }
}
