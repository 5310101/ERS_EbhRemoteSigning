using IntrustCA_Domain.Dtos;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Cache
{
    public static class SessionCache
    {
        private static MemoryCache _sessionCache = MemoryCache.Default;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public static SignSessionStore GetOrSetStore(string uid, ICACertificate cert)
        {
            if (_sessionCache.Contains(uid)) return (SignSessionStore)_sessionCache.Get(uid);
            var store = new SignSessionStore(uid, cert);
            var policy = new CacheItemPolicy
            {
                //sau 5 phut session het han ma ko goi refresh session thi se bi xoa khoi cache    
                AbsoluteExpiration = store.ValidUntil.AddMinutes(5),
            };
            _sessionCache.Add(uid, store, policy);
            return store;   
        }

        public static async Task<SignSessionStore> GetOrSetStoreAsync(string uid, ICACertificate cert)
        {
            if (_sessionCache.Contains(uid)) return (SignSessionStore)_sessionCache.Get(uid);
            var sem = _locks.GetOrAdd(uid, k => new SemaphoreSlim(1, 1));
            //chan thread
            await sem.WaitAsync();
            try
            {
                if (_sessionCache.Contains(uid)) return (SignSessionStore)_sessionCache.Get(uid);
                var store = new SignSessionStore(uid, cert);
                var policy = new CacheItemPolicy
                {   
                    AbsoluteExpiration = store.ValidUntil.AddMinutes(30),
                };
                _sessionCache.Add(uid, store, policy);
                return store;
            }
            //catch(Exception ex)
            //{
            //    Utilities.logger.ErrorLog(ex,"Error when creating SessionStore");
            //    return null;
            //}
            finally
            {
                sem.Release();
                _locks.TryRemove(uid, out _);
            }
        }

        public static bool ExistStore(string uid)
        {
            return _sessionCache.Contains(uid);
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
