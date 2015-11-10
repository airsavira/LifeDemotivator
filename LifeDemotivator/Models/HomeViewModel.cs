using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LifeDemotivator.Models
{
    public class HomeViewModel
    {
        public ICollection<Demotivators> demotivators { get; set; }

        public int DemCount { get; set; }

    }

}