using DataTransfer;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using WebApplication.Models;

namespace WebApplication.Pages
{
    public class IndexModel : PageModel
    {
        // List of all active sessions that users can view data of
        private List<Session> sessions = new List<Session>();

        // Sorting sessions by application name to present in the menu seen in the homepage
        public Dictionary<String, List<String>> sessionsByApp = new Dictionary<String, List<String>>();

        // Deserialized versison of user's input, may not be a valid session
        private Session selectedSession { get; set; } = new Session();

        // Valid session that the user has selected, available to the rest of the web application to fetch data accordingly
        public static Session userSession { get; set; }

        // User feedback that lets user's know if their input is valid and how to proceed
        public String indexMessage = "Please select the name of the application and process you would like to examine.";
        private readonly AppConfig appSettings;
        private readonly FetchDataService _fetchDataService;
        public IndexModel(IOptions<AppConfig> options, FetchDataService fetchDataService)
        {
            appSettings = options.Value;
            _fetchDataService = fetchDataService;
        }

        public async Task OnGet() // Method that gets called as page is loaded and refreshed
        {
            sessions = await _fetchDataService.getSessionData();
            sortSessionsByApp();
        }

        // Sorts sessions by application and updates sessionsByApp
        public void sortSessionsByApp()
        {
            foreach (Session s in sessions)
            {
                if (sessionsByApp.ContainsKey(s.application))
                {
                    List<String> sess = sessionsByApp.GetValueOrDefault(s.application);
                    sess.Add(s.process);
                    sessionsByApp[s.application] = sess;
                }
                else
                {
                    List<String> newSessList = new List<String>();
                    newSessList.Add(s.process);
                    sessionsByApp.Add(s.application, newSessList);
                }
            }
        }

        // Triggered when user clicks "Examine" button
        // Parameters: app - what user clicked in the first menu, pro - retrieved from second portion of menu
        public async Task<IActionResult> OnPostExamineAsync(String app, String pro)
        {
            // Sending http request to api to get session that matches application and process defined by user
            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri(appSettings.ApiUrl);
            String htmlAddress = "SESSIONBYAPPANDPRO?app=" +
                Uri.EscapeDataString(app) + // Application name user defined
                "&pro=" +
                Uri.EscapeDataString(pro); // Process name user defined
            HttpResponseMessage response = await client.GetAsync(htmlAddress);

            var result = response.Content.ReadAsStringAsync().Result;

            selectedSession = JsonConvert.DeserializeObject<Session>(result);

            // Checking session user requested is valid, should not be hit unless user selected only application
            if (selectedSession == null || !selectedSession.application.Equals(app) || !selectedSession.process.Equals(pro))
            {
                indexMessage = "Error. Please try re-entering the application and process name.";
                return Redirect("Index");
            }
            else
            {
                userSession = selectedSession; // Copying over selectedSession to public userSession
                // Positive user feedback
                indexMessage = "Showing information of " + userSession.application + " application and " + userSession.process + " process.";
                return Redirect("/Metrics/CPU_Memory");
            }
        }

        public IActionResult OnPostRecord()
        {
            return Redirect("/Metrics/MontiorAPP_Details");
        }

        public async Task<IActionResult> OnPostPause()
        {
            // Sending http request to api to get session that matches application and process defined by user
            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri(appSettings.ApiUrl);

            HttpResponseMessage response = await client.GetAsync("api/v1/Monitor/Pause");

            var result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                return Redirect("/Metrics/CPU_Memory");
            }
            else
            {
                return RedirectToPage();
            }
        }
        public async Task<IActionResult> OnPostDelete(String app, String pro)
        {
            if (!string.IsNullOrEmpty(app) || !string.IsNullOrEmpty(pro))
            {
                // Sending http request to api to get session that matches application and process defined by user
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(appSettings.ApiUrl);

                string deleteURL = "api/v1/Monitor/Delete";
                string URLparams = string.Empty;

                if (!string.IsNullOrEmpty(app))
                    URLparams = "?app=" + app;

                if (!string.IsNullOrEmpty(app) && !string.IsNullOrEmpty(pro))
                    URLparams = "?app=" + app + "&pro=" + pro;

                HttpResponseMessage response = await client.GetAsync(deleteURL + URLparams);

                var result = response.Content.ReadAsStringAsync().Result;

                return RedirectToPage();

            }
            else
            {
                indexMessage = "Please select App or Process to delete.";
                return RedirectToPage();
            }
        }
    }
}
