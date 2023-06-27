using API.Models;
using API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class PopsicleController : ControllerBase
    {
        private readonly IPopsicleService _popsicleService;

        public PopsicleController(IPopsicleService popsicleService)
        {
            _popsicleService = popsicleService;
            _popsicleService.Seed();
        }

        /// <summary>
        /// Searches products in the database.
        /// </summary>
        /// <response code="200">Successfully found products, products returned</response>      
        /// <response code="204">Searched but no products were found</response>
        [HttpGet()]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Popsicle>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(string searchValue)
        {
            var ouput = await _popsicleService.Search(searchValue);
            return ouput == null || ouput.Count == 0 ? NoContent() : Ok(ouput);
        }

        /// <summary>
        /// Returns product from the database.
        /// </summary>
        /// <response code="200">Successfully found product, product returned</response>      
        /// <response code="404">Product not found</response>
        /// <response code="400">Invalid product id</response>
        [HttpGet("{id}", Name ="GetPopsicle")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Popsicle))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get(int id)
        {
            if(id <= 0)
            {
                return BadRequest(new { id });
            }
            var ouput = await _popsicleService.Get(id);
            return ouput == null? NotFound($"Popsicle with Id: {id} not found") : Ok(ouput);
        }

        /// <summary>
        /// Creates a new product in the database.
        /// </summary>
        /// <response code="200">Successfully created new product returned</response>      
        /// <response code="400">Product failed to meet basic requirements could not create</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Popsicle))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDictionary<string, string[]>))]
        public async Task<IActionResult> Create([FromBody] Popsicle value)
        {
            await _popsicleService.Validate(null, value, ModelState);
            if (!ModelState.IsValid)
            {
                var dict = GetErrors();
                return BadRequest(dict);
            }

            var pop = await _popsicleService.Create(value);
            return Ok(pop);
        }

        /// <summary>
        /// Replace the enitre product in the database.
        /// </summary>
        /// <response code="200">Successfully updated update product returned</response>
        /// <response code="201">Product not found in database, successfully created new product returned</response>
        /// <response code="400">Product failed to meet basic requirements could not update or create</response>
        [HttpPut("{id:int?}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Popsicle))]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Popsicle))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDictionary<string, string[]>))]
        public async Task<IActionResult> Replace(int? id, [FromBody] Popsicle value)
        {
            await _popsicleService.Validate(id, value, ModelState);
            if (!ModelState.IsValid)
            {
                var dict = GetErrors();
                return BadRequest(dict);
            }

            var ouput = await _popsicleService.CreateOrUpdate(id, value);
            if (ouput.Item1)
            {
                var uri = new Uri(Url.Link("GetPopsicle", new { id = ouput.Item2.Id }));
                return Created(uri, ouput.Item2);
            }
            else
            {
                return Ok(ouput.Item2);
            }
        }


        /// <summary>
        /// Deletes from the database a popsicle this cannot be undone.
        /// </summary>
        /// <response code="200">Product deleted</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        
        public async Task<IActionResult> Delete(int id)
        {
            await _popsicleService.Delete(id);
            return Ok(id);
        }


        /// <summary>
        /// Update fields included to the database.
        /// </summary>
        /// <response code="200">Product update and updated product returned</response>
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Popsicle))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<IActionResult> Patch([FromRoute] int id, [FromBody] JsonPatchDocument patchDoc)
        {
            var output= await _popsicleService.Patch(id, patchDoc);
            return output == null ? NotFound($"Popsicle with Id: {id} not found") : Ok(output);
        }

        private IDictionary<string, string[]> GetErrors()
        {
            return ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
        }

    }
}
