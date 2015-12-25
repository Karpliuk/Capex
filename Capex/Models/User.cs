using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capex.Models
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int Id { get; set; }

        [Display(Name = "Пользователь")]
        public string UserID { get; set; }

        [Display(Name = "Роль")]
        public UserRole Role { get; set; }

        [Display(Name = "Пользователь")]
        public string FullName { get; set; }

        [Display(Name = "Подразделение")]
        public Unit Unit { get; set; }

        [Display(Name = "Руководитель")]
        public string ManagerID { get; set; }
    }

    public enum UserRole
    {
        [Display(Name = "Пользователь")]
        User,

        [Display(Name = "Менеджер")]
        Manager,

        [Display(Name = "CFOMedicove")]
        CFOMedicove,

        [Display(Name = "Финансовый директор")]
        FinancialManager,

        [Display(Name = "")]
        ViewAll
    }
}