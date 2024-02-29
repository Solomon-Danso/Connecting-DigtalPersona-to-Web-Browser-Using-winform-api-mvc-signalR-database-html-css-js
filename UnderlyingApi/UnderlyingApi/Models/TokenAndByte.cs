using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnderlyingApi.Models
{
   public class TokenAndByte
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty; // Initialize to empty string
    public byte[]? ImageBitmap { get; set; }
}

}