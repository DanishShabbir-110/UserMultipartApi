using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using UserMultipartApi.Models;

namespace UserMultipartApi.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private readonly string appDataPath = HttpContext.Current.Server.MapPath("~/App_Data/");
        private readonly string uploadsPath;
        private readonly string usersJsonPath;

        public UsersController()
        {
            uploadsPath = Path.Combine(appDataPath, "Uploads");
            usersJsonPath = Path.Combine(appDataPath, "Users.json");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
            if (!File.Exists(usersJsonPath)) File.WriteAllText(usersJsonPath, "[]");
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> PostUser()
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Unsupported media type.");

            // Your final upload folder
            //var uploadsPath = HttpContext.Current.Server.MapPath("~/UploadedUsers");
            //if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            // Custom provider that forces files into uploadsPath
            var provider = new MultipartFormDataStreamProvider(uploadsPath);
            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);

                // Read form fields
                var form = provider.FormData;
                var name = form.Get("name") ?? "";
                var dob = form.Get("dob") ?? "";
                var gender = form.Get("gender") ?? "";

                string savedFileName = null;
                // Process file if exists (single file "file")
                if (provider.FileData.Any())
                {
                    var fileData = provider.FileData.First();
                    var originalName = fileData.Headers.ContentDisposition.FileName?.Trim('"') ?? "upload.jpg";
                    var ext = Path.GetExtension(originalName);
                    savedFileName = Guid.NewGuid().ToString() + ext;
                    var dest = Path.Combine(uploadsPath, savedFileName);
                    File.Move(fileData.LocalFileName, dest);
                }

                // Build user model
                var user = new UserModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Dob = dob,
                    Gender = gender,
                    ImageFileName = savedFileName,
                    ImageUrl = savedFileName != null ? Url.Content("~/App_Data/Uploads/" + savedFileName) : null
                };

                // Persist to Users.json
                var users = ReadUsers();
                users.Add(user);
                File.WriteAllText(usersJsonPath, JsonConvert.SerializeObject(users, Formatting.Indented));
                return Ok(user);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetUsers()
        {
            try
            {
                var users = ReadUsers();
                // Update ImageUrl to reachable endpoint (image route below)
                foreach (var u in users)
                {
                    if (!string.IsNullOrEmpty(u.ImageFileName))
                    {
                        // create URL to api/users/image/{filename}
                        var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority) + Request.GetRequestContext().VirtualPathRoot.TrimEnd('/');
                        var imageUrl = $"{baseUrl}/App_Data/Uploads/{u.ImageFileName}";
                        u.ImageUrl = imageUrl;
                    }
                }
                return Ok(users);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private List<UserModel> ReadUsers()
        {
            var json = File.ReadAllText(usersJsonPath);
            var users = JsonConvert.DeserializeObject<List<UserModel>>(json) ?? new List<UserModel>();
            return users;
        }
    }
}
