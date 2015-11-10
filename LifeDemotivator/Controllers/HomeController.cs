using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using LifeDemotivator.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.IO;
using System.Web.Helpers;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Data;
using System.Data.Entity;
using LifeDemotivator.Models;
using System;
using PagedList.Mvc;
using PagedList;

namespace LifeDemotivator.Controllers
{
    public class HomeController : Controller
    {
        private Entities db = new Entities();
        public ActionResult Index()
        {
            DemoTagsViewModel model = new DemoTagsViewModel();
            using (var context = new Entities())
            {
                model.demoList = context.Demotivators.ToList();
                model.tagsList = context.Tags.ToList();
            }
            return View(model);
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult FilterResults(int? parameter, int? tagId)
        {
            int numberDemoDisplayed = 10;
            int numberDemo = db.Demotivators.ToList().Count();
            List<Demotivators> demoList = db.Demotivators.ToList();
            List<Demotivators> finalList = new List<Demotivators>();
            if (parameter == 1)
            {
                finalList = demoList;
            }
            else if (parameter == 2)
            {
                float[] scoreArray = new float[numberDemo];
                int k = 0;
                foreach (Demotivators item in demoList)
                {
                    Single m_totalNumberOfVotes = 0;
                    Single m_totalVoteCount = 0;
                    Single m_currentVotesCount = 0;
                    string[] votes = item.Rating.Split(',');
                    for (int i = 0; i < votes.Length; i++)
                    {
                        m_currentVotesCount = int.Parse(votes[i]);
                        m_totalNumberOfVotes = m_totalNumberOfVotes + m_currentVotesCount;
                        m_totalVoteCount = m_totalVoteCount + (m_currentVotesCount * (i + 1));
                    }
                    if (m_totalNumberOfVotes == 0)
                        scoreArray[k] = 0;
                    else
                        scoreArray[k] = m_totalVoteCount / m_totalNumberOfVotes;
                    k++;
                }
                bubbleSort(demoList, scoreArray, numberDemo);
                for (int i = numberDemo-1, j = 0; i >= 0 && j < numberDemoDisplayed; i--, j++)
                {
                    finalList.Add(demoList[i]);
                }
            }
            else if (parameter == 3)
            {
                for (int i = numberDemo-1, j = 0; i >= 0 && j < numberDemoDisplayed; i--, j++)
                {
                    finalList.Add(demoList[i]);
                }
            } else if(parameter == 4)
            {
                var tags = db.TagsDemotivators.Where(d => d.TagId == tagId).ToList();
                foreach (var item in tags)
                {
                    var demotivator1 = db.Demotivators.Where(s => s.Id == item.DemotivatorId).ToList();
                    foreach (var item1 in demotivator1)
                    {
                        finalList.Add(item1);
                    }
                }
            }
            return View(finalList);
        }

        void bubbleSort(List<Demotivators> demoList, float[] arr, int n)
        {
            bool swapped = true;
            int j = 0;
            float tmp;
            Demotivators tmp1;
            while (swapped)
            {
                swapped = false;
                j++;
                for (int i = 0; i < n - j; i++)
                {
                    if (arr[i] > arr[i + 1])
                    {
                        tmp = arr[i];
                        arr[i] = arr[i + 1];
                        arr[i + 1] = tmp;
                        tmp1 = demoList[i];
                        demoList[i] = demoList[i + 1];
                        demoList[i + 1] = tmp1;
                        
                        swapped = true;
                    }
                }
            }
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public JsonResult SendRating(string r, string s, string id, string url)
        {
            int autoId = 0;
            Int16 thisVote = 0;
            Int16.TryParse(r, out thisVote);
            int.TryParse(id, out autoId);

            if (!User.Identity.IsAuthenticated)
            {
                return Json("Not authenticated!");
            }

            if (autoId.Equals(0))
            {
                return Json("Sorry, the record to vote doesn't exist");
            }
            var currentUserId = User.Identity.GetUserId();
            var isIt = db.Rates.Where(v => v.UserId.Equals(currentUserId) && v.DemotivatorId == autoId).FirstOrDefault();
            if (isIt != null)
            {
               HttpCookie cookie = new HttpCookie(url, "true");
               Response.Cookies.Add(cookie);
               return Json("<br />You have already rated this post, thanks !");
            }

            var dem = db.Demotivators.Where(d => d.Id == autoId).FirstOrDefault();
            if (dem != null)
            {
                object obj = dem.Rating;

                string updatedVotes = string.Empty;
                string[] votes = null;
                if (obj != null && obj.ToString().Length > 0)
                {
                    string currentVotes = obj.ToString(); // votes pattern will be 0,0,0,0,0
                    votes = currentVotes.Split(',');
                    // if proper vote data is there in the database
                    if (votes.Length.Equals(5))
                    {
                        // get the current number of vote count of the selected vote, always say -1 than the current vote in the array 
                        int currentNumberOfVote = int.Parse(votes[thisVote - 1]);
                        // increase 1 for this vote
                        currentNumberOfVote++;
                        // set the updated value into the selected votes
                        votes[thisVote - 1] = currentNumberOfVote.ToString();
                    }
                    else
                    {
                        votes = new string[] { "0", "0", "0", "0", "0" };
                        votes[thisVote - 1] = "1";
                    }
                }
                else
                {
                    votes = new string[] { "0", "0", "0", "0", "0" };
                    votes[thisVote - 1] = "1";
                }

                // concatenate all arrays now
                foreach (string ss in votes)
                {
                    updatedVotes += ss + ",";
                }
                updatedVotes = updatedVotes.Substring(0, updatedVotes.Length - 1);

                db.Entry(dem).State = EntityState.Modified;
                dem.Rating = updatedVotes;
                db.SaveChanges();

                Rates vm = new Rates()
                {
                    IsRated = true,
                    UserId = User.Identity.GetUserId(),
                    Score = thisVote,
                    DemotivatorId = autoId
                };

                db.Rates.Add(vm);

                db.SaveChanges();

                // keep the school voting flag to stop voting by this member
                HttpCookie cookie = new HttpCookie(url, "true");
                Response.Cookies.Add(cookie);
            }
            return Json("<br />You rated " + r + " star(s), thanks !");
        }
    }
}