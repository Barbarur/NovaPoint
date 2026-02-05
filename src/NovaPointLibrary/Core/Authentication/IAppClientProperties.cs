using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Core.Authentication
{
    public interface IAppClientProperties
    {
        Guid Id { get; set; }
        string ClientTitle { get; set; }
    }
}
