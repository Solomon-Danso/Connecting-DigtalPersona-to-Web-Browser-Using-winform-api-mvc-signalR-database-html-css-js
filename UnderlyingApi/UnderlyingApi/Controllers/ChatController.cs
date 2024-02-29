using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UnderlyingApi.Data;
using UnderlyingApi.Models;

namespace UnderliningAPi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly DataContext context;

        public ChatController(DataContext dataContext){
            context = dataContext;
        }

[HttpPost("JustSendTheApi")]
public async Task<IActionResult> JustSendTheApi(string Token, IFormFile imageFile)
{
    if (imageFile == null || imageFile.Length == 0)
    {
        return BadRequest("No image file uploaded.");
    }

    // Convert the image file to a byte array
    using (var memoryStream = new MemoryStream())
    {
        await imageFile.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        var tandB = new TokenAndByte
        {
            Token = Token,
            ImageBitmap = imageBytes // Save image as byte array
        };

        context.TokenImage.Add(tandB);
        await context.SaveChangesAsync();
    }

    return Ok();
}


        [HttpGet("JustGetTheApi")]
    public async Task<IActionResult>JustGetTheApi(string Token){
        var img = context.TokenImage.FirstOrDefault(a=>a.Token == Token);

        var cvt = Convert.ToBase64String(img.ImageBitmap);

        return Ok(cvt);
    }











        
    }
}