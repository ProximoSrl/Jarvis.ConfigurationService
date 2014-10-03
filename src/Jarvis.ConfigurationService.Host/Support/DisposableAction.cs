using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Host.Support
{
    public struct DisposableAction : IDisposable
    {

        private Action _guardFunction;

        public DisposableAction(Action guardFunction)
        {
            this._guardFunction = guardFunction;
        }


        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (_guardFunction != null)
                _guardFunction();
        }

        #endregion


    }
}
