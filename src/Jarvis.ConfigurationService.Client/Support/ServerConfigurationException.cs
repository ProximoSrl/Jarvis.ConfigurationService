using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Client.Support
{
    /// <summary>
    /// Exception launched when environment detect that the 
    /// configuration of the server
    /// </summary>
    public class ServerConfigurationException : Exception
    {
        public String ServerResponse { get; set; }

        public ServerConfigurationException(string message, string serverResponse) : base(message)
        {
            ServerResponse = serverResponse;
        }

        public ServerConfigurationException(string message, string serverResponse, Exception innerException)
            : base(message, innerException)
        {
            ServerResponse = serverResponse;
        }
    }
}
