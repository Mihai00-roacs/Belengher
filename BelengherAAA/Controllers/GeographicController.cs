using Microsoft.AspNetCore.Mvc;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Nancy.Json;
using System.Collections.Generic;
using System.Text;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Metrics;
using System.Reflection;
using BelengherAAA;

namespace BELENGHER.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeographicController : Controller
    {
        private const string SearchKey = "pXStedvXkA9pMcNK1tWvx_4DesmTsIZ47qfTa6WkqFxgrCvCqJA0mpALQ53J";
        private const string MatchKey = "Lk34BnMBMFDj07xGbkQ_aNikeD4_NSKq643WxEEuQUAcjtbrVJStX9FpASw7";
        private static Lazy<HttpClient> lazySearchClient = new Lazy<HttpClient>(InitializeHttpSearchClient);
        private static Lazy<HttpClient> lazyMatchClient = new Lazy<HttpClient>(InitializeHttpMatchClient);
        private static HttpClient searchClient => lazySearchClient.Value;
        private static HttpClient matchClient => lazyMatchClient.Value;
        [HttpGet]
        private static HttpClient InitializeHttpSearchClient()
        {
            var client = new HttpClient();

            // Perform any initialization here
            client.DefaultRequestHeaders.Add("x-api-key", SearchKey);
            client.DefaultRequestHeaders
        .Accept
        .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        private static HttpClient InitializeHttpMatchClient()
        {
            var client = new HttpClient();

            // Perform any initialization here
            client.DefaultRequestHeaders.Add("x-api-key", MatchKey);
            client.DefaultRequestHeaders
        .Accept
        .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        [HttpGet("GetCitiesInRangeAsync")]
        public async Task<IEnumerable<City>> GetCitiesInRangeAsync(double latitude, double longitude)
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync("https://revgeocode.search.hereapi.com/v1/revgeocode?types=city&apiKey=ftWQDHD12hXKn_33LqAOBSnbA1yD8FlLN686IxJcbpg&in=circle:" + latitude + "," + longitude + ";r=30000&limit=10");
            if (response.IsSuccessStatusCode)
            {
                var product = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(product);
                var cities = new List<City>();
                foreach (var item in json["items"])
                {
                    var address = item["address"];
                    var position = item["position"];
                    cities.Add(new City(address["countryCode"].ToString(), address["county"].ToString(), address["city"].ToString(), Double.Parse(item["distance"].ToString()), Double.Parse(position["lat"].ToString()), Double.Parse(position["lng"].ToString())));

                }
                return cities;
            }
            return null;
        }
        [HttpPost("GetCompaniesCountByType")]
        public async Task<int> GetCompaniesCountByType(List<String> types)
        {
            string json = new JavaScriptSerializer().Serialize(new
            {
                filters = new
                {
                    and = new List<Object>()
                    {
                        new
                        {
                        attribute = "company_location",
                        relation = "in",
                        value = new List<Object>()
                        {
                            new
                            {
                                country = "RO",
                                region = "Bucharest"
                            }
                        },
                        strictness = 1
                    },
                        new
                        {
                             attribute = "company_products",
                             relation = "match_expression",
                             value = new
                             {

                                     match = new
                                     {
                                         @operator = "Or",
                                         operands = new List<Object>(types)
                                     }

                             },
                             strictness=1
                        }
                        }
                },
            });
            HttpRequestMessage request = new(HttpMethod.Post, "https://data.veridion.com/search/v2/companies")
            {
                Content = new StringContent(json,
                                    Encoding.UTF8,
                                    "application/json")
            };
            HttpResponseMessage response = await searchClient.SendAsync(request);
            var responseJSON = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            response.EnsureSuccessStatusCode();
            return Int32.Parse(responseJSON["count"].ToString());
        }
        [HttpGet("GetNumberOfCompaniesWithSameActivityLevelInRangeAsync")]
        public async Task<IEnumerable<City>> GetNumberOfCompaniesWithSameActivityLevelInRangeAsync(Double latitude, Double longitude, String activityLevels)
        {
            var cities = new List<City>();
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = "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"
            });
            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
    {
        ChatMessage.FromUser("I will give you some coordinates, and I want you to give me only the country code, with no extra text"),
        ChatMessage.FromSystem("Certainly! Please provide the coordinates, and I'll give you the corresponding country code."),
        ChatMessage.FromUser(latitude+" "+longitude)
    },
                Model = Models.Gpt_4,
            });
            var countryCode = "";
            if (completionResult.Successful)
            {
                countryCode = completionResult.Choices.First().Message.Content;
            }
            string json = new JavaScriptSerializer().Serialize(new
            {
                filters = new
                {
                    and = new List<Object>()
                    {new
                        {
                        attribute = "company_location",
                        relation = "in",
                        value = new List<Object>()
                        {
                            new
                            {
                                country = countryCode
                            }
                        },
                        strictness = 1
                    },
                        new
                        {
                             attribute = "company_products",
                             relation = "match_expression",
                             value = new
                             {

                                     match = new
                                     {
                                         @operator = "Or",
                                         operands = new List<Object>(){activityLevels}
                                     }

                             },
                             strictness=1
                        }
                        }
                },
            });
            HttpRequestMessage request = new(HttpMethod.Post, "https://data.veridion.com/search/v2/companies?page_size=200")
            {
                Content = new StringContent(json,
                                    Encoding.UTF8,
                                    "application/json")
            };
            HttpResponseMessage response = await searchClient.SendAsync(request);
            var responseJSON = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            response.EnsureSuccessStatusCode();
            var next_page = responseJSON["pagination"]["next"];
            var count = 0;
            var jsonStringsForOpenAI = new List<string>();
            foreach (var item in responseJSON["result"])
            {
                foreach (var location in item["locations"])
                {
                    if (location["latitude"].ToString() != String.Empty && location["longitude"].ToString() != String.Empty)
                    {
                        var x = getDistanceFromLatLonInKm(latitude, longitude, Double.Parse(location["latitude"].ToString()), Double.Parse(location["longitude"].ToString()));
                        /*var x = Math.Abs(Math.Acos(
                            Math.Sin(latitude) * Math.Sin(Double.Parse(location["latitude"].ToString()))
                            + Math.Cos(latitude) * Math.Cos(Double.Parse(location["latitude"].ToString()))
                            * Math.Cos(Double.Parse(location["longitude"].ToString()) - longitude))) * 6371;*/

                        if (x < 50)
                        {
                            var javaScriptSerializer = new JavaScriptSerializer();
                            cities.Add(new City(location["country_code"].ToString(), location["region"].ToString(), location["city"].ToString(), 0, Double.Parse(location["latitude"].ToString()), Double.Parse(location["longitude"].ToString())));
                            count++;
                            if (item["employee_count"].ToString() != String.Empty || item["estimated_revenue"].ToString() != String.Empty)
                                jsonStringsForOpenAI.Add(new JavaScriptSerializer().Serialize(new
                                {
                                    estimatedRevenue = item["employee_count"].ToString(),
                                    employyesCount = item["estimated_revenue"].ToString()
                                }));
                        }
                    }
                }
            }
            while (next_page != null)
            {
                request = new(HttpMethod.Post, "https://data.veridion.com/search/v2/companies?page_size=200&pagination_token=" + next_page.ToString())
                {
                    Content = new StringContent(json,
                                    Encoding.UTF8,
                                    "application/json")
                };
                response = await searchClient.SendAsync(request);
                responseJSON = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                response.EnsureSuccessStatusCode();
                next_page = responseJSON["pagination"]["next"];
                foreach (var item in responseJSON["result"])
                {
                    foreach (var location in item["locations"])
                    {
                        if (location["latitude"].ToString() != String.Empty && location["longitude"].ToString() != String.Empty)
                        {
                            var x = getDistanceFromLatLonInKm(latitude, longitude, Double.Parse(location["latitude"].ToString()), Double.Parse(location["longitude"].ToString()));
                            /*var x = Math.Abs(Math.Acos(
                                Math.Sin(latitude) * Math.Sin(Double.Parse(location["latitude"].ToString()))
                                + Math.Cos(latitude) * Math.Cos(Double.Parse(location["latitude"].ToString()))
                                * Math.Cos(Double.Parse(location["longitude"].ToString()) - longitude))) * 6371;*/
                            if (x < 50)
                            {
                                count++;
                                cities.Add(new City(location["country_code"].ToString(), location["region"].ToString(), location["city"].ToString(), 0, Double.Parse(location["latitude"].ToString()), Double.Parse(location["longitude"].ToString())));
                                if (item["employee_count"].ToString() != String.Empty || item["estimated_revenue"].ToString() != String.Empty)
                                    jsonStringsForOpenAI.Add(new JavaScriptSerializer().Serialize(new
                                    {
                                        estimatedRevenue = item["employee_count"].ToString(),
                                        employyesCount = item["estimated_revenue"].ToString()
                                    }));
                            }
                        }
                    }
                }
            }

            completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
    {
        ChatMessage.FromUser("Given the JSON strings that I will sent you, can you estimate the range of employees a company needs to have and an estimated revenue, both computed as an average of each entry in the JSON input? If no data is provided, you will provide null for those entries. Can you make the output in a JSON format, with no other explanation?"),
        ChatMessage.FromSystem("Certainly!"),
        ChatMessage.FromUser("["+string.Join(",",jsonStringsForOpenAI)+"]")
    },
                Model = Models.Gpt_4,
            });
            if (completionResult.Successful)
            {
                Console.WriteLine(completionResult.Choices.First().Message.Content);
            }
            return cities;
        }
        [HttpGet("AskGPT")]
        public async Task<string> AskGPTAsync(Double latitude, Double longitude)
        {
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = "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"
            });
            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
    {
        ChatMessage.FromUser("I will give you some coordinates, and I want you to give me the region and the country in a format like this: Region,Country with no extra text"),
        ChatMessage.FromSystem("Certainly! Please provide the coordinates, and I'll give you the corresponding country code."),
        ChatMessage.FromUser(latitude+" "+longitude)
    },
                Model = Models.Gpt_4,
            });
            var countryCode = "";
            if (completionResult.Successful)
            {
                countryCode = completionResult.Choices.First().Message.Content;
            }
             openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = "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"
            });
             completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
    {
        ChatMessage.FromUser("I will ask you informations about various locations and I want to answer me with aproximate data, to the best of your knowledge. I am intersted in: criminality rate(procents), average rental price(euros), population density, public tranpsortation accesability, polution rate. I want those datas to be concised in a JSON Array and alongside each value to be put an interpretation of it, without any other text and without the location field. I want it in a strict format like this: [{\"field_name\":{\"field_value\":value, \"field_explanation\":}}], with all beein lowercase and field_name taking values in: criminality_rate,average_rental_price,population_density,public_transportation_accessibility,pollution_rate. Please do NOT change the names of this fields."),
        ChatMessage.FromSystem("Certainly!"),
        ChatMessage.FromUser(countryCode)
    },
                Model = Models.Gpt_4,
            });
            if (completionResult.Successful)
            {
                return completionResult.Choices.First().Message.Content;
            }
            return "";
        }
        private Double getDistanceFromLatLonInKm(Double lat1, Double lon1, Double lat2, Double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = Deg2rad(lat2 - lat1);  // deg2rad below
            var dLon = Deg2rad(lon2 - lon1);
            var a =
              Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
              Math.Cos(Deg2rad(lat1)) * Math.Cos(Deg2rad(lat2)) *
              Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
              ;
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        private Double Deg2rad(Double deg)
        {
            return deg * (Math.PI / 180);
        }

    }
}
