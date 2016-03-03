using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace Capex.Models
{
    public class RequestsController : Controller
    {
        private CapexContext db = new CapexContext();

        // GET: Requests
        public ActionResult Index(bool filter=false)
        {
            if (filter)
            {
                return Index((RequestState?)Session["State"], (Unit?)Session["Unit"], (DateTime?)Session["StartCreationDate"], (DateTime?)Session["EndCreationDate"]);
            }
            SetUnitAndState();
            // проверяем есть ли такой пользователь в  бд таблице USER
            if(((from c in db.Users where c.UserID == User.Identity.Name select c.FullName).FirstOrDefault()) == null)
            {
                return RedirectToAction("Error");
            }

            UserRole role = (from c in db.Users
                             where c.UserID == User.Identity.Name
                             select c.Role).FirstOrDefault();

            // получаем заявки текущего пользователя
            var requests = (from Rq in db.Requests
                            where Rq.User.UserID == User.Identity.Name
                            select Rq).Union
                           (from R in db.Requests
                            where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.User.UserID)
                            select R);

            if (role == Capex.Models.UserRole.ViewAll)
            {
                ViewBag.Access = false;
                // получаем All заявки
                requests = (from R in db.Requests select R);
                return View(requests.ToList());
            }
            else
            {
                ViewBag.Access = true;
            }

            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove)
            {
                var requests_ = (from Rq in db.Requests select Rq);
                return View(requests_.ToList());
            }

            bool? view_All_Request_for_User =
               db.Users.Where(x => x.UserID == User.Identity.Name).Select(x => x.ViewAll).FirstOrDefault();
            if (view_All_Request_for_User == true)
            {
                // получаем заявки текущего пользователя
                ViewBag.All_Request = (from Rq in db.Requests
                                       where Rq.User.UserID == User.Identity.Name
                                       select Rq.RequestID).Union
                               (from R in db.Requests
                                where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.User.UserID)
                                select R.RequestID).ToList();

                requests = (from Rq in db.Requests select Rq);
            }
            return View(requests.ToList());
        }

        [HttpPost]
        public ActionResult Index(RequestState? State, Unit? Unit, DateTime? StartCreationDate, DateTime? EndCreationDate)
        {
            SetUnitAndState();
            InitializationFilterVariable(State, Unit, StartCreationDate, EndCreationDate);
            UserRole role = (from c in db.Users
                             where c.UserID == User.Identity.Name
                             select c.Role).FirstOrDefault();
            if (role == Capex.Models.UserRole.ViewAll)
            {
                ViewBag.Access = false;
            }
            else
            {
                ViewBag.Access = true;
            }
            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove || role == UserRole.ViewAll)
            {
                IEnumerable<Request> requests = (from Rq in db.Requests select Rq).ToList();
                List<Request> filteredList = GetRequests(State, Unit, StartCreationDate, EndCreationDate, requests);
                return View(filteredList.ToList());
            }
            else
            {
                bool? view_All_Request_for_User =
                 db.Users.Where(x => x.UserID == User.Identity.Name).Select(x => x.ViewAll).FirstOrDefault();
                if (view_All_Request_for_User == true)
                {
                    // получаем заявки текущего пользователя
                    ViewBag.All_Request = (from Rq in db.Requests
                                           where Rq.User.UserID == User.Identity.Name
                                           select Rq.RequestID).Union
                                   (from R in db.Requests
                                    where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.User.UserID)
                                    select R.RequestID).ToList();
                    IEnumerable<Request> requests = db.Requests.ToList();
                    List<Request> filteredList = GetRequests(State, Unit, StartCreationDate, EndCreationDate, requests);
                    return View(filteredList.ToList());
                }
                else
                {
                    // получить список заявок user, manager
                    IEnumerable<Request> requests = (from Rq in db.Requests
                                                     where Rq.User.UserID == User.Identity.Name
                                                     select Rq).Union
                                                     (from R in db.Requests
                                                      where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.User.UserID)
                                                      select R).ToList();
                    List<Request> filteredList = GetRequests(State, Unit, StartCreationDate, EndCreationDate, requests);
                    return View(filteredList.ToList());
                }
            }                         
        }

        private void InitializationFilterVariable(RequestState? State, Unit? Unit, DateTime? StartCreationDate,DateTime? EndCreationDate)
        {
            Session["State"] = State;
            Session["Unit"] = Unit;
            Session["StartCreationDate"] = StartCreationDate;
            Session["EndCreationDate"] = EndCreationDate;
        }

        [HttpPost]
        public ActionResult ShowAll()
        {
            return RedirectToAction("Index");
        }

        // GET: Requests/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = db.Requests.Find(id);
            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }

        public ActionResult Details_ViewAllRole(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = db.Requests.Find(id);
            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }

        // GET: Requests/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Requests/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RequestID,CreationDate,Description,Value,Currency,LongDescription,CommentRequest,State,User_Id")] Request request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // некоторые значения дефолтные
                    request.User = (from c in db.Users
                                    where c.UserID == User.Identity.Name
                                    select c).FirstOrDefault();
                    request.CreationDate = DateTime.Now;
                    // получаем роль текущего пользователя
                    UserRole role = (from c in db.Users
                                     where c.UserID == User.Identity.Name
                                     select c.Role).FirstOrDefault();
                    if(role == UserRole.User)
                    {
                        request.State = RequestState.Created; // "Created"
                    }
                    else
                    if(role == UserRole.Manager)
                    {
                        request.State = RequestState.ApprovedByManager;
                    }
                    else
                    if (role == UserRole.CFOMedicove)
                    {
                        request.State = RequestState.ApprovedByMedicover;
                    }
                    else
                    {
                        request.State = RequestState.Created; // "Created"
                    }                        

                    request.Value = decimal.Round(request.Value, 2); // округлим до 2 знака после запятой
                    db.Requests.Add(request);
                    db.SaveChanges();
                    EmailNotification(request.User, request.RequestID, request.Description);
                    return RedirectToAction("Index");
                }
            }
           catch(Exception ex)
            {

            }
            return View(request);
        }

        // GET: Requests/Edit/5
        public ActionResult Edit(int? id)
        {            
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = db.Requests.Find(id);
            if (request == null)
            {
                return HttpNotFound();
            }

            try
            {
                // получаем роль текущего пользователя
                UserRole role = (from c in db.Users
                                 where c.UserID == User.Identity.Name
                                 select c.Role).FirstOrDefault();
                switch (role)
                {
                    case UserRole.User:
                                        if ((request.State > RequestState.Created) && (request.State != RequestState.Cancelled))
                                        {
                                            ViewBag.PriceReadOnly = true;
                                        }
                                        else
                                        {
                                            ViewBag.PriceReadOnly = false;
                                        }
                                        break;
                    case UserRole.Manager:
                                        if (request.State > RequestState.ApprovedByManager)
                                        {
                                            ViewBag.PriceReadOnly = true;
                                        }
                                        else
                                        {
                                            ViewBag.PriceReadOnly = false;
                                        }
                                        break;
                    case UserRole.CFOMedicove:
                                        ViewBag.PriceReadOnly = false;
                                        break;
                    default:
                                        ViewBag.PriceReadOnly = false;
                                        break;
                }
                GetPossibleStates(request);
            }
            catch(Exception ex)
            {

            }           

            return View(request);
        }

        // POST: Requests/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestID,UserID,CreationDate,Description,Value,Currency,LongDescription,CommentRequest,State")] Request request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(request).State = EntityState.Modified;
                    request.Value = decimal.Round(request.Value, 2); 
                    db.SaveChanges();
                    return RedirectToAction("Index", new {filter=true});
                }
                else
                {
                    GetPossibleStates(request);
                }
            }
           catch(Exception ex)
            {
               
            }

            return View(request);
        }

        private void EmailNotification(User sendingUser, int requestID, string description)
        {
            List<string> destinations = new List<string>();
            if (!String.IsNullOrEmpty(sendingUser.ManagerID))
            {
                destinations.Add(sendingUser.ManagerID.Remove(0, 7) + "@synevo.ua"); //SYNEVO\\
            }
            else
            {
                // список получателей Role = 3
               var users = (from c in db.Users
                                 where c.Role == UserRole.CFOMedicove 
                                 select c.UserID).ToList();
                foreach (var user in users)
                {
                    destinations.Add(user.Remove(0, 7) + "@synevo.ua"); //SYNEVO\\
                }
            }
            // Подключите здесь службу электронной почты для отправки сообщения электронной почты.
            SmtpSection section = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");

            SmtpClient client = new SmtpClient(section.Network.Host, section.Network.Port);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(section.Network.UserName, section.Network.Password);
            client.EnableSsl = section.Network.EnableSsl;

            // создаем письмо: message.Destination - адрес получателя
            var mail = new MailMessage(section.From, destinations[0]);
            if (destinations.Count > 1)
            {
                for (int i = 1; i < destinations.Count; i++)
                {
                    mail.To.Add(destinations[i]);
                }
            }

            mail.Subject = ConfigurationManager.AppSettings["emailSubject"];
            mail.Body = GetEmailBody(sendingUser.FullName, requestID, description);
            mail.IsBodyHtml = true;
            client.Send(mail);
        }

        private string GetEmailBody(string fullName, int requestID, string description)
        {
            string pathBodyFile = "Content\\" + ConfigurationManager.AppSettings["emailBody"];
            string bodyFile = $@"{System.Web.HttpContext.Current.Server.MapPath("~/")}{pathBodyFile}\EmailBody.html";
            if (!System.IO.File.Exists(bodyFile))
                bodyFile = $@"{System.Web.HttpContext.Current.Server.MapPath("~/")}{pathBodyFile}\EmailBody{""}.html";
            string emailBody;
            try
            {
                emailBody = new StreamReader(bodyFile, System.Text.Encoding.ASCII).ReadToEnd();
                emailBody = emailBody.Replace("{0}", fullName)
                                     .Replace("{1}", requestID.ToString())
                                     .Replace("{2}", description);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return emailBody;
        }

        private void GetPossibleStates(Request request)
        {
            try
            {
                UserRole role = (from c in db.Users
                            where c.UserID == User.Identity.Name
                            select c.Role).FirstOrDefault();

                switch (role)
                {
                    case Models.UserRole.User:
                        List<SelectListItem> UserRole = new List<SelectListItem>();
                        if (request.State == RequestState.ApprovedByMedicover)
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Finalized),
                                Value = RequestState.Finalized.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.ApprovedByManager)
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.Created)
                        {

                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        if(request.State == RequestState.ClarificationNeeded)
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Created),
                                Value = RequestState.Created.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Created),
                                Value = RequestState.Created.ToString()
                            });
                        }
                        ViewBag.State = UserRole;
                        break;
                    case Models.UserRole.Manager:
                        List<SelectListItem> ManagerRole = new List<SelectListItem>();
                        if (request.State == RequestState.Created)
                        {
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByManager),
                                Value = RequestState.ApprovedByManager.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else if (request.State == RequestState.Cancelled)
                        {
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByManager),
                                Value = RequestState.ApprovedByManager.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Created),
                                Value = RequestState.Created.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.ClarificationNeeded)
                        {
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Created),
                                Value = RequestState.Created.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByManager),
                                Value = RequestState.ApprovedByManager.ToString()
                            });
                        }
                        else
                        {
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                        }

                        ViewBag.State = ManagerRole;
                        break;
                    case Models.UserRole.CFOMedicove:
                        List<SelectListItem> CFOMedicoveRole = new List<SelectListItem>();
                        if (request.State == RequestState.ApprovedByManager)
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.SentToMedicover),
                                Value = RequestState.SentToMedicover.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByMedicover),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else if (request.State == RequestState.SentToMedicover)
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByMedicover),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        if (request.State != RequestState.Cancelled)
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                        }
                        ViewBag.State = CFOMedicoveRole;
                        break;
                    case Models.UserRole.FinancialManager:
                        List<SelectListItem> FinancialManagerRole = new List<SelectListItem>();
                        if (request.State == RequestState.Created)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByManager),
                                Value = RequestState.ApprovedByManager.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByMedicover),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Finalized),
                                Value = RequestState.Finalized.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.ApprovedByManager)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                               Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.SentToMedicover),
                                Value = RequestState.SentToMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                               Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByMedicover),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else if (request.State == RequestState.SentToMedicover)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                               Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                               Text = GetDisplayNameFromEnumRequestState(RequestState.ApprovedByMedicover),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Finalized),
                                Value = RequestState.Finalized.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.ApprovedByMedicover)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                               Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.SentToMedicover),
                                Value = RequestState.SentToMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Finalized),
                                Value = RequestState.Finalized.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.ClarificationNeeded),
                                Value = RequestState.ClarificationNeeded.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                               Text = GetDisplayNameFromEnumRequestState(request.State),
                                Value = request.State.ToString()
                            });
                        }
                        ViewBag.State = FinancialManagerRole;

                        // как в ТЗ
                        //List<SelectListItem> FinancialManagerRole = new List<SelectListItem>();
                        //if (request.State == RequestState.ApprovedByMedicover)
                        //{
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //       Text = GetDisplayNameFromEnumRequestState(request.State),
                        //        Value = request.State.ToString()
                        //    });
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = GetDisplayNameFromEnumRequestState(RequestState.Finalized),
                        //        Value = RequestState.Finalized.ToString()
                        //    });
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                        //        Value = RequestState.Cancelled.ToString()
                        //    });
                        //}
                        //else
                        //if (request.State != RequestState.Cancelled)
                        //{
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //       Text = GetDisplayNameFromEnumRequestState(request.State),
                        //        Value = request.State.ToString()
                        //    });
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = GetDisplayNameFromEnumRequestState(RequestState.Cancelled),
                        //        Value = RequestState.Cancelled.ToString()
                        //    });
                        //}
                        //else
                        //{
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //       Text = GetDisplayNameFromEnumRequestState(request.State),
                        //        Value = request.State.ToString()
                        //    });
                        //}
                        //ViewBag.State = FinancialManagerRole;
                        break;
                }
            }
            catch(Exception ex) { }           
        }

        private void SetUnitAndState()
        {          
            // All State
            List<SelectListItem> itemsRequests = new List<SelectListItem>();
            foreach (Enum item in Enum.GetValues(typeof(RequestState)))
            {
                itemsRequests.Add(new SelectListItem
                {
                    Text = GetDisplayNameFromEnumRequestState(item),
                    Value = item.ToString()
                });
            }
            ViewBag.State = itemsRequests;

            // All Unit
            List<SelectListItem> itemsUsers = new List<SelectListItem>();
            foreach (Enum item in Enum.GetValues(typeof(Unit)))
            {
                itemsUsers.Add(new SelectListItem
                {
                    Text = GetDisplayNameFromEnumUnit(item),
                    Value = item.ToString()
                });
            }
            ViewBag.Unit = itemsUsers;
        }

        public string GetDisplayNameFromEnumRequestState(Enum item)
        {
            try
            {
                var type = typeof(RequestState);
                var member = type.GetMember(item.ToString());
                var attributes = member[0].GetCustomAttributes(typeof(DisplayAttribute), false);
                return ((DisplayAttribute)attributes[0]).Name;
            }   
            catch(Exception ex)
            {

            }      
            return "";
        }

        public string GetDisplayNameFromEnumUnit(Enum item)
        {
            try
            {
                var type = typeof(Unit);
                var member = type.GetMember(item.ToString());
                var attributes = member[0].GetCustomAttributes(typeof(DisplayAttribute), false);
                return ((DisplayAttribute)attributes[0]).Name;
            }
            catch (Exception ex)
            {

            }
            return "";
        }

        // GET: Requests/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = db.Requests.Find(id);
            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }
        
        public ActionResult Error()
        {
            return View("Error");
        }

        // POST: Requests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Request request = db.Requests.Find(id);
            db.Requests.Remove(request);
            db.SaveChanges();
            return RedirectToAction("Index");
        }       

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public List<Request> GetRequests(RequestState? State, Unit? Unit, DateTime? StartCreationDate, DateTime? EndCreationDate, IEnumerable<Request> requests)
        {           
           // var requests_ = (from Rq in db.Requests select Rq).ToList();
           List<Request> filteredList = new List<Request>();

            foreach (var request in requests)
            {
                if ((StartCreationDate != null) && (EndCreationDate != null))
                {
                    if ((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate))
                    {
                        if (State == Capex.Models.RequestState.All)
                        {
                            if (Unit == Capex.Models.Unit.All)
                            {
                                if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)))
                                {
                                    filteredList.Add(request);
                                }
                            }
                            else
                            if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                            {
                                filteredList.Add(request);
                            }
                        }
                        else
                        {
                            if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)) && (request.State == State))
                            {
                                if (Unit == Capex.Models.Unit.All)
                                {
                                    if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)))
                                    {
                                        filteredList.Add(request);
                                    }
                                }
                                else
                                if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                                {
                                    filteredList.Add(request);
                                }
                            }
                        }

                    }
                }
                else
                if (StartCreationDate != null)
                {
                    if (request.CreationDate.Date >= StartCreationDate)
                    {
                        if (State == Capex.Models.RequestState.All)
                        {
                            if (Unit == Capex.Models.Unit.All)
                            {
                                if (request.CreationDate.Date >= StartCreationDate)
                                {
                                    filteredList.Add(request);
                                }
                            }
                            else
                                if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                            {
                                filteredList.Add(request);
                            }
                        }
                        else
                        {
                            if ((request.CreationDate.Date >= StartCreationDate) && (request.State == State))
                            {
                                if (Unit == Capex.Models.Unit.All)
                                {
                                    if (request.CreationDate.Date >= StartCreationDate)
                                    {
                                        filteredList.Add(request);
                                    }
                                }
                                else
                                if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                                {
                                    filteredList.Add(request);
                                }
                            }
                        }
                    }
                }
                else
                if (EndCreationDate != null)
                {
                    if (request.CreationDate.Date <= EndCreationDate)

                    {
                        if (State == Capex.Models.RequestState.All)
                        {
                            if (Unit == Capex.Models.Unit.All)
                            {
                                if (request.CreationDate.Date <= EndCreationDate)
                                {
                                    filteredList.Add(request);
                                }
                            }
                            else
                            if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                            {
                                filteredList.Add(request);
                            }
                        }
                        else
                        {
                            if ((request.CreationDate.Date <= EndCreationDate) && (request.State == State))
                            {
                                if (Unit == Capex.Models.Unit.All)
                                {
                                    if (request.CreationDate.Date <= EndCreationDate)
                                    {
                                        filteredList.Add(request);
                                    }
                                }
                                else
                                if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                                {
                                    filteredList.Add(request);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (State == Capex.Models.RequestState.All)
                    {
                        if (Unit == Capex.Models.Unit.All)
                        {
                            filteredList.Add(request);
                        }
                        else
                            if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                        {
                            filteredList.Add(request);
                        }
                    }
                    else
                    if (request.State == State)
                    {
                        if (Unit == Capex.Models.Unit.All)
                        {
                            filteredList.Add(request);
                        }
                        else
                        if (Unit == (from Us in db.Users where Us.UserID == request.User.UserID select Us.Unit).FirstOrDefault())
                        {
                            filteredList.Add(request);
                        }
                    }                    
               }           
          }
            return filteredList.ToList();
        }       
    }
}
