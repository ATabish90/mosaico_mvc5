using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Mosaico_Dot_Net.Controllers
{
    public class MosaicoAPIController : ApiController
    {
        public IHttpActionResult NewAdd()
        {
            return Ok();
        }
    }
}
