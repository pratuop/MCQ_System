using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuizApp.Models
{
    public class ResultModel
    {
        public int Id { get; set; }
        public string TopicName { get; set; }
        public int Score { get; set; }
        public int Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}