using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using LifeDemotivator.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.IO;
using System.Web.Helpers;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace LifeDemotivator.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private static string UrlAvatar;
        private Entities db = new Entities();
        private const int AvatarStoredWidth = 100;  // ToDo - Change the size of the stored avatar image
        private const int AvatarStoredHeight = 100; // ToDo - Change the size of the stored avatar image
        private const int AvatarScreenWidth = 400;  // ToDo - Change the value of the width of the image on the screen

        private const string TempFolder = "/Temp";
        private const string MapTempFolder = "~" + TempFolder;
        private const string AvatarPath = "/Avatars";

        private readonly string[] _imageFileExtensions = { ".jpg", ".png", ".gif", ".jpeg" };
        // GET: Profile
        [HttpGet]
        public ActionResult Profile()
        {
            var UserManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var userId = User.Identity.GetUserId();
            var context = new ApplicationDbContext();
            var allDemotivators = db.Demotivators.ToList();
            var model = new CurrentUserViewModel()
            {
                UserName = UserManager.FindById(userId).UserName,
                AvatarUrl = UserManager.FindById(userId).AvatarUrl,
                Email = UserManager.FindById(userId).Email,
                demoList = allDemotivators.Where(demo => demo.CreatorId == userId).ToList()
            };
            return View(model);
        }

        [HttpGet]
        public ActionResult _Profile()
        {
            return PartialView();
        }

        [ValidateAntiForgeryToken]
        public ActionResult _Profile(IEnumerable<HttpPostedFileBase> files)
        {
            if (files == null || !files.Any()) return Json(new { success = false, errorMessage = "No file uploaded." });
            var file = files.FirstOrDefault();  // get ONE only
            if (file == null || !IsImage(file)) return Json(new { success = false, errorMessage = "File is of wrong format." });
            if (file.ContentLength <= 0) return Json(new { success = false, errorMessage = "File cannot be zero length." });
            var webPath = GetTempSavedFilePath(file);
            return Json(new { success = true, fileName = webPath.Replace("/", "\\") }); // success
        }

        [HttpPost]
        public ActionResult Save(string t, string l, string h, string w, string fileName)
        {
            try
            {
                // Calculate dimensions
                var top = Convert.ToInt32(t.Replace("-", "").Replace("px", ""));
                var left = Convert.ToInt32(l.Replace("-", "").Replace("px", ""));
                var height = Convert.ToInt32(h.Replace("-", "").Replace("px", ""));
                var width = Convert.ToInt32(w.Replace("-", "").Replace("px", ""));

                // Get file from temporary folder
                var fn = Path.Combine(Server.MapPath(MapTempFolder), Path.GetFileName(fileName));
                // ...get image and resize it, ...
                var img = new WebImage(fn);
                img.Resize(width, height);
                // ... crop the part the user selected, ...
                img.Crop(top, left, img.Height - top - AvatarStoredHeight, img.Width - left - AvatarStoredWidth);
                // ... delete the temporary file,...
                System.IO.File.Delete(fn);
                // ... and save the new one.

                var newFileName = Path.GetFileName(fn);
                img.Save(Server.MapPath("~/" + newFileName));

                Account account = new Account(
                "lifedemotivator",
                "366978761796466",
                "WMYLmdaTODdm4U6VcUGhxapkcjI"
                );
                ImageUploadResult uploadResult = new ImageUploadResult();
                CloudinaryDotNet.Cloudinary cloudinary = new CloudinaryDotNet.Cloudinary(account);
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(Server.MapPath("~/" + newFileName)),
                    PublicId = User.Identity.Name + newFileName,
                };
                uploadResult = cloudinary.Upload(uploadParams);
                System.IO.File.Delete(Server.MapPath("~/" + newFileName));
                UrlAvatar = uploadResult.Uri.ToString();

                return Json(new { success = true, avatarFileLocation = UrlAvatar });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: " + ex.Message });
            }
        }

        private bool IsImage(HttpPostedFileBase file)
        {
            if (file == null) return false;
            return file.ContentType.Contains("image") ||
                _imageFileExtensions.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private string GetTempSavedFilePath(HttpPostedFileBase file)
        {
            // Define destination
            var serverPath = HttpContext.Server.MapPath(TempFolder);
            if (Directory.Exists(serverPath) == false)
            {
                Directory.CreateDirectory(serverPath);
            }

            // Generate unique file name
            var fileName = Path.GetFileName(file.FileName);
            fileName = SaveTemporaryAvatarFileImage(file, serverPath, fileName);

            // Clean up old files after every save
            CleanUpTempFolder(1);
            return Path.Combine(TempFolder, fileName);
        }

        private static string SaveTemporaryAvatarFileImage(HttpPostedFileBase file, string serverPath, string fileName)
        {
            var img = new WebImage(file.InputStream);
            var ratio = img.Height / (double)img.Width;
            img.Resize(AvatarScreenWidth, (int)(AvatarScreenWidth * ratio));

            var fullFileName = Path.Combine(serverPath, fileName);
            if (System.IO.File.Exists(fullFileName))
            {
                System.IO.File.Delete(fullFileName);
            }

            img.Save(fullFileName);
           
            return Path.GetFileName(img.FileName);
        }

        private void CleanUpTempFolder(int hoursOld)
        {
            try
            {
                var currentUtcNow = DateTime.UtcNow;
                var serverPath = HttpContext.Server.MapPath("/Temp");
                if (!Directory.Exists(serverPath)) return;
                var fileEntries = Directory.GetFiles(serverPath);
                foreach (var fileEntry in fileEntries)
                {
                    var fileCreationTime = System.IO.File.GetCreationTimeUtc(fileEntry);
                    var res = currentUtcNow - fileCreationTime;
                    if (res.TotalHours > hoursOld)
                    {
                        System.IO.File.Delete(fileEntry);
                    }
                }
            }
            catch
            {
                // Deliberately empty.
            }
        }

        [HttpGet]
        public async Task<ActionResult> SaveUrlCloud()
        {
            var currentUser = db.AspNetUsers.Find(User.Identity.GetUserId());
            currentUser.AvatarUrl = UrlAvatar;
            await db.SaveChangesAsync();
            return Redirect(Request.UrlReferrer.ToString());
        }
    }
}