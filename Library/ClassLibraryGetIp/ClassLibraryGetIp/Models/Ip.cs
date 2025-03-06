using System;

namespace ClassLibraryGetIp.Models
{
    public class Ip
    {
        private string _ip = default;
        private string _error = default;

        public Ip() { }

        public Ip(string ip, string error)
        {
            _ip = ip;
            _error = error;
        }

        public string GetIp()
        {
            if (_ip == null)
            {
                throw new InvalidOperationException("IP address is not set.");
            }
            return _ip;
        }

        public string GetError() 
        {
            if (_error == null) 
            {
                throw new InvalidOperationException("Error is not set");
            }
            return _error;
        }

        public void CreateError(string error)
        {
            if (_error == null)
                _error = error;
        }

        public void CreateIp(string ip)
        {
            if (_ip == null)
                _ip = ip;
        }
    }
}
