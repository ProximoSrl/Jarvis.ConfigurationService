using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Host.Model
{
    /// <summary>
    /// Specify what happens when a parameter is missing from the list.
    /// </summary>
    public enum MissingParametersAction
    {
        /// <summary>
        /// This should never be used.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// If a parameter is missing, an exception will be thrown.
        /// </summary>
        Throw = 1,

        /// <summary>
        /// If a parameter is missing, completely ignore the problem, and leaving
        /// the original text in the value.
        /// </summary>
        Ignore = 2,

        /// <summary>
        /// Set an empty string when a parameters value is missing instead of leaving
        /// everything as is.
        /// </summary>
        Blank = 3
    }
}
