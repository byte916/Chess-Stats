using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessStat.Classes;
using Microsoft.AspNetCore.Mvc;

namespace ChessStat.Controllers
{
    public class Stat: ControllerBase
    {
        [HttpGet]
        [Route("[controller]")]
        public JsonResult Get(string chessId)
        {
            var userInfo = new Tournament().Get(chessId);
            return new JsonResult(userInfo);
        }
    }
}
