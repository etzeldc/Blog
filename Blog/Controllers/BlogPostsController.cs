using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Blog.Models;
using Blog.Utilities;
using PagedList;
using PagedList.Mvc;

namespace Blog.Controllers
{
    [RequireHttps]
    [Authorize(Roles ="Admin")]
    public class BlogPostsController : Controller
    {

        private ApplicationDbContext db = new ApplicationDbContext();

        //[Authorize]
        //[Authorize(Roles = "Admin, Moderator")]
        
        // GET: BlogPosts
        [AllowAnonymous]
        public ActionResult Index(int? page, string searchStr)
        {
            ViewBag.Search = searchStr;
            var blogList = IndexSearch(searchStr);

            int pageSize = 3;
            int pageNumber = page ?? 1;

            //***This is the BlogPost index for the public to see, Unseen unless marked as published
            //var publishedBlogPosts = blogList.db.BlogPosts.Where(b => b.Published).OrderByDescending(b => b.Created).ToPagedList(pageNumber, pageSize);
            //Or...
            //return View("Index", db.BlogPosts.OrderByDescending(b => b.Created).ToList());

            return View(blogList.Where(b => b.Published).ToPagedList(pageNumber, pageSize));
        }

        public ActionResult AdminIndex(int? page, string searchStr)
        {
            ViewBag.Search = searchStr;
            var adminList = IndexSearch(searchStr);

            int pageSize = 3;
            int pageNumber = page ?? 1;

            return View(adminList.Where(b => b.Published).OrderByDescending(b => b.Created).ToPagedList(pageNumber, pageSize));
        }
        public ActionResult Unpublished(int? page, string searchStr)
        {
            ViewBag.Search = searchStr;
            var unList = IndexSearch(searchStr);

            int pageSize = 3;
            int pageNumber = page ?? 1;

            return View("AdminIndex", unList.Where(b => !b.Published).OrderByDescending(b => b.Created).ToPagedList(pageNumber, pageSize));
        }

        public IQueryable<BlogPost> IndexSearch(string searchStr)
        {
            IQueryable<BlogPost> result = null;
            if (searchStr != null)
            {
                result = db.BlogPosts.AsQueryable();
                result = result.Where(p => p.Title.Contains(searchStr) ||
                                        p.Body.Contains(searchStr) ||
                                        p.Comments.Any(c => c.Body.Contains(searchStr) ||
                                        c.Author.FirstName.Contains(searchStr) ||
                                        c.Author.LastName.Contains(searchStr) ||
                                        c.Author.DisplayName.Contains(searchStr) ||
                                        c.Author.Email.Contains(searchStr)));
            }
            else
            {
                result = db.BlogPosts.AsQueryable();
            }
            return result.OrderByDescending(p => p.Created);
        }

        // GET: BlogPosts/Details/5
        [AllowAnonymous]
        public ActionResult Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.BlogPosts.FirstOrDefault(p => p.Slug == slug);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // GET: BlogPosts/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: BlogPosts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Title,Abstract,Body,MediaUrl,Published")] BlogPost blogPost, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                //Linked to StringUtilities.cs
                //Non -static call
                //var slugMaker = new StringUtilities();
                //var slug = slugMaker.MakeSlug(blogPost.Title);
                //....or....
                //Static call (uses more memory because of constant usage)
                var slug = StringUtilities.MakeSlug(blogPost.Title);

                if (string.IsNullOrWhiteSpace(slug))
                {
                    ModelState.AddModelError("Title", "Invalid title");
                    return View(blogPost);
                }

                //If the slug is already present in the database, thats bad
                if (db.BlogPosts.Any(p => p.Slug == slug))
                {
                    ModelState.AddModelError("Title", "The title must be unique");
                    return View(blogPost);
                }

                if (ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    var fileName = Path.GetFileName(image.FileName);
                    image.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), fileName));
                    blogPost.MediaUrl = "/Uploads/" + fileName;
                }

                if (blogPost.Published == true)
                {
                    blogPost.Created = DateTimeOffset.Now;
                }
                blogPost.Slug = slug;
                db.BlogPosts.Add(blogPost);
                db.SaveChanges();
                return RedirectToAction("Details", new { slug = blogPost.Slug }) ;
            }
            return View(blogPost);
        }

        // GET: BlogPosts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.BlogPosts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // POST: BlogPosts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Abstract,Slug,Body,MediaUrl,Published,Created,Updated")] BlogPost blogPost, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                #region Slug Section
                //This creates a new slug if the Title is changed during editing 
                var newSlug = StringUtilities.MakeSlug(blogPost.Title);

                //If the slug has changed, we need perform the same checks as in create
                if (newSlug != blogPost.Slug)
                {
                    //No guarantee we can use the new slug if its blank
                    if (string.IsNullOrWhiteSpace(newSlug))
                    {
                        ModelState.AddModelError("Title", "Invalid title");
                        return View(blogPost);
                    }

                    //If the slug is already present in the database, thats bad
                    if (db.BlogPosts.Any(p => p.Slug == newSlug))
                    {
                        ModelState.AddModelError("Title", "The title must be unique");
                        return View(blogPost);
                    }
                }
                #endregion

                #region Image Section
                if (ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    var fileName = Path.GetFileName(image.FileName);
                    image.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), fileName));
                    blogPost.MediaUrl = "/Uploads/" + fileName;
                }
                #endregion

                if (blogPost.Published)
                {
                blogPost.Created = DateTimeOffset.Now;
                }

                blogPost.Slug = newSlug;
                blogPost.Updated = DateTimeOffset.Now;
                db.Entry(blogPost).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Details", new { slug = blogPost.Slug });
            }
            return View(blogPost);
        }

        // GET: BlogPosts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.BlogPosts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // POST: BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            BlogPost blogPost = db.BlogPosts.Find(id);
            //if (blogPost.MediaUrl != null)
            //{
                //Something to do with the following syntax to grab the file path and delete
                //var fileName = Path.GetFileName(image.FileName);
                //image.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), fileName));
                //blogPost.MediaUrl = "/Uploads/" + fileName;
            //}
            if (blogPost.Published)
            {
            db.BlogPosts.Remove(blogPost);
            db.SaveChanges();
            return RedirectToAction("AdminIndex");
            }
            else
            {
            db.BlogPosts.Remove(blogPost);
            db.SaveChanges();
            return RedirectToAction("Unpublished");
            }
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
