using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TMS.Controllers
{
	public class CustomerAccountsController : Controller
	{
		// GET HTTP request, <base address>/CustomerAccounts/Index
		public IActionResult Index()
		{
			return View();
		}
	}
}
