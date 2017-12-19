using Mosaico_Dot_Net.Helpers;
using Mosaico_Dot_Net.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
//using System.Web.Http;

namespace Mosaico_Dot_Net.Controllers
{
    [Route("mosaico")]
    public class MosaicoController : Controller
    {
        private List<MosaicoEmail> listemails;
        private readonly HostingEnvironment hostingEnvironment;

        ////private readonly HostingEnvironment hostingEnvironment;
        ////private readonly Options<SmtpOptions> smtpOptions;


        public MosaicoController()
        {
            listemails = new List<MosaicoEmail>();
            listemails.Add(new MosaicoEmail() { Id = 1, Name = "Template1", Metadata = "", Template = MosaicoTemplate.TEDC15, Content = "" });
            listemails.Add(new MosaicoEmail() { Id = 2, Name = "Template2", Metadata = "", Template = MosaicoTemplate.Tutorial, Content = "" });
            listemails.Add(new MosaicoEmail() { Id = 3, Name = "Template1", Metadata = "", Template = MosaicoTemplate.Versafix1, Content = "" });


        }

        [Route("")]
        // GET: Mosaico
        public ActionResult Index()
        {

            ViewBag.Title = "Drag and drop editor";
            return View(listemails);
        }

        //[Route("")]
        //public ActionResult Index()
        //{
        //    // TODO: Obviously in a real situation we need to have paging.
        //    //  But for demo purposes, this will do...
        //    var model = context.MosaicoEmails.ToList();
        //    ViewBag.Title = "Free responsive email template editor | Mosaico.io";
        //    return View(model);
        //}

        [Route("editor/{name}/{template}/{id?}")]
        public ActionResult Editor(string name, MosaicoTemplate template, int id = 0)
        {
            MosaicoEmail model;
            if (id > 0)
            {
                model = listemails.FirstOrDefault(x => x.Id == id);
            }
            else
            {
                model = new MosaicoEmail
                {
                    Name = name,
                    Template = template
                };
            }

            ViewBag.Title = "Mosaico Editor";

            // TODO: Add your own tokens here
            ViewBag.FieldTokens = new Dictionary<string, string>
            {
                { "Title", "Title" },
                { "FirstName", "First Name" },
                { "LastName", "Last Name" },
            };

            return View(model);
        }

        //[HttpPost]
        //[Route("dl")]
        //public ActionResult dl(
        //    string action,
        //    string filename,
        //    string rcpt,
        //    string subject,
        //    string html)
        //{
        //    switch (action)
        //    {
        //        case "download":
        //            {
        //                var bytes = Encoding.UTF8.GetBytes(html);
        //                return File(bytes, "text/html", filename);
        //            }
        //        case "email":
        //            {
        //                var message = new MimeMessage
        //                {
        //                    Subject = subject,
        //                    Body = new TextPart(TextFormat.Html) { Text = html }
        //                };

        //                message.From.Add(new MailboxAddress(smtpOptions.Value.FromName, smtpOptions.Value.FromEmail));
        //                message.To.Add(new MailboxAddress(rcpt));

        //                using (var smtpClient = new SmtpClient())
        //                {
        //                    await smtpClient.ConnectAsync(smtpOptions.Value.Host, smtpOptions.Value.Port, false);
        //                    await smtpClient.AuthenticateAsync(smtpOptions.Value.UserName, smtpOptions.Value.Password);
        //                    await smtpClient.SendAsync(message);
        //                    await smtpClient.DisconnectAsync(true);
        //                }

        //                return Ok();
        //            }
        //        default: throw new ArgumentException("Unsuported action type: " + action);
        //    }
        //}

        [HttpGet]
        [Route("upload")]
        public ActionResult upload()
        {
            string path = Server.MapPath("~/Media/Uploads");

            var files = (new DirectoryInfo(path)).GetFiles().Select(x => new MosaicoFileInfo
            {
                Name = x.Name,
                Size = x.Length,
                Type = System.Web.MimeMapping.GetMimeMapping(x.Name),

                Url = Server.MapPath(string.Concat("~/Media/Uploads/", x.Name)),
                ThumbnailUrl = Server.MapPath(string.Concat("~/Media/Thumbs/", x.Name)),


                DeleteUrl = string.Concat("/mosaico/img-delete/", x.Name),
                DeleteType = "DELETE"
            });

            //return Content(HttpStatusCode.OK, new { files = files });
            return Json(new { files = files });
        }

        [HttpPost]
        [Route("upload")]
        public JsonResult Upload()
        {
            var files = Request.Files;
            var returnList = new List<MosaicoFileInfo>();

            foreach (string fileName in files)
            {
                HttpPostedFileBase file = Request.Files[fileName];
                if (file.ContentLength > 0)
                {
                    string _FileName = Path.GetFileName(file.FileName);
                    string _path = Path.Combine(Server.MapPath("~/Media/Uploads/"), _FileName);
                    string _thumb = Path.Combine(Server.MapPath("~/Media/Thumbs/"), _FileName);
                    file.SaveAs(_path);


                    try
                    {

                        var image = System.Drawing.Image.FromFile(_path);
                        var thumbnail = ImageHelper.Resize(image, 120, 90);
                        thumbnail.Save(_thumb);
                    }
                    catch (Exception) { }

                    returnList.Add(new MosaicoFileInfo
                    {
                        Name = file.FileName,
                        Size = file.ContentLength,
                        Type = System.Web.MimeMapping.GetMimeMapping(file.FileName),
                        Url =  Path.Combine(Server.MapPath("~/Media/Uploads/"), _FileName),
                        ThumbnailUrl = Path.Combine(Server.MapPath("~/Media/Thumbs/"), _FileName),
                        DeleteUrl = string.Concat("/mosaico/img-delete/", file.FileName),
                        DeleteType = "DELETE"
                    });
                }
            }
            return Json (new
            {
                files = returnList
            });
            //return Ok(new { files = returnList });
        }

        [Route("img")]
        public async Task<ActionResult> img(string src, string method, string @params)
        {
            var split = @params.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            switch (method)
            {
                case "cover":
                case "resize":
                    {
                        string filePath = HttpContext.Server.MapPath("~/Media/Uploads/" + src.LastIndexOf('/'));

                        //string filePath = Path.Combine(hostingEnvironment.WebRootPath, "Media\\Uploads\\", src.RightOfLastIndexOf('/'));
                        byte[] bytes = System.IO.File.ReadAllBytes(filePath);

                        Image result;
                        using (var stream = new MemoryStream(bytes))
                        {
                            var image = Bitmap.FromStream(stream);

                            int? destinationWidth = split[0] == "null" ? null : (int?)int.Parse(split[0]);
                            int? destinationHeight = split[1] == "null" ? null : (int?)int.Parse(split[1]);

                            if (destinationWidth.HasValue && destinationHeight.HasValue)
                            {
                                if (method == "cover")
                                {
                                    result = ImageHelper.Crop(image, destinationWidth.Value, destinationHeight.Value, AnchorPosition.Center);
                                }
                                else
                                {
                                    result = ImageHelper.Resize(image, destinationWidth.Value, destinationHeight.Value);
                                }
                            }
                            else if (destinationWidth.HasValue)
                            {
                                var newHeight = destinationWidth.Value * image.Height / image.Width;
                                result = ImageHelper.Resize(image, destinationWidth.Value, newHeight);
                            }
                            else if (destinationHeight.HasValue)
                            {
                                var newWidth = destinationHeight.Value * image.Width / image.Height;
                                result = ImageHelper.Resize(image, newWidth, destinationHeight.Value);
                            }
                            else
                            {
                                throw new ArgumentException("A destination width and/or height must be specified.");
                            }
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            result.Save(memoryStream, ImageFormat.Jpeg);
                            byte[] newBytes = memoryStream.ToArray();
                            return File(newBytes, "image/jpg");
                        }
                    }
                case "placeholder":
                default:
                    {
                        string width = split[0];
                        string height = split[1];

                        HttpClient client = new HttpClient();

                        string url = string.Format("http://via.placeholder.com/{0}x{1}", width, height);

                        //string data = client.DownloadString(new Uri(url));
                        byte[] bytes = await client.GetByteArrayAsync(new Uri(url));
                        return File(bytes, "image/jpg");

                    }
            }
        }

        [HttpDelete]
        [Route("img-delete/{fileName}")]
        public ActionResult Delete(string fileName)
        {
            string filePath = Path.Combine(Server.MapPath("~/Media/Uploads/"), fileName);

          

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            return Ok();
        }

        ////[HttpPost]
        ////[Route("save")]
        ////public async Task<IActionResult> Save(
        ////    int id,
        ////    string name,
        ////    MosaicoTemplate template,
        ////    string metadata,
        ////    string content,
        ////    string html)
        ////{
        ////    try
        ////    {
        ////        var record = await context.MosaicoEmails.FindAsync(id);

        ////        bool isNew = (record == null);

        ////        if (isNew)
        ////        {
        ////            record = new MosaicoEmail();
        ////        }

        ////        record.Name = name;
        ////        record.Template = template;
        ////        record.Metadata = metadata;
        ////        record.Content = content;
        ////        // Save the HTML so we can use it for mass emailing. Example: User will input tokens like {FirstName}, {LastName}, etc into the template,
        ////        //  then we can do a search and replace with regex when sending emails (Your own logic, somewhere in your app).
        ////        record.Html = html;

        ////        if (isNew)
        ////        {
        ////            await context.MosaicoEmails.AddAsync(record);
        ////        }
        ////        else
        ////        {
        ////            context.MosaicoEmails.Update(record);
        ////        }

        ////        await context.SaveChangesAsync();

        ////        return Ok(new { Success = true, Message = "Sucessfully saved email." });
        ////    }
        ////    catch (Exception x)
        ////    {
        ////        return Json(new { Success = false, Message = x.GetBaseException().Message });
        ////    }
        ////}

        public class MosaicoFileInfo
        {
            public string Name { get; set; }

            public long Size { get; set; }

            public string Type { get; set; }

            public string Url { get; set; }

            public string ThumbnailUrl { get; set; }

            public string DeleteUrl { get; set; }

            public string DeleteType { get; set; }
        }
    }
}