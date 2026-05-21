using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuizApp.Models
{
    public class ResultDetailsModel
    {
        public string Question { get; set; }
        public string Selected { get; set; }
        public string Correct { get; set; }
    }
}