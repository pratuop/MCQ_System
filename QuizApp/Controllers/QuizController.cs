using OfficeOpenXml;
using QuizApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;

public class QuizController : Controller
{
    SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["con"].ConnectionString);

    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Create(string topicName, HttpPostedFileBase file)
    {
        OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Pratik");
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        con.Open();

        // 1. Insert Topic
        SqlCommand topicCmd = new SqlCommand("INSERT INTO QuizTopics (TopicName) OUTPUT INSERTED.Id VALUES (@name)", con);
        topicCmd.Parameters.AddWithValue("@name", topicName);
        int topicId = (int)topicCmd.ExecuteScalar();

        // 2. Insert Questions
        using (var package = new OfficeOpenXml.ExcelPackage(file.InputStream))
        {
            var sheet = package.Workbook.Worksheets[0];

            for (int i = 2; i <= sheet.Dimension.Rows; i++)
            {
                SqlCommand cmd = new SqlCommand(
                "INSERT INTO Questions (Question,OptionA,OptionB,OptionC,OptionD,CorrectAnswer,TopicId,ImageUrl,Marks) VALUES (@q,@a,@b,@c,@d,@ans,@tid,@img,@marks)", con);

                cmd.Parameters.AddWithValue("@q", sheet.Cells[i, 1].Text.Trim());
                cmd.Parameters.AddWithValue("@a", sheet.Cells[i, 2].Text.Trim());
                cmd.Parameters.AddWithValue("@b", sheet.Cells[i, 3].Text.Trim());
                cmd.Parameters.AddWithValue("@c", sheet.Cells[i, 4].Text.Trim());
                cmd.Parameters.AddWithValue("@d", sheet.Cells[i, 5].Text.Trim());
                cmd.Parameters.AddWithValue("@ans", sheet.Cells[i, 6].Text.Trim());

                cmd.Parameters.AddWithValue("@tid", topicId);

                cmd.Parameters.AddWithValue("@img", sheet.Cells[i, 7].Text.Trim());
                cmd.Parameters.AddWithValue("@marks", Convert.ToInt32(sheet.Cells[i, 8].Text));

                cmd.ExecuteNonQuery();
            }
        }
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        return RedirectToAction("QuizList");
    }

    public ActionResult Start(int topicId)
    {
        List<Question> list = new List<Question>();

        int userId = 1;
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        con.Open();

        SqlCommand cmd = new SqlCommand(@"
    SELECT 
        q.*,
        ta.SelectedAnswer
    FROM Questions q

    LEFT JOIN TempAnswers ta
        ON q.Id = ta.QuestionId
        AND ta.UserId = @u
        AND ta.TopicId = @tid

    WHERE q.TopicId = @tid", con);

        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@tid", topicId);

        var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            list.Add(new Question
            {
                Id = Convert.ToInt32(dr["Id"]),

                QuestionText = dr["Question"].ToString(),

                OptionA = dr["OptionA"].ToString(),

                OptionB = dr["OptionB"].ToString(),

                OptionC = dr["OptionC"].ToString(),

                OptionD = dr["OptionD"].ToString(),

                CorrectAnswer = dr["CorrectAnswer"].ToString(),

                ImageUrl = dr["ImageUrl"] == DBNull.Value
    ? ""
    : dr["ImageUrl"].ToString(),

                Marks = dr["Marks"] == DBNull.Value
    ? 1
    : Convert.ToInt32(dr["Marks"]),

                SelectedAnswer = dr["SelectedAnswer"] == DBNull.Value
    ? ""
    : dr["SelectedAnswer"].ToString()
            });
        }

        ViewBag.TopicId = topicId;
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        return View(list);
    }
    [HttpPost]
    public ActionResult Submit(FormCollection form)
    {
        int topicId = Convert.ToInt32(form["topicId"]);

        int score = 0;
        int total = 0;
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        con.Open();

        SqlCommand cmd = new SqlCommand("SELECT * FROM Questions WHERE TopicId=@tid", con);
        cmd.Parameters.AddWithValue("@tid", topicId);

        var dr = cmd.ExecuteReader();

        List<dynamic> answers = new List<dynamic>();

        while (dr.Read())
        {
            int marks = dr["Marks"] == DBNull.Value
            ? 1
            : Convert.ToInt32(dr["Marks"]);

            total += marks;

            int qId = Convert.ToInt32(dr["Id"]);

            string correct = dr["CorrectAnswer"] == DBNull.Value
            ? ""
            : dr["CorrectAnswer"].ToString().Trim();

            string selected = form["q_" + qId];

            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.Trim().ToLower() == correct.ToLower())
                {
                    score += marks;
                }
            }

            answers.Add(new
            {
                QuestionId = qId,
                Selected = selected ?? "",
                Correct = correct
            });
        }

        dr.Close();

        // 🔥 Save Result
        SqlCommand save = new SqlCommand(
        "INSERT INTO Results(UserId,Score,Total,TopicId,CreatedAt) OUTPUT INSERTED.Id VALUES(@u,@s,@t,@tid,GETDATE())", con);

        save.Parameters.AddWithValue("@u", 1);
        save.Parameters.AddWithValue("@s", score);
        save.Parameters.AddWithValue("@t", total);
        save.Parameters.AddWithValue("@tid", topicId);

        int resultId = (int)save.ExecuteScalar();

        // 🔥 Save each answer
        foreach (var a in answers)
        {
            SqlCommand ansCmd = new SqlCommand(
            "INSERT INTO UserAnswers(ResultId,QuestionId,SelectedAnswer,CorrectAnswer) VALUES(@rid,@qid,@sel,@cor)", con);

            ansCmd.Parameters.AddWithValue("@rid", resultId);
            ansCmd.Parameters.AddWithValue("@qid", a.QuestionId);
            ansCmd.Parameters.AddWithValue("@sel", a.Selected ?? "");
            ansCmd.Parameters.AddWithValue("@cor", a.Correct);

            ansCmd.ExecuteNonQuery();
        }
        SqlCommand clear = new SqlCommand(
"DELETE FROM TempAnswers WHERE UserId=@u AND TopicId=@t", con);

        clear.Parameters.AddWithValue("@u", 1);
        clear.Parameters.AddWithValue("@t", topicId);

        clear.ExecuteNonQuery();
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        return RedirectToAction("ResultDetails", new { id = resultId });
    }

    public ActionResult ResultDetails(int id)
    {
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        List<ResultDetailsModel> list = new List<ResultDetailsModel>();

        con.Open();

        SqlCommand cmd = new SqlCommand(@"
    SELECT q.Question, ua.SelectedAnswer, ua.CorrectAnswer
    FROM UserAnswers ua
    INNER JOIN Questions q ON ua.QuestionId = q.Id
    WHERE ua.ResultId = @id", con);

        cmd.Parameters.AddWithValue("@id", id);

        var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            list.Add(new ResultDetailsModel
            {
                Question = dr["Question"].ToString(),
                Selected = dr["SelectedAnswer"].ToString(),
                Correct = dr["CorrectAnswer"].ToString()
            });
        }
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        return View(list);
    }
    public ActionResult QuizList()
    {
        List<QuizTopic> list = new List<QuizTopic>();

        int userId = Convert.ToInt32(Session["UserId"]);

        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }

        con.Open();

        SqlCommand cmd = new SqlCommand(@"

    SELECT
        t.*,

        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM Results r
                WHERE r.TopicId = t.Id
                AND r.UserId = @u
            )
            THEN 1
            ELSE 0
        END AS IsAttempted

    FROM QuizTopics t

    ", con);

        cmd.Parameters.AddWithValue("@u", 1);

        SqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            list.Add(new QuizTopic
            {
                Id = Convert.ToInt32(dr["Id"]),

                TopicName = dr["topicname"].ToString(),

                IsAttempted =
                Convert.ToBoolean(dr["IsAttempted"])
            });
        }

        dr.Close();

        con.Close();

        return View(list);
    }

    public ActionResult Result()
    {
        List<ResultModel> list = new List<ResultModel>();
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        con.Open();

        SqlCommand cmd = new SqlCommand(@"
    SELECT r.Id, r.Score, r.Total, r.CreatedAt, t.TopicName
    FROM Results r
    INNER JOIN QuizTopics t ON r.TopicId = t.Id
    WHERE r.UserId = @u
    ORDER BY r.Id DESC", con);

        cmd.Parameters.AddWithValue("@u", 1);

        var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            list.Add(new ResultModel
            {
                Id = Convert.ToInt32(dr["Id"]),
                Score = Convert.ToInt32(dr["Score"]),
                Total = Convert.ToInt32(dr["Total"]),
                CreatedAt = Convert.ToDateTime(dr["CreatedAt"]),
                TopicName = dr["TopicName"].ToString()
            });
        }
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        return View(list);
    }
    [HttpPost]
    public JsonResult SaveAnswer(int questionId, int topicId, string answer)
    {
        int userId = 1;
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        con.Open();

        SqlCommand check = new SqlCommand(
        "SELECT COUNT(*) FROM TempAnswers WHERE UserId=@u AND TopicId=@t AND QuestionId=@q", con);

        check.Parameters.AddWithValue("@u", userId);
        check.Parameters.AddWithValue("@t", topicId);
        check.Parameters.AddWithValue("@q", questionId);

        int exists = (int)check.ExecuteScalar();

        if (exists > 0)
        {
            SqlCommand update = new SqlCommand(
            "UPDATE TempAnswers SET SelectedAnswer=@a WHERE UserId=@u AND TopicId=@t AND QuestionId=@q", con);

            update.Parameters.AddWithValue("@a", answer);
            update.Parameters.AddWithValue("@u", userId);
            update.Parameters.AddWithValue("@t", topicId);
            update.Parameters.AddWithValue("@q", questionId);

            update.ExecuteNonQuery();
        }
        else
        {
            SqlCommand insert = new SqlCommand(
            "INSERT INTO TempAnswers(UserId,TopicId,QuestionId,SelectedAnswer) VALUES(@u,@t,@q,@a)", con);

            insert.Parameters.AddWithValue("@u", userId);
            insert.Parameters.AddWithValue("@t", topicId);
            insert.Parameters.AddWithValue("@q", questionId);
            insert.Parameters.AddWithValue("@a", answer);

            insert.ExecuteNonQuery();
        }
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        return Json(true);
    }
}