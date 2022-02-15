using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using WebApplication.Pages;
using DataTransfer;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WebApplication.Models;
using WebApplication.Common;

namespace WebApplication
{
    public class FetchDataService
    {
        private readonly IOptions<AppConfig> appSettings;
        public FetchDataService(IOptions<AppConfig> options) 
        {
            appSettings = options;
        }

        // Generic method that takes timestamps and makes a call to the API based off of the class T
        public async Task<List<T>> getData<T>(DateTime oldStamp, DateTime newStamp)
        {
            // Creating HttpClient that will call the web upapi
            HttpClient client = new HttpClient();
            // Constructing url that will be called, the domain is hardcoded for now
            client.BaseAddress = new Uri(appSettings.Value.ApiUrl);

            // Specifying which controller to call upon based off the object of T
            String type = "";
            if (typeof(T).ToString().Equals("DataTransfer.CPU_Usage"))
            {
                type = "CPU";
            }
            else if (typeof(T).ToString().Equals("DataTransfer.Mem_Usage"))
            {
                type = "Memory";
            }
            else if (typeof(T).ToString().Equals("DataTransfer.Http_Request"))
            {
                type = "HTTP";
            }
            else if (typeof(T).ToString().Equals("DataTransfer.Exceptions"))
            {
                type = "Exception";
            }
            else if (typeof(T).ToString().Equals("DataTransfer.Contention"))
            {
                type = "Contention";
            }
            else if (typeof(T).ToString().Equals("DataTransfer.GC"))
            {
                type = "GC"; 
            }
            else if (typeof(T).ToString().Equals("DataTransfer.Jit"))
            {
                type = "Jit";
            }
            else
            {
                type = "error"; // Should never hit this because T can only take on values defined above
            }

            // Constructing string that will pass timestamps to web api controllers
            String dateRange = CommonMethods.convertDateTime(oldStamp) + "&end=" + CommonMethods.convertDateTime(newStamp);
            // Passing session information to the web api controllers
            String sessionId = "&id=" + IndexModel.userSession.Id.ToString();

            // Stringing all components of http request together and actually calling web api
            HttpResponseMessage response = await client.GetAsync("api/v1/" + 
                type + 
                "/Daterange?start=" + 
                dateRange +
                sessionId);

            List<T> data = new List<T>();
            if (response.IsSuccessStatusCode) // If the response is successfull, update data
            {
                var result = response.Content.ReadAsStringAsync().Result;
                data = JsonConvert.DeserializeObject<List<T>>(result);
            }

            return data; 
        }

        // getData method gets information for metrics based off of a daterange whereas the session 
        // RETURNALL controller doesn't require a daterange, so getSessionData is separated from getData
        public async Task<List<Session>> getSessionData()
        {
            // Creating HttpClient that will call the web api
            HttpClient client = new HttpClient();
            // Constructing url that will be called, the domain is hardcoded for now, will be more variable in the future
            client.BaseAddress = new Uri(appSettings.Value.ApiUrl);
            HttpResponseMessage response = await client.GetAsync("RETURNALL");

            List<Session> sessionData = new List<Session>();
            if (response.IsSuccessStatusCode) // If the response is successfull, update sessionData
            {
                var result = response.Content.ReadAsStringAsync().Result;
                sessionData = JsonConvert.DeserializeObject<List<Session>>(result);
            }

            return sessionData; 
        }

        
    }
}
