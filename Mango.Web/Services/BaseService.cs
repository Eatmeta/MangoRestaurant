using System.Net.Http.Headers;
using System.Text;
using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Newtonsoft.Json;

namespace Mango.Web.Services;

public class BaseService : IBaseService
{
    public ResponseDto responseModel { get; set; }
    public IHttpClientFactory httpClient { get; set; }

    public BaseService(IHttpClientFactory httpClient)
    {
        responseModel = new ResponseDto();
        this.httpClient = httpClient;
    }

    public async Task<T> SendAsync<T>(ApiRequest apiRequest)
    {
        try
        {
            var client = httpClient.CreateClient("MangoAPI");
            var message = new HttpRequestMessage();
            
            message.Headers.Add("Accept", "application/json");
            message.RequestUri = new Uri(apiRequest.Url);
            client.DefaultRequestHeaders.Clear();
            
            if (apiRequest.Data != null)
            {
                message.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data),
                    Encoding.UTF8, "application/json");
            }

            if (!string.IsNullOrEmpty(apiRequest.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiRequest.AccessToken);
            }

            HttpResponseMessage apiResponse = null;
            
            message.Method = apiRequest.ApiType switch
            {
                SD.ApiType.POST => HttpMethod.Post,
                SD.ApiType.PUT => HttpMethod.Put,
                SD.ApiType.DELETE => HttpMethod.Delete,
                _ => HttpMethod.Get
            };
            apiResponse = await client.SendAsync(message);

            var apiContent = await apiResponse.Content.ReadAsStringAsync();
            var apiResponseDto = JsonConvert.DeserializeObject<T>(apiContent);
            return apiResponseDto;
        }
        catch (Exception e)
        {
            var dto = new ResponseDto
            {
                DisplayMessage = "Error",
                ErrorMessages = new List<string> {Convert.ToString(e.Message)},
                IsSuccess = false
            };
            var res = JsonConvert.SerializeObject(dto);
            var apiResponseDto = JsonConvert.DeserializeObject<T>(res);
            return apiResponseDto;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(true);
    }
}
