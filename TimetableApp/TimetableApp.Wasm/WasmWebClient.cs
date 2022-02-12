using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.Foundation;

namespace TimetableApp.Core
{
    public partial class WasmWebClient : IDisposable
    {
        public static async Task FetchNoCors(string url)
        {
            Console.WriteLine("LMAO");
            var result = await WebAssemblyRuntime.InvokeAsync($"fetch('{url}', {{mode: 'no-cors'}}).then(res => res.blob()).then(blob => {{return blob.text();}});");
            Console.WriteLine(result);
            Console.WriteLine($"{result.Length} bytes received.");
        }

        public static async Task FetchNoCors(string url, long from, long to)
        {
            Console.WriteLine("LMAO");
            var result = await WebAssemblyRuntime.InvokeAsync($"fetch('{url}', {{mode: 'no-cors', headers: {{'range': 'bytes={from}-{to}'}}}});");
            Console.WriteLine(result);
        }

        public static async Task<byte[]> FetchProxy(string url)
        {
            var result = await WebAssemblyRuntime.InvokeAsync($@"
            {{
                    const buf2hex = (buffer) => 
                    {{ // buffer is an ArrayBuffer
                        return Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join('');
                    }}
                    return fetch('{CorsServer}/{url}')
                    .then(response => response.blob())
                    .then(blob => blob.arrayBuffer())
                    .then(buffer => buf2hex(buffer));
            }}");

            return HexStringToByte(result);
        }

        public async Task<byte[]> DownloadDataTaskAsync(string url)
        {
            var result = await WebAssemblyRuntime.InvokeAsync($@"FetchToBuffer('{WebAssemblyRuntime.EscapeJs($"{CorsServer}/{url}")}')");
            long[] ints = result.Split(',').Select(x => long.Parse(x)).ToArray();
            IntPtr ptr = (IntPtr)ints[0];
            int length = (int)ints[1];

            byte[] managedArray = new byte[length];
            Marshal.Copy(ptr, managedArray, 0, length);

            WebAssemblyRuntime.InvokeJS($@"Free({ints[0]})");

            return managedArray;
        }

        private static byte[] HexStringToByte(string hex)
        {
            if ((hex.Length & 1) == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public void Dispose()
        {
            // no-op
        }
    }
}