using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using web_backend.Livestreams;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace web_backend.Controllers
{
    [Route("livestream")]
    [ApiController]
    public class LivestreamController : AbpController
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

            // Check access control for non-free livestreams
            if (!result.FreeLivestream && !User.Identity.IsAuthenticated)
            {
                return Forbid("This livestream requires a subscription.");
            }

            return Ok(result);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<LivestreamDto>> Create([FromBody] CreateLivestreamDto dto)
        {
            var result = await _livestreamAppService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<LivestreamDto>> Update(Guid id, [FromBody] UpdateLivestreamDto dto)
        {
            var result = await _livestreamAppService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _livestreamAppService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("access-check/{id}")]
        public async Task<IActionResult> CheckAccess(Guid id)
        {
            var livestream = await _livestreamAppService.GetAsync(id);
            if (livestream == null)
            {
                return NotFound(new { Message = "Livestream not found." });
            }

            bool hasAccess = livestream.FreeLivestream || User.Identity.IsAuthenticated;

            return Ok(new
            {
                HasAccess = hasAccess,
                IsFreeLivestream = livestream.FreeLivestream,
                RequiresSubscription = !livestream.FreeLivestream,
                IsAuthenticated = User.Identity.IsAuthenticated
            });
        }
    }
}
