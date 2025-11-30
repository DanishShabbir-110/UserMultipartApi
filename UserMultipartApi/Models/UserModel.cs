using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserMultipartApi.Models
{
    public class UserModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Dob { get; set; }
        public string Gender { get; set; }
        public string ImageFileName { get; set; }
        public string ImageUrl { get; set; }
    }
}