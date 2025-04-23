using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using web_backend.Livestreams;
using Volo.Abp.AspNetCore.Mvc;

namespace web_backend.Controllers
{
    [Route("livestream")]
    [ApiController]
    public class LivestreamController : AbpController  // Required for ABP-based APIs
    {
        private readonly ILivestreamAppService _livestreamAppService;

        public LivestreamController(ILivestreamAppService livestreamAppService)
        {
            _livestreamAppService = livestreamAppService;
        }

        [HttpGet]
        public async Task<ActionResult<List<LivestreamDto>>> GetAll()
        {
            var result = await _livestreamAppService.GetListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LivestreamDto>> GetById(Guid id)
        {
            var result = await _livestreamAppService.GetAsync(id);
            if (result == null)
            {
                return NotFound(new { Message = "Livestream not found." });
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<LivestreamDto>> Create([FromBody] CreateLivestreamDto dto)
        {
            var result = await _livestreamAppService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<LivestreamDto>> Update(Guid id, [FromBody] UpdateLivestreamDto dto)
        {
            var result = await _livestreamAppService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _livestreamAppService.DeleteAsync(id);
            return NoContent();
        }
    }
}
