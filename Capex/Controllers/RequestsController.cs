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
            ViewBag.NameSortParm = String.IsNullOrEmpty(SortRequest) ? "name_desc" : "";
            ViewBag.DateSortParm = SortRequest == "Date" ? "date_desc" : "Date";
            ViewBag.ValueSortParm = SortRequest == "Value" ? "value_desc" : "Value";


            UserRole role = (from c in db.Users
                             where c.UserID == User.Identity.Name
                             select c.Role).FirstOrDefault();

             var requests = (from Rq in db.Requests
                            where Rq.UserID == User.Identity.Name
                             select Rq).Union
                            (from R in db.Requests
                            where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.UserID)
                            select R);


            var setRequests = (from d in requests select d.State).Distinct();
            List<SelectListItem> itemsRequests = new List<SelectListItem>();
            foreach (var s in setRequests)
            {
                itemsRequests.Add(new SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString()
                });
            }
            ViewBag.State = itemsRequests;


            var users = (from d in db.Users select d.Unit).Distinct();
            List<SelectListItem> itemsUsers = new List<SelectListItem>();
            foreach (var s in users)
            {
                itemsUsers.Add(new SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString()
                });
            }
            ViewBag.Unit = itemsUsers;

            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove)
            {
                var requests_ = (from Rq in db.Requests select Rq);
                //сортировки
                switch (SortRequest)
                {
                    case "name_desc":
                        requests_ = requests_.OrderByDescending(s => s.RequestID);
                        break;
                    case "Date":
                        requests_ = requests_.OrderBy(s => s.CreationDate);
                        break;
                    case "date_desc":
                        requests_ = requests_.OrderByDescending(s => s.CreationDate);
                        break;
                    case "Value":
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

            //сортировки
            switch (SortRequest)
            {
                case "name_desc":
                    requests = requests.OrderByDescending(s => s.RequestID);
                    break;
                case "Date":
                    requests = requests.OrderBy(s => s.CreationDate);
                    break;
                case "date_desc":
                    requests = requests.OrderByDescending(s => s.CreationDate);
                    break;
                case "Value":
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
        public ActionResult Index(RequestState State, Unit Unit, DateTime? StartCreationDate, DateTime? EndCreationDate,  string SortRequest)
        {
            // SYNEVO\a.titarenko
            // SYNEVO\Alexander.Karpliuk
            // SYNEVO\b.veremiy

            var rq = (from Rq in db.Requests
                            where Rq.UserID == User.Identity.Name
                            select Rq).Union
                           (from R in db.Requests
                            where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.UserID)
                            select R);


            var setRequests = (from d in rq select d.State).Distinct();
            List<SelectListItem> itemsRequests = new List<SelectListItem>();
            foreach (var s in setRequests)
            {
                itemsRequests.Add(new SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString()
                });
            }
            ViewBag.State = itemsRequests;


            var users = (from d in db.Users select d.Unit).Distinct();
            List<SelectListItem> itemsUsers = new List<SelectListItem>();
            foreach (var s in users)
            {
                itemsUsers.Add(new SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString()
                });
            }
            ViewBag.Unit = itemsUsers;

            UserRole role = (from c in db.Users
                             where c.UserID == User.Identity.Name
                             select c.Role).FirstOrDefault();

            if (role == UserRole.FinancialManager || role == UserRole.CFOMedicove)
            {
                var requests_ = (from Rq in db.Requests select Rq);                
                return View(requests_.ToList());
            }

            // получить список заявок 
            var requests = (from Rq in db.Requests
                            where Rq.UserID == User.Identity.Name
                            select Rq).Union
                           (from R in db.Requests
                            where (from U in db.Users where U.ManagerID == User.Identity.Name select U.UserID).Contains(R.UserID)
                            select R).ToList();

            List<Request> filteredList = new List<Request>();

            foreach (var request in requests)
            {
                if((StartCreationDate != null) && (EndCreationDate != null))
                {
                    if (((request.CreationDate.Date >= StartCreationDate) && (request.CreationDate.Date <= EndCreationDate)) && (request.State == State))
                    {
                        if(Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
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
                        if (Unit == (from Us in db.Users where Us.UserID == request.UserID select Us.Unit).FirstOrDefault())
                        {
                            filteredList.Add(request);
                        }
                    }
                }
            }
            return View(filteredList.ToList());
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
        public ActionResult Create([Bind(Include = "RequestID,UserID,CreationDate,Description,Value,Currency,LongDescription,State")] Request request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // некоторые значения дефолтные
                    request.UserID = User.Identity.Name;
                    request.CreationDate = DateTime.Now;
                    request.State = 0; // "Created"
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
        public ActionResult Edit([Bind(Include = "RequestID,UserID,CreationDate,Description,Value,Currency,LongDescription,State")] Request request)
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
                int role;
                if (request.UserID == User.Identity.Name)
                {
                    role = 0;
                }
                else
                {
                    role = (int)(from c in db.Users
                                 where c.UserID == User.Identity.Name
                                 select c.Role).FirstOrDefault();
                }

                switch (role)
                {
                    case 0:
                        List<SelectListItem> UserRole = new List<SelectListItem>();
                        if ((int)request.State == 3)
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = "Finalized",
                                Value = "Finalized"
                            });
                        }
                        else
                        if ((int)request.State == 2)
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                        }
                        else
                        if ((int)request.State == 0)
                        {

                            UserRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            UserRole.Add(new SelectListItem
                            {
                                Text = "Cancelled",
                                Value = "Cancelled"
                            });
                        }
                        else
                        {
                            UserRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                        }

                        ViewBag.State = UserRole;
                        break;
                    case 1:
                        List<SelectListItem> ManagerRole = new List<SelectListItem>();
                        if ((int)request.State == 0)
                        {
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = "ApprovedByManager",
                                Value = "ApprovedByManager"
                            });
                            ManagerRole.Add(new SelectListItem
                            {
                                Text = "Cancelled",
                                Value = "Cancelled"
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
                    case 2:
                        List<SelectListItem> FinancialManagerRole = new List<SelectListItem>();
                        if ((int)request.State == 3)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = "Finalized",
                                Value = "Finalized"
                            });
                        }
                        else
                        if ((int)request.State != 1)
                        {
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            FinancialManagerRole.Add(new SelectListItem
                            {
                                Text = "Cancelled",
                                Value = "Cancelled"
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
                        break;
                    case 3:
                        List<SelectListItem> CFOMedicoveRole = new List<SelectListItem>();
                        if ((int)request.State == 2)
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });

                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = "ApprovedByMedicover",
                                Value = "ApprovedByMedicover"
                            });
                        }
                        else
                        if ((int)request.State != 1)
                        {
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = request.State.ToString(),
                                Value = request.State.ToString()
                            });
                            CFOMedicoveRole.Add(new SelectListItem
                            {
                                Text = "Cancelled",
                                Value = "Cancelled"
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
                }
            }
            catch(Exception ex) { }
           
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
