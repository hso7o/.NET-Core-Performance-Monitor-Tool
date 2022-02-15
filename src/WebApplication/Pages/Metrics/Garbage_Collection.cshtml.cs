using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataTransfer;
using WebApplication.ClientSideData;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Models;
using Microsoft.Extensions.Options;

namespace WebApplication.Pages.Metrics
{
    public class Garbage_CollectionModel : PageModel
    {
        // List of data
        public List<DataTransfer.GC> gc { get; set; } = new List<DataTransfer.GC>();

        // Will decide later on oldStamp, automatically set to a month previous to current time (gets data for a month range)
        public DateTime oldStamp = DateTime.Today.AddMonths(-1).ToUniversalTime();
        public DateTime newStamp = DateTime.Now.ToUniversalTime();
        private readonly FetchDataService _fetchDataService;
        [BindProperty]
        public string ApiUrl { get; set; }
        public Garbage_CollectionModel(FetchDataService fetchDataService,IOptions<AppConfig> options)
        {
            _fetchDataService = fetchDataService;
            ApiUrl = options.Value.ApiUrl;
        }
        public async Task OnGet()
        {
            newStamp = DateTime.Now.ToUniversalTime();
            List<DataTransfer.GC> addOn = await _fetchDataService.getData<DataTransfer.GC>(oldStamp, newStamp); // Get data

            foreach (DataTransfer.GC g in addOn)
            {
                gc.Add(g);
            }
        }
    }
}