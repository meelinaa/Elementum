using System.Text.Json;

namespace Elementum_Cli.Providers
{
    public class HttpCall
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/api/v1/")
        };

        public static async Task<string> GetMetalListAsync()
        {
            return await SendRequestAsync<string>("metals/all");
        }

        public static async Task<string> GetPriceHistoryAllTodayAsync()
        {
            return await SendRequestAsync<string>("history/all/latest");
        }

        public static async Task<string> GetPriceHistoryTodayAsync(string metalSymbol)
        {
            return await SendRequestAsync<string>($"history/{metalSymbol}/latest");
        }

        public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol)
        {
            return await SendRequestAsync<string>($"history/{metalSymbol}");
        }

        public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol, string firstDate, string lastDate) // use this date format: yyyy-MM-dd
        {
            return await SendRequestAsync<string>($"history/{metalSymbol}/{firstDate}/{lastDate}");
        }

        /// <summary>
        /// History with aggregation (daily/weekly/monthly/yearly).
        /// API suggestion: GET history/{symbol}/aggregated?aggregation={daily|weekly|monthly|yearly}&amp;count={n}
        /// Returns exactly count values, displayed as chart in the CLI.
        /// </summary>
        public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol, string aggregation, int count)
        {
            var encodedAgg = Uri.EscapeDataString(aggregation);
            return await SendRequestAsync<string>($"history/{metalSymbol}/aggregated?aggregation={encodedAgg}&count={count}");
        }

        private static async Task<T> SendRequestAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request failed: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

            if (typeof(T) == typeof(string))
                return (T)(object)json;

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }
    }
}