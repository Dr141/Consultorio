using Consultorio.Infraestrutura.Interfaces.Base;
using Microsoft.AspNetCore.Mvc;
using Consultorio.Modelo.Entidades;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Consultorio.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PessoaController(IRepositorio<Pessoa> repositorio) : ControllerBase
{
    // GET: api/<PessoaController>
    [HttpGet("/ObterTodos")]
    public async Task<object> Get()
    {
        try
        {
            var result = await repositorio.ObterTodos();
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // GET api/<PessoaController>/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/<PessoaController>
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<PessoaController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<PessoaController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}
