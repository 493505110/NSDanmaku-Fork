using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;


namespace NSDanmaku.WinUI.Helper
{
    class WebHelper
    {
        public async Task<string> GetResults(Uri url)
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip,
            };
            using (HttpClient hc = new HttpClient(handler))
            {
               
                HttpResponseMessage hr = await hc.GetAsync(url);
                hr.EnsureSuccessStatusCode();
                var encodeResults = await hr.Content.ReadAsByteArrayAsync();
                string results = Encoding.Default.GetString(encodeResults);
                return results;
            }
        }


    }
}
