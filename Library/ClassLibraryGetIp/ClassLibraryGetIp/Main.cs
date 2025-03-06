using ClassLibraryGetIp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ClassLibraryGetIp
{
    public class Main
    {
        private readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://192.168.100.100:5048")
        };

        public async Task<Ip> GetIp(string serverName)
        {
            Ip ip = new Ip();
            string _errorMessage = "Ошибка";

            try
            {
                var cancellationToken = new CancellationTokenSource();
                cancellationToken.CancelAfter(TimeSpan.FromSeconds(30)); // Отмена через 30 секунд

                // Формируем строку запроса
                var requestUriString = $"/api/Macaddresstables/GetIpAsync?serverName={serverName}";

                // Выполняем запрос
                var response = await client.GetAsync(requestUriString, cancellationToken.Token);

                // Проверяем успешность запроса
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                    if (result.FirstOrDefault().Value != null)
                    {
                        ip.CreateError(result.FirstOrDefault().Value);
                        return ip;
                    }
                    else
                    {
                        ip.CreateIp(result.FirstOrDefault().Key);
                    }
                }
                else
                {
                    // Логируем ошибку HTTP
                    _errorMessage = $"Ошибка HTTP-запроса: {(int)response.StatusCode} - {response.ReasonPhrase}";
                }
            }
            catch (HttpRequestException ex)
            {
                _errorMessage = $"Ошибка HTTP-запроса: {ex.Message}";
            }
            catch (TaskCanceledException ex)
            {
                _errorMessage = $"Запрос был отменён (таймаут или отмена токеном)\n: {ex.Message}";
                Console.WriteLine(_errorMessage);
            }
            catch(InvalidOperationException ex)
            {
                _errorMessage = $"{ex.Message}";
            }
            catch (Exception ex)
            {
                _errorMessage = "Произошла неожиданная ошибка.";
            }

            ip.CreateError(_errorMessage);

            return ip;
        }
    }
}
