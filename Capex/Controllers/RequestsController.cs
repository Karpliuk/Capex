using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace Capex.Models
{
    public class RequestsController : Controller
    {
        private CapexContext db = new CapexContext();

        // GET: Requests
        public ActionResult Index(string SortRequest)
        {
            // сортировка
            ViewBag.NameSortParm = SortRequest == "name_asc" ? "name_desc" : "name_asc";
            ViewBag.DateSortParm = SortRequest == "date_asc" ? "date_desc" : "date_asc";
            ViewBag.ValueSortParm = SortRequest == "value_asc" ? "value_desc" : "value_asc";


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
                            where Rq.UserID == User.Identity.Name
                            select Rq).Union
                           (from R in db.Requests
                            where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.UserID)
                            select R);

            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove)
            {
                // получаем все заявки
                requests = (from R in db.Requests select R);
            }

            SetUnitAndState();

            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove)
            {
                var requests_ = (from Rq in db.Requests select Rq);
                switch (SortRequest)
                {
                    case "name_desc":
                        requests_ = requests_.OrderByDescending(s => s.RequestID);
                        break;
                    case "name_asc":
                        requests_ = requests_.OrderBy(s => s.RequestID);
                        break;
                    case "date_asc":
                        requests_ = requests_.OrderBy(s => s.CreationDate);
                        break;
                    case "date_desc":
                        requests_ = requests_.OrderByDescending(s => s.CreationDate);
                        break;
                    case "value_asc":
                        requests_ = requests_.OrderBy(s => s.Value);
                        break;
                    case "value_desc":
                        requests_ = requests_.OrderByDescending(s => s.Value);
                        break;
                    default:
                        requests_ = requests_.OrderBy(s => s.RequestID);
                        break;
                }
                return View(requests_.ToList());
            }

            switch (SortRequest)
            {
                case "name_desc":
                    requests = requests.OrderByDescending(s => s.RequestID);
                    break;
                case "name_asc":
                    requests = requests.OrderBy(s => s.RequestID);
                    break;
                case "date_asc":
                    requests = requests.OrderBy(s => s.CreationDate);
                    break;
                case "date_desc":
                    requests = requests.OrderByDescending(s => s.CreationDate);
                    break;
                case "value_asc":
                    requests = requests.OrderBy(s => s.Value);
                    break;
                case "value_desc":
                    requests = requests.OrderByDescending(s => s.Value);
                    break;
                default:
                    requests = requests.OrderBy(s => s.RequestID);
                    break;
            }

            return View(requests.ToList());
        }

        [HttpPost]
        public ActionResult Index(RequestState? State, Unit? Unit, DateTime? StartCreationDate, DateTime? EndCreationDate,  string SortRequest)
        {
            //if(AllRequesrts)
            //{
            //    return RedirectToAction("Index");
            //}

            UserRole role = (from c in db.Users
                             where c.UserID == User.Identity.Name
                             select c.Role).FirstOrDefault();

            var rq = (from Rq in db.Requests
                            where Rq.UserID == User.Identity.Name
                            select Rq).Union
                           (from R in db.Requests
                            where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.UserID)
                            select R);
            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove)
            {
                rq = (from R in db.Requests select R);
            }

            SetUnitAndState();

            List<Request> filteredList;
            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove)
            {
                var requests_ = (from Rq in db.Requests select Rq).ToList();
                filteredList = new List<Request>();

                foreach (var request in requests_)
                {
                    if ((StartCreationDate != null) && (EndCreationDate != null))
                    {
                        if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)) && (request.State == State))
                        {
                            if (Unit == Capex.Models.Unit.Все)
                            {
                                if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)))
                                {
                                    filteredList.Add(request);
                                }
                            }
                            else
                            if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                            {
                                filteredList.Add(request);
                            }
                        }
                    }
                    else
                    if (StartCreationDate != null)
                    {
                        if ((request.CreationDate.Date >= StartCreationDate) && (request.State == State))
                        {
                            if (Unit == Capex.Models.Unit.Все)
                            {
                                if (request.CreationDate.Date >= StartCreationDate)
                                {
                                    filteredList.Add(request);
                                }
                            }
                            else
                           if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                            {
                                filteredList.Add(request);
                            }
                        }
                    }
                    else
                    if (EndCreationDate != null)
                    {
                       if ((request.CreationDate.Date <= EndCreationDate) && (request.State == State))
                        {
                            if (Unit == Capex.Models.Unit.Все)
                            {
                                if (request.CreationDate.Date <= EndCreationDate)
                                {
                                    filteredList.Add(request);
                                }
                            }
                            else
                            if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                            {
                                filteredList.Add(request);
                            }
                        }
                    }
                    else
                    {
                        if (request.State == State)
                        {

                            if (Unit == Capex.Models.Unit.Все)
                            {
                                filteredList.Add(request);
                            }
                            else
                                if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault()) 
                            {
                                filteredList.Add(request);
                            }
                        }
                    }
                }

                //List<Request> filteredSortList = null;
                //foreach (var filter in filteredList)
                //{
                //    filteredSortList.Add((from R in db.Requests where R.RequestID == filter.RequestID select R).FirstOrDefault());
                //}

                return View(filteredList.ToList());
            }

            // получить список заявок 
            var requests = (from Rq in db.Requests
                            where Rq.UserID == User.Identity.Name
                            select Rq).Union
                           (from R in db.Requests
                            where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.UserID)
                            select R).ToList();

            filteredList = new List<Request>();
            foreach (var request in requests)
            {
                if((StartCreationDate != null) && (EndCreationDate != null))
                {
                    if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)) && (request.State == State))
                    {
                        if (Unit == Capex.Models.Unit.Все)
                        {
                            if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)))
                            {
                                filteredList.Add(request);
                            }
                        }
                        else
                       if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                        {
                            filteredList.Add(request);
                        }                        
                    }
                }
                else
                if(StartCreationDate != null)
                {
                    if ((request.CreationDate.Date >= StartCreationDate) && (request.State == State))
                    {
                        if (Unit == Capex.Models.Unit.Все)
                        {
                            if (request.CreationDate.Date >= StartCreationDate)
                            {
                                filteredList.Add(request);
                            }
                        }
                        else
                        if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                        {
                            filteredList.Add(request);
                        }                       
                    }
                }
                else
                if (EndCreationDate != null)
                {
                   if ((request.CreationDate.Date <= EndCreationDate) && (request.State == State))
                    {
                        if (Unit == Capex.Models.Unit.Все)
                        {
                            if (request.CreationDate.Date <= EndCreationDate)
                            {
                                filteredList.Add(request);
                            }
                        }
                       else
                       if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                        {
                            filteredList.Add(request);
                        }
                    }
                }
                else
                {
                   if (request.State == State)
                    {
                       if (Unit == Capex.Models.Unit.Все)
                       {
                           filteredList.Add(request);
                       }
                       else
                       if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                        {
                            filteredList.Add(request);
                        }
                    }
                }
            }
            return View(filteredList.ToList());
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
        public ActionResult Create([Bind(Include = "RequestID,UserID,CreationDate,Description,Value,Currency,LongDescription,CommentRequest,State")] Request request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // некоторые значения дефолтные
                    request.UserID = User.Identity.Name;
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
                    return RedirectToAction("Index");
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
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = RequestState.Finalized.ToString(),
                                Value = RequestState.Finalized.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.ApprovedByManager)
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.Created)
                        {

                            UserRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = RequestState.Created.ToString(),
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
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.ApprovedByManager.ToString(),
                                Value = RequestState.ApprovedByManager.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else if (request.State == RequestState.Cancelled)
                        {
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.ApprovedByManager.ToString(),
                                Value = RequestState.ApprovedByManager.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Created.ToString(),
                                Value = RequestState.Created.ToString()
                            });
                        }
                        else
                        {
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
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
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = RequestState.SentToMedicover.ToString(),
                                Value = RequestState.SentToMedicover.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = RequestState.ApprovedByMedicover.ToString(),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else if (request.State == RequestState.SentToMedicover)
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = RequestState.ApprovedByMedicover.ToString(),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        if (request.State != RequestState.Cancelled)
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
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
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.ApprovedByManager.ToString(),
                                Value = RequestState.ApprovedByManager.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.ApprovedByMedicover.ToString(),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Finalized.ToString(),
                                Value = RequestState.Finalized.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.ApprovedByManager)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.SentToMedicover.ToString(),
                                Value = RequestState.SentToMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else if (request.State == RequestState.SentToMedicover)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.ApprovedByMedicover.ToString(),
                                Value = RequestState.ApprovedByMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Finalized.ToString(),
                                Value = RequestState.Finalized.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        if (request.State == RequestState.ApprovedByMedicover)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.SentToMedicover.ToString(),
                                Value = RequestState.SentToMedicover.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Finalized.ToString(),
                                Value = RequestState.Finalized.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = RequestState.Cancelled.ToString(),
                                Value = RequestState.Cancelled.ToString()
                            });
                        }
                        else
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
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
                        //        Text = request.State.ToString(),
                        //        Value = request.State.ToString()
                        //    });
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = RequestState.Finalized.ToString(),
                        //        Value = RequestState.Finalized.ToString()
                        //    });
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = RequestState.Cancelled.ToString(),
                        //        Value = RequestState.Cancelled.ToString()
                        //    });
                        //}
                        //else
                        //if (request.State != RequestState.Cancelled)
                        //{
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = request.State.ToString(),
                        //        Value = request.State.ToString()
                        //    });
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = RequestState.Cancelled.ToString(),
                        //        Value = RequestState.Cancelled.ToString()
                        //    });
                        //}
                        //else
                        //{
                        //    FinancialManagerRole.Add(new SelectListItem
                        //    {
                        //        Text = request.State.ToString(),
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
            foreach (object item in Enum.GetValues(typeof(RequestState)))
            {
                itemsRequests.Add(new SelectListItem
                {
                    Text = item.ToString(),
                    Value = item.ToString()
                });
            }
            ViewBag.State = itemsRequests;

            // All Unit
            List<SelectListItem> itemsUsers = new List<SelectListItem>();
            foreach (object item in Enum.GetValues(typeof(Unit)))
            {
                itemsUsers.Add(new SelectListItem
                {
                    Text = item.ToString(),
                    Value = item.ToString()
                });
            }
            ViewBag.Unit = itemsUsers;
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
    }
}
