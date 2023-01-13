using System;

namespace RateLimiter 
{
    internal class DisposeAction : IDisposable
    {
        private Action _Act;

        public DisposeAction(Action act) 
        {
            _Act = act;
        }

        public void Dispose() 
        {
            _Act?.Invoke();
            _Act = null;
        }
    }
}
