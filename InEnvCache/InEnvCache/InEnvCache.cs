using System;
using System.Text;
using System.Security.Cryptography;
using k8s;
using k8s.Models;
using Newtonsoft.Json;

namespace InEnvCache
{

    public class InEnvCache
    {
        private readonly Dictionary<string, object> cache = new Dictionary<string, object>();
        private bool enableEncryption = false;
        private byte[] key;
        private readonly string configmapName;
        private readonly string namespaceName;
        private readonly bool kubernetesCache = false;
        private readonly IKubernetes kubernetesClient;
        private readonly object lockObject = new object();

        public InEnvCache(string key = null, string configmapName = "iec-configmap", string namespaceName = "default")
        {
            this.configmapName = configmapName;
            this.namespaceName = namespaceName;

            if (!string.IsNullOrEmpty(key))
            {
                this.enableEncryption = true;
                this.key = Encoding.UTF8.GetBytes(key);
            }

            try
            {
                kubernetesClient = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
                kubernetesClient.CoreV1.GetAPIResources(); // Check if Kubernetes API is available
                kubernetesCache = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load Kubernetes usinf env");
                kubernetesCache = false;
            }
        }

        private byte[] PadKey(byte[] key)
        {
            int[] validSizes = { 16, 24, 32 };
            int len = key.Length;
            foreach (var size in validSizes)
            {
                if (len <= size)
                {
                    Array.Resize(ref key, size);
                    return key;
                }
            }
            throw new ArgumentException("Key size is not valid.");
        }

        private string[] Encrypt(string data)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = PadKey(key);
                aesAlg.GenerateIV();
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(data);
                        }
                    }

                    var iv = Convert.ToBase64String(aesAlg.IV);
                    var encryptedData = Convert.ToBase64String(msEncrypt.ToArray());

                    return new string[] { iv, encryptedData };
                }
            }
        }

        private string Decrypt(string[] encryptedDataWithIv)
        {
            string iv = encryptedDataWithIv[0];
            string encryptedData = encryptedDataWithIv[1];

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = PadKey(key);
                aesAlg.IV = Convert.FromBase64String(iv);
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedData)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        public void Set(string key, string value, int? ttl = null)
        {
            lock (lockObject)
            {
                key = "InEnvCache_" + key;
                if (enableEncryption)
                {
                    var encryptedData = Encrypt(value);
                    value = string.Join(":", encryptedData);
                }

                var cacheValue = new
                {
                    value = value,
                    expiry = ttl.HasValue ? DateTime.UtcNow.AddSeconds(ttl.Value).ToString("o") : null
                };

                if (kubernetesCache)
                {
                    // Read the ConfigMap
                    var configMap = kubernetesClient.CoreV1.ReadNamespacedConfigMap(configmapName, namespaceName);

                    // Update the ConfigMap
                    if (configMap.Data == null)
                    {
                        configMap.Data = new Dictionary<string, string>();
                    }
                    configMap.Data[key] = JsonConvert.SerializeObject(cacheValue);

                    kubernetesClient.CoreV1.PatchNamespacedConfigMap(new V1Patch(new { data = configMap.Data }, V1Patch.PatchType.MergePatch), configmapName, namespaceName);
                }
                else
                {
                    Environment.SetEnvironmentVariable(key, JsonConvert.SerializeObject(cacheValue));
                }
            }
        }

        public string Get(string key)
        {
            lock (lockObject)
            {
                key = "InEnvCache_" + key;
                string serializedValue;

                if (kubernetesCache)
                {
                    // Read the ConfigMap
                    var configMap = kubernetesClient.CoreV1.ReadNamespacedConfigMap(configmapName, namespaceName);
                    configMap.Data.TryGetValue(key, out serializedValue);
                }
                else
                {
                    serializedValue = Environment.GetEnvironmentVariable(key);
                }

                if (string.IsNullOrEmpty(serializedValue))
                {
                    return null;
                }

                var cacheValue = JsonConvert.DeserializeObject<dynamic>(serializedValue);
                if (cacheValue.expiry != null && cacheValue.expiry < DateTime.UtcNow)
                {
                    // Cache expired
                    return null;
                }

                var value = (string)cacheValue.value;
                if (enableEncryption)
                {
                    var encryptedParts = value.Split(':');
                    value = Decrypt(encryptedParts);
                }

                return value;
            }
        }

        public void Delete(string key)
        {
            lock (lockObject)
            {
                key = "InEnvCache_" + key;
                if (kubernetesCache)
                {
                    // Read the ConfigMap
                    var configMap = kubernetesClient.CoreV1.ReadNamespacedConfigMap(configmapName, namespaceName);
                    if (configMap.Data != null && configMap.Data.ContainsKey(key))
                    {
                        // Remove the key from ConfigMap
                        configMap.Data.Remove(key);

                        // Update the ConfigMap
                        kubernetesClient.CoreV1.PatchNamespacedConfigMap(new V1Patch(new { data = configMap.Data }, V1Patch.PatchType.MergePatch), configmapName, namespaceName);
                    }
                }
                else
                {
                    // Remove the environment variable
                    Environment.SetEnvironmentVariable(key, null);
                }
            }
        }

        public void FlushAll()
        {
            lock (lockObject)
            {
                if (kubernetesCache)
                {
                    // Read the ConfigMap
                    var configMap = kubernetesClient.CoreV1.ReadNamespacedConfigMap(configmapName, namespaceName);
                    if (configMap.Data != null)
                    {
                        // Clear all entries from the ConfigMap
                        configMap.Data.Clear();

                        // Update the ConfigMap
                        kubernetesClient.CoreV1.PatchNamespacedConfigMap(new V1Patch(new { data = configMap.Data }, V1Patch.PatchType.MergePatch), configmapName, namespaceName);
                    }
                }
                else
                {
                    // Get all environment variables
                    var allVars = Environment.GetEnvironmentVariables();
                    foreach (System.Collections.DictionaryEntry item in allVars)
                    {
                        var key = (string)item.Key;
                        if (key.StartsWith("InEnvCache_"))
                        {
                            // Remove the environment variable
                            Environment.SetEnvironmentVariable(key, null);
                        }
                    }
                }
            }
        }
    }
}
