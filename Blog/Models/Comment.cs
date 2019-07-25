using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Blog.Models //file path... Comment.cs is in the Models folder of the Blog solution
{
    public class Comment
    {
        public int Id { get; set; } //public integer providing a primary key 
        public int BlogPostId { get; set; } //identifying a blog post
        public string AuthorId { get; set; } //identifying which user wrote the post
        public string Body { get; set; } //identifying the body of the blog post
            public DateTimeOffset Created { get; set; } //timestamp idenitfying the blog post's creation time
            public DateTimeOffset? Updated { get; set; } //identifying the blog post's update times ('?'=condition can be null)
            public string UpdateReason { get; set; } //identifying the updater's resaoning
           
            /*Virtual Navigation Section
            (connection allowing comment to query its parent)*/
            public virtual BlogPost BlogPost { get; set; } //"BlogPost(2)" is the type of "BlogPost(1)" class
            public virtual ApplicationUser Author { get; set; } //"Author" is the type of "ApplcationUser" class
    }
}