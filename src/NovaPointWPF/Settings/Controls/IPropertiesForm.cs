using NovaPointLibrary.Core.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NovaPointWPF.Settings.Controls
{
    public interface IPropertiesForm
    {
        IAppClientProperties Properties { get; }
        void EnableForm();
        void DisableForm();
    }
}
