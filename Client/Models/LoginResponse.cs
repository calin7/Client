using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Models
{
    public class LoginResponse
    {
        public string Username { get; set; }
        public string JwtToken { get; set; }
    }
}
