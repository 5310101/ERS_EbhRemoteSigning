using System;
using System.Runtime.Caching;

namespace ws_GetResult_RemoteSigning.Cache
{
    public static class SigningCache
    {
        private static MemoryCache _profileCache = MemoryCache.Default;

        public static void SetSigningCache<T>(string transactionId, T signer)
        {
            if (ExistSigner(transactionId))
            {
                throw new Exception($"Profile with ID {transactionId} already exists in cache.");
            }
            CacheItemPolicy policy = new CacheItemPolicy
            {
                SlidingExpiration = System.TimeSpan.FromMinutes(30)
            };
            _profileCache.Add(transactionId, signer, policy);
        }

        public static T GetSignerCache<T>(string transactionId)
        {
            if (ExistSigner(transactionId))
            {
                return (T)_profileCache.Get(transactionId);
            }
            else
            {
                throw new Exception($"Profile with ID {transactionId} does not exist in cache.");
            }
        }

        public static bool ExistSigner(string transactionId)
        {
            return _profileCache.Contains(transactionId);
        }

        public static void RemoveSigner(string transactionId)
        {
            if (ExistSigner(transactionId))
            {
                _profileCache.Remove(transactionId);
            }
        }
    }
}
