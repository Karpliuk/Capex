using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capex.Models
{
    public class Request
    {
        [Display(Name = "№ Заявки")]
        public int RequestID { get; set; }

        [Display(Name = "Пользователь")]
        public string UserID { get; set; }
        public virtual User User { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreationDate { get; set; }

        [Required(ErrorMessage = "Требуется поле Описание")]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Стоимость")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Value { get; set; }

        [Display(Name = "Валюта")]
        public Currency Currency { get; set; }

        [Display(Name = "Длинное описание")]
        public string LongDescription { get; set; }

        [Display(Name = "Состояние заявки")]
        public RequestState State { get; set; }
    }

    public enum Currency
    {
        [Display(Name = "UAH")]
        UAH,

        [Display(Name = "EUR")]
        EUR,

        [Display(Name = "USD")]
        USD,

        [Display(Name = "RUR")]
        RUR
    }

    public enum RequestState
    {
        [Display(Name = "Создана")]
        Created,

        [Display(Name = "Отменена")]
        Cancelled,

        [Display(Name = "Подтверждена менеджером")]
        ApprovedByManager,

        [Display(Name = " Подтверждена CFO Medicover")]
        ApprovedByMedicover,

        [Display(Name = "Завершена")]
        Finalized
    }

    public enum Unit
    {
        [Display(Name = "Украина")]
        Украина,

        [Display(Name = "Россия")]
        Россия,

        [Display(Name = "Беларусь")]
        Беларусь
    }
}