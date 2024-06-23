// See https://aka.ms/new-console-template for more information
using InEnvCache;

internal class TestInEnvCache
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var iec = new InEnvCache.InEnvCache("test-key");

        Console.WriteLine("set key");
        iec.Set("aa", "aa-value");
        var aa = iec.Get("aa");
        Console.WriteLine(aa);
        iec.Delete("aa");
        iec.Set("bb", "bb-value", 10);
        var bb = iec.Get("bb");
        Console.WriteLine(bb);
        while (bb != null)
        {
            Thread.Sleep(new TimeSpan(0, 0, 2));
            bb = iec.Get("bb");
            Console.WriteLine(bb);
        }
        Console.WriteLine("bb cache timed out");
        iec.Set("bb", "bb-value", 10);
        iec.FlushAll();
        Console.WriteLine("Flush all");
        bb = iec.Get("bb");
        Console.WriteLine(bb);
    }
}