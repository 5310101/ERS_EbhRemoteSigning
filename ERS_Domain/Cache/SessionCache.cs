using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using System.Runtime.Caching;

namespace ERS_Domain.Cache
{
    public static class SessionCache
    {
        private static MemoryCache _sessionCache = MemoryCache.Default;

        public static SignSessionStore GetOrSetStore(string uid, ICACertificate cert)
        {
            if (_sessionCache.Contains(uid)) return (SignSessionStore)_sessionCache.Get(uid);
            var store = new SignSessionStore(uid, cert);
            var policy = new CacheItemPolicy
            {
                //sau 30 phut session het han ma ko goi refresh session thi se bi xoa khoi cache    
                AbsoluteExpiration = store.ValidUntil.AddMinutes(30),
            };
            _sessionCache.Add(uid, store, policy);
            return store;   
        }

        public static void RemoveStore(string uid)
        {
            if (_sessionCache.Contains(uid))
            {
                _sessionCache.Remove(uid);
            }
        }
    }
}
