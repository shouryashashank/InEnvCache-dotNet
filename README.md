# InEnvCache
InEnvCache is a dotNet library designed to facilitate caching across pods within an environment, leveraging the Kubernetes API for distributed cache management. This framework allows for efficient and secure storage and retrieval of cached objects, making it ideal for applications running in Kubernetes clusters.

## Features

- **Encryption Support**: Secure your cached data with optional encryption.
- **Kubernetes Integration**: Seamlessly integrates with Kubernetes for distributed caching across pods.
- **Environment Variable Fallback**: Uses environment variables for caching if Kubernetes API is not accessible.
- **Thread-Safe Operations**: Ensures thread safety with lock mechanisms for concurrent operations.

### Quick Start

To get started with InEnvCache, follow these simple steps:

1. **Installation**: First, install the library using pip:

    ```bash
    dotnet add package InEnv.Cache 
    ```

2. **Initialization**: Import and initialize InEnvCache in your dotNet application:

    ```dotNet
    using InEnvCache;

    # Create an instance of InEnvCache with optional encryption key
    var cache = new InEnvCache.InEnvCache("test-key");
    ```

3. **Set a Cache Value**: Store a value in the cache with an optional time-to-live (TTL):

    ```dotNet
    cache.Set("key1", "value1", 600);
    ```

4. **Get a Cache Value**: Retrieve a value from the cache:

    ```dotNet
    value = cache.Get("key1")
    Console.WriteLine(value);  # Output: value1
    ```

5. **Delete a Cache Value**: Remove a value from the cache:

    ```dotNet
    cache.Delete("key1")
    ```

6. **Flush the Cache**: Clear all values from the cache:

    ```dotNet
    cache.FlushAll()
    ```

With these steps, you can easily integrate InEnvCache into your Kubernetes-based applications for efficient caching solutions.
## License

This project is licensed under multiple licenses:

- For **free users**, the project is licensed under the terms of the GNU Affero General Public License (AGPL). See  [`LICENSE-AGPL`](LICENSE-AGPL) for more details.

- For **paid users**, there are two options:
    - A perpetual commercial license. See [`LICENSE-COMMERCIAL-PERPETUAL`](LICENSE-COMMERCIAL-PERPETUAL) for more details.
    - A yearly commercial license. See [`LICENSE-COMMERCIAL-YEARLY`](LICENSE-COMMERCIAL-YEARLY) for more details.

Please ensure you understand and comply with the license that applies to you.
