using System;
using System.IO;
using UnityEngine;

namespace SGH.Tests
{
    internal static class LiveTestConfig
    {
        public static string Region
        {
            get
            {
                string region = Get("SGH_PHOTON_REGION")?.Trim().ToLowerInvariant();
                return string.IsNullOrEmpty(region) || region == "sg" ? "asia" : region;
            }
        }
        [Serializable] private class Config { public string appId; public string chatAppId; public string region; }
        public static string Get(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(value)) return value;
            const string path = ".sgh-ci-test-config.json";
            if (!File.Exists(path)) return null;
            var config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
            if (name == "SGH_PHOTON_APP_ID") return config.appId;
            if (name == "SGH_PHOTON_CHAT_APP_ID") return config.chatAppId;
            if (name == "SGH_PHOTON_REGION") return config.region;
            return null;
        }
    }
}
