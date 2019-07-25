using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Blog.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string Slug { get; set; }
        [AllowHtml]
        public string Body { get; set; }
        public string MediaUrl { get; set; }
        public bool Published { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }

        //Virtual Nav Section
        public virtual ICollection<Comment> Comments { get; set; } //interface collection (list or hashset)


        //A constructor is a method that shares its name with its class
        public BlogPost() 
        {
            this.Comments = new HashSet<Comment>(); //"HashSet" class satisfies the interface collection
        }
    }
}