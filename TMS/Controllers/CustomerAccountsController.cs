﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMS.Controllers
{
	public class CustomerAccountsController : Controller
	{
        // GET HTTP request, <base address>/CustomerAccounts/Index
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
