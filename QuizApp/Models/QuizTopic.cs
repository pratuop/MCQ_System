using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuizApp.Models
{
    public class QuizTopic
    {
        public int Id { get; set; }
        public string TopicName { get; set; }
        public bool IsAttempted { get; set; }
    }
}