using System;
using System.Runtime.Caching;

namespace CA2_Winservice.Cache
{
    public static class ProfileCache
    {
        private static MemoryCache _profileCache = MemoryCache.Default;

        public static void SetProfileCache<T>(string profileID, T profile) 
        {
            //thong thuong ko dc de ton tai truong hop nay, lay thang docid cua profile lam khoa
            if (ExistProfile(profileID))
            {
                throw new Exception($"Profile with ID {profileID} already exists in cache.");
            }
            CacheItemPolicy policy = new CacheItemPolicy
            {
                SlidingExpiration = System.TimeSpan.FromMinutes(30) 
            };
            _profileCache.Add(profileID, profile, policy);  
        }

        public static T GetProfileCache<T>(string profileID)
        {
            if (ExistProfile(profileID))
            {
                return (T)_profileCache.Get(profileID);
            }
            else
            {
                throw new Exception($"Profile with ID {profileID} does not exist in cache.");
            }
        }   

        public static bool ExistProfile(string profileID)
        {
            return _profileCache.Contains(profileID);
        }   

        public static void RemoveProfile(string profileID)
        {
            if (ExistProfile(profileID))
            {
                _profileCache.Remove(profileID);
            }
        }   
    }
}
