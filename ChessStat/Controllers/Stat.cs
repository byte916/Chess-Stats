using ChessStat.Classes;
using Microsoft.AspNetCore.Mvc;

namespace ChessStat.Controllers
{
    public class Stat: ControllerBase
    {
        [HttpGet]
        [Route("[controller]")]
        public JsonResult Get(string chessId, string timeControl)
        {
            var userInfo = new UserInfo().Get(chessId, timeControl);
            return new JsonResult(userInfo);
        }
    }
}
