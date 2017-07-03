using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NotesDB.Models;

namespace NotesDB.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(new WelcomeModel
            {
                Db = Startup.DbName,
                Url = Startup.Url
            });
        }

       

        public IActionResult Error()
        {
            return View();
        }
    }
}
