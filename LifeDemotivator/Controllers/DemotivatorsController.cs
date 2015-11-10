using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LifeDemotivator.Models;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI.WebControls;
using System.IO;
using System.Text.RegularExpressions;

namespace LifeDemotivator.Controllers
{
    public class DemotivatorsController : Controller
    {
        static string path;
        private Entities db = new Entities();

        // GET: Demotivators
        public ActionResult Index()
        {
            var demotivators = db.Demotivators.Include(d => d.AspNetUsers);
            return View(demotivators.ToList());
        }

        // GET: Demotivators/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Demotivators demotivators = db.Demotivators.Find(id);
            if (demotivators == null)
            {
                return HttpNotFound();
            }
            return View(demotivators);
        }

        // GET: Demotivators/Create
        public ActionResult Create()
        {
            path = Server.MapPath("~/");
            ViewBag.CreatorId = new SelectList(db.AspNetUsers, "Id", "Email");
            return View();
        }
        public ActionResult Createh()
        {
            path = Server.MapPath("~/");
            ViewBag.CreatorId = new SelectList(db.AspNetUsers, "Id", "Email");
            return View();
        }
      



        [HttpPost]
        public async Task<ActionResult> SaveDemotivator(string imageData)
        {
            string fileNameWitPath = path + "MyPicture.png";
            using (FileStream fs = new FileStream(fileNameWitPath, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = Convert.FromBase64String(imageData);
                    bw.Write(data);
                    bw.Close();
                }
            }
            return Redirect(Request.UrlReferrer.ToString());
        }

        private void SaveDemotivatorToDatabase(string Name, string FirstString, string SecondString, string OriginalUrl, string ModifiedUrl)
        {
            Demotivators demotivator = new Demotivators();
            var CurrentUser = db.AspNetUsers.Find(User.Identity.GetUserId());
            demotivator.CreatorId = User.Identity.GetUserId();
            demotivator.Date = DateTime.Now;
            demotivator.Name = Name;
            demotivator.FirstString = FirstString;
            demotivator.SecondString = SecondString;
            demotivator.OriginalUrl = OriginalUrl;
            demotivator.ModifiedUrl = ModifiedUrl;
            demotivator.Rating = "0,0,0,0,0";
            db.Demotivators.Add(demotivator);
            CurrentUser.Demotivators.Add(demotivator);
            db.SaveChanges();
        }



        [HttpPost]
        public async Task<ActionResult> SaveDemotivatorToCloud(string tag, Demotivators model)
        {
            string fileName = "";
            ImageUploadResult uploadResult = new ImageUploadResult();
            ImageUploadParams uploadParams = new ImageUploadParams();

            Account account = new Account(
                "lifedemotivator",
                "366978761796466",
                "WMYLmdaTODdm4U6VcUGhxapkcjI"
                );
            CloudinaryDotNet.Cloudinary cloudinary = new CloudinaryDotNet.Cloudinary(account);
            var upload = Request.Files[0];
            fileName = System.IO.Path.GetFileName(upload.FileName);
            upload.SaveAs(Server.MapPath("~/" + fileName));

            var TargetPath = Server.MapPath("~/" + fileName);
             uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(Server.MapPath("~/" + fileName)),
                PublicId = User.Identity.Name + fileName + DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second,
            };
            uploadResult = cloudinary.Upload(uploadParams);
            System.IO.File.Delete(Server.MapPath("~/" + fileName));
            System.IO.File.Delete(Server.MapPath("~/img.png"));
            var OriginalUrl = uploadResult.Uri.ToString();

            uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(path + "MyPicture.png"),
                PublicId = User.Identity.Name + DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second,
            };
            uploadResult = cloudinary.Upload(uploadParams);
            System.IO.File.Delete(path + "MyPicture.png");

            var ModifiedUrl = uploadResult.Uri.ToString();

            SaveDemotivatorToDatabase(model.Name, model.FirstString , model.SecondString, OriginalUrl, ModifiedUrl);
            SaveTag(tag, db.Demotivators.ToList().Last().Id);
            return Redirect(Request.UrlReferrer.ToString());
        }

        private void SaveTag(string tag, int demId)
        {
            Regex regular = new Regex(@"#\w+");
            MatchCollection parsedTags = regular.Matches(tag);
            var tags = db.Tags.ToList();
            bool flag = false;
            foreach (var item in parsedTags)
            {
                string bareItem = "";
                for (int j = 1; j < item.ToString().Length; j++ )
                {
                    bareItem += item.ToString()[j];
                }
                    flag = false;
                foreach (var item1 in tags)
                {
                    if (item1.Name.Equals(bareItem))
                    {
                        SaveTagRecord(demId, item1.Id);
                        flag = true;
                    }
                }
                if (!flag)
                {
                    Tags newTag = new Tags();
                    newTag.Name = bareItem;
                    db.Tags.Add(newTag);
                    db.SaveChanges();
                    SaveTagRecord(demId, newTag.Id);
                }
            }
        }

        private void SaveTagRecord(int demId, int tagId)
        { 
            TagsDemotivators tag = new TagsDemotivators();
            tag.DemotivatorId = demId;
            tag.TagId = tagId;
            db.TagsDemotivators.Add(tag);
            db.SaveChanges();

        }

        public ActionResult AutocompleteSearch(string term)
        {
            var models = db.Tags.Where(a => a.Name.Contains(term))
                            .Select(a => new { value = a.Name })
                            .Distinct();

            return Json(models, JsonRequestBehavior.AllowGet);
        }



       




        // POST: Demotivators/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье http://go.microsoft.com/fwlink/?LinkId=317598.
        

        // GET: Demotivators/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Demotivators demotivators = db.Demotivators.Find(id);
        //    if (demotivators == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.CreatorId = new SelectList(db.AspNetUsers, "Id", "Email", demotivators.CreatorId);
        //    return View(demotivators);
        //}

        // POST: Demotivators/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "Id,CreatorId,Date,Name,FirstString,SecondString,OriginalUrl,ModifiedUrl,Rating")] Demotivators demotivators)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(demotivators).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.CreatorId = new SelectList(db.AspNetUsers, "Id", "Email", demotivators.CreatorId);
        //    return View(demotivators);
        //}

        // GET: Demotivators/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Demotivators demotivators = db.Demotivators.Find(id);
        //    if (demotivators == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(demotivators);
        //}

        // POST: Demotivators/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Demotivators demotivators = db.Demotivators.Find(id);
        //    db.Demotivators.Remove(demotivators);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing)
//            {
//                db.Dispose();
//            }
//            base.Dispose(disposing);
//        }
   }
}
