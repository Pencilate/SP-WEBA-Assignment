using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TMS.Controllers
{
    public class AccountRatesController : Controller
    {
        // GET: /<controller>/
        [Authorize("ADMIN")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize("ADMIN")]
        public IActionResult Create()
        {
            return View();
        }
    }
}
