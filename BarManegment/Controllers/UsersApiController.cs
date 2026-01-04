// File Path: BarManegment/Controllers/UsersApiController.cs
using BarManegment.Models;
using System.Linq;
using System.Web.Http;

namespace BarManegment.Controllers
{
    // DTO (Data Transfer Object) to prevent circular references and expose only needed data
    public class UserApiDto
    {
        public int Id { get; set; }
        public string FullNameArabic { get; set; }
        public string Username { get; set; }
        public string IdentificationNumber { get; set; }
        public string UserTypeName { get; set; }
        public bool IsActive { get; set; }
    }

    public class UsersApiController : ApiController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: api/UsersApi
        public IHttpActionResult GetUsers()
        {
            var users = db.Users
                .Select(u => new UserApiDto
                {
                    Id = u.Id,
                    FullNameArabic = u.FullNameArabic,
                    Username = u.Username,
                    IdentificationNumber = u.IdentificationNumber,
                    UserTypeName = u.UserType.NameArabic,
                    IsActive = u.IsActive
                })
                .ToList();

            return Ok(users);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
