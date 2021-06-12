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
            var userInfo = new UserInfo().Get(chessId);
            return new JsonResult(userInfo);
        }
    }
}
