﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataTransfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApplication.Models;

namespace WebApplication.Pages.Metrics
{
    public class JITModel : PageModel
    {
        // List of data
        public List<Jit> jit { get; set; } = new List<Jit>();

        // Will decide later on oldStamp, automatically set to a month previous to current time (gets data for a month range)
        public DateTime oldStamp = DateTime.Today.AddMonths(-1).ToUniversalTime();
        public DateTime newStamp = DateTime.Now.ToUniversalTime();

        private readonly FetchDataService _fetchDataService;
        [BindProperty]
        public string ApiUrl { get; set; }
        public JITModel(FetchDataService fetchDataService,IOptions<AppConfig> options)
        {
            _fetchDataService = fetchDataService;
            ApiUrl = options.Value.ApiUrl;
        }

        public async Task OnGet()
        {
            newStamp = DateTime.Now.ToUniversalTime();
            List<Jit> addOn = await _fetchDataService.getData<Jit>(oldStamp, newStamp); // Get data

            foreach (Jit j in addOn)
            {
                jit.Add(j);
            }
        }
    }
}