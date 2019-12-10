using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMS.Helpers
{
    public interface IAppDateTime
    {
        DateTime GetDateTimeNow();
    }
   public class AppDateTime : IAppDateTime
    {
        public DateTime GetDateTimeNow()
        {
            //return DateTime.Now();
            DateTime fakeDate = new DateTime(2019, 05, 01);
            return fakeDate;
        }
    }
}
