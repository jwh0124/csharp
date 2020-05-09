using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CmdApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CmdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommandsController : ControllerBase
    {

        private readonly CommandContext _context;

        public CommandsController(CommandContext context) => _context = context;

        [HttpGet]   
        public  ActionResult<IEnumerable<Command>> GetCommandItems() 
        {
            return _context.CommandItems;
        }

        [HttpGet("{id}")]
        public ActionResult<Command> GetCommandItem(int id)
        {
            var commandItem = _context.CommandItems.Find(id);
            
            if(commandItem == null){
                return NotFound();
            }

            return commandItem;
        }

        [HttpPost]
        public ActionResult<Command> PostCommandItem(Command command)
        {
            _context.Add(command);
            _context.SaveChanges();

            return CreatedAtAction("GetCommandItem", new Command{Id = command.Id}, command);
        }

        [HttpPut]
        public ActionResult PutCommandItem(int id, Command command)
        {
            if(id != command.Id)
            {
                return BadRequest();    
            }

            _context.Entry(command).State = EntityState.Modified;
            _context.SaveChanges();

            return NoContent();
        }

        [HttpDelete]
        public ActionResult<Command> DeleteCommandItem(int id)
        {
            var commandItem = _context.CommandItems.Find(id);
            
            if(commandItem == null){
                return NotFound();
            }

            _context.CommandItems.Remove(commandItem);
            _context.SaveChanges();

            return commandItem;
        }
    }
}