using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Threading;

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
                kubernetesClient.GetAPIResources(); // Check if Kubernetes API is available
                kubernetesCache = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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

                // Logic to interact with Kubernetes ConfigMap or Environment Variables
                // Similar to Python implementation
            }
        }

        public string Get(string key)
        {
            lock (lockObject)
            {
                key = "InEnvCache_" + key;
                // Logic to retrieve data from Kubernetes ConfigMap or Environment Variables
                // Decrypt if encryption is enabled
                // Similar to Python implementation
                return ""; // Placeholder return
            }
        }

        public void Delete(string key)
        {
            lock (lockObject)
            {
                key = "InEnvCache_" + key;
                // Logic to delete data from Kubernetes ConfigMap or Environment Variables
                // Similar to Python implementation
            }
        }

        public void FlushAll()
        {
            lock (lockObject)
            {
                // Logic to flush all data from Kubernetes ConfigMap or Environment Variables
                // Similar to Python implementation
            }
        }
    }
}
