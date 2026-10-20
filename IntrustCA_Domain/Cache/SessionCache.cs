using IntrustCA_Domain.Dtos;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Cache
{
    public static class SessionCache
    {
        private static MemoryCache _sessionCache = MemoryCache.Default;
        //giu lock theo uid suot vong doi service, ko xoa de tranh 2 thread cung tao phien cho 1 uid
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        //luu lan tao phien loi gan nhat theo uid, trong thoi gian cooldown ko gui yeu cau tao phien moi len app nua
        private static readonly ConcurrentDictionary<string, (DateTime FailedAt, Exception Error)> _failures = new ConcurrentDictionary<string, (DateTime, Exception)>();

        private static int FailCooldownSeconds
        {
            get
            {
                int seconds;
                return int.TryParse(ConfigurationManager.AppSettings["SESSION_FAIL_COOLDOWN"], out seconds) ? seconds : 60;
            }
        }

        public static SignSessionStore GetStore(string uid)
        {
            return _sessionCache.Get(uid) as SignSessionStore;
        }

        public static async Task<SignSessionStore> GetOrSetStoreAsync(string uid, ICACertificate cert)
        {
            var cached = GetStore(uid);
            if (cached != null) return cached;
            var sem = _locks.GetOrAdd(uid, k => new SemaphoreSlim(1, 1));
            //chan thread
            await sem.WaitAsync();
            try
            {
                cached = GetStore(uid);
                if (cached != null) return cached;

                //vua tao phien loi (user tu choi/ko xac nhan kip) thi ko gui them yeu cau len app, de message retry sau
                if (_failures.TryGetValue(uid, out var failure) && (DateTime.Now - failure.FailedAt).TotalSeconds < FailCooldownSeconds)
                {
                    throw new Exception($"Create session failed recently, wait before retry: {failure.Error.Message}", failure.Error);
                }

                SignSessionStore store;
                try
                {
                    store = new SignSessionStore(uid, cert);
                }
                catch (Exception ex)
                {
                    _failures[uid] = (DateTime.Now, ex);
                    throw;
                }
                _failures.TryRemove(uid, out _);
                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = store.ValidUntil.AddMinutes(30),
                };
                _sessionCache.Set(uid, store, policy);
                return store;
            }
            finally
            {
                sem.Release();
            }
        }

        public static bool ExistStore(string uid)
        {
            return _sessionCache.Contains(uid);
        }

        public static void RemoveStore(string uid)
        {
            _sessionCache.Remove(uid);
        }

        /// <summary>
        /// Chi xoa neu store trong cache van la store da dung, tranh xoa nham phien moi vua duoc tao boi thread khac
        /// </summary>
        public static void RemoveStore(string uid, SignSessionStore store)
        {
            if (ReferenceEquals(_sessionCache.Get(uid), store))
            {
                _sessionCache.Remove(uid);
            }
        }
    }
}
