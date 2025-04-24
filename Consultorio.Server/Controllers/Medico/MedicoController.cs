using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Consultorio.Server.Controllers.Medico
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicoController : ControllerBase
    {
        // GET: api/<MedicoController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<MedicoController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<MedicoController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<MedicoController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<MedicoController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
