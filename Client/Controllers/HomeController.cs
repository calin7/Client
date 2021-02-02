using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Client.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _client;
        private readonly string baseUrl = "https://localhost:44382";
        private readonly IDistributedCache _cache;
        private readonly string tokenKey = "token";

        public HomeController(ILogger<HomeController> logger, IDistributedCache cache)
        {
            _logger = logger;
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            _client = new HttpClient(clientHandler);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _cache = cache;
        }

        public async Task<ActionResult> Index()
        {
            return View();

        }
        [HttpPost]
        public async Task<JsonResult> LoginMethod(string username, string password)
        {
            var uri = baseUrl + "/gateway/authenticate";
            var request = new LoginRequest()
            {
                Username = username,
                Password = password
            };
            var jsonInString = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonInString, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(uri, content);

            if (response.IsSuccessStatusCode)
            {
                var stringResult = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<LoginResponse>(stringResult);
                _cache.SetString(tokenKey, result.JwtToken);
                return Json(new { valid = true });

            }
            else
            {
                return Json(new { valid = false });

            }
        }

        [HttpGet]
        public async Task<JsonResult> GetMemes()
        {
            var uri = baseUrl + "/gateway/meme";
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            try
            {
                AddHeaders(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }


            var res = await _client.SendAsync(request);

            if (res.IsSuccessStatusCode)
            {
                var memeResponse =await res.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<MemeModel>>(memeResponse);
                return Json(result);

            }
            else
            {
                return Json(new { valid = false });
            }
        }

        [HttpGet]
        public async Task<string> GenerateMeme(string topText, string bottomText)
        {
            var uri = baseUrl + "/gateway/generate";
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["meme"] = "10-Guy";
            query["top"] = topText;
            query["bottom"] = bottomText;
            uriBuilder.Query = query.ToString();

            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            try
            {
                AddHeaders(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            var res = await _client.SendAsync(request);

            if (res.IsSuccessStatusCode)
            {
                var memeResponse = await res.Content.ReadAsStringAsync();
                byte[] imageBytes = Encoding.Unicode.GetBytes(memeResponse);
                var encode = System.Convert.ToBase64String(imageBytes);
                return encode;

            }
            else
            {
                return "asd";
            }
        }
        private void AddHeaders(HttpRequestMessage request)
        {
            var token = _cache.GetString(tokenKey);
            if (token == null)
            {
                throw new Exception("unauthorized");
            }
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", "Bearer " + token);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AddMeme()
        {
            return View();
        }

        public async Task<ActionResult> PostMeme(MemeModel meme)
        {
            var uri = baseUrl + "/gateway/meme";
            string url;
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            try
            {
                AddHeaders(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            var jsonInString = JsonConvert.SerializeObject(meme);
            var content = new StringContent(jsonInString, Encoding.UTF8, "application/json");
            request.Content = content;
            var res = await _client.SendAsync(request);

            if (res.IsSuccessStatusCode)
            {
                return View("Privacy");

            }
            else
            {
                return Json(new { valid = false });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
