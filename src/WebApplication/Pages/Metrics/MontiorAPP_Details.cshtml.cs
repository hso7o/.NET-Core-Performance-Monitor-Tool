using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DataTransfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApplication.Models;

namespace WebApplication.Pages.Metrics
{
    public class MontiorAPP_DetailsModel : PageModel
    {
        [BindProperty]
        public MonitorModel monitorModel { get; set; } 
        public String indexMessage = "Please select the name of the application and process you would like to examine.";
        public AppConfig appConfig;
        public MontiorAPP_DetailsModel(IOptions<AppConfig> options)
        {
            appConfig = options.Value;
        }
        public void OnGet()
        {
            monitorModel = new MonitorModel();
        }

        public async Task<IActionResult> OnPostSaveAppAsync()
        {
            try
            {
                // Sending http request to api to get session that matches application and process defined by user
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(appConfig.ApiUrl);

                HttpResponseMessage response = await client.PostAsJsonAsync("api/v1/Monitor/Record", monitorModel);

                var result = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? ex.InnerException != null ? ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message : ex.Message : ex.Message);
            }
           

            return Redirect("../Index");
            
        }
    }
}
