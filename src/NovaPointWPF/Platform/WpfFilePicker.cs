using Microsoft.Win32;
using NovaPointLibrary.Core.Platform;
using System.Threading.Tasks;

namespace NovaPointWPF.Platform
{
    public class WpfFilePicker : IFilePicker
    {
        public Task<string?> PickFileAsync(string title, string filter)
        {
            OpenFileDialog dialog = new()
            {
                Title = title,
                Filter = filter,
            };

            string? result = dialog.ShowDialog() == true ? dialog.FileName : null;
            return Task.FromResult(result);
        }
    }
}
