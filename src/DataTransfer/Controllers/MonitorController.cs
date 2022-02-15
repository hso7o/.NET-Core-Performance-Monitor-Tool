using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataTransfer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MonitorController : ControllerBase
    {
        public PerformanceDataContext _MetricContext;
        private readonly Monitor _monitor;
        public MonitorController(PerformanceDataContext context,Monitor monitor)
        {
            _MetricContext = context ?? throw new ArgumentNullException(nameof(context));
            _monitor = monitor;
        }

        [HttpPost]
        [Route("Record")]
        public IActionResult Record(MonitorModel monitorModel)
        {
            if (ModelState.IsValid)
            {
                if (_monitor.isRunning())
                    _monitor.Pause();

                if (_monitor.Record(monitorModel, !string.IsNullOrEmpty(monitorModel.url) ? monitorModel.url : "http://"+HttpContext.Request.Host.Value+"/") == 1)
                    return Ok();
            }
            return Conflict();
        }

        [HttpGet]
        [Route("Pause")]
        public IActionResult Pause()
        {
           
                if (_monitor.Pause() == 1)
                    return Ok();

            return Conflict();
        }

        [HttpGet]
        [Route("Delete")]
        public IActionResult Delete(String app, String pro)
        {
            bool isDeleted = false;
            Session session;

            if (!string.IsNullOrEmpty(app) && !string.IsNullOrEmpty(pro))
            {
                session = _MetricContext.Session.Where(x => x.application.Equals(app) && x.process.Equals(pro)).FirstOrDefault();
                _MetricContext.Session.Remove(session);
                _MetricContext.SaveChanges();
                isDeleted = true;
            }
            else if (!string.IsNullOrEmpty(app))
            {
                session = _MetricContext.Session.Where(x => x.application.Equals(app)).FirstOrDefault();
                _MetricContext.Session.Remove(session);
                _MetricContext.SaveChanges();
                isDeleted = true;
            }
            else
            {
                session = _MetricContext.Session.Where(x => x.process.Equals(pro)).FirstOrDefault();
                _MetricContext.Session.Remove(session);
                _MetricContext.SaveChanges();
                isDeleted = true;
            }

            if (isDeleted)
                return Ok();
            else
                return Conflict();
        }
    }
}
